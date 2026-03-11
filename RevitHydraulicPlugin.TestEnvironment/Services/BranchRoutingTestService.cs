using RevitHydraulicPlugin.TestEnvironment.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.TestEnvironment.Services
{
    /// <summary>
    /// Serviço de roteamento de colunas e ramais para testes.
    /// Replica a lógica de ColumnRoutingService + BranchRoutingService do plugin principal.
    /// 
    /// Calcula posições de colunas e gera ramais conectando equipamentos às colunas,
    /// criando MockPipes como resultado.
    /// </summary>
    public class BranchRoutingTestService
    {
        /// <summary>
        /// Offset horizontal (mm) da coluna em relação ao centro do ambiente.
        /// Mesmo valor de PluginSettings.ColumnOffsetFromCenterMm.
        /// </summary>
        private const double ColumnOffsetMm = 300;

        /// <summary>
        /// Offset entre coluna de água fria e coluna de esgoto (mm).
        /// </summary>
        private const double ColumnSeparationMm = 200;

        /// <summary>
        /// Offset vertical do ramal de esgoto em relação ao piso (mm).
        /// </summary>
        private const double SewerBranchHeightMm = -50;

        /// <summary>
        /// Altura do ramal de água fria em relação ao piso (mm).
        /// </summary>
        private const double ColdWaterBranchHeightMm = 600;

        private static int _pipeIdCounter = 5000;

        /// <summary>
        /// Calcula posições de colunas hidráulicas para os ambientes detectados.
        /// Cria pares de colunas (Água Fria + Esgoto) por grupo de ambientes.
        /// </summary>
        public List<MockColumn> CalculateColumns(
            List<DetectedRoom> hydraulicRooms,
            List<MockLevel> levels)
        {
            var columns = new List<MockColumn>();
            int colIndex = 1;

            // Agrupa rooms por posição horizontal similar
            var groups = GroupRoomsByPosition(hydraulicRooms);

            foreach (var group in groups)
            {
                var referenceRoom = group.First().Room;
                var center = referenceRoom.CenterPoint;

                // Coluna de Água Fria
                var coldWaterPos = center.Offset(ColumnOffsetMm, 0, 0);
                var cafColumn = new MockColumn
                {
                    ColumnId = $"CAF-{colIndex:D2}",
                    SystemType = ConnectorSystemType.ColdWater,
                    BasePosition = coldWaterPos,
                    DiameterMm = HydraulicRulesTestService.DefaultColdWaterColumnDiameterMm,
                    Levels = levels,
                    AssociatedRoom = referenceRoom
                };

                // Criar segmentos de pipe verticais
                for (int i = 0; i < levels.Count - 1; i++)
                {
                    cafColumn.Pipes.Add(new MockPipe
                    {
                        Id = _pipeIdCounter++,
                        StartPoint = coldWaterPos.AtElevation(levels[i].ElevationMm),
                        EndPoint = coldWaterPos.AtElevation(levels[i + 1].ElevationMm),
                        DiameterMm = cafColumn.DiameterMm,
                        SystemType = "Domestic Cold Water",
                        Material = "PVC Água Fria",
                        SlopePercent = 0
                    });
                }

                columns.Add(cafColumn);

                // Coluna de Esgoto
                var sewerPos = coldWaterPos.Offset(ColumnSeparationMm, 0, 0);
                var cesColumn = new MockColumn
                {
                    ColumnId = $"CES-{colIndex:D2}",
                    SystemType = ConnectorSystemType.Sewer,
                    BasePosition = sewerPos,
                    DiameterMm = HydraulicRulesTestService.DefaultSewerColumnDiameterMm,
                    Levels = levels,
                    AssociatedRoom = referenceRoom
                };

                for (int i = 0; i < levels.Count - 1; i++)
                {
                    cesColumn.Pipes.Add(new MockPipe
                    {
                        Id = _pipeIdCounter++,
                        StartPoint = sewerPos.AtElevation(levels[i].ElevationMm),
                        EndPoint = sewerPos.AtElevation(levels[i + 1].ElevationMm),
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

        /// <summary>
        /// Gera ramais conectando equipamentos às colunas mais próximas.
        /// Para cada equipamento, cria ramais de esgoto e água fria (quando aplicável).
        /// </summary>
        public List<MockBranch> GenerateBranches(
            List<DetectedEquipment> equipment,
            List<MockColumn> columns)
        {
            var branches = new List<MockBranch>();

            var sewerColumns = columns.Where(c => c.SystemType == ConnectorSystemType.Sewer).ToList();
            var coldWaterColumns = columns.Where(c => c.SystemType == ConnectorSystemType.ColdWater).ToList();

            foreach (var equip in equipment)
            {
                var position = equip.Fixture.Position;

                // === Ramal de Esgoto ===
                var nearestSewer = FindNearestColumn(position, sewerColumns);
                if (nearestSewer != null)
                {
                    var sewerSpec = HydraulicRulesTestService.GetSewerSpec(equip.Type);

                    // Ponto de partida: posição do equipamento + offset de altura
                    var startPoint = position.Offset(0, 0, SewerBranchHeightMm);

                    // Ponto de chegada: posição XY da coluna com queda por inclinação
                    double horizontalDist = position.HorizontalDistanceTo(nearestSewer.BasePosition);
                    double slopeDrop = horizontalDist * (sewerSpec.SlopePercent / 100.0);

                    var endPoint = new Point3D(
                        nearestSewer.BasePosition.X,
                        nearestSewer.BasePosition.Y,
                        startPoint.Z - slopeDrop);

                    branches.Add(new MockBranch
                    {
                        Fixture = equip.Fixture,
                        TargetColumn = nearestSewer,
                        SystemType = ConnectorSystemType.Sewer,
                        Pipe = new MockPipe
                        {
                            Id = _pipeIdCounter++,
                            StartPoint = startPoint,
                            EndPoint = endPoint,
                            DiameterMm = sewerSpec.DiameterMm,
                            SystemType = sewerSpec.SystemTypeName,
                            Material = sewerSpec.Material,
                            SlopePercent = sewerSpec.SlopePercent
                        }
                    });
                }

                // === Ramal de Água Fria ===
                // Ralo não precisa de água fria
                if (equip.Type != EquipmentType.Ralo)
                {
                    var nearestColdWater = FindNearestColumn(position, coldWaterColumns);
                    if (nearestColdWater != null)
                    {
                        var waterSpec = HydraulicRulesTestService.GetColdWaterSpec(equip.Type);

                        var startPoint = position.Offset(0, 0, ColdWaterBranchHeightMm);
                        var endPoint = new Point3D(
                            nearestColdWater.BasePosition.X,
                            nearestColdWater.BasePosition.Y,
                            startPoint.Z);

                        branches.Add(new MockBranch
                        {
                            Fixture = equip.Fixture,
                            TargetColumn = nearestColdWater,
                            SystemType = ConnectorSystemType.ColdWater,
                            Pipe = new MockPipe
                            {
                                Id = _pipeIdCounter++,
                                StartPoint = startPoint,
                                EndPoint = endPoint,
                                DiameterMm = waterSpec.DiameterMm,
                                SystemType = waterSpec.SystemTypeName,
                                Material = waterSpec.Material,
                                SlopePercent = 0
                            }
                        });
                    }
                }
            }

            return branches;
        }

        /// <summary>
        /// Encontra a coluna mais próxima de um ponto (distância horizontal).
        /// </summary>
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

        /// <summary>
        /// Agrupa ambientes hidráulicos por posição horizontal similar.
        /// Ambientes empilhados (mesma posição XY, níveis diferentes)
        /// compartilham a mesma prumada.
        /// </summary>
        private List<List<DetectedRoom>> GroupRoomsByPosition(List<DetectedRoom> rooms)
        {
            var groups = new List<List<DetectedRoom>>();
            var assigned = new HashSet<int>();
            double tolerance = 2500; // mm

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

                    double dist = room.Room.CenterPoint.HorizontalDistanceTo(
                        other.Room.CenterPoint);

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
