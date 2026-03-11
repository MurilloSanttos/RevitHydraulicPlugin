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
    /// Analisa todos os Rooms do documento e filtra aqueles que possuem
    /// instalações hidráulicas baseado em padrões de nome.
    /// </summary>
    public class RoomDetectionService
    {
        private readonly Document _document;

        /// <summary>
        /// Inicializa o serviço com o documento Revit ativo.
        /// </summary>
        /// <param name="document">Documento Revit a ser analisado.</param>
        public RoomDetectionService(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// Detecta todos os ambientes hidráulicos no modelo.
        /// 
        /// Fluxo:
        /// 1. Coleta todos os Rooms do documento usando FilteredElementCollector.
        /// 2. Para cada Room, tenta classificar pelo nome usando RoomClassification.
        /// 3. Cria um HydraulicRoom com dados relevantes para os classificados.
        /// 4. Retorna apenas os ambientes reconhecidos como hidráulicos.
        /// </summary>
        /// <returns>Lista de HydraulicRoom detectados.</returns>
        public List<HydraulicRoom> DetectHydraulicRooms()
        {
            Logger.Info("Iniciando detecção de ambientes hidráulicos...");

            var hydraulicRooms = new List<HydraulicRoom>();

            // Coleta todos os Rooms do documento
            var rooms = new FilteredElementCollector(_document)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>()
                .Where(r => r.Area > 0) // Ignora Rooms sem área (não colocados)
                .ToList();

            Logger.Info($"Total de Rooms encontrados no modelo: {rooms.Count}");

            foreach (var room in rooms)
            {
                string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
                string roomNumber = room.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "";

                // Tenta classificar o ambiente por nome
                if (RoomClassification.TryClassify(roomName, out RoomType roomType))
                {
                    // Obtém informações do nível
                    var level = _document.GetElement(room.LevelId) as Level;

                    // Obtém BoundingBox e calcula centróide
                    var bbox = room.get_BoundingBox(null);
                    var centerPoint = RevitGeometryHelper.GetHorizontalCentroid(bbox);

                    var hydraulicRoom = new HydraulicRoom
                    {
                        RoomId = room.Id,
                        RoomName = roomName,
                        RoomNumber = roomNumber,
                        LevelId = room.LevelId,
                        LevelName = level?.Name ?? "Indefinido",
                        CenterPoint = centerPoint,
                        BoundingBox = bbox,
                        Type = roomType
                    };

                    hydraulicRooms.Add(hydraulicRoom);

                    Logger.Info($"  ✓ Detectado: {hydraulicRoom}");
                }
                else
                {
                    Logger.Debug($"  ✗ Ignorado: '{roomName}' (não classificado como hidráulico)");
                }
            }

            Logger.Info($"Total de ambientes hidráulicos detectados: {hydraulicRooms.Count}");
            return hydraulicRooms;
        }

        /// <summary>
        /// Detecta ambientes hidráulicos apenas em um nível específico.
        /// </summary>
        /// <param name="levelId">ElementId do nível a filtrar.</param>
        /// <returns>Lista de HydraulicRoom no nível especificado.</returns>
        public List<HydraulicRoom> DetectHydraulicRoomsOnLevel(ElementId levelId)
        {
            return DetectHydraulicRooms()
                .Where(r => r.LevelId == levelId)
                .ToList();
        }
    }
}
