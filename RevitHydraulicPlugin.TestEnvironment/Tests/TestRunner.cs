using System;
using System.Collections.Generic;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// Executa todos os testes sequencialmente e gera um relatório consolidado.
    /// 
    /// VERSÃO 2.0 — Inclui testes de:
    /// - Logger (sessões, formato, mock target)
    /// - FixtureClassifier (3 camadas, PT/EN)
    /// - RoomClassifier (multi-critério, suíte/PCD)
    /// - HydraulicRules (PipeRuleProvider, FixtureType)
    /// - BranchRouting (ortogonal, regras)
    /// - Integração (pipeline completo)
    /// </summary>
    public static class TestRunner
    {
        /// <summary>
        /// Executa todos os testes e retorna true se todos passaram.
        /// </summary>
        public static bool RunAll()
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("  ██████╗ ███████╗██╗   ██╗██╗████████╗");
            Console.WriteLine("  ██╔══██╗██╔════╝██║   ██║██║╚══██╔══╝");
            Console.WriteLine("  ██████╔╝█████╗  ██║   ██║██║   ██║   ");
            Console.WriteLine("  ██╔══██╗██╔══╝  ╚██╗ ██╔╝██║   ██║   ");
            Console.WriteLine("  ██║  ██║███████╗ ╚████╔╝ ██║   ██║   ");
            Console.WriteLine("  ╚═╝  ╚═╝╚══════╝  ╚═══╝  ╚═╝   ╚═╝   ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  HYDRAULIC PLUGIN — TEST ENVIRONMENT v2.0");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ─────────────────────────────────────────");
            Console.WriteLine($"  Executando em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            Console.ResetColor();

            // ── Define lista de testes (ordem de execução) ──
            var tests = new List<BaseTest>
            {
                // Parte 1: Logger
                new LoggerTests(),

                // Parte 2: Classificação de Equipamentos
                new FixtureClassifierTests(),

                // Parte 3: Classificação de Ambientes
                new RoomClassifierTests(),

                // Parte 4: Regras Hidráulicas
                new HydraulicRulesTests(),

                // Parte 5: Geração de Ramais
                new BranchRoutingTests(),

                // Parte 6: Integração
                new IntegrationTests()
            };

            int passed = 0;
            int failed = 0;
            var failedNames = new List<string>();
            var testResults = new List<(string name, bool success)>();

            // ── Executa cada teste ──
            foreach (var test in tests)
            {
                bool success = false;
                try
                {
                    success = test.Run();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ EXCECAO em {test.TestName}: {ex.Message}");
                    Console.ResetColor();
                }

                testResults.Add((test.TestName, success));

                if (success)
                {
                    passed++;
                }
                else
                {
                    failed++;
                    failedNames.Add(test.TestName);
                }
            }

            // ── Relatório consolidado ──
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  ╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║              RELATÓRIO CONSOLIDADO v2.0                     ║");
            Console.WriteLine("  ╠══════════════════════════════════════════════════════════════╣");

            // Lista de testes com status
            foreach (var (name, success) in testResults)
            {
                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ║  ✓ {name,-56}║");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ║  ✗ {name,-56}║");
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  ╠══════════════════════════════════════════════════════════════╣");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"  ║  Total de suites: {tests.Count,-42}║");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ║  ✓ Passaram:      {passed,-42}║");

            if (failed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ║  ✗ Falharam:      {failed,-42}║");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ║  ✗ Falharam:      {failed,-42}║");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  ╠══════════════════════════════════════════════════════════════╣");

            if (failed == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ║        ✓ TODOS OS TESTES PASSARAM COM SUCESSO!             ║");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ║        ✗ ALGUNS TESTES FALHARAM!                            ║");

                foreach (var name in failedNames)
                {
                    Console.WriteLine($"  ║    → {name,-55}║");
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  ╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            return failed == 0;
        }
    }
}
