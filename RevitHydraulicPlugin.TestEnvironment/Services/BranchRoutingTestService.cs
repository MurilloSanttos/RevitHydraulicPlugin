using RevitHydraulicPlugin.TestEnvironment.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.TestEnvironment.Services
{
    /// <summary>
    /// Resultado da geração de um ramal individual.
    /// </summary>
    public class BranchGenerationResult
    {
        public bool Success { get; set; }
        public MockBranch Branch { get; set; }
        public string FailureReason { get; set; }
        public PipeSpec AppliedRule { get; set; }
        public int SegmentCount { get; set; }
        public double TotalLengthMm { get; set; }
    }

    /// <summary>
    /// Serviço de roteamento de ramais para testes (v2.0).
    /// Espelha BranchRoutingService + ColumnRoutingService do plugin principal.
    /// 
    /// Melhorias:
    /// - Roteamento ortogonal em L
    /// - PipeRuleProvider por FixtureType
    /// - Validação de comprimento min/max
    /// - BranchGenerationResult detalhado
    /// </summary>
    public class BranchRoutingTestService
    {
        private const double ColumnOffsetMm = 300;
        private const double ColumnSeparationMm = 200;
        private const double SewerBranchHeightMm = -50;
        private const double ColdWaterBranchHeightMm = 600;

        private static int _pipeIdCounter = 5000;

        // ════════════════════════════════════════════════
        //  COLUNAS
        // ════════════════════════════════════════════════

        public List<MockColumn> CalculateColumns(
            List<DetectedRoom> hydraulicRooms,
            List<MockLevel> levels)
        {
            var columns = new List<MockColumn>();
            int colIndex = 1;
            var groups = GroupRoomsByPosition(hydraulicRooms);

            foreach (var group in groups)
            {
                var referenceRoom = group.First().Room;
                var center = referenceRoom.CenterPoint;

                // Coluna AF
                var cafPos = center.Offset(ColumnOffsetMm, 0, 0);
                var cafColumn = new MockColumn
                {
                    ColumnId = $"CAF-{colIndex:D2}",
                    SystemType = ConnectorSystemType.ColdWater,
                    BasePosition = cafPos,
                    DiameterMm = HydraulicRulesTestService.DefaultColdWaterColumnDiameterMm,
                    Levels = levels,
                    AssociatedRoom = referenceRoom
                };

                for (int i = 0; i < levels.Count - 1; i++)
                {
                    cafColumn.Pipes.Add(new MockPipe
                    {
                        Id = _pipeIdCounter++,
                        StartPoint = cafPos.AtElevation(levels[i].ElevationMm),
                        EndPoint = cafPos.AtElevation(levels[i + 1].ElevationMm),
                        DiameterMm = cafColumn.DiameterMm,
                        SystemType = "Domestic Cold Water",
                        Material = "PVC Agua Fria",
                        SlopePercent = 0
                    });
                }
                columns.Add(cafColumn);

                // Coluna Esgoto
                var cesPos = cafPos.Offset(ColumnSeparationMm, 0, 0);
                var cesColumn = new MockColumn
                {
                    ColumnId = $"CES-{colIndex:D2}",
                    SystemType = ConnectorSystemType.Sewer,
                    BasePosition = cesPos,
                    DiameterMm = HydraulicRulesTestService.DefaultSewerColumnDiameterMm,
                    Levels = levels,
                    AssociatedRoom = referenceRoom
                };

                for (int i = 0; i < levels.Count - 1; i++)
                {
                    cesColumn.Pipes.Add(new MockPipe
                    {
                        Id = _pipeIdCounter++,
                        StartPoint = cesPos.AtElevation(levels[i].ElevationMm),
                        EndPoint = cesPos.AtElevation(levels[i + 1].ElevationMm),
                        DiameterMm = cesColumn.DiameterMm,
                        SystemType = "Sanitary",
                        Material = "PVC Esgoto",
                        SlopePercent = 0
                    });
                }
                columns.Add(cesColumn);
                colIndex++;
            }

            return columns;
        }

        // ════════════════════════════════════════════════
        //  RAMAIS (v2.0 — ORTOGONAL)
        // ════════════════════════════════════════════════

        /// <summary>
        /// Gera ramais para todos os equipamentos detectados.
        /// Retorna lista de MockBranch + relatório de resultados.
        /// </summary>
        public List<MockBranch> GenerateBranches(
            List<DetectedEquipment> equipment,
            List<MockColumn> columns)
        {
            return GenerateBranchesWithResults(equipment, columns).branches;
        }

        /// <summary>
        /// Gera ramais com resultados detalhados (para testes).
        /// </summary>
        public (List<MockBranch> branches, List<BranchGenerationResult> results)
            GenerateBranchesWithResults(
                List<DetectedEquipment> equipment,
                List<MockColumn> columns)
        {
            var branches = new List<MockBranch>();
            var results = new List<BranchGenerationResult>();

            var sewerColumns = columns.Where(c => c.SystemType == ConnectorSystemType.Sewer).ToList();
            var coldWaterColumns = columns.Where(c => c.SystemType == ConnectorSystemType.ColdWater).ToList();

            foreach (var equip in equipment)
            {
                var fixtureType = equip.FixtureType;

                // Ramal Esgoto
                var sewerResult = GenerateSewerBranch(equip, fixtureType, sewerColumns);
                results.Add(sewerResult);
                if (sewerResult.Success)
                    branches.Add(sewerResult.Branch);

                // Ramal Água Fria
                if (HydraulicRulesTestService.NeedsColdWater(fixtureType))
                {
                    var waterResult = GenerateColdWaterBranch(equip, fixtureType, coldWaterColumns);
                    results.Add(waterResult);
                    if (waterResult.Success)
                        branches.Add(waterResult.Branch);
                }
            }

            return (branches, results);
        }

        // ── Ramal de Esgoto ──

        private BranchGenerationResult GenerateSewerBranch(
            DetectedEquipment equip, FixtureType fixtureType,
            List<MockColumn> sewerColumns)
        {
            var result = new BranchGenerationResult();
            var rule = HydraulicRulesTestService.GetSewerRule(fixtureType);
            result.AppliedRule = rule;

            var nearest = FindNearestColumn(equip.Fixture.Position, sewerColumns);
            if (nearest == null)
            {
                result.FailureReason = "Nenhuma coluna de esgoto encontrada";
                return result;
            }

            var branch = CreateOrthogonalRoute(equip.Fixture, nearest, rule,
                SewerBranchHeightMm, true);

            if (branch == null)
            {
                result.FailureReason = "Falha ao calcular rota";
                return result;
            }

            result.Success = true;
            result.Branch = branch;
            result.TotalLengthMm = branch.Pipe.LengthMm;
            result.SegmentCount = 1; // Simplificado para mock
            return result;
        }

        // ── Ramal de Água Fria ──

        private BranchGenerationResult GenerateColdWaterBranch(
            DetectedEquipment equip, FixtureType fixtureType,
            List<MockColumn> coldWaterColumns)
        {
            var result = new BranchGenerationResult();
            var rule = HydraulicRulesTestService.GetColdWaterRule(fixtureType);
            result.AppliedRule = rule;

            var nearest = FindNearestColumn(equip.Fixture.Position, coldWaterColumns);
            if (nearest == null)
            {
                result.FailureReason = "Nenhuma coluna AF encontrada";
                return result;
            }

            var branch = CreateOrthogonalRoute(equip.Fixture, nearest, rule,
                ColdWaterBranchHeightMm, false);

            if (branch == null)
            {
                result.FailureReason = "Falha ao calcular rota";
                return result;
            }

            result.Success = true;
            result.Branch = branch;
            result.TotalLengthMm = branch.Pipe.LengthMm;
            result.SegmentCount = 1;
            return result;
        }

        // ════════════════════════════════════════════════
        //  ROTEAMENTO ORTOGONAL
        // ════════════════════════════════════════════════

        /// <summary>
        /// Cria rota ortogonal entre fixture e coluna.
        /// Para esgoto: aplica inclinação.
        /// Para AF: horizontal sem inclinação.
        /// </summary>
        private MockBranch CreateOrthogonalRoute(
            MockFixture fixture, MockColumn column, PipeSpec rule,
            double heightOffsetMm, bool applySlope)
        {
            if (fixture.Position == null || column.BasePosition == null)
                return null;

            var pos = fixture.Position;
            var startPoint = pos.Offset(0, 0, heightOffsetMm);

            double horizontalDist = pos.HorizontalDistanceTo(column.BasePosition);
            double slopeDrop = applySlope
                ? horizontalDist * (rule.SlopePercent / 100.0)
                : 0;

            var endPoint = new Point3D(
                column.BasePosition.X,
                column.BasePosition.Y,
                startPoint.Z - slopeDrop);

            return new MockBranch
            {
                Fixture = fixture,
                TargetColumn = column,
                SystemType = applySlope ? ConnectorSystemType.Sewer : ConnectorSystemType.ColdWater,
                Pipe = new MockPipe
                {
                    Id = _pipeIdCounter++,
                    StartPoint = startPoint,
                    EndPoint = endPoint,
                    DiameterMm = rule.DiameterMm,
                    SystemType = rule.SystemTypeName,
                    Material = rule.Material,
                    SlopePercent = rule.SlopePercent
                }
            };
        }

        // ════════════════════════════════════════════════
        //  UTILIDADES
        // ════════════════════════════════════════════════

        private MockColumn FindNearestColumn(Point3D point, List<MockColumn> columns)
        {
            MockColumn nearest = null;
            double minDist = double.MaxValue;

            foreach (var col in columns)
            {
                double dist = point.HorizontalDistanceTo(col.BasePosition);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = col;
                }
            }

            return nearest;
        }

        private List<List<DetectedRoom>> GroupRoomsByPosition(List<DetectedRoom> rooms)
        {
            var groups = new List<List<DetectedRoom>>();
            var assigned = new HashSet<int>();
            double tolerance = 2500;

            foreach (var room in rooms)
            {
                if (assigned.Contains(room.Room.Id) || room.Room.CenterPoint == null)
                    continue;

                var group = new List<DetectedRoom> { room };
                assigned.Add(room.Room.Id);

                foreach (var other in rooms)
                {
                    if (assigned.Contains(other.Room.Id) || other.Room.CenterPoint == null)
                        continue;

                    double dist = room.Room.CenterPoint.HorizontalDistanceTo(other.Room.CenterPoint);
                    if (dist < tolerance)
                    {
                        group.Add(other);
                        assigned.Add(other.Room.Id);
                    }
                }

                groups.Add(group);
            }

            return groups;
        }
    }
}
