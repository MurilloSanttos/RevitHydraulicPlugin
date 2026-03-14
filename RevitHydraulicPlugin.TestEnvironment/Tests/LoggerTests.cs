using RevitHydraulicPlugin.TestEnvironment.Mocks;
using System;
using System.IO;
using System.Linq;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// TESTE — Sistema de Logs
    /// 
    /// Valida:
    /// - Criação de diretório e arquivo
    /// - Formato correto das entradas
    /// - Níveis INFO, WARNING, ERROR
    /// - Múltiplas entradas
    /// - Mock target
    /// - Tratamento de falhas de escrita
    /// - Sessões de log
    /// </summary>
    public class LoggerTests : BaseTest
    {
        public override string TestName => "Sistema de Logs";
        public override string Description => "Valida logger com sessoes, formato e mock target";

        public override bool Run()
        {
            PrintHeader();

            Test_ShouldCreateDirectory_WhenNotExists();
            Test_ShouldCreateLogFile_WhenFirstEntryIsWritten();
            Test_ShouldWriteInfoLog_InExpectedFormat();
            Test_ShouldWriteWarningLog_InExpectedFormat();
            Test_ShouldWriteErrorLog_InExpectedFormat();
            Test_ShouldAppendMultipleEntries();
            Test_ShouldUseMockTarget_WhenRunningUnitTests();
            Test_ShouldHandleWriteFailures_WithoutBreakingMainFlow();
            Test_ShouldTrackSessionCounters();

            PrintFooter();
            return FailCount == 0;
        }

        // ═══════ TESTES INDIVIDUAIS ═══════

        private void Test_ShouldCreateDirectory_WhenNotExists()
        {
            PrintSection("LoggerService_ShouldCreateDirectory");

            string tempDir = Path.Combine(Path.GetTempPath(),
                $"HydraulicTest_{Guid.NewGuid():N}");

            try
            {
                var logger = new TestLoggerService();
                logger.Initialize(tempDir);

                AssertTrue(Directory.Exists(tempDir),
                    "Diretorio de log criado automaticamente");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        private void Test_ShouldCreateLogFile_WhenFirstEntryIsWritten()
        {
            PrintSection("LoggerService_ShouldCreateLogFile");

            string tempDir = Path.Combine(Path.GetTempPath(),
                $"HydraulicTest_{Guid.NewGuid():N}");

            try
            {
                var logger = new TestLoggerService();
                logger.Initialize(tempDir);
                logger.Info("Primeira entrada de teste");

                AssertTrue(File.Exists(logger.LogFilePath),
                    "Arquivo de log criado apos primeira entrada");

                string content = File.ReadAllText(logger.LogFilePath);
                AssertTrue(content.Contains("Primeira entrada de teste"),
                    "Conteudo gravado corretamente no arquivo");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        private void Test_ShouldWriteInfoLog_InExpectedFormat()
        {
            PrintSection("LoggerService_ShouldWriteInfoLog_InExpectedFormat");

            var logger = new TestLoggerService();
            logger.Info("Plugin execution started");

            var lastLine = logger.LogLines.Last();
            AssertTrue(TestLoggerService.IsValidFormat(lastLine),
                "Formato: [YYYY-MM-DD HH:mm:ss] [INFO] mensagem");
            AssertTrue(lastLine.Contains("[INFO]"),
                "Nivel INFO presente na linha");
            AssertTrue(lastLine.Contains("Plugin execution started"),
                "Mensagem presente na linha");

            PrintInfo($"Exemplo: {lastLine}");
        }

        private void Test_ShouldWriteWarningLog_InExpectedFormat()
        {
            PrintSection("LoggerService_ShouldWriteWarningLog_InExpectedFormat");

            var logger = new TestLoggerService();
            logger.Warning("Room sem fixtures detectados");

            var lastLine = logger.LogLines.Last();
            AssertTrue(TestLoggerService.IsValidFormat(lastLine),
                "Formato valido para WARNING");
            AssertTrue(lastLine.Contains("[WARNING]"),
                "Nivel WARNING presente");

            PrintInfo($"Exemplo: {lastLine}");
        }

        private void Test_ShouldWriteErrorLog_InExpectedFormat()
        {
            PrintSection("LoggerService_ShouldWriteErrorLog_InExpectedFormat");

            var logger = new TestLoggerService();
            logger.Error("Failed to connect pipe");

            var lastLine = logger.LogLines.Last();
            AssertTrue(TestLoggerService.IsValidFormat(lastLine),
                "Formato valido para ERROR");
            AssertTrue(lastLine.Contains("[ERROR]"),
                "Nivel ERROR presente");
            AssertTrue(lastLine.Contains("Failed to connect pipe"),
                "Mensagem de erro presente");

            PrintInfo($"Exemplo: {lastLine}");
        }

        private void Test_ShouldAppendMultipleEntries()
        {
            PrintSection("LoggerService_ShouldAppendMultipleEntries");

            var logger = new TestLoggerService();
            logger.Info("Entrada 1");
            logger.Warning("Entrada 2");
            logger.Error("Entrada 3");
            logger.Info("Entrada 4");
            logger.Debug("Entrada 5");

            AssertEqual(5, logger.LogLines.Count,
                "5 entradas registradas");

            AssertTrue(logger.LogLines[0].Contains("[INFO]") && logger.LogLines[0].Contains("Entrada 1"),
                "Primeira entrada e INFO");
            AssertTrue(logger.LogLines[1].Contains("[WARNING]"),
                "Segunda entrada e WARNING");
            AssertTrue(logger.LogLines[2].Contains("[ERROR]"),
                "Terceira entrada e ERROR");
        }

        private void Test_ShouldUseMockTarget_WhenRunningUnitTests()
        {
            PrintSection("LoggerService_ShouldUseMockTarget");

            var mockTarget = new MockLoggerTarget();
            var logger = new TestLoggerService();
            logger.UseMockTarget(mockTarget);

            logger.Info("Test via mock");
            logger.Warning("Warning via mock");
            logger.Error("Error via mock");

            AssertEqual(3, mockTarget.Entries.Count,
                "Mock target recebeu 3 entradas");
            AssertEqual("INFO", mockTarget.Entries[0].Level,
                "Primeira entrada no mock e INFO");
            AssertEqual("WARNING", mockTarget.Entries[1].Level,
                "Segunda entrada no mock e WARNING");
            AssertEqual("ERROR", mockTarget.Entries[2].Level,
                "Terceira entrada no mock e ERROR");
            AssertTrue(mockTarget.Entries[0].Message == "Test via mock",
                "Mensagem preservada no mock");
        }

        private void Test_ShouldHandleWriteFailures_WithoutBreakingMainFlow()
        {
            PrintSection("LoggerService_ShouldHandleWriteFailures");

            var mockTarget = new MockLoggerTarget { HasWriteFailure = true };
            var logger = new TestLoggerService();
            logger.UseMockTarget(mockTarget);

            bool noException = true;
            try
            {
                logger.Info("Esta entrada deve falhar silenciosamente");
                logger.Warning("Esta tambem");
                logger.Error("E esta");
            }
            catch
            {
                noException = false;
            }

            AssertTrue(noException,
                "Falha de escrita NAO deve gerar excecao no fluxo principal");
            AssertTrue(logger.LogLines.Count == 3,
                "Entradas em memoria preservadas mesmo com falha de escrita");
        }

        private void Test_ShouldTrackSessionCounters()
        {
            PrintSection("LoggerService_ShouldTrackSessionCounters");

            var logger = new TestLoggerService();
            logger.StartSession("Teste de Sessao");

            AssertTrue(logger.SessionActive, "Sessao ativa apos StartSession");

            logger.Info("Info 1");
            logger.Info("Info 2");
            logger.Warning("Aviso 1");
            logger.Error("Erro 1");

            AssertEqual(3, logger.InfoCount, "3 INFOs (2 + StartSession)");
            AssertEqual(1, logger.WarningCount, "1 WARNING");
            AssertEqual(1, logger.ErrorCount, "1 ERROR");

            var summary = logger.EndSession();

            AssertTrue(!logger.SessionActive, "Sessao inativa apos EndSession");
            AssertTrue(summary.Contains("Teste de Sessao"),
                "Resumo contem nome da sessao");
            AssertTrue(summary.Contains("WARN: 1"),
                "Resumo contem contagem de warnings");
        }
    }
}
