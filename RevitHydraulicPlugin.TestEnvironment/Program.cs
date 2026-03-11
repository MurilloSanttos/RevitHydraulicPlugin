using RevitHydraulicPlugin.TestEnvironment.Tests;
using System;

namespace RevitHydraulicPlugin.TestEnvironment
{
    /// <summary>
    /// Ponto de entrada do ambiente de testes do RevitHydraulicPlugin.
    /// 
    /// Este programa executa todos os testes de lógica do plugin
    /// SEM depender da Autodesk Revit API.
    /// 
    /// Uso:
    ///   1. Abra o projeto no Visual Studio
    ///   2. Defina RevitHydraulicPlugin.TestEnvironment como Startup Project
    ///   3. Pressione F5 (Debug) ou Ctrl+F5 (sem debug)
    ///   4. Observe os resultados no console
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                bool allPassed = TestRunner.RunAll();

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  Pressione qualquer tecla para sair...");
                Console.ResetColor();
                Console.ReadKey(true);

                return allPassed ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine("  ═══ ERRO FATAL ═══");
                Console.WriteLine($"  {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"  {ex.StackTrace}");
                Console.ResetColor();

                Console.ReadKey(true);
                return 2;
            }
        }
    }
}
