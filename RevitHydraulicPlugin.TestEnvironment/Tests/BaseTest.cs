using System;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// Classe base para testes com métodos auxiliares de formatação
    /// e verificação de resultados (assert).
    /// </summary>
    public abstract class BaseTest
    {
        /// <summary>
        /// Nome do teste para exibição no console.
        /// </summary>
        public abstract string TestName { get; }

        /// <summary>
        /// Descrição do que o teste valida.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Contador de asserções passadas e falhadas.
        /// </summary>
        protected int PassCount { get; private set; }
        protected int FailCount { get; private set; }

        /// <summary>
        /// Executa o teste e retorna true se todas as asserções passaram.
        /// </summary>
        public abstract bool Run();

        /// <summary>
        /// Imprime o cabeçalho do teste no console.
        /// </summary>
        protected void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  TESTE: {TestName,-52}║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"║  {Description,-58}║");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            PassCount = 0;
            FailCount = 0;
        }

        /// <summary>
        /// Imprime o rodapé do teste com contadores.
        /// </summary>
        protected void PrintFooter()
        {
            Console.WriteLine();
            Console.Write("  Resultado: ");

            if (FailCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ PASSOU ({PassCount}/{PassCount + FailCount} asserções)");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ FALHOU ({PassCount} passaram, {FailCount} falharam)");
            }

            Console.ResetColor();
            Console.WriteLine(new string('─', 64));
        }

        /// <summary>
        /// Verifica se uma condição é verdadeira.
        /// </summary>
        protected void AssertTrue(bool condition, string description)
        {
            if (condition)
            {
                PassCount++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    ✓ {description}");
            }
            else
            {
                FailCount++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"    ✗ FALHOU: {description}");
            }
            Console.ResetColor();
        }

        /// <summary>
        /// Verifica se dois valores são iguais.
        /// </summary>
        protected void AssertEqual<T>(T expected, T actual, string description)
        {
            bool passed = expected.Equals(actual);
            if (passed)
            {
                PassCount++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    ✓ {description} (= {actual})");
            }
            else
            {
                FailCount++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"    ✗ FALHOU: {description} — Esperado: {expected}, Obtido: {actual}");
            }
            Console.ResetColor();
        }

        /// <summary>
        /// Verifica se dois doubles são iguais com tolerância.
        /// </summary>
        protected void AssertApprox(double expected, double actual, string description,
            double tolerance = 0.01)
        {
            bool passed = Math.Abs(expected - actual) < tolerance;
            if (passed)
            {
                PassCount++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    ✓ {description} (= {actual:F2})");
            }
            else
            {
                FailCount++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"    ✗ FALHOU: {description} — Esperado: {expected:F2}, Obtido: {actual:F2}");
            }
            Console.ResetColor();
        }

        /// <summary>
        /// Imprime uma linha informativa (sem asserção).
        /// </summary>
        protected void PrintInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"    ℹ {message}");
            Console.ResetColor();
        }

        /// <summary>
        /// Imprime um subtítulo de seção dentro do teste.
        /// </summary>
        protected void PrintSection(string title)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine($"  ▸ {title}");
            Console.ResetColor();
        }
    }
}
