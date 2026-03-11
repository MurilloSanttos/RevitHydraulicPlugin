using RevitHydraulicPlugin.TestEnvironment.Mocks;
using RevitHydraulicPlugin.TestEnvironment.Services;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// TESTE 1 — Detecção de Ambientes Hidráulicos
    /// 
    /// Cenário:
    /// Cria um conjunto de MockRooms com nomes variados e verifica se o sistema
    /// classifica corretamente quais são hidráulicos e quais não são.
    /// 
    /// Resultado esperado:
    /// - Banheiro     → Hidráulico (RoomType.Banheiro)
    /// - Sala         → NÃO hidráulico
    /// - Cozinha      → Hidráulico (RoomType.Cozinha)
    /// - Quarto       → NÃO hidráulico
    /// - Lavanderia   → Hidráulico (RoomType.Lavanderia)
    /// - WC           → Hidráulico (RoomType.Banheiro)
    /// - Escritório   → NÃO hidráulico
    /// - Lavabo       → Hidráulico (RoomType.Lavabo)
    /// - Área de Serviço → Hidráulico (RoomType.AreaDeServico)
    /// </summary>
    public class RoomDetectionTests : BaseTest
    {
        public override string TestName => "Detecção de Ambientes Hidráulicos";
        public override string Description => "Valida classificação de Rooms por nome";

        public override bool Run()
        {
            PrintHeader();

            var service = new RoomDetectionTestService();
            var level = new MockLevel { Id = 1, Name = "Térreo", ElevationMm = 0 };

            // Cria projeto com ambientes de diversos tipos
            var project = new MockProject
            {
                ProjectName = "Teste Detecção",
                Levels = new List<MockLevel> { level },
                Rooms = new List<MockRoom>
                {
                    CreateRoom(1, "Banheiro", level),
                    CreateRoom(2, "Sala", level),
                    CreateRoom(3, "Cozinha", level),
                    CreateRoom(4, "Quarto", level),
                    CreateRoom(5, "Lavanderia", level),
                    CreateRoom(6, "WC", level),
                    CreateRoom(7, "Escritório", level),
                    CreateRoom(8, "Lavabo", level),
                    CreateRoom(9, "Área de Serviço", level),
                }
            };

            // Executa a detecção
            var results = service.DetectRooms(project);

            PrintSection("Resultado da classificação:");
            foreach (var r in results)
            {
                PrintInfo(r.ToString());
            }

            // === ASSERÇÕES ===
            PrintSection("Verificando ambientes HIDRÁULICOS:");
            var hydraulicRooms = results.Where(r => r.IsHydraulic).ToList();

            AssertEqual(6, hydraulicRooms.Count, "Total de ambientes hidráulicos detectados");

            // Banheiro
            var banheiro = results.First(r => r.Room.Name == "Banheiro");
            AssertTrue(banheiro.IsHydraulic, "Banheiro é hidráulico");
            AssertEqual(RoomType.Banheiro, banheiro.Type, "Banheiro classificado como Banheiro");

            // WC (variação de nome para banheiro)
            var wc = results.First(r => r.Room.Name == "WC");
            AssertTrue(wc.IsHydraulic, "WC é hidráulico");
            AssertEqual(RoomType.Banheiro, wc.Type, "WC classificado como Banheiro");

            // Cozinha
            var cozinha = results.First(r => r.Room.Name == "Cozinha");
            AssertTrue(cozinha.IsHydraulic, "Cozinha é hidráulica");
            AssertEqual(RoomType.Cozinha, cozinha.Type, "Cozinha classificada como Cozinha");

            // Lavanderia
            var lavanderia = results.First(r => r.Room.Name == "Lavanderia");
            AssertTrue(lavanderia.IsHydraulic, "Lavanderia é hidráulica");
            AssertEqual(RoomType.Lavanderia, lavanderia.Type, "Lavanderia classificada como Lavanderia");

            // Lavabo
            var lavabo = results.First(r => r.Room.Name == "Lavabo");
            AssertTrue(lavabo.IsHydraulic, "Lavabo é hidráulico");
            AssertEqual(RoomType.Lavabo, lavabo.Type, "Lavabo classificado como Lavabo");

            // Área de Serviço
            var areaServico = results.First(r => r.Room.Name == "Área de Serviço");
            AssertTrue(areaServico.IsHydraulic, "Área de Serviço é hidráulica");
            AssertEqual(RoomType.AreaDeServico, areaServico.Type,
                "Área de Serviço classificada como AreaDeServico");

            PrintSection("Verificando ambientes NÃO HIDRÁULICOS:");

            var sala = results.First(r => r.Room.Name == "Sala");
            AssertTrue(!sala.IsHydraulic, "Sala NÃO é hidráulica");

            var quarto = results.First(r => r.Room.Name == "Quarto");
            AssertTrue(!quarto.IsHydraulic, "Quarto NÃO é hidráulico");

            var escritorio = results.First(r => r.Room.Name == "Escritório");
            AssertTrue(!escritorio.IsHydraulic, "Escritório NÃO é hidráulico");

            PrintFooter();
            return FailCount == 0;
        }

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
