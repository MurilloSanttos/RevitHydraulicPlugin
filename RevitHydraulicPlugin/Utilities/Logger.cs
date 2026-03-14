using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RevitHydraulicPlugin.Utilities
{
    /// <summary>
    /// Sistema de logging robusto para o plugin hidráulico.
    /// 
    /// Funcionalidades:
    /// - Log em arquivo com timestamp e nível
    /// - Sessões de execução com ID único
    /// - Contadores de eventos por nível
    /// - Logging de performance (tempo de execução)
    /// - Thread-safe via lock
    /// - Criação automática de diretórios
    /// - Relatório de sessão ao finalizar
    /// 
    /// Formato:
    ///   [2026-03-14 10:42:31.123] [INFO ] [SessaoId] mensagem
    /// 
    /// Local dos logs:
    ///   Documents/RevitHydraulicPlugin/logs/plugin-log.txt
    ///   (e backup diário: plugin-log_2026-03-14.txt)
    /// </summary>
    public static class Logger
    {
        // ────────── Configuração ──────────

        private static readonly string LogDirectory;
        private static readonly string MainLogFilePath;
        private static readonly string DailyLogFilePath;
        private static readonly object LockObj = new object();

        // ────────── Estado da Sessão ──────────

        private static string _currentSessionId;
        private static DateTime _sessionStartTime;
        private static int _infoCount;
        private static int _warnCount;
        private static int _errorCount;
        private static int _debugCount;
        private static bool _sessionActive;

        // ────────── Nível mínimo de log ──────────

        /// <summary>
        /// Nível mínimo para gravar no arquivo de log.
        /// Mensagens abaixo deste nível serão ignoradas.
        /// </summary>
        public static LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

        // ────────── Inicialização ──────────

        static Logger()
        {
            LogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "RevitHydraulicPlugin",
                "logs");

            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
            }
            catch
            {
                // Se não conseguir criar o diretório, usa TEMP como fallback
                LogDirectory = Path.Combine(Path.GetTempPath(), "RevitHydraulicPlugin", "logs");
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
            }

            // Arquivo principal persistente
            MainLogFilePath = Path.Combine(LogDirectory, "plugin-log.txt");

            // Arquivo diário para histórico
            DailyLogFilePath = Path.Combine(LogDirectory,
                $"plugin-log_{DateTime.Now:yyyy-MM-dd}.txt");
        }

        // ════════════════════════════════════════════════
        //  SESSÃO — Agrupa logs de uma execução
        // ════════════════════════════════════════════════

        /// <summary>
        /// Inicia uma nova sessão de logging.
        /// Cada execução de comando deve iniciar uma sessão.
        /// </summary>
        /// <param name="commandName">Nome do comando sendo executado.</param>
        public static void StartSession(string commandName)
        {
            _currentSessionId = DateTime.Now.ToString("HHmmss") + "-" + commandName;
            _sessionStartTime = DateTime.Now;
            _infoCount = 0;
            _warnCount = 0;
            _errorCount = 0;
            _debugCount = 0;
            _sessionActive = true;

            WriteLog(LogLevel.Info, "========================================");
            WriteLog(LogLevel.Info, $"SESSAO INICIADA: {commandName}");
            WriteLog(LogLevel.Info, $"  Sessao ID: {_currentSessionId}");
            WriteLog(LogLevel.Info, $"  Inicio: {_sessionStartTime:yyyy-MM-dd HH:mm:ss}");
            WriteLog(LogLevel.Info, $"  Log: {MainLogFilePath}");
            WriteLog(LogLevel.Info, "========================================");
        }

        /// <summary>
        /// Finaliza a sessão atual e grava um relatório resumo.
        /// </summary>
        /// <param name="success">Se a operação foi bem-sucedida.</param>
        public static void EndSession(bool success)
        {
            if (!_sessionActive) return;

            var elapsed = DateTime.Now - _sessionStartTime;

            WriteLog(LogLevel.Info, "========================================");
            WriteLog(LogLevel.Info, "SESSAO FINALIZADA");
            WriteLog(LogLevel.Info, $"  Status: {(success ? "SUCESSO" : "FALHA")}");
            WriteLog(LogLevel.Info, $"  Duracao: {elapsed.TotalSeconds:F2}s");
            WriteLog(LogLevel.Info, $"  Eventos: INFO={_infoCount} WARN={_warnCount} ERROR={_errorCount} DEBUG={_debugCount}");
            WriteLog(LogLevel.Info, "========================================");
            WriteLog(LogLevel.Info, ""); // linha em branco para separar sessões

            _sessionActive = false;
        }

        // ════════════════════════════════════════════════
        //  MÉTODOS PRINCIPAIS DE LOG
        // ════════════════════════════════════════════════

        /// <summary>
        /// Registra uma mensagem informativa.
        /// Uso: ações normais do plugin (detecção, criação, etc.)
        /// </summary>
        public static void Info(string message)
        {
            _infoCount++;
            WriteLog(LogLevel.Info, message);
        }

        /// <summary>
        /// Registra uma mensagem de aviso.
        /// Uso: situações inesperadas mas não críticas.
        /// </summary>
        public static void Warning(string message)
        {
            _warnCount++;
            WriteLog(LogLevel.Warning, message);
        }

        /// <summary>
        /// Registra uma mensagem de erro.
        /// Uso: falhas na execução.
        /// </summary>
        public static void Error(string message)
        {
            _errorCount++;
            WriteLog(LogLevel.Error, message);
        }

        /// <summary>
        /// Registra um erro com exceção completa (tipo, mensagem e stack trace).
        /// Uso: catch blocks com informação da Exception.
        /// </summary>
        public static void Error(string message, Exception ex)
        {
            _errorCount++;
            WriteLog(LogLevel.Error, $"{message}");
            WriteLog(LogLevel.Error, $"  Exception: {ex.GetType().Name}: {ex.Message}");

            if (ex.InnerException != null)
            {
                WriteLog(LogLevel.Error, $"  InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }

            WriteLog(LogLevel.Error, $"  StackTrace: {ex.StackTrace}");
        }

        /// <summary>
        /// Registra uma mensagem de depuração.
        /// Somente gravada em builds DEBUG ou se MinimumLevel = Debug.
        /// </summary>
        public static void Debug(string message)
        {
            _debugCount++;
            WriteLog(LogLevel.Debug, message);
        }

        // ════════════════════════════════════════════════
        //  MÉTODOS ESPECIALIZADOS
        // ════════════════════════════════════════════════

        /// <summary>
        /// Registra a detecção de um ambiente hidráulico.
        /// </summary>
        public static void LogRoomDetected(string roomName, string roomType, double areaSqM)
        {
            Info($"[ROOM] Detectado: '{roomName}' -> {roomType} ({areaSqM:F1} m2)");
        }

        /// <summary>
        /// Registra a detecção de um equipamento hidráulico.
        /// </summary>
        public static void LogFixtureDetected(string familyName, string fixtureType, string roomName)
        {
            Info($"[FIXTURE] Detectado: '{familyName}' -> {fixtureType} em '{roomName}'");
        }

        /// <summary>
        /// Registra a classificação de um equipamento com detalhes.
        /// </summary>
        public static void LogFixtureClassification(string familyName, string classifiedAs,
            string method, int connectorCount)
        {
            Debug($"[CLASSIFY] '{familyName}' -> {classifiedAs} (metodo: {method}, conectores: {connectorCount})");
        }

        /// <summary>
        /// Registra a criação de uma tubulação.
        /// </summary>
        public static void LogPipeCreated(string pipeType, double diameterMm, double lengthMm, string systemType)
        {
            Info($"[PIPE] Criado: {pipeType} D{diameterMm:F0}mm L={lengthMm:F0}mm Sistema={systemType}");
        }

        /// <summary>
        /// Registra a criação de uma coluna hidráulica.
        /// </summary>
        public static void LogColumnCreated(string columnId, int segmentCount, string systemType)
        {
            Info($"[COLUMN] Criada: {columnId} ({segmentCount} segmentos, {systemType})");
        }

        /// <summary>
        /// Mede e registra o tempo de execução de uma etapa.
        /// Uso:
        ///   using (Logger.MeasureTime("Detecção de Rooms"))
        ///   {
        ///       // código da etapa
        ///   }
        /// </summary>
        public static TimeMeasurement MeasureTime(string operationName)
        {
            return new TimeMeasurement(operationName);
        }

        // ════════════════════════════════════════════════
        //  ACESSO AO ESTADO
        // ════════════════════════════════════════════════

        /// <summary>
        /// Retorna o caminho do arquivo de log principal.
        /// </summary>
        public static string GetCurrentLogFilePath()
        {
            return MainLogFilePath;
        }

        /// <summary>
        /// Retorna o caminho do diretório de logs.
        /// </summary>
        public static string GetLogDirectory()
        {
            return LogDirectory;
        }

        /// <summary>
        /// Retorna os contadores da sessão atual.
        /// </summary>
        public static string GetSessionStats()
        {
            if (!_sessionActive) return "Nenhuma sessao ativa";
            var elapsed = DateTime.Now - _sessionStartTime;
            return $"INFO={_infoCount} WARN={_warnCount} ERROR={_errorCount} ({elapsed.TotalSeconds:F1}s)";
        }

        // ════════════════════════════════════════════════
        //  ESCRITA NO ARQUIVO
        // ════════════════════════════════════════════════

        /// <summary>
        /// Escreve uma linha no arquivo de log.
        /// Thread-safe. Grava tanto no arquivo principal quanto no diário.
        /// </summary>
        private static void WriteLog(LogLevel level, string message)
        {
            // Verifica nível mínimo
            if (level < MinimumLevel) return;

            try
            {
                string levelTag = level.ToString().ToUpper().PadRight(5);
                string sessionTag = _sessionActive ? _currentSessionId : "---";
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{levelTag}] [{sessionTag}] {message}";

                lock (LockObj)
                {
                    // Grava no arquivo principal (persistente)
                    File.AppendAllText(MainLogFilePath, logEntry + Environment.NewLine);

                    // Grava no arquivo diário (para histórico)
                    File.AppendAllText(DailyLogFilePath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Falha silenciosa para não interromper a execução do plugin.
                // Em produção, não há muito o que fazer se o log falhar.
            }
        }
    }

    // ════════════════════════════════════════════════
    //  TIPOS AUXILIARES
    // ════════════════════════════════════════════════

    /// <summary>
    /// Níveis de log suportados (do menos ao mais severo).
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    /// <summary>
    /// Medição de tempo de execução usando o padrão IDisposable.
    /// Ao ser descartada (saindo do using), registra automaticamente
    /// o tempo decorrido no log.
    /// </summary>
    public class TimeMeasurement : IDisposable
    {
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        public TimeMeasurement(string operationName)
        {
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
            Logger.Debug($"[TIMER] Iniciando: {operationName}");
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            Logger.Info($"[TIMER] {_operationName} concluido em {_stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
