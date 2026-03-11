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
    /// Comando do Revit para gerar ramais hidráulicos básicos.
    /// Conecta equipamentos às colunas existentes ou recém-calculadas.
    /// 
    /// Este comando MODIFICA o modelo — cria elementos Pipe.
    /// NOTA: Deve ser executado após a criação das colunas, ou como parte do pipeline completo.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class GenerateBranchesCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                Logger.Info("=== Comando GenerateBranches iniciado ===");

                var document = commandData.Application.ActiveUIDocument.Document;

                // Etapa 1: Detectar ambientes
                var roomDetection = new RoomDetectionService(document);
                var rooms = roomDetection.DetectHydraulicRooms();

                if (rooms.Count == 0)
                {
                    TaskDialog.Show("Gerar Ramais", "Nenhum ambiente hidráulico detectado.");
                    return Result.Succeeded;
                }

                // Etapa 2: Identificar equipamentos
                var equipmentDetection = new EquipmentDetectionService(document);
                var equipment = equipmentDetection.DetectEquipment(rooms);

                if (equipment.Count == 0)
                {
                    TaskDialog.Show("Gerar Ramais", "Nenhum equipamento hidráulico detectado.");
                    return Result.Succeeded;
                }

                // Etapa 3: Calcular colunas (para referência)
                var columnRouting = new ColumnRoutingService(document);
                var columns = columnRouting.CalculateColumns(rooms);

                // Etapa 4: Calcular ramais
                var branchRouting = new BranchRoutingService(document);
                var branches = branchRouting.CalculateBranches(rooms, columns);

                if (branches.Count == 0)
                {
                    TaskDialog.Show("Gerar Ramais", "Nenhum ramal calculado.");
                    return Result.Succeeded;
                }

                // Confirmação
                var confirmDialog = new TaskDialog("Confirmar Geração de Ramais");
                confirmDialog.MainContent =
                    $"Serão gerados {branches.Count} ramais hidráulicos " +
                    $"para {equipment.Count} equipamento(s).\n\n" +
                    "Deseja continuar?";
                confirmDialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

                if (confirmDialog.Show() != TaskDialogResult.Yes)
                {
                    return Result.Cancelled;
                }

                // Etapa 5: Criar ramais no modelo
                var pipeCreation = new PipeCreationService(document);
                int totalPipes = 0;

                using (var transaction = new Transaction(document, "Gerar Ramais Hidráulicos"))
                {
                    transaction.Start();

                    try
                    {
                        foreach (var branch in branches)
                        {
                            var createdIds = pipeCreation.CreateBranchPipes(branch);
                            totalPipes += createdIds.Count;
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.RollBack();
                        message = $"Erro durante a geração: {ex.Message}";
                        Logger.Error("Erro no GenerateBranchesCommand - Transaction rolled back", ex);
                        return Result.Failed;
                    }
                }

                // Relatório
                TaskDialog.Show("Ramais Hidráulicos Gerados",
                    $"✓ {totalPipes} ramais criados com sucesso!\n" +
                    $"  Equipamentos conectados: {equipment.Count}");

                Logger.Info("=== Comando GenerateBranches finalizado ===");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Erro ao gerar ramais: {ex.Message}";
                Logger.Error("Erro no GenerateBranchesCommand", ex);
                return Result.Failed;
            }
        }
    }
}
