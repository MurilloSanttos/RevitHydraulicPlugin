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
            try
            {
                Logger.Info("=== Comando RunFullPipeline iniciado ===");

                var document = commandData.Application.ActiveUIDocument.Document;

                // Confirmação antes de executar
                var confirmDialog = new TaskDialog("Pipeline Hidráulico Completo");
                confirmDialog.MainContent =
                    "Este comando irá executar a automação hidráulica completa:\n\n" +
                    "  1. Detectar ambientes hidráulicos\n" +
                    "  2. Identificar equipamentos\n" +
                    "  3. Criar colunas hidráulicas\n" +
                    "  4. Gerar ramais de conexão\n\n" +
                    "O modelo será modificado. Deseja continuar?";
                confirmDialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;
                confirmDialog.DefaultButton = TaskDialogResult.No;

                if (confirmDialog.Show() != TaskDialogResult.Yes)
                {
                    return Result.Cancelled;
                }

                // Executa o pipeline completo
                var pipelineService = new HydraulicPipelineService(document);
                var result = pipelineService.RunFullPipeline();

                // Exibe relatório ao usuário
                var resultDialog = new TaskDialog("Resultado da Automação Hidráulica");
                resultDialog.MainContent = result.ToReport();

                if (result.Success)
                {
                    resultDialog.MainIcon = TaskDialogIcon.TaskDialogIconInformation;
                }
                else
                {
                    resultDialog.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                }

                resultDialog.Show();

                Logger.Info("=== Comando RunFullPipeline finalizado ===");
                return result.Success ? Result.Succeeded : Result.Failed;
            }
            catch (Exception ex)
            {
                message = $"Erro na automação hidráulica: {ex.Message}";
                Logger.Error("Erro no RunFullPipelineCommand", ex);
                return Result.Failed;
            }
        }
    }
}
