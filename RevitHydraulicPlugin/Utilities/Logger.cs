using System;
using System.IO;

namespace RevitHydraulicPlugin.Utilities
{
    /// <summary>
    /// Sistema de log simples para registrar operações e erros do plugin.
    /// Os logs são escritos em arquivo de texto na pasta de documentos do usuário.
    /// 
    /// Em versões futuras, pode ser substituído por um framework de logging
    /// como NLog ou Serilog.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDirectory;
        private static readonly string LogFilePath;
        private static readonly object LockObj = new object();

        static Logger()
        {
            LogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "RevitHydraulicPlugin",
                "Logs");

            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            LogFilePath = Path.Combine(LogDirectory,
                $"plugin_log_{DateTime.Now:yyyy-MM-dd}.txt");
        }

        /// <summary>
        /// Registra uma mensagem informativa.
        /// </summary>
        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// Registra uma mensagem de aviso.
        /// </summary>
        public static void Warning(string message)
        {
            WriteLog("WARN", message);
        }

        /// <summary>
        /// Registra uma mensagem de erro.
        /// </summary>
        public static void Error(string message)
        {
            WriteLog("ERROR", message);
        }

        /// <summary>
        /// Registra um erro com exceção.
        /// </summary>
        public static void Error(string message, Exception ex)
        {
            WriteLog("ERROR", $"{message} | Exception: {ex.GetType().Name}: {ex.Message}");
            WriteLog("ERROR", $"StackTrace: {ex.StackTrace}");
        }

        /// <summary>
        /// Registra uma mensagem de depuração (debug).
        /// </summary>
        public static void Debug(string message)
        {
            #if DEBUG
            WriteLog("DEBUG", message);
            #endif
        }

        /// <summary>
        /// Escreve uma linha no arquivo de log com timestamp e nível.
        /// Thread-safe via lock.
        /// </summary>
        private static void WriteLog(string level, string message)
        {
            try
            {
                lock (LockObj)
                {
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Falha silenciosa no log para não interromper a execução do plugin
            }
        }

        /// <summary>
        /// Retorna o caminho do arquivo de log atual.
        /// </summary>
        public static string GetCurrentLogFilePath()
        {
            return LogFilePath;
        }
    }
}
