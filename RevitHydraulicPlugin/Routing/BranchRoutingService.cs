using Autodesk.Revit.DB;
using RevitHydraulicPlugin.Configuration;
using RevitHydraulicPlugin.Models;
using RevitHydraulicPlugin.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.Routing
{
    /// <summary>
    /// Serviço responsável por calcular rotas de ramais hidráulicos,
    /// conectando equipamentos às colunas hidráulicas mais próximas.
    /// 
    /// Gera rotas simples (tubulação reta) com inclinação aplicada
    /// para ramais de esgoto conforme regras configuradas.
    /// </summary>
    public class BranchRoutingService
    {
        private readonly Document _document;

        public BranchRoutingService(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// Calcula as rotas de ramais para todos os equipamentos dos ambientes detectados.
        /// 
        /// Para cada equipamento:
        /// 1. Encontra a coluna mais próxima do tipo adequado.
        /// 2. Calcula ponto de partida (conector ou posição do equipamento).
        /// 3. Calcula ponto de chegada na coluna (mesma elevação, ajustada pela inclinação).
        /// 4. Para esgoto, aplica inclinação (ponto final mais baixo que o inicial).
        /// 5. Atribui especificação de tubulação conforme HydraulicRules.
        /// </summary>
        /// <param name="rooms">Ambientes com equipamentos detectados.</param>
        /// <param name="columns">Colunas hidráulicas calculadas.</param>
        /// <returns>Lista de BranchConnection definindo cada ramal.</returns>
        public List<BranchConnection> CalculateBranches(
            List<HydraulicRoom> rooms,
            List<HydraulicColumn> columns)
        {
            Logger.Info("Iniciando cálculo de ramais hidráulicos...");

            var branches = new List<BranchConnection>();

            if (columns == null || columns.Count == 0)
            {
                Logger.Warning("Nenhuma coluna disponível para gerar ramais.");
                return branches;
            }

            // Separa colunas por tipo
            var sewerColumns = columns.Where(c => c.SystemType == ColumnSystemType.Esgoto).ToList();
            var coldWaterColumns = columns.Where(c => c.SystemType == ColumnSystemType.AguaFria).ToList();

            foreach (var room in rooms)
            {
                foreach (var equipment in room.Equipment)
                {
                    // === Ramal de Esgoto ===
                    var nearestSewerColumn = FindNearestColumn(equipment, sewerColumns);
                    if (nearestSewerColumn != null)
                    {
                        var sewerBranch = CreateBranch(equipment, nearestSewerColumn,
                            HydraulicRules.GetSewerSpec(equipment.Type));

                        if (sewerBranch != null)
                        {
                            branches.Add(sewerBranch);
                            Logger.Info($"  ✓ Ramal esgoto: {sewerBranch}");
                        }
                    }

                    // === Ramal de Água Fria ===
                    // (Ralo não precisa de ramal de água fria)
                    if (equipment.Type != EquipmentType.Ralo)
                    {
                        var nearestColdWaterColumn = FindNearestColumn(equipment, coldWaterColumns);
                        if (nearestColdWaterColumn != null)
                        {
                            var waterBranch = CreateBranch(equipment, nearestColdWaterColumn,
                                HydraulicRules.GetColdWaterSpec(equipment.Type));

                            if (waterBranch != null)
                            {
                                branches.Add(waterBranch);
                                Logger.Info($"  ✓ Ramal água fria: {waterBranch}");
                            }
                        }
                    }
                }
            }

            Logger.Info($"Total de ramais calculados: {branches.Count}");
            return branches;
        }

        /// <summary>
        /// Cria um BranchConnection entre equipamento e coluna.
        /// Calcula pontos de partida e chegada com inclinação aplicada.
        /// </summary>
        private BranchConnection CreateBranch(
            HydraulicEquipment equipment,
            HydraulicColumn column,
            PipeSpecification pipeSpec)
        {
            if (equipment.Position == null || column.BasePosition == null)
                return null;

            // Ponto de partida: posição do equipamento com ajuste de altura
            double heightOffset = pipeSpec.SlopePercent > 0
                ? UnitConversionHelper.MmToFeet(PluginSettings.BranchHeightOffsetMm)
                : UnitConversionHelper.MmToFeet(PluginSettings.ColdWaterBranchHeightMm);

            XYZ startPoint = new XYZ(
                equipment.Position.X,
                equipment.Position.Y,
                equipment.Position.Z + heightOffset);

            // Ponto de chegada: posição X,Y da coluna com ajuste de inclinação
            double horizontalDist = RevitGeometryHelper.HorizontalDistance(
                equipment.Position, column.BasePosition);

            // Calcula queda por inclinação (para esgoto)
            double slopeDrop = horizontalDist *
                UnitConversionHelper.SlopePercentToRevitSlope(pipeSpec.SlopePercent);

            XYZ endPoint = new XYZ(
                column.BasePosition.X,
                column.BasePosition.Y,
                startPoint.Z - slopeDrop);

            return new BranchConnection
            {
                Equipment = equipment,
                TargetColumn = column,
                StartPoint = startPoint,
                EndPoint = endPoint,
                PipeSpec = pipeSpec
            };
        }

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

            return nearest;
        }
    }
}
