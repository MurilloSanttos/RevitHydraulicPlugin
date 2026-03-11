using Autodesk.Revit.DB;
using RevitHydraulicPlugin.Configuration;
using RevitHydraulicPlugin.Models;
using RevitHydraulicPlugin.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.Routing
{
    /// <summary>
    /// Serviço responsável por calcular a posição e criar colunas hidráulicas verticais.
    /// As colunas atravessam todos os níveis do projeto, servindo como tronco principal
    /// para distribuição de água fria e coleta de esgoto.
    /// </summary>
    public class ColumnRoutingService
    {
        private readonly Document _document;

        public ColumnRoutingService(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// Calcula e gera as definições de colunas hidráulicas para os ambientes detectados.
        /// 
        /// Estratégia:
        /// - Agrupa Rooms por posição horizontal similar (mesmo "prumada").
        /// - Para cada grupo, cria uma coluna de água fria e uma de esgoto.
        /// - A coluna é posicionada próximo ao centro do primeiro ambiente do grupo.
        /// - A coluna atravessa todos os níveis do projeto.
        /// 
        /// NOTA: Esta é uma implementação simplificada para a versão 1.
        /// Versões futuras poderão considerar posição de shafts existentes.
        /// </summary>
        /// <param name="rooms">Lista de ambientes hidráulicos detectados com equipamentos.</param>
        /// <returns>Lista de colunas hidráulicas a serem criadas.</returns>
        public List<HydraulicColumn> CalculateColumns(List<HydraulicRoom> rooms)
        {
            Logger.Info("Iniciando cálculo de colunas hidráulicas...");

            var columns = new List<HydraulicColumn>();

            if (rooms == null || rooms.Count == 0)
            {
                Logger.Warning("Nenhum ambiente hidráulico para gerar colunas.");
                return columns;
            }

            // Obtém todos os níveis do projeto, ordenados por elevação
            var levels = GetProjectLevels();
            if (levels.Count == 0)
            {
                Logger.Error("Nenhum nível encontrado no projeto.");
                return columns;
            }

            var levelIds = levels.Select(l => l.Id).ToList();

            // Agrupa Rooms por posição horizontal (prumadas)
            var roomGroups = GroupRoomsByPosition(rooms);

            int columnIndex = 1;

            foreach (var group in roomGroups)
            {
                // Usa o centro do primeiro Room como base para a coluna
                var referenceRoom = group.First();
                XYZ basePosition = referenceRoom.CenterPoint;

                if (basePosition == null)
                {
                    Logger.Warning($"Room '{referenceRoom.RoomName}' sem ponto central. Coluna ignorada.");
                    continue;
                }

                // Aplica offset para não ficar exatamente no centro do ambiente
                double offsetFeet = UnitConversionHelper.MmToFeet(PluginSettings.ColumnOffsetFromCenterMm);
                XYZ columnPosition = RevitGeometryHelper.OffsetPoint(basePosition, offsetFeet, 0, 0);

                // Cria coluna de Água Fria
                columns.Add(new HydraulicColumn
                {
                    ColumnId = $"CAF-{columnIndex:D2}",
                    SystemType = ColumnSystemType.AguaFria,
                    BasePosition = columnPosition,
                    LevelIds = levelIds,
                    DiameterMm = HydraulicRules.DefaultColdWaterColumnDiameterMm,
                    AssociatedRoomId = referenceRoom.RoomId
                });

                Logger.Info($"  ✓ Coluna Água Fria CAF-{columnIndex:D2} em ({columnPosition.X:F2}, {columnPosition.Y:F2})");

                // Cria coluna de Esgoto (com pequeno offset para não sobrepor)
                XYZ sewerPosition = RevitGeometryHelper.OffsetPoint(columnPosition,
                    UnitConversionHelper.MmToFeet(200), 0, 0);

                columns.Add(new HydraulicColumn
                {
                    ColumnId = $"CES-{columnIndex:D2}",
                    SystemType = ColumnSystemType.Esgoto,
                    BasePosition = sewerPosition,
                    LevelIds = levelIds,
                    DiameterMm = HydraulicRules.DefaultSewerColumnDiameterMm,
                    AssociatedRoomId = referenceRoom.RoomId
                });

                Logger.Info($"  ✓ Coluna Esgoto CES-{columnIndex:D2} em ({sewerPosition.X:F2}, {sewerPosition.Y:F2})");

                columnIndex++;
            }

            Logger.Info($"Total de colunas calculadas: {columns.Count}");
            return columns;
        }

        /// <summary>
        /// Obtém todos os níveis do projeto, ordenados por elevação (do mais baixo ao mais alto).
        /// </summary>
        private List<Level> GetProjectLevels()
        {
            return new FilteredElementCollector(_document)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();
        }

        /// <summary>
        /// Agrupa Rooms que estão na mesma posição horizontal (mesma prumada).
        /// Rooms empilhados verticalmente são agrupados para compartilhar a mesma coluna.
        /// 
        /// A tolerância de agrupamento é definida em PluginSettings.
        /// </summary>
        private List<List<HydraulicRoom>> GroupRoomsByPosition(List<HydraulicRoom> rooms)
        {
            var groups = new List<List<HydraulicRoom>>();
            var assigned = new HashSet<ElementId>();

            double toleranceFeet = UnitConversionHelper.MmToFeet(
                PluginSettings.ProximityToleranceMm * 20); // Tolerância ampla para agrupamento

            foreach (var room in rooms)
            {
                if (assigned.Contains(room.RoomId) || room.CenterPoint == null)
                    continue;

                var group = new List<HydraulicRoom> { room };
                assigned.Add(room.RoomId);

                // Busca outros Rooms na mesma posição horizontal
                foreach (var otherRoom in rooms)
                {
                    if (assigned.Contains(otherRoom.RoomId) || otherRoom.CenterPoint == null)
                        continue;

                    double distance = RevitGeometryHelper.HorizontalDistance(
                        room.CenterPoint, otherRoom.CenterPoint);

                    if (distance < toleranceFeet)
                    {
                        group.Add(otherRoom);
                        assigned.Add(otherRoom.RoomId);
                    }
                }

                groups.Add(group);
            }

            Logger.Info($"Rooms agrupados em {groups.Count} prumada(s).");
            return groups;
        }
    }
}
