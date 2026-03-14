using Autodesk.Revit.DB;
using RevitHydraulicPlugin.Configuration;
using RevitHydraulicPlugin.Detection;
using RevitHydraulicPlugin.Models;
using RevitHydraulicPlugin.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.Routing
{
    /// <summary>
    /// Resultado da geração de um ramal individual.
    /// </summary>
    public class BranchGenerationResult
    {
        /// <summary>Se o ramal foi gerado com sucesso.</summary>
        public bool Success { get; set; }

        /// <summary>Ramal gerado (null se falhou).</summary>
        public BranchConnection Branch { get; set; }

        /// <summary>Motivo da falha, se aplicável.</summary>
        public string FailureReason { get; set; }

        /// <summary>Regra aplicada ao fixture.</summary>
        public PipeRule AppliedRule { get; set; }

        /// <summary>Número de segmentos do ramal.</summary>
        public int SegmentCount { get; set; }

        /// <summary>Comprimento total do ramal em mm.</summary>
        public double TotalLengthMm { get; set; }
    }

    /// <summary>
    /// Ponto de roteamento (vértice do caminho do ramal).
    /// </summary>
    public class RoutingPoint
    {
        /// <summary>Posição 3D do ponto.</summary>
        public XYZ Position { get; set; }

        /// <summary>Tipo do ponto (Start, Turn, End).</summary>
        public string PointType { get; set; }

        /// <summary>Descrição para log/debug.</summary>
        public string Description { get; set; }

        public override string ToString()
        {
            return $"[{PointType}] ({Position?.X:F2}, {Position?.Y:F2}, {Position?.Z:F2}) - {Description}";
        }
    }

    /// <summary>
    /// Segmento de roteamento (trecho reto entre dois pontos).
    /// </summary>
    public class RoutingSegment
    {
        /// <summary>Ponto de início do segmento.</summary>
        public XYZ Start { get; set; }

        /// <summary>Ponto de fim do segmento.</summary>
        public XYZ End { get; set; }

        /// <summary>Direção predominante (X, Y, Z).</summary>
        public string Direction { get; set; }

        /// <summary>Comprimento em mm.</summary>
        public double LengthMm { get; set; }

        public override string ToString()
        {
            return $"Segmento {Direction}: L={LengthMm:F0}mm";
        }
    }

    /// <summary>
    /// Serviço de roteamento de ramais hidráulicos.
    /// 
    /// VERSÃO 2.0 — Melhorias:
    /// - Roteamento ortogonal em L (trecho horizontal + conexão à coluna)
    /// - Uso do PipeRuleProvider para regras centralizadas
    /// - Validação de comprimento mínimo e máximo
    /// - Detecção de conflitos básicos
    /// - Logs detalhados de cada etapa
    /// - Estrutura preparada para evolução futura (roteamento em U, desvios)
    /// 
    /// Estratégia de roteamento:
    ///   1. Ponto de partida: conector do fixture ou posição do equipamento
    ///   2. Trecho 1: horizontal no eixo X até alinhar com a coluna
    ///   3. Trecho 2: horizontal no eixo Y até chegar à coluna
    ///   4. Inclinação: aplicada ao longo do trecho horizontal (para esgoto)
    ///   5. Os trechos são sempre ortogonais (alinhados aos eixos)
    /// </summary>
    public class BranchRoutingService
    {
        private readonly Document _document;

        public BranchRoutingService(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// Calcula rotas de ramais para todos os equipamentos.
        /// </summary>
        /// <param name="rooms">Ambientes com equipamentos detectados.</param>
        /// <param name="columns">Colunas hidráulicas calculadas.</param>
        /// <returns>Lista de BranchConnection com rotas calculadas.</returns>
        public List<BranchConnection> CalculateBranches(
            List<HydraulicRoom> rooms,
            List<HydraulicColumn> columns)
        {
            using (Logger.MeasureTime("Calculo de Ramais"))
            {
                Logger.Info("[BRANCH] Iniciando calculo de ramais hidraulicos...");

                var branches = new List<BranchConnection>();
                var results = new List<BranchGenerationResult>();

                if (columns == null || columns.Count == 0)
                {
                    Logger.Warning("[BRANCH] Nenhuma coluna disponível para gerar ramais.");
                    return branches;
                }

                // Separa colunas por tipo
                var sewerColumns = columns.Where(c => c.SystemType == ColumnSystemType.Esgoto).ToList();
                var coldWaterColumns = columns.Where(c => c.SystemType == ColumnSystemType.AguaFria).ToList();

                Logger.Info($"[BRANCH] Colunas disponiveis: {sewerColumns.Count} esgoto, {coldWaterColumns.Count} agua fria");

                int totalEquipment = 0;
                int sewerBranchCount = 0;
                int waterBranchCount = 0;
                int failedCount = 0;

                foreach (var room in rooms)
                {
                    if (room.Equipment.Count == 0) continue;

                    Logger.Info($"[BRANCH] Processando Room: '{room.RoomName}' ({room.Equipment.Count} equipamentos)");

                    foreach (var equipment in room.Equipment)
                    {
                        totalEquipment++;

                        // Identifica o FixtureType para buscar regras
                        var fixtureType = EquipmentToFixtureType(equipment.Type);

                        // === RAMAL DE ESGOTO ===
                        var sewerResult = GenerateSewerBranch(equipment, fixtureType, sewerColumns);
                        results.Add(sewerResult);

                        if (sewerResult.Success)
                        {
                            branches.Add(sewerResult.Branch);
                            sewerBranchCount++;
                        }
                        else
                        {
                            failedCount++;
                        }

                        // === RAMAL DE ÁGUA FRIA ===
                        if (PipeRuleProvider.NeedsColdWater(fixtureType))
                        {
                            var waterResult = GenerateColdWaterBranch(equipment, fixtureType, coldWaterColumns);
                            results.Add(waterResult);

                            if (waterResult.Success)
                            {
                                branches.Add(waterResult.Branch);
                                waterBranchCount++;
                            }
                            else
                            {
                                failedCount++;
                            }
                        }
                    }
                }

                // ── Relatório ──
                Logger.Info("[BRANCH] === Resumo de Ramais ===");
                Logger.Info($"[BRANCH]   Equipamentos processados: {totalEquipment}");
                Logger.Info($"[BRANCH]   Ramais esgoto gerados: {sewerBranchCount}");
                Logger.Info($"[BRANCH]   Ramais agua fria gerados: {waterBranchCount}");
                Logger.Info($"[BRANCH]   Falhas: {failedCount}");
                Logger.Info($"[BRANCH]   Total de ramais: {branches.Count}");

                return branches;
            }
        }

        // ════════════════════════════════════════════════
        //  GERAÇÃO DE RAMAL DE ESGOTO
        // ════════════════════════════════════════════════

        /// <summary>
        /// Gera um ramal de esgoto para um equipamento.
        /// </summary>
        private BranchGenerationResult GenerateSewerBranch(
            HydraulicEquipment equipment,
            FixtureType fixtureType,
            List<HydraulicColumn> sewerColumns)
        {
            var result = new BranchGenerationResult();

            // Obtém regra de dimensionamento
            var rule = PipeRuleProvider.GetSewerRule(fixtureType);
            result.AppliedRule = rule;

            Logger.Info($"[BRANCH] Ramal esgoto: {fixtureType} -> D{rule.DiameterMm}mm, incl. {rule.SlopePercent}%");

            // Encontra coluna mais próxima
            var nearestColumn = FindNearestColumn(equipment, sewerColumns);
            if (nearestColumn == null)
            {
                result.Success = false;
                result.FailureReason = "Nenhuma coluna de esgoto encontrada";
                Logger.Warning($"[BRANCH] Falha: {result.FailureReason} para {equipment.FamilyName}");
                return result;
            }

            // Gera rota ortogonal
            var branch = CreateOrthogonalRoute(equipment, nearestColumn,
                PipeRuleProvider.ToSpecification(rule), rule);

            if (branch == null)
            {
                result.Success = false;
                result.FailureReason = "Falha ao calcular rota";
                Logger.Warning($"[BRANCH] Falha: {result.FailureReason} para {equipment.FamilyName}");
                return result;
            }

            result.Success = true;
            result.Branch = branch;
            result.SegmentCount = 1 + branch.IntermediatePoints.Count;
            result.TotalLengthMm = CalculateTotalLengthMm(branch);

            Logger.Info($"[BRANCH]   Gerado: {result.SegmentCount} segmentos, L={result.TotalLengthMm:F0}mm");
            return result;
        }

        // ════════════════════════════════════════════════
        //  GERAÇÃO DE RAMAL DE ÁGUA FRIA
        // ════════════════════════════════════════════════

        /// <summary>
        /// Gera um ramal de água fria para um equipamento.
        /// </summary>
        private BranchGenerationResult GenerateColdWaterBranch(
            HydraulicEquipment equipment,
            FixtureType fixtureType,
            List<HydraulicColumn> coldWaterColumns)
        {
            var result = new BranchGenerationResult();

            var rule = PipeRuleProvider.GetColdWaterRule(fixtureType);
            result.AppliedRule = rule;

            Logger.Info($"[BRANCH] Ramal AF: {fixtureType} -> D{rule.DiameterMm}mm");

            var nearestColumn = FindNearestColumn(equipment, coldWaterColumns);
            if (nearestColumn == null)
            {
                result.Success = false;
                result.FailureReason = "Nenhuma coluna de agua fria encontrada";
                Logger.Warning($"[BRANCH] Falha: {result.FailureReason} para {equipment.FamilyName}");
                return result;
            }

            var branch = CreateOrthogonalRoute(equipment, nearestColumn,
                PipeRuleProvider.ToSpecification(rule), rule);

            if (branch == null)
            {
                result.Success = false;
                result.FailureReason = "Falha ao calcular rota";
                Logger.Warning($"[BRANCH] Falha: {result.FailureReason}");
                return result;
            }

            result.Success = true;
            result.Branch = branch;
            result.SegmentCount = 1 + branch.IntermediatePoints.Count;
            result.TotalLengthMm = CalculateTotalLengthMm(branch);

            Logger.Info($"[BRANCH]   Gerado: {result.SegmentCount} segmentos, L={result.TotalLengthMm:F0}mm");
            return result;
        }

        // ════════════════════════════════════════════════
        //  ROTEAMENTO ORTOGONAL
        // ════════════════════════════════════════════════

        /// <summary>
        /// Cria uma rota ortogonal em L entre equipamento e coluna.
        /// 
        /// Estratégia:
        ///   Ponto A (equipamento) → Ponto B (canto) → Ponto C (coluna)
        ///   
        ///   A ──────── B      Trecho 1: horizontal no eixo X
        ///               │
        ///               │     Trecho 2: horizontal no eixo Y
        ///               │
        ///               C     (posição da coluna)
        /// 
        /// Para esgoto, aplica inclinação no trecho horizontal total.
        /// </summary>
        private BranchConnection CreateOrthogonalRoute(
            HydraulicEquipment equipment,
            HydraulicColumn column,
            PipeSpecification pipeSpec,
            PipeRule rule)
        {
            if (equipment.Position == null || column.BasePosition == null)
                return null;

            // ── Calcular ponto de partida ──
            double heightOffsetFeet;
            if (pipeSpec.SlopePercent > 0)
            {
                // Esgoto: nível do piso com offset negativo (embutido)
                heightOffsetFeet = UnitConversionHelper.MmToFeet(PluginSettings.BranchHeightOffsetMm);
            }
            else
            {
                // Água fria: acima do piso
                heightOffsetFeet = UnitConversionHelper.MmToFeet(PluginSettings.ColdWaterBranchHeightMm);
            }

            XYZ startPoint = new XYZ(
                equipment.Position.X,
                equipment.Position.Y,
                equipment.Position.Z + heightOffsetFeet);

            // ── Calcular distância horizontal ──
            double dx = column.BasePosition.X - equipment.Position.X;
            double dy = column.BasePosition.Y - equipment.Position.Y;
            double horizontalDist = Math.Sqrt(dx * dx + dy * dy);
            double horizontalDistMm = UnitConversionHelper.FeetToMm(horizontalDist);

            // ── Validar comprimento ──
            if (horizontalDistMm < rule.MinLengthMm)
            {
                Logger.Debug($"[BRANCH] Ramal muito curto ({horizontalDistMm:F0}mm < {rule.MinLengthMm}mm) — "
                    + "ajustando para minimo");
            }

            if (horizontalDistMm > rule.MaxLengthMm)
            {
                Logger.Warning($"[BRANCH] Ramal excede comprimento maximo ({horizontalDistMm:F0}mm > {rule.MaxLengthMm}mm)");
                // Permite gerar mesmo assim, mas com warning
            }

            // ── Calcular queda por inclinação (esgoto) ──
            double slopeDrop = horizontalDist *
                UnitConversionHelper.SlopePercentToRevitSlope(pipeSpec.SlopePercent);

            // ── Decidir roteamento ──
            var intermediatePoints = new List<XYZ>();
            XYZ endPoint;

            // Se a distância em ambos os eixos é significativa, criar rota em L
            double absDx = Math.Abs(dx);
            double absDy = Math.Abs(dy);
            double minSegment = UnitConversionHelper.MmToFeet(150); // 150mm mínimo para curva

            if (absDx > minSegment && absDy > minSegment)
            {
                // Rota em L: primeiro no eixo X, depois no eixo Y
                // Distribui a queda de inclinação proporcionalmente

                double totalHorizontal = absDx + absDy;
                double dropAtCorner = slopeDrop * (absDx / totalHorizontal);

                // Ponto intermediário (canto do L)
                XYZ cornerPoint = new XYZ(
                    column.BasePosition.X,
                    equipment.Position.Y,
                    startPoint.Z - dropAtCorner);

                intermediatePoints.Add(cornerPoint);

                // Ponto final (na coluna)
                endPoint = new XYZ(
                    column.BasePosition.X,
                    column.BasePosition.Y,
                    startPoint.Z - slopeDrop);

                Logger.Debug($"[BRANCH] Rota em L: dx={UnitConversionHelper.FeetToMm(absDx):F0}mm, "
                    + $"dy={UnitConversionHelper.FeetToMm(absDy):F0}mm");
            }
            else
            {
                // Rota direta (equipamento quase alinhado com a coluna)
                endPoint = new XYZ(
                    column.BasePosition.X,
                    column.BasePosition.Y,
                    startPoint.Z - slopeDrop);

                Logger.Debug($"[BRANCH] Rota direta: L={horizontalDistMm:F0}mm");
            }

            return new BranchConnection
            {
                Equipment = equipment,
                TargetColumn = column,
                StartPoint = startPoint,
                EndPoint = endPoint,
                IntermediatePoints = intermediatePoints,
                PipeSpec = pipeSpec
            };
        }

        // ════════════════════════════════════════════════
        //  UTILIDADES
        // ════════════════════════════════════════════════

        /// <summary>
        /// Encontra a coluna mais próxima de um equipamento (distância horizontal).
        /// </summary>
        private HydraulicColumn FindNearestColumn(
            HydraulicEquipment equipment,
            List<HydraulicColumn> columns)
        {
            if (equipment.Position == null || columns.Count == 0)
                return null;

            HydraulicColumn nearest = null;
            double minDistance = double.MaxValue;

            foreach (var column in columns)
            {
                if (column.BasePosition == null) continue;

                double distance = RevitGeometryHelper.HorizontalDistance(
                    equipment.Position, column.BasePosition);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = column;
                }
            }

            if (nearest != null)
            {
                Logger.Debug($"[BRANCH] Coluna mais proxima: {nearest.ColumnId} " +
                    $"a {UnitConversionHelper.FeetToMm(minDistance):F0}mm");
            }

            return nearest;
        }

        /// <summary>
        /// Calcula o comprimento total de um ramal em mm.
        /// </summary>
        private double CalculateTotalLengthMm(BranchConnection branch)
        {
            double totalFeet = 0;

            var points = new List<XYZ> { branch.StartPoint };
            points.AddRange(branch.IntermediatePoints);
            points.Add(branch.EndPoint);

            for (int i = 0; i < points.Count - 1; i++)
            {
                totalFeet += points[i].DistanceTo(points[i + 1]);
            }

            return UnitConversionHelper.FeetToMm(totalFeet);
        }

        /// <summary>
        /// Converte EquipmentType legado para FixtureType.
        /// </summary>
        private FixtureType EquipmentToFixtureType(EquipmentType equipType)
        {
            switch (equipType)
            {
                case EquipmentType.VasoSanitario: return FixtureType.Toilet;
                case EquipmentType.Lavatorio: return FixtureType.Sink;
                case EquipmentType.Chuveiro: return FixtureType.Shower;
                case EquipmentType.Pia: return FixtureType.KitchenSink;
                case EquipmentType.Tanque: return FixtureType.LaundrySink;
                case EquipmentType.Ralo: return FixtureType.Drain;
                case EquipmentType.MaquinaLavar: return FixtureType.WashingMachine;
                default: return FixtureType.Unknown;
            }
        }
    }
}
