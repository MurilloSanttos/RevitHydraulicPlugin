using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitHydraulicPlugin.Services;
using RevitHydraulicPlugin.Utilities;
using System;
using System.Linq;

namespace RevitHydraulicPlugin.UI
{
    /// <summary>
    /// Comando do Revit para geração da automação hidráulica com confirmação.
    /// 
    /// Fluxo:
    ///   1. Executar PreviewAnalysisService (análise)
    ///   2. Exibir resumo ao usuário (PreviewFormatter)
    ///   3. Pedir confirmação via TaskDialog
    ///   4. Se confirmado, criar tubulações no modelo
    ///   5. Exibir relatório final
    /// 
    /// Acessível via Ribbon: Hydraulic Tools → Hydraulic Automation → Generate
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class GenerateCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            Logger.StartSession("GenerateHydraulicLayout");
            bool success = false;

            try
            {
                var document = commandData.Application.ActiveUIDocument.Document;

                Logger.Info("[GENERATE] Hydraulic generation started");
                Logger.Info($"[GENERATE] Project: {document.Title}");

                // ════════════════════════════════════════════════
                //  ETAPA 1: ANÁLISE (sem modificar o modelo)
                // ════════════════════════════════════════════════

                Logger.Info("[GENERATE] Running analysis...");
                var analysisService = new PreviewAnalysisService(document);
                var previewResult = analysisService.RunAnalysis();

                if (!previewResult.AnalysisSuccessful)
                {
                    ShowErrorDialog("Analysis Failed", previewResult.ErrorMessage);
                    Logger.Error($"[GENERATE] Analysis failed: {previewResult.ErrorMessage}");
                    return Result.Failed;
                }

                if (previewResult.HydraulicRoomsDetected == 0)
                {
                    ShowInfoDialog("No Data",
                        "No hydraulic rooms were detected in the model.\n\n" +
                        "Make sure your project has Rooms with hydraulic names " +
                        "(e.g., Banheiro, Cozinha, Lavanderia).");
                    Logger.Warning("[GENERATE] No hydraulic rooms detected");
                    return Result.Cancelled;
                }

                if (previewResult.FixturesDetected == 0)
                {
                    ShowInfoDialog("No Fixtures",
                        "Hydraulic rooms were detected but no fixtures were found.\n\n" +
                        "Make sure plumbing fixtures are placed in the model.");
                    Logger.Warning("[GENERATE] No fixtures detected");
                    return Result.Cancelled;
                }

                // ════════════════════════════════════════════════
                //  ETAPA 2: CONFIRMAÇÃO DO USUÁRIO
                // ════════════════════════════════════════════════

                string confirmText = PreviewFormatter.FormatConfirmation(previewResult);

                var confirmDialog = new TaskDialog("Hydraulic Generation — Confirm");
                confirmDialog.TitleAutoPrefix = false;
                confirmDialog.MainInstruction = "Ready to Generate Hydraulic Layout";
                confirmDialog.MainContent = confirmText;
                confirmDialog.MainIcon = TaskDialogIcon.TaskDialogIconWarning;

                confirmDialog.CommonButtons =
                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;
                confirmDialog.DefaultButton = TaskDialogResult.No;

                // Conteúdo expandido
                confirmDialog.ExpandedContent =
                    PreviewFormatter.FormatDetailed(previewResult);

                confirmDialog.FooterText =
                    "The model will be modified. You can Undo with Ctrl+Z.";

                if (confirmDialog.Show() != TaskDialogResult.Yes)
                {
                    Logger.Info("[GENERATE] Hydraulic generation canceled by user");
                    Logger.EndSession(true);
                    return Result.Cancelled;
                }

                // ════════════════════════════════════════════════
                //  ETAPA 3: GERAÇÃO (modifica o modelo)
                // ════════════════════════════════════════════════

                Logger.Info("[GENERATE] User confirmed. Starting generation...");
                Logger.Info("[GENERATE] Branch generation started");

                var pipeCreation = new PipeCreationService(document);
                var columns = analysisService.GetCalculatedColumns();
                var branches = analysisService.GetCalculatedBranches();

                int columnsCreated = 0;
                int branchesCreated = 0;

                using (var transaction = new Transaction(document, "Generate Hydraulic Layout"))
                {
                    transaction.Start();

                    try
                    {
                        // Criar colunas
                        foreach (var column in columns)
                        {
                            pipeCreation.CreateColumnPipes(column);
                            columnsCreated++;
                        }

                        Logger.Info($"[GENERATE] Columns created: {columnsCreated}");

                        // Criar ramais
                        foreach (var branch in branches)
                        {
                            pipeCreation.CreateBranchPipes(branch);
                            branchesCreated++;
                        }

                        Logger.Info($"[GENERATE] Branches created: {branchesCreated}");

                        transaction.Commit();
                        success = true;

                        Logger.Info("[GENERATE] Branch generation completed");
                        Logger.Info("[GENERATE] Transaction committed successfully");
                    }
                    catch (Exception ex)
                    {
                        transaction.RollBack();
                        Logger.Error("[GENERATE] Error during generation — rolled back", ex);

                        ShowErrorDialog("Generation Failed",
                            $"Error creating pipes: {ex.Message}\n\n" +
                            "The transaction was rolled back. No changes were made.");
                        return Result.Failed;
                    }
                }

                // ════════════════════════════════════════════════
                //  ETAPA 4: RESULTADO FINAL
                // ════════════════════════════════════════════════

                int totalPipes = columns.Sum(c => c.CreatedPipeIds?.Count ?? 0)
                               + branches.Sum(b => b.CreatedPipeIds?.Count ?? 0);

                ShowResultDialog(previewResult, columnsCreated, branchesCreated, totalPipes);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error in generation: {ex.Message}";
                Logger.Error("[GENERATE] Fatal error in GenerateCommand", ex);
                return Result.Failed;
            }
            finally
            {
                Logger.EndSession(success);
            }
        }

        // ════════════════════════════════════════════════
        //  DIÁLOGOS
        // ════════════════════════════════════════════════

        private void ShowErrorDialog(string title, string content)
        {
            var dialog = new TaskDialog(title);
            dialog.TitleAutoPrefix = false;
            dialog.MainInstruction = title;
            dialog.MainContent = content;
            dialog.MainIcon = TaskDialogIcon.TaskDialogIconError;
            dialog.CommonButtons = TaskDialogCommonButtons.Ok;
            dialog.Show();
        }

        private void ShowInfoDialog(string title, string content)
        {
            var dialog = new TaskDialog(title);
            dialog.TitleAutoPrefix = false;
            dialog.MainInstruction = title;
            dialog.MainContent = content;
            dialog.MainIcon = TaskDialogIcon.TaskDialogIconInformation;
            dialog.CommonButtons = TaskDialogCommonButtons.Ok;
            dialog.Show();
        }

        private void ShowResultDialog(Models.PreviewResult preview,
            int columnsCreated, int branchesCreated, int totalPipes)
        {
            var dialog = new TaskDialog("Hydraulic Generation — Complete");
            dialog.TitleAutoPrefix = false;
            dialog.MainInstruction = "Hydraulic Layout Generated Successfully!";
            dialog.MainIcon = TaskDialogIcon.TaskDialogIconInformation;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══ RESULTS ═══");
            sb.AppendLine();
            sb.AppendLine($"  Hydraulic rooms:  {preview.HydraulicRoomsDetected}");
            sb.AppendLine($"  Fixtures used:    {preview.FixturesDetected}");
            sb.AppendLine($"  Columns created:  {columnsCreated}");
            sb.AppendLine($"  Branches created: {branchesCreated}");
            sb.AppendLine($"  Total pipes:      {totalPipes}");
            sb.AppendLine();
            sb.AppendLine("You can Undo this operation with Ctrl+Z.");

            dialog.MainContent = sb.ToString();

            dialog.ExpandedContent = PreviewFormatter.FormatDetailed(preview);

            dialog.FooterText =
                $"Log: {Logger.GetCurrentLogFilePath()}";

            dialog.CommonButtons = TaskDialogCommonButtons.Ok;
            dialog.Show();
        }
    }
}
