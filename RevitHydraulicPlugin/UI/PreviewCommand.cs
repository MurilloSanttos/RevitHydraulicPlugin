using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitHydraulicPlugin.Services;
using RevitHydraulicPlugin.Utilities;
using System;

namespace RevitHydraulicPlugin.UI
{
    /// <summary>
    /// Comando do Revit para preview da automação hidráulica.
    /// 
    /// Executa análise completa do modelo (detecção de rooms, fixtures,
    /// cálculo de colunas e ramais) SEM modificar o projeto.
    /// 
    /// Exibe o resultado em um TaskDialog organizado para que o usuário
    /// possa avaliar o impacto antes de executar a geração.
    /// 
    /// Acessível via Ribbon: Hydraulic Tools → Hydraulic Automation → Preview
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class PreviewCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            Logger.StartSession("PreviewHydraulicLayout");

            try
            {
                var document = commandData.Application.ActiveUIDocument.Document;

                Logger.Info("[PREVIEW] Hydraulic preview started");
                Logger.Info($"[PREVIEW] Project: {document.Title}");

                // ── Executar análise ──
                var analysisService = new PreviewAnalysisService(document);
                var previewResult = analysisService.RunAnalysis();

                // ── Formatar resultado ──
                string mainContent = PreviewFormatter.FormatForDialog(previewResult);
                string detailedContent = PreviewFormatter.FormatDetailed(previewResult);

                Logger.Info("[PREVIEW] Preview completed successfully");

                // ── Exibir TaskDialog ──
                var dialog = new TaskDialog("Hydraulic Automation Preview");

                dialog.TitleAutoPrefix = false;
                dialog.MainInstruction = GetMainInstruction(previewResult);
                dialog.MainContent = mainContent;

                // Ícone baseado no resultado
                if (!previewResult.AnalysisSuccessful)
                {
                    dialog.MainIcon = TaskDialogIcon.TaskDialogIconError;
                }
                else if (previewResult.Warnings.Count > 0)
                {
                    dialog.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                }
                else
                {
                    dialog.MainIcon = TaskDialogIcon.TaskDialogIconInformation;
                }

                // Conteúdo expandido com detalhes por room
                dialog.ExpandedContent = detailedContent;

                // Rodapé com caminho do log
                dialog.FooterText =
                    $"Log: {Logger.GetCurrentLogFilePath()}";

                // Botão OK
                dialog.CommonButtons = TaskDialogCommonButtons.Ok;

                dialog.Show();

                Logger.EndSession(true);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error in preview: {ex.Message}";
                Logger.Error("[PREVIEW] Fatal error in PreviewCommand", ex);
                Logger.EndSession(false);
                return Result.Failed;
            }
        }

        /// <summary>
        /// Gera a instrução principal do diálogo com base no resultado.
        /// </summary>
        private string GetMainInstruction(Models.PreviewResult result)
        {
            if (!result.AnalysisSuccessful)
                return "Analysis Failed";

            if (result.HydraulicRoomsDetected == 0)
                return "No Hydraulic Rooms Detected";

            string summary = PreviewFormatter.FormatSummary(result);
            return $"Analysis Complete — {summary}";
        }
    }
}
