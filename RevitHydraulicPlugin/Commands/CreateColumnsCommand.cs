using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitHydraulicPlugin.Detection;
using RevitHydraulicPlugin.Routing;
using RevitHydraulicPlugin.Services;
using RevitHydraulicPlugin.Utilities;
using System;
using System.Linq;
using System.Text;

namespace RevitHydraulicPlugin.Commands
{
    /// <summary>
    /// Comando do Revit para criar colunas hidráulicas no modelo.
    /// Detecta ambientes, calcula posições ideais e cria as colunas.
    /// 
    /// Este comando MODIFICA o modelo — cria elementos Pipe.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CreateColumnsCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                Logger.Info("=== Comando CreateColumns iniciado ===");

                var document = commandData.Application.ActiveUIDocument.Document;

                // Etapa 1: Detectar ambientes
                var roomDetection = new RoomDetectionService(document);
                var rooms = roomDetection.DetectHydraulicRooms();

                if (rooms.Count == 0)
                {
                    TaskDialog.Show("Criar Colunas Hidráulicas",
                        "Nenhum ambiente hidráulico detectado.");
                    return Result.Succeeded;
                }

                // Etapa 2: Calcular colunas
                var columnRouting = new ColumnRoutingService(document);
                var columns = columnRouting.CalculateColumns(rooms);

                if (columns.Count == 0)
                {
                    TaskDialog.Show("Criar Colunas Hidráulicas",
                        "Nenhuma coluna calculada.");
                    return Result.Succeeded;
                }

                // Confirmação do usuário antes de modificar o modelo
                var confirmDialog = new TaskDialog("Confirmar Criação");
                confirmDialog.MainContent =
                    $"Serão criadas {columns.Count} colunas hidráulicas " +
                    $"({columns.Count(c => c.SystemType == Models.ColumnSystemType.AguaFria)} água fria, " +
                    $"{columns.Count(c => c.SystemType == Models.ColumnSystemType.Esgoto)} esgoto).\n\n" +
                    "Deseja continuar?";
                confirmDialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

                if (confirmDialog.Show() != TaskDialogResult.Yes)
                {
                    return Result.Cancelled;
                }

                // Etapa 3: Criar colunas no modelo
                var pipeCreation = new PipeCreationService(document);
                int totalSegments = 0;

                using (var transaction = new Transaction(document, "Criar Colunas Hidráulicas"))
                {
                    transaction.Start();

                    try
                    {
                        foreach (var column in columns)
                        {
                            var createdIds = pipeCreation.CreateColumnPipes(column);
                            totalSegments += createdIds.Count;
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.RollBack();
                        message = $"Erro durante a criação: {ex.Message}";
                        Logger.Error("Erro no CreateColumnsCommand - Transaction rolled back", ex);
                        return Result.Failed;
                    }
                }

                // Relatório
                TaskDialog.Show("Colunas Hidráulicas Criadas",
                    $"✓ {columns.Count} colunas criadas com sucesso!\n" +
                    $"  Segmentos de tubulação: {totalSegments}");

                Logger.Info("=== Comando CreateColumns finalizado ===");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Erro ao criar colunas: {ex.Message}";
                Logger.Error("Erro no CreateColumnsCommand", ex);
                return Result.Failed;
            }
        }
    }
}
