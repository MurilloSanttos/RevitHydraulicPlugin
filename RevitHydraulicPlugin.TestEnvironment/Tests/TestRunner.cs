using System;
using System.Collections.Generic;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// Executa todos os testes sequencialmente e gera um relatório consolidado.
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
            Console.WriteLine("  HYDRAULIC PLUGIN — TEST ENVIRONMENT");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ─────────────────────────────────────");
            Console.WriteLine($"  Executando em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            Console.ResetColor();

            // Define lista de testes
            var tests = new List<BaseTest>
            {
                new RoomDetectionTests(),
                new EquipmentDetectionTests(),
                new HydraulicRulesTests(),
                new BranchRoutingTests()
            };

            int passed = 0;
            int failed = 0;
            var failedNames = new List<string>();

            // Executa cada teste
            foreach (var test in tests)
            {
                bool success = test.Run();
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

            // Relatório consolidado
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  ╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║                  RELATÓRIO CONSOLIDADO                      ║");
            Console.WriteLine("  ╠══════════════════════════════════════════════════════════════╣");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"  ║  Total de testes: {tests.Count,-42}║");

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
                Console.WriteLine("  ║          ✓ TODOS OS TESTES PASSARAM COM SUCESSO!           ║");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ║          ✗ ALGUNS TESTES FALHARAM!                         ║");

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
