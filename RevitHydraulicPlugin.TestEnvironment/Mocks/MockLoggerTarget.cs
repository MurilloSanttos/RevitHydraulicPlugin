using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace RevitHydraulicPlugin.TestEnvironment.Mocks
{
    /// <summary>
    /// Destino de log mockado para testes.
    /// Captura todas as entradas em memória para validação.
    /// </summary>
    public class MockLoggerTarget
    {
        public List<LogEntry> Entries { get; } = new List<LogEntry>();
        public bool HasWriteFailure { get; set; }
        public int FailAfterEntries { get; set; } = -1;

        public void Write(string level, string message)
        {
            // Simula falha de escrita
            if (HasWriteFailure || (FailAfterEntries >= 0 && Entries.Count >= FailAfterEntries))
                throw new IOException("Simulated write failure");

            Entries.Add(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                FormattedLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}"
            });
        }

        public void Clear() => Entries.Clear();
    }

    /// <summary>
    /// Entrada de log capturada.
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string FormattedLine { get; set; }
    }

    /// <summary>
    /// Serviço de logger para testes.
    /// Espelha a interface do Logger do plugin principal, mas grava em memória.
    /// Também suporta gravação em arquivo temporário para testar persistência.
    /// </summary>
    public class TestLoggerService
    {
        private readonly List<string> _logLines = new List<string>();
        private string _logFilePath;
        private string _logDirectory;
        private bool _sessionActive;
        private string _sessionName;
        private DateTime _sessionStart;
        private int _infoCount;
        private int _warningCount;
        private int _errorCount;

        private MockLoggerTarget _mockTarget;

        // ═══════ PROPRIEDADES ═══════

        public List<string> LogLines => _logLines;
        public string LogFilePath => _logFilePath;
        public string LogDirectory => _logDirectory;
        public bool SessionActive => _sessionActive;
        public int InfoCount => _infoCount;
        public int WarningCount => _warningCount;
        public int ErrorCount => _errorCount;
        public int TotalCount => _infoCount + _warningCount + _errorCount;

        // ═══════ CONFIGURAÇÃO DE MOCK ═══════

        /// <summary>
        /// Configura destino mockado (em memória).
        /// </summary>
        public void UseMockTarget(MockLoggerTarget target)
        {
            _mockTarget = target;
        }

        // ═══════ INICIALIZAÇÃO ═══════

        /// <summary>
        /// Inicializa o logger com diretório de saída.
        /// </summary>
        public bool Initialize(string logDirectory)
        {
            _logDirectory = logDirectory;

            try
            {
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                string fileName = $"HydraulicPlugin_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                _logFilePath = Path.Combine(logDirectory, fileName);
                return true;
            }
            catch (Exception ex)
            {
                // Fallback para TEMP
                _logDirectory = Path.GetTempPath();
                string fileName = $"HydraulicPlugin_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                _logFilePath = Path.Combine(_logDirectory, fileName);
                WriteToFile($"[WARN] Fallback para TEMP: {ex.Message}");
                return false;
            }
        }

        // ═══════ SESSÃO ═══════

        public void StartSession(string sessionName)
        {
            _sessionActive = true;
            _sessionName = sessionName;
            _sessionStart = DateTime.Now;
            _infoCount = 0;
            _warningCount = 0;
            _errorCount = 0;

            Info($"=== SESSAO INICIADA: {sessionName} ===");
        }

        public string EndSession()
        {
            if (!_sessionActive) return "";

            var elapsed = DateTime.Now - _sessionStart;
            var summary = $"=== SESSAO ENCERRADA: {_sessionName} | " +
                $"Tempo: {elapsed.TotalSeconds:F1}s | " +
                $"INFO: {_infoCount} | WARN: {_warningCount} | ERROR: {_errorCount} ===";

            Info(summary);
            _sessionActive = false;
            return summary;
        }

        // ═══════ MÉTODOS DE LOG ═══════

        public void Info(string message)
        {
            WriteLog("INFO", message);
            _infoCount++;
        }

        public void Warning(string message)
        {
            WriteLog("WARNING", message);
            _warningCount++;
        }

        public void Error(string message)
        {
            WriteLog("ERROR", message);
            _errorCount++;
        }

        public void Error(string message, Exception ex)
        {
            WriteLog("ERROR", $"{message}: {ex.Message}");
            _errorCount++;
        }

        public void Debug(string message)
        {
            WriteLog("DEBUG", message);
        }

        // ═══════ ESCRITA ═══════

        private void WriteLog(string level, string message)
        {
            string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            _logLines.Add(formatted);

            // Envia para mock target se definido
            if (_mockTarget != null)
            {
                try
                {
                    _mockTarget.Write(level, message);
                }
                catch
                {
                    // Falha de escrita não deve quebrar o fluxo principal
                }
                return;
            }

            // Grava em arquivo se inicializado
            WriteToFile(formatted);
        }

        private void WriteToFile(string line)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;

            try
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
            catch
            {
                // Falha de escrita silenciosa
            }
        }

        // ═══════ VALIDAÇÃO ═══════

        /// <summary>
        /// Verifica se uma linha de log está no formato esperado.
        /// Formato: [YYYY-MM-DD HH:mm:ss] [LEVEL] mensagem
        /// </summary>
        public static bool IsValidFormat(string line)
        {
            return Regex.IsMatch(line,
                @"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\] \[(INFO|WARNING|ERROR|DEBUG)\] .+$");
        }

        /// <summary>
        /// Retorna entradas filtradas por nível.
        /// </summary>
        public List<string> GetByLevel(string level)
        {
            return _logLines.FindAll(l => l.Contains($"[{level}]"));
        }
    }
}
