using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitHydraulicPlugin.Services;
using RevitHydraulicPlugin.Utilities;
using System;

namespace RevitHydraulicPlugin.Commands
{
    /// <summary>
    /// Comando do Revit para executar o pipeline completo de automação hidráulica.
    /// Executa todas as 4 etapas em sequência:
    /// 
    /// 1. Detecção de ambientes hidráulicos
    /// 2. Identificação de equipamentos
    /// 3. Criação de colunas hidráulicas
    /// 4. Geração de ramais básicos
    /// 
    /// Este é o comando principal do plugin, ideal para uso rápido
    /// quando o projetista deseja gerar toda a infraestrutura hidráulica de uma vez.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class RunFullPipelineCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            // Inicia sessão de logging
            Logger.StartSession("RunFullPipeline");
            bool success = false;

            try
            {
                var document = commandData.Application.ActiveUIDocument.Document;

                Logger.Info($"Projeto: {document.Title}");
                Logger.Info($"Arquivo: {document.PathName}");

                // Confirmação antes de executar
                var confirmDialog = new TaskDialog("Pipeline Hidraulico Completo");
                confirmDialog.MainContent =
                    "Este comando ira executar a automacao hidraulica completa:\n\n" +
                    "  1. Detectar ambientes hidraulicos\n" +
                    "  2. Identificar equipamentos\n" +
                    "  3. Criar colunas hidraulicas\n" +
                    "  4. Gerar ramais de conexao\n\n" +
                    "O modelo sera modificado. Deseja continuar?";
                confirmDialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;
                confirmDialog.DefaultButton = TaskDialogResult.No;

                if (confirmDialog.Show() != TaskDialogResult.Yes)
                {
                    Logger.Info("Execucao cancelada pelo usuario.");
                    Logger.EndSession(true);
                    return Result.Cancelled;
                }

                // Executa o pipeline completo
                var pipelineService = new HydraulicPipelineService(document);
                var result = pipelineService.RunFullPipeline();

                // Exibe relatório ao usuário
                var resultDialog = new TaskDialog("Resultado da Automacao Hidraulica");
                resultDialog.MainContent = result.ToReport();

                if (result.Success)
                {
                    resultDialog.MainIcon = TaskDialogIcon.TaskDialogIconInformation;
                }
                else
                {
                    resultDialog.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                }

                // Adiciona informação de log no diálogo
                resultDialog.ExpandedContent =
                    $"Log salvo em:\n{Logger.GetCurrentLogFilePath()}";

                resultDialog.Show();

                success = result.Success;
                return result.Success ? Result.Succeeded : Result.Failed;
            }
            catch (Exception ex)
            {
                message = $"Erro na automacao hidraulica: {ex.Message}";
                Logger.Error("Erro fatal no RunFullPipelineCommand", ex);
                return Result.Failed;
            }
            finally
            {
                Logger.EndSession(success);
            }
        }
    }
}
