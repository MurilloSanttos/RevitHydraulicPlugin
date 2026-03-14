using Autodesk.Revit.UI;
using RevitHydraulicPlugin.Utilities;
using System;

namespace RevitHydraulicPlugin.UI
{
    /// <summary>
    /// Ponto de entrada principal do plugin como IExternalApplication.
    /// 
    /// Registrado no .addin como Application (não Command).
    /// Chamado automaticamente pelo Revit durante o startup.
    /// 
    /// Responsabilidades:
    /// - Construir a Ribbon (aba, painéis, botões)
    /// - Registrar event handlers (futuro)
    /// - Log de inicialização
    /// </summary>
    public class HydraulicApp : IExternalApplication
    {
        /// <summary>
        /// Chamado quando o Revit inicia e carrega o plugin.
        /// </summary>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                Logger.Info("[APP] RevitHydraulicPlugin starting...");

                // Construir a interface na Ribbon
                RibbonBuilder.Build(application);

                Logger.Info("[APP] Ribbon created: Hydraulic Tools");
                Logger.Info("[APP] RevitHydraulicPlugin started successfully.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("[APP] Failed to start RevitHydraulicPlugin", ex);

                // Mesmo com falha na Ribbon, os comandos individuais
                // ainda funcionam via Add-Ins > External Tools
                return Result.Failed;
            }
        }

        /// <summary>
        /// Chamado quando o Revit fecha.
        /// </summary>
        public Result OnShutdown(UIControlledApplication application)
        {
            Logger.Info("[APP] RevitHydraulicPlugin shutting down.");
            return Result.Succeeded;
        }
    }
}
