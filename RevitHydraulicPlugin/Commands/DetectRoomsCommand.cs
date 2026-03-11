using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitHydraulicPlugin.Detection;
using RevitHydraulicPlugin.Utilities;
using System;
using System.Linq;
using System.Text;

namespace RevitHydraulicPlugin.Commands
{
    /// <summary>
    /// Comando do Revit para detectar ambientes (Rooms) hidráulicos no modelo.
    /// Analisa os Rooms e exibe um relatório dos ambientes identificados.
    /// 
    /// Este comando é somente leitura — não modifica o modelo.
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class DetectRoomsCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                Logger.Info("=== Comando DetectRooms iniciado ===");

                var document = commandData.Application.ActiveUIDocument.Document;
                var roomDetection = new RoomDetectionService(document);

                // Executa a detecção
                var rooms = roomDetection.DetectHydraulicRooms();

                // Monta relatório para o usuário
                var sb = new StringBuilder();
                sb.AppendLine($"Ambientes hidráulicos detectados: {rooms.Count}");
                sb.AppendLine();

                foreach (var room in rooms)
                {
                    sb.AppendLine($"  • {room.RoomName} ({room.Type})");
                    sb.AppendLine($"    Nível: {room.LevelName}");
                    sb.AppendLine($"    Número: {room.RoomNumber}");
                    sb.AppendLine();
                }

                if (rooms.Count == 0)
                {
                    sb.AppendLine("Nenhum ambiente hidráulico encontrado.");
                    sb.AppendLine("Verifique se os Rooms possuem nomes reconhecíveis");
                    sb.AppendLine("(Banheiro, Cozinha, Lavabo, etc.).");
                }

                TaskDialog.Show("Detecção de Ambientes Hidráulicos", sb.ToString());

                Logger.Info("=== Comando DetectRooms finalizado ===");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Erro ao detectar ambientes: {ex.Message}";
                Logger.Error("Erro no DetectRoomsCommand", ex);
                return Result.Failed;
            }
        }
    }
}
