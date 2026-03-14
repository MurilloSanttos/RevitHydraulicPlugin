using RevitHydraulicPlugin.TestEnvironment.Mocks;
using RevitHydraulicPlugin.TestEnvironment.Services;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// TESTE — Classificação de Ambientes Hidráulicos (v2.0)
    /// 
    /// Valida RoomClassifierTestService com:
    /// - Nomes em PT e EN
    /// - Tipos específicos (Suíte, PCD, Copa)
    /// - Classificação combinada (nome + fixtures)
    /// - Exclusão de ambientes não-hidráulicos
    /// </summary>
    public class RoomClassifierTests : BaseTest
    {
        public override string TestName => "Classificacao de Ambientes v2.0";
        public override string Description => "Valida RoomClassifier multi-criterio";

        private readonly RoomClassifierTestService _classifier =
            new RoomClassifierTestService();

        public override bool Run()
        {
            PrintHeader();

            Test_ShouldClassifyBathroom_FromName();
            Test_ShouldClassifyBathroom_FromAbbreviation();
            Test_ShouldClassifyLavatory();
            Test_ShouldClassifySuiteBathroom();
            Test_ShouldClassifyAccessibleBathroom();
            Test_ShouldClassifyKitchen_WhenSinkExists();
            Test_ShouldClassifyPantry_WhenPantryNameAndSinkExist();
            Test_ShouldClassifyLaundry_WhenLaundrySinkExists();
            Test_ShouldClassifyServiceArea_WhenServiceEvidenceExists();
            Test_ShouldReturnUnknown_ForNonHydraulicRoom();
            Test_ShouldPreferFixtureEvidence_WhenNameIsGeneric();
            Test_ShouldHandleEnglishNames();
            Test_FullProject_DetectionFlow();

            PrintFooter();
            return FailCount == 0;
        }

        // ═══════ BANHEIRO ═══════

        private void Test_ShouldClassifyBathroom_FromName()
        {
            PrintSection("RoomClassifier: Banheiro por nome");

            var names = new[] { "Banheiro", "Banheiro 01", "Banheiro Social" };

            foreach (var name in names)
            {
                var result = _classifier.ClassifyByName(name);
                AssertEqual(RoomType.Bathroom, result.Type,
                    $"'{name}' -> Bathroom");
            }
        }

        private void Test_ShouldClassifyBathroom_FromAbbreviation()
        {
            PrintSection("RoomClassifier: Banheiro por abreviacao");

            var abbreviations = new[] { "WC", "BWC", "Sanitário" };

            foreach (var name in abbreviations)
            {
                var result = _classifier.ClassifyByName(name);
                AssertEqual(RoomType.Bathroom, result.Type,
                    $"'{name}' -> Bathroom");
            }
        }

        // ═══════ LAVABO ═══════

        private void Test_ShouldClassifyLavatory()
        {
            PrintSection("RoomClassifier: Lavabo");

            var names = new[] { "Lavabo", "Powder Room", "Half Bath" };

            foreach (var name in names)
            {
                var result = _classifier.ClassifyByName(name);
                AssertEqual(RoomType.Lavatory, result.Type,
                    $"'{name}' -> Lavatory");
            }
        }

        // ═══════ BANHEIRO SUÍTE ═══════

        private void Test_ShouldClassifySuiteBathroom()
        {
            PrintSection("RoomClassifier: Banheiro Suite");

            var names = new[] { "Banheiro Suíte", "Banheiro Suite", "WC Suíte", "Master Bath" };

            foreach (var name in names)
            {
                var result = _classifier.ClassifyByName(name);
                AssertEqual(RoomType.SuiteBathroom, result.Type,
                    $"'{name}' -> SuiteBathroom");
            }
        }

        // ═══════ BANHEIRO PCD ═══════

        private void Test_ShouldClassifyAccessibleBathroom()
        {
            PrintSection("RoomClassifier: Banheiro PCD");

            var names = new[] { "Banheiro PCD", "Banheiro Acessível", "WC PNE" };

            foreach (var name in names)
            {
                var result = _classifier.ClassifyByName(name);
                AssertEqual(RoomType.AccessibleBathroom, result.Type,
                    $"'{name}' -> AccessibleBathroom");
            }
        }

        // ═══════ COZINHA COM FIXTURES ═══════

        private void Test_ShouldClassifyKitchen_WhenSinkExists()
        {
            PrintSection("RoomClassifier: Cozinha + KitchenSink");

            var fixtures = new List<FixtureType> { FixtureType.KitchenSink };
            var result = _classifier.ClassifyWithFixtures("Cozinha", fixtures);

            AssertEqual(RoomType.Kitchen, result.Type,
                "Cozinha + KitchenSink -> Kitchen");
            AssertTrue(result.Confidence > 0.8,
                $"Confianca alta ({result.Confidence:P0})");
        }

        // ═══════ COPA ═══════

        private void Test_ShouldClassifyPantry_WhenPantryNameAndSinkExist()
        {
            PrintSection("RoomClassifier: Copa + Sink");

            // Copa por nome
            var nameResult = _classifier.ClassifyByName("Copa");
            AssertEqual(RoomType.Pantry, nameResult.Type,
                "'Copa' -> Pantry");

            // Copa + sink
            var fixtures = new List<FixtureType> { FixtureType.Sink };
            var combined = _classifier.ClassifyWithFixtures("Copa", fixtures);
            AssertEqual(RoomType.Pantry, combined.Type,
                "Copa + Sink -> Pantry");
        }

        // ═══════ LAVANDERIA ═══════

        private void Test_ShouldClassifyLaundry_WhenLaundrySinkExists()
        {
            PrintSection("RoomClassifier: Lavanderia + LaundrySink");

            var fixtures = new List<FixtureType> { FixtureType.LaundrySink };
            var result = _classifier.ClassifyWithFixtures("Lavanderia", fixtures);

            AssertEqual(RoomType.Laundry, result.Type,
                "Lavanderia + LaundrySink -> Laundry");
        }

        // ═══════ ÁREA DE SERVIÇO ═══════

        private void Test_ShouldClassifyServiceArea_WhenServiceEvidenceExists()
        {
            PrintSection("RoomClassifier: Area de Servico");

            var names = new[] { "Área de Serviço", "Area de Servico", "Service Area" };

            foreach (var name in names)
            {
                var result = _classifier.ClassifyByName(name);
                AssertEqual(RoomType.ServiceArea, result.Type,
                    $"'{name}' -> ServiceArea");
            }
        }

        // ═══════ NÃO HIDRÁULICO ═══════

        private void Test_ShouldReturnUnknown_ForNonHydraulicRoom()
        {
            PrintSection("RoomClassifier: Nao-hidraulicos");

            var names = new[] { "Sala", "Quarto", "Corredor", "Escritório", "Varanda" };

            foreach (var name in names)
            {
                var result = _classifier.ClassifyByName(name);
                AssertEqual(RoomType.Unknown, result.Type,
                    $"'{name}' -> Unknown");
                AssertTrue(!result.IsHydraulic,
                    $"'{name}' NAO e hidraulico");
            }
        }

        // ═══════ FIXTURES PROMOVEM PARA HIDRÁULICO ═══════

        private void Test_ShouldPreferFixtureEvidence_WhenNameIsGeneric()
        {
            PrintSection("RoomClassifier: Nome generico + fixtures promove");

            // "Sala" normalmente é Unknown, mas se tem Toilet + Sink → Bathroom
            var fixtures = new List<FixtureType>
            {
                FixtureType.Toilet,
                FixtureType.Sink
            };

            var result = _classifier.ClassifyWithFixtures("Sala", fixtures);
            AssertTrue(result.IsHydraulic,
                "'Sala' com Toilet+Sink deve ser promovida");
            AssertEqual(RoomType.Bathroom, result.Type,
                "'Sala' + Toilet+Sink -> Bathroom");
            AssertTrue(result.Method == "Fixture",
                "Metodo Fixture usado para classificacao");
        }

        // ═══════ NOMES EM INGLÊS ═══════

        private void Test_ShouldHandleEnglishNames()
        {
            PrintSection("RoomClassifier: Nomes em ingles");

            var cases = new (string name, RoomType expected)[]
            {
                ("Bathroom", RoomType.Bathroom),
                ("Kitchen", RoomType.Kitchen),
                ("Laundry", RoomType.Laundry),
                ("Pantry", RoomType.Pantry),
                ("Ensuite", RoomType.SuiteBathroom)
            };

            foreach (var (name, expected) in cases)
            {
                var result = _classifier.ClassifyByName(name);
                AssertEqual(expected, result.Type,
                    $"'{name}' -> {expected}");
            }
        }

        // ═══════ FLUXO COMPLETO COM PROJETO ═══════

        private void Test_FullProject_DetectionFlow()
        {
            PrintSection("RoomClassifier: Fluxo completo com MockProject");

            var service = new RoomDetectionTestService();
            var level = new MockLevel { Id = 1, Name = "Terreo", ElevationMm = 0 };

            var project = new MockProject
            {
                ProjectName = "Teste Classificacao v2",
                Levels = new List<MockLevel> { level },
                Rooms = new List<MockRoom>
                {
                    CreateRoom(1, "Banheiro", level),
                    CreateRoom(2, "Sala", level),
                    CreateRoom(3, "Cozinha", level),
                    CreateRoom(4, "Quarto", level),
                    CreateRoom(5, "Lavanderia", level),
                    CreateRoom(6, "WC", level),
                    CreateRoom(7, "Lavabo", level),
                    CreateRoom(8, "Área de Serviço", level),
                    CreateRoom(9, "Banheiro Suíte", level),
                    CreateRoom(10, "Banheiro PCD", level),
                    CreateRoom(11, "Copa", level),
                    CreateRoom(12, "Corredor", level)
                }
            };

            var results = service.DetectRooms(project);
            var hydraulic = results.Where(r => r.IsHydraulic).ToList();
            var nonHydraulic = results.Where(r => !r.IsHydraulic).ToList();

            PrintInfo($"Hidraulicos: {hydraulic.Count}, Nao-hidraulicos: {nonHydraulic.Count}");

            // 9 ambientes hidráulicos (Banheiro, Cozinha, Lavanderia, WC, Lavabo,
            //                          Area de Servico, Banheiro Suite, Banheiro PCD, Copa)
            AssertEqual(9, hydraulic.Count,
                "9 ambientes hidraulicos detectados");

            // 3 não hidráulicos (Sala, Quarto, Corredor)
            AssertEqual(3, nonHydraulic.Count,
                "3 ambientes nao-hidraulicos");

            // Verificar tipos específicos
            var suite = results.First(r => r.Room.Name == "Banheiro Suíte");
            AssertEqual(RoomType.SuiteBathroom, suite.Type,
                "Banheiro Suite classificado corretamente");

            var pcd = results.First(r => r.Room.Name == "Banheiro PCD");
            AssertEqual(RoomType.AccessibleBathroom, pcd.Type,
                "Banheiro PCD classificado corretamente");

            var copa = results.First(r => r.Room.Name == "Copa");
            AssertEqual(RoomType.Pantry, copa.Type,
                "Copa classificada como Pantry");
        }

        // ═══════ HELPER ═══════

        private MockRoom CreateRoom(int id, string name, MockLevel level)
        {
            return new MockRoom
            {
                Id = id,
                Name = name,
                Number = $"{id:D3}",
                AreaM2 = 10.0,
                Level = level,
                CenterPoint = new Point3D(id * 1000, 0, 0),
                BBoxMin = new Point3D(id * 1000 - 500, -500, 0),
                BBoxMax = new Point3D(id * 1000 + 500, 500, 3000)
            };
        }
    }
}
