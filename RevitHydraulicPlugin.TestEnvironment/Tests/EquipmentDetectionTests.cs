using RevitHydraulicPlugin.TestEnvironment.Mocks;
using RevitHydraulicPlugin.TestEnvironment.Services;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// TESTE 2 — Identificação de Equipamentos Hidráulicos
    /// 
    /// Cenário:
    /// Cria um banheiro contendo vaso sanitário, lavatório e chuveiro.
    /// Verifica se o sistema reconhece corretamente cada tipo e seus conectores.
    /// </summary>
    public class EquipmentDetectionTests : BaseTest
    {
        public override string TestName => "Identificação de Equipamentos";
        public override string Description => "Valida reconhecimento e classificação de equipamentos";

        public override bool Run()
        {
            PrintHeader();

            var roomService = new RoomDetectionTestService();
            var equipService = new EquipmentDetectionTestService();
            var level = new MockLevel { Id = 1, Name = "1º Pavimento", ElevationMm = 3000 };

            // Monta um banheiro com 3 equipamentos
            var banheiro = new MockRoom
            {
                Id = 100, Name = "Banheiro", Number = "101",
                AreaM2 = 5.0, Level = level,
                CenterPoint = new Point3D(2000, 2000, 3000),
                BBoxMin = new Point3D(0, 0, 3000),
                BBoxMax = new Point3D(4000, 4000, 6000),
                Fixtures = new List<MockFixture>
                {
                    new MockFixture
                    {
                        Id = 201,
                        FamilyName = "Vaso Sanitário com Caixa Acoplada",
                        TypeName = "6 Litros",
                        Category = "Plumbing Fixtures",
                        Position = new Point3D(1000, 1000, 3000),
                        Level = level,
                        Connectors = new List<MockConnector>
                        {
                            new MockConnector
                            {
                                Position = new Point3D(1000, 800, 3000),
                                Direction = new Point3D(0, -1, 0),
                                SystemType = ConnectorSystemType.Sewer,
                                DiameterMm = 100
                            },
                            new MockConnector
                            {
                                Position = new Point3D(850, 1000, 3200),
                                Direction = new Point3D(-1, 0, 0),
                                SystemType = ConnectorSystemType.ColdWater,
                                DiameterMm = 25
                            }
                        }
                    },
                    new MockFixture
                    {
                        Id = 202,
                        FamilyName = "Lavatório de Louça",
                        TypeName = "Padrão",
                        Category = "Plumbing Fixtures",
                        Position = new Point3D(3000, 1000, 3850),
                        Level = level,
                        Connectors = new List<MockConnector>
                        {
                            new MockConnector
                            {
                                Position = new Point3D(3000, 1000, 3450),
                                Direction = new Point3D(0, 0, -1),
                                SystemType = ConnectorSystemType.Sewer,
                                DiameterMm = 50
                            },
                            new MockConnector
                            {
                                Position = new Point3D(3000, 900, 3850),
                                Direction = new Point3D(0, -1, 0),
                                SystemType = ConnectorSystemType.ColdWater,
                                DiameterMm = 25
                            }
                        }
                    },
                    new MockFixture
                    {
                        Id = 203,
                        FamilyName = "Chuveiro Elétrico",
                        TypeName = "220V",
                        Category = "Plumbing Fixtures",
                        Position = new Point3D(3000, 3000, 3000),
                        Level = level,
                        Connectors = new List<MockConnector>
                        {
                            new MockConnector
                            {
                                Position = new Point3D(3000, 3000, 2950),
                                Direction = new Point3D(0, 0, -1),
                                SystemType = ConnectorSystemType.Sewer,
                                DiameterMm = 50
                            },
                            new MockConnector
                            {
                                Position = new Point3D(3000, 3000, 5100),
                                Direction = new Point3D(0, 0, 1),
                                SystemType = ConnectorSystemType.ColdWater,
                                DiameterMm = 25
                            }
                        }
                    }
                }
            };

            // Sala sem equipamentos (controle)
            var sala = new MockRoom
            {
                Id = 101, Name = "Sala", Number = "102",
                AreaM2 = 20.0, Level = level,
                CenterPoint = new Point3D(8000, 2000, 3000),
                BBoxMin = new Point3D(6000, 0, 3000),
                BBoxMax = new Point3D(10000, 4000, 6000)
            };

            var project = new MockProject
            {
                ProjectName = "Teste Equipamentos",
                Levels = new List<MockLevel> { level },
                Rooms = new List<MockRoom> { banheiro, sala }
            };

            // Detecta ambientes primeiro
            var detectedRooms = roomService.DetectRooms(project);

            // Detecta equipamentos
            var equipment = equipService.DetectEquipment(detectedRooms);

            PrintSection("Equipamentos detectados:");
            foreach (var e in equipment)
            {
                PrintInfo($"{e.Type}: {e.Fixture.FamilyName} → Room: {e.Room.Name}");
                PrintInfo($"  Conectores: {e.Fixture.Connectors.Count}");
                foreach (var conn in e.Fixture.Connectors)
                {
                    PrintInfo($"    {conn}");
                }
            }

            // === ASSERÇÕES ===
            PrintSection("Verificando detecção:");

            AssertEqual(3, equipment.Count, "Total de equipamentos detectados");

            // Vaso sanitário
            var vaso = equipment.FirstOrDefault(e => e.Type == EquipmentType.VasoSanitario);
            AssertTrue(vaso != null, "Vaso sanitário detectado");
            AssertEqual(2, vaso.Fixture.Connectors.Count, "Vaso tem 2 conectores");
            AssertTrue(
                vaso.Fixture.Connectors.Any(c => c.SystemType == ConnectorSystemType.Sewer),
                "Vaso tem conector de esgoto");
            AssertTrue(
                vaso.Fixture.Connectors.Any(c => c.SystemType == ConnectorSystemType.ColdWater),
                "Vaso tem conector de água fria");

            // Lavatório
            var lavatorio = equipment.FirstOrDefault(e => e.Type == EquipmentType.Lavatorio);
            AssertTrue(lavatorio != null, "Lavatório detectado");
            AssertEqual(2, lavatorio.Fixture.Connectors.Count, "Lavatório tem 2 conectores");
            AssertTrue(
                lavatorio.Fixture.Connectors.Any(c => c.DiameterMm == 50),
                "Lavatório tem conector Ø50mm (esgoto)");

            // Chuveiro
            var chuveiro = equipment.FirstOrDefault(e => e.Type == EquipmentType.Chuveiro);
            AssertTrue(chuveiro != null, "Chuveiro detectado");
            AssertEqual(2, chuveiro.Fixture.Connectors.Count, "Chuveiro tem 2 conectores");

            PrintSection("Verificando que Sala não gerou equipamentos:");
            var salaDetected = detectedRooms.First(r => r.Room.Name == "Sala");
            AssertTrue(!salaDetected.IsHydraulic, "Sala não é hidráulica");
            AssertEqual(0, sala.Fixtures.Count, "Sala não tem equipamentos");

            PrintSection("Verificando classificação por keywords:");
            AssertEqual(EquipmentType.VasoSanitario,
                equipService.ClassifyEquipment("Vaso Sanitário com Caixa Acoplada", "6L"),
                "Keyword 'vaso' → VasoSanitario");
            AssertEqual(EquipmentType.Lavatorio,
                equipService.ClassifyEquipment("Lavatório de Louça", "Padrão"),
                "Keyword 'lavatório' → Lavatorio");
            AssertEqual(EquipmentType.Chuveiro,
                equipService.ClassifyEquipment("Chuveiro Elétrico", "220V"),
                "Keyword 'chuveiro' → Chuveiro");
            AssertEqual(EquipmentType.Pia,
                equipService.ClassifyEquipment("Pia de Cozinha", "Inox"),
                "Keyword 'pia' → Pia");
            AssertEqual(EquipmentType.Ralo,
                equipService.ClassifyEquipment("Ralo Seco", "100mm"),
                "Keyword 'ralo' → Ralo");
            AssertEqual(EquipmentType.Tanque,
                equipService.ClassifyEquipment("Tanque de Lavar", "Concreto"),
                "Keyword 'tanque' → Tanque");
            AssertEqual(EquipmentType.Outro,
                equipService.ClassifyEquipment("Elemento Genérico", "Tipo X"),
                "Keyword desconhecida → Outro");

            PrintFooter();
            return FailCount == 0;
        }
    }
}
