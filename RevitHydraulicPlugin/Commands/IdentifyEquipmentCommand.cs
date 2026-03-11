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
    /// Comando do Revit para identificar equipamentos hidráulicos nos ambientes.
    /// Primeiro detecta os ambientes, depois identifica os equipamentos em cada um.
    /// 
    /// Este comando é somente leitura — não modifica o modelo.
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class IdentifyEquipmentCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                Logger.Info("=== Comando IdentifyEquipment iniciado ===");

                var document = commandData.Application.ActiveUIDocument.Document;

                // Etapa 1: Detectar ambientes
                var roomDetection = new RoomDetectionService(document);
                var rooms = roomDetection.DetectHydraulicRooms();

                if (rooms.Count == 0)
                {
                    TaskDialog.Show("Identificação de Equipamentos",
                        "Nenhum ambiente hidráulico detectado. " +
                        "Execute primeiro a detecção de ambientes.");
                    return Result.Succeeded;
                }

                // Etapa 2: Identificar equipamentos
                var equipmentDetection = new EquipmentDetectionService(document);
                var equipment = equipmentDetection.DetectEquipment(rooms);

                // Monta relatório
                var sb = new StringBuilder();
                sb.AppendLine($"Equipamentos hidráulicos encontrados: {equipment.Count}");
                sb.AppendLine($"Em {rooms.Count} ambiente(s) hidráulico(s)");
                sb.AppendLine();

                foreach (var room in rooms.Where(r => r.Equipment.Count > 0))
                {
                    sb.AppendLine($"▸ {room.RoomName} ({room.LevelName}):");
                    foreach (var equip in room.Equipment)
                    {
                        sb.AppendLine($"    • {equip.Type}: {equip.FamilyName}");
                        sb.AppendLine($"      Conectores: {equip.Connectors.Count}");
                    }
                    sb.AppendLine();
                }

                var emptyRooms = rooms.Where(r => r.Equipment.Count == 0).ToList();
                if (emptyRooms.Count > 0)
                {
                    sb.AppendLine("Ambientes sem equipamentos:");
                    foreach (var room in emptyRooms)
                    {
                        sb.AppendLine($"    • {room.RoomName} ({room.LevelName})");
                    }
                }

                TaskDialog.Show("Identificação de Equipamentos", sb.ToString());

                Logger.Info("=== Comando IdentifyEquipment finalizado ===");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Erro ao identificar equipamentos: {ex.Message}";
                Logger.Error("Erro no IdentifyEquipmentCommand", ex);
                return Result.Failed;
            }
        }
    }
}
