using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RevitHydraulicPlugin.Configuration;
using RevitHydraulicPlugin.Models;
using RevitHydraulicPlugin.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.Detection
{
    /// <summary>
    /// Serviço responsável por detectar ambientes (Rooms) hidráulicos no modelo Revit.
    /// 
    /// VERSÃO 2.0 — Usa RoomClassifierService para classificação multi-critério.
    /// Suporta classificação em 2 fases:
    ///   Fase 1: Classificação por nome (rápida, sem Transaction)
    ///   Fase 2: Reclassificação com fixtures (após detecção de equipamentos)
    /// </summary>
    public class RoomDetectionService
    {
        private readonly Document _document;
        private readonly RoomClassifierService _classifier;

        public RoomDetectionService(Document document)
        {
            _document = document;
            _classifier = new RoomClassifierService();
        }

        /// <summary>
        /// Detecta ambientes hidráulicos usando classificação por nome (Fase 1).
        /// Esta é a primeira passada — antes da detecção de equipamentos.
        /// </summary>
        /// <returns>Lista de HydraulicRoom classificados por nome.</returns>
        public List<HydraulicRoom> DetectHydraulicRooms()
        {
            using (Logger.MeasureTime("Deteccao de Ambientes"))
            {
                Logger.Info("[ROOM] Iniciando deteccao de ambientes hidraulicos...");

                var hydraulicRooms = new List<HydraulicRoom>();

                // Coleta todos os Rooms do documento
                var rooms = new FilteredElementCollector(_document)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .Cast<Room>()
                    .Where(r => r.Area > 0) // Ignora Rooms não colocados
                    .ToList();

                Logger.Info($"[ROOM] Total de Rooms no modelo: {rooms.Count}");

                int detectedCount = 0;
                int ignoredCount = 0;

                foreach (var room in rooms)
                {
                    string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
                    string roomNumber = room.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "";

                    // Classificação por nome usando RoomClassifierService
                    var analysis = _classifier.ClassifyByName(roomName);

                    if (analysis.IsHydraulic)
                    {
                        var level = _document.GetElement(room.LevelId) as Level;
                        var bbox = room.get_BoundingBox(null);
                        var centerPoint = RevitGeometryHelper.GetHorizontalCentroid(bbox);

                        // Calcula área em m² (Room.Area vem em pés²)
                        double areaSqM = UnitConversionHelper.SqFeetToSqM(room.Area);

                        var hydraulicRoom = new HydraulicRoom
                        {
                            RoomId = room.Id,
                            RoomName = roomName,
                            RoomNumber = roomNumber,
                            LevelId = room.LevelId,
                            LevelName = level?.Name ?? "Indefinido",
                            CenterPoint = centerPoint,
                            BoundingBox = bbox,
                            Type = analysis.Type,
                            ClassificationConfidence = analysis.Confidence,
                            ClassificationMethod = analysis.Method,
                            AreaSqM = areaSqM
                        };

                        hydraulicRooms.Add(hydraulicRoom);
                        detectedCount++;

                        Logger.LogRoomDetected(roomName, analysis.Type.ToString(), areaSqM);
                        Logger.Debug($"[ROOM]   Confianca: {analysis.Confidence:P0}, Motivo: {analysis.Reason}");
                    }
                    else
                    {
                        ignoredCount++;
                        Logger.Debug($"[ROOM] Ignorado: '{roomName}' — {analysis.Reason}");
                    }
                }

                Logger.Info($"[ROOM] === Resumo ===");
                Logger.Info($"[ROOM]   Detectados: {detectedCount}");
                Logger.Info($"[ROOM]   Ignorados: {ignoredCount}");

                return hydraulicRooms;
            }
        }

        /// <summary>
        /// Reclassifica ambientes usando fixtures detectados (Fase 2).
        /// 
        /// Deve ser chamado APÓS a detecção de equipamentos.
        /// Permite que ambientes com nome genérico mas com fixtures hidráulicos
        /// sejam promovidos a ambientes hidráulicos.
        /// </summary>
        /// <param name="existingRooms">Rooms já detectados na Fase 1.</param>
        /// <returns>Lista atualizada (pode conter novos Rooms promovidos).</returns>
        public List<HydraulicRoom> ReclassifyWithFixtures(List<HydraulicRoom> existingRooms)
        {
            Logger.Info("[ROOM] Reclassificando ambientes com dados de fixtures...");

            foreach (var room in existingRooms)
            {
                if (room.Equipment.Count == 0) continue;

                // Coleta os FixtureTypes presentes no Room
                var fixtureTypes = new List<FixtureType>();
                foreach (var equip in room.Equipment)
                {
                    var ft = EquipmentTypeToFixtureType(equip.Type);
                    if (ft != FixtureType.Unknown)
                        fixtureTypes.Add(ft);
                }

                if (fixtureTypes.Count == 0) continue;

                // Reclassifica usando nome + fixtures
                var reclassification = _classifier.ClassifyWithFixtures(room.RoomName, fixtureTypes);

                // Atualiza se a reclassificação melhorou a confiança
                if (reclassification.Confidence > room.ClassificationConfidence)
                {
                    var previousType = room.Type;
                    room.Type = reclassification.Type;
                    room.ClassificationConfidence = reclassification.Confidence;
                    room.ClassificationMethod = reclassification.Method;

                    if (previousType != room.Type)
                    {
                        Logger.Info($"[ROOM] Reclassificado: '{room.RoomName}' {previousType} -> {room.Type} " +
                            $"(confianca: {reclassification.Confidence:P0}, motivo: {reclassification.Reason})");
                    }
                    else
                    {
                        Logger.Debug($"[ROOM] Confirmado: '{room.RoomName}' = {room.Type} " +
                            $"(confianca subiu para {reclassification.Confidence:P0})");
                    }
                }
            }

            return existingRooms;
        }

        /// <summary>
        /// Detecta ambientes apenas em um nível específico.
        /// </summary>
        public List<HydraulicRoom> DetectHydraulicRoomsOnLevel(ElementId levelId)
        {
            return DetectHydraulicRooms()
                .Where(r => r.LevelId == levelId)
                .ToList();
        }

        /// <summary>
        /// Converte EquipmentType legado para FixtureType.
        /// </summary>
        private FixtureType EquipmentTypeToFixtureType(EquipmentType equipType)
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
