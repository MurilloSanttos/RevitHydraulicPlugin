using RevitHydraulicPlugin.TestEnvironment.Mocks;
using RevitHydraulicPlugin.TestEnvironment.Services;
using System.Collections.Generic;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// TESTE — Classificação de Equipamentos Hidráulicos
    /// 
    /// Valida FixtureClassifierTestService com:
    /// - Nomes em português e inglês
    /// - Variações e abreviações
    /// - Classificação por conectores
    /// - Fixtures desconhecidos
    /// </summary>
    public class FixtureClassifierTests : BaseTest
    {
        public override string TestName => "Classificacao de Equipamentos";
        public override string Description => "Valida FixtureClassifier com 3 camadas";

        private readonly FixtureClassifierTestService _classifier =
            new FixtureClassifierTestService();

        public override bool Run()
        {
            PrintHeader();

            Test_ShouldClassifyToilet_FromPortugueseName();
            Test_ShouldClassifyToilet_FromEnglishName();
            Test_ShouldClassifySink();
            Test_ShouldClassifyKitchenSink();
            Test_ShouldClassifyShower();
            Test_ShouldClassifyLaundrySink();
            Test_ShouldClassifyDrain();
            Test_ShouldReturnUnknown_WhenNoEvidence();
            Test_ShouldUseConnectorEvidence_WhenNameIsGeneric();
            Test_ConfidenceShouldBeHighForKnownNames();

            PrintFooter();
            return FailCount == 0;
        }

        // ═══════ TOILET ═══════

        private void Test_ShouldClassifyToilet_FromPortugueseName()
        {
            PrintSection("FixtureClassifier: Toilet (PT)");

            var names = new[]
            {
                "Vaso Sanitário com Caixa Acoplada",
                "Vaso Sanitario Padrao",
                "Bacia Sanitária Deca"
            };

            foreach (var name in names)
            {
                var result = _classifier.ClassifyByName(name, "");
                AssertEqual(FixtureType.Toilet, result.Type,
                    $"'{name}' -> Toilet");
            }
        }

        private void Test_ShouldClassifyToilet_FromEnglishName()
        {
            PrintSection("FixtureClassifier: Toilet (EN)");

            var result = _classifier.ClassifyByName("Toilet", "Standard");
            AssertEqual(FixtureType.Toilet, result.Type,
                "'Toilet Standard' -> Toilet");

            var result2 = _classifier.ClassifyByName("WC", "Wall Mounted");
            AssertEqual(FixtureType.Toilet, result2.Type,
                "'WC Wall Mounted' -> Toilet");
        }

        // ═══════ SINK ═══════

        private void Test_ShouldClassifySink()
        {
            PrintSection("FixtureClassifier: Sink");

            var names = new[]
            {
                ("Lavatório de Louça", "Padrão"),
                ("Lavatorio Suspenso", "40cm"),
                ("Sink", "Wall Mount"),
                ("Basin", "Standard")
            };

            foreach (var (family, type) in names)
            {
                var result = _classifier.ClassifyByName(family, type);
                AssertEqual(FixtureType.Sink, result.Type,
                    $"'{family}' -> Sink");
            }
        }

        // ═══════ KITCHEN SINK ═══════

        private void Test_ShouldClassifyKitchenSink()
        {
            PrintSection("FixtureClassifier: KitchenSink");

            var names = new[]
            {
                "Pia de Cozinha Inox",
                "Pia Cozinha 1.20m",
                "Pia Inox"
            };

            foreach (var name in names)
            {
                var result = _classifier.ClassifyByName(name, "");
                AssertEqual(FixtureType.KitchenSink, result.Type,
                    $"'{name}' -> KitchenSink");
            }
        }

        // ═══════ SHOWER ═══════

        private void Test_ShouldClassifyShower()
        {
            PrintSection("FixtureClassifier: Shower");

            var names = new[]
            {
                ("Chuveiro Elétrico", "220V"),
                ("Shower Head", "Chrome"),
                ("Ducha Higiênica", "Standard")
            };

            foreach (var (family, type) in names)
            {
                var result = _classifier.ClassifyByName(family, type);
                AssertEqual(FixtureType.Shower, result.Type,
                    $"'{family}' -> Shower");
            }
        }

        // ═══════ LAUNDRY SINK ═══════

        private void Test_ShouldClassifyLaundrySink()
        {
            PrintSection("FixtureClassifier: LaundrySink");

            var names = new[]
            {
                "Tanque de Lavar Roupa",
                "Tanque Concreto"
            };

            foreach (var name in names)
            {
                var result = _classifier.ClassifyByName(name, "");
                AssertEqual(FixtureType.LaundrySink, result.Type,
                    $"'{name}' -> LaundrySink");
            }
        }

        // ═══════ DRAIN ═══════

        private void Test_ShouldClassifyDrain()
        {
            PrintSection("FixtureClassifier: Drain");

            var names = new[]
            {
                "Ralo Seco",
                "Ralo Sifonado",
                "Floor Drain"
            };

            foreach (var name in names)
            {
                var result = _classifier.ClassifyByName(name, "");
                AssertEqual(FixtureType.Drain, result.Type,
                    $"'{name}' -> Drain");
            }
        }

        // ═══════ UNKNOWN ═══════

        private void Test_ShouldReturnUnknown_WhenNoEvidence()
        {
            PrintSection("FixtureClassifier: Unknown");

            var fixture = new MockFixture
            {
                Id = 999,
                FamilyName = "Generic Equipment XYZ",
                TypeName = "Model A",
                Category = "Mechanical",
                Connectors = new List<MockConnector>()
            };

            var result = _classifier.Classify(fixture);
            AssertEqual(FixtureType.Unknown, result.Type,
                "'Generic Equipment XYZ' sem conectores -> Unknown");
        }

        // ═══════ CONECTORES ═══════

        private void Test_ShouldUseConnectorEvidence_WhenNameIsGeneric()
        {
            PrintSection("FixtureClassifier: Evidencia por Conectores");

            // Nome genérico, mas com conector esgoto DN100 → Toilet
            var fixture = new MockFixture
            {
                Id = 998,
                FamilyName = "Aparelho Generico",
                TypeName = "Modelo X",
                Category = "Plumbing Fixtures",
                Connectors = new List<MockConnector>
                {
                    new MockConnector
                    {
                        Position = new Point3D(0, 0, 0),
                        Direction = new Point3D(0, -1, 0),
                        SystemType = ConnectorSystemType.Sewer,
                        DiameterMm = 100
                    },
                    new MockConnector
                    {
                        Position = new Point3D(0, 0, 200),
                        Direction = new Point3D(-1, 0, 0),
                        SystemType = ConnectorSystemType.ColdWater,
                        DiameterMm = 25
                    }
                }
            };

            var result = _classifier.Classify(fixture);
            AssertEqual(FixtureType.Toilet, result.Type,
                "Nome generico + conector DN100 esgoto -> Toilet");
            AssertTrue(result.Method.Contains("Connector"),
                "Metodo indica uso de conectores");
        }

        // ═══════ CONFIANÇA ═══════

        private void Test_ConfidenceShouldBeHighForKnownNames()
        {
            PrintSection("FixtureClassifier: Confianca");

            var result = _classifier.ClassifyByName("Vaso Sanitário", "Caixa Acoplada");
            AssertTrue(result.Confidence >= 0.7,
                $"Confianca alta para nome conhecido ({result.Confidence:P0})");

            var unknown = _classifier.ClassifyByName("Equipamento XYZ", "");
            AssertTrue(unknown.Confidence == 0,
                $"Confianca zero para desconhecido ({unknown.Confidence:P0})");
        }
    }
}
