using RevitHydraulicPlugin.TestEnvironment.Mocks;
using RevitHydraulicPlugin.TestEnvironment.Services;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// TESTE DE INTEGRAÇÃO — Fluxo completo do plugin
    /// 
    /// Simula a execução do pipeline inteiro:
    ///   1. Classificação de ambientes (Fase 1: por nome)
    ///   2. Detecção de equipamentos
    ///   3. Reclassificação de ambientes (Fase 2: com fixtures)
    ///   4. Geração de colunas
    ///   5. Geração de ramais
    ///   6. Logging integrado
    ///   7. Resumo final
    /// 
    /// Cenário:
    ///   Room 1: Banheiro Suíte (vaso + lavatório + chuveiro)
    ///   Room 2: Cozinha (pia de cozinha)
    ///   Room 3: Sala (sem fixture → ignorada)
    ///   Room 4: Lavanderia (tanque + ralo)
    /// </summary>
    public class IntegrationTests : BaseTest
    {
        public override string TestName => "Integracao: Pipeline Completo";
        public override string Description => "Simula fluxo completo sem Revit";

        public override bool Run()
        {
            PrintHeader();

            // ═══════ SETUP ═══════

            var logger = new TestLoggerService();
            logger.StartSession("Teste Integracao Pipeline");

            var level = new MockLevel { Id = 1, Name = "Terreo", ElevationMm = 0 };
            var levels = new List<MockLevel>
            {
                level,
                new MockLevel { Id = 2, Name = "1 Pavimento", ElevationMm = 3000 }
            };

            var project = CreateIntegrationProject(level);

            logger.Info($"Projeto: {project.ProjectName}");
            logger.Info($"Rooms no projeto: {project.Rooms.Count}");

            // ═══════ ETAPA 1: CLASSIFICAÇÃO DE AMBIENTES ═══════

            PrintSection("PluginFlow_ShouldClassifyRoomsCorrectly");

            var roomService = new RoomDetectionTestService();
            var detectedRooms = roomService.DetectRooms(project);

            logger.Info($"Rooms analisados: {detectedRooms.Count}");

            var hydraulicRooms = detectedRooms.Where(r => r.IsHydraulic).ToList();
            var nonHydraulicRooms = detectedRooms.Where(r => !r.IsHydraulic).ToList();

            AssertEqual(3, hydraulicRooms.Count,
                "Fase 1: 3 ambientes hidraulicos por nome (Banheiro, Cozinha, Lavanderia)");
            AssertEqual(1, nonHydraulicRooms.Count,
                "Fase 1: 1 ambiente nao-hidraulico (Sala)");

            // Verificar tipos
            var banheiro = detectedRooms.First(r => r.Room.Name == "Banheiro Suíte");
            AssertEqual(RoomType.SuiteBathroom, banheiro.Type,
                "Banheiro Suite classificado como SuiteBathroom");

            var cozinha = detectedRooms.First(r => r.Room.Name == "Cozinha");
            AssertEqual(RoomType.Kitchen, cozinha.Type,
                "Cozinha classificada como Kitchen");

            var lavanderia = detectedRooms.First(r => r.Room.Name == "Lavanderia");
            AssertEqual(RoomType.Laundry, lavanderia.Type,
                "Lavanderia classificada como Laundry");

            logger.Info($"Hidraulicos: {hydraulicRooms.Count}, Ignorados: {nonHydraulicRooms.Count}");

            // ═══════ ETAPA 2: DETECÇÃO DE EQUIPAMENTOS ═══════

            PrintSection("PluginFlow_ShouldClassifyFixturesCorrectly");

            var equipService = new EquipmentDetectionTestService();
            var detectedEquipment = equipService.DetectEquipment(detectedRooms);

            logger.Info($"Equipamentos detectados: {detectedEquipment.Count}");

            // Banheiro: 3, Cozinha: 1, Lavanderia: 2 = 6 total
            AssertEqual(6, detectedEquipment.Count,
                "6 equipamentos detectados nos ambientes hidraulicos");

            // Verificar tipos
            var toilets = detectedEquipment.Where(e => e.FixtureType == FixtureType.Toilet).ToList();
            AssertEqual(1, toilets.Count, "1 Toilet detectado");

            var sinks = detectedEquipment.Where(e => e.FixtureType == FixtureType.Sink).ToList();
            AssertEqual(1, sinks.Count, "1 Sink detectado");

            var showers = detectedEquipment.Where(e => e.FixtureType == FixtureType.Shower).ToList();
            AssertEqual(1, showers.Count, "1 Shower detectado");

            var kitchenSinks = detectedEquipment.Where(e => e.FixtureType == FixtureType.KitchenSink).ToList();
            AssertEqual(1, kitchenSinks.Count, "1 KitchenSink detectado");

            var laundrySinks = detectedEquipment.Where(e => e.FixtureType == FixtureType.LaundrySink).ToList();
            AssertEqual(1, laundrySinks.Count, "1 LaundrySink detectado");

            var drains = detectedEquipment.Where(e => e.FixtureType == FixtureType.Drain).ToList();
            AssertEqual(1, drains.Count, "1 Drain detectado");

            // ═══════ ETAPA 2.5: RECLASSIFICAÇÃO COM FIXTURES ═══════

            PrintSection("PluginFlow_ShouldReclassifyWithFixtures");

            roomService.ReclassifyWithFixtures(detectedRooms);

            // Banheiro Suíte deve manter SuiteBathroom
            var recheckBanheiro = detectedRooms.First(r => r.Room.Name == "Banheiro Suíte");
            AssertEqual(RoomType.SuiteBathroom, recheckBanheiro.Type,
                "Banheiro Suite continua SuiteBathroom apos reclassificacao");
            AssertTrue(recheckBanheiro.Confidence > 0.8,
                $"Confianca alta apos reclassificacao ({recheckBanheiro.Confidence:P0})");

            // ═══════ ETAPA 3: IGNORAR NÃO-HIDRÁULICOS ═══════

            PrintSection("PluginFlow_ShouldIgnoreNonHydraulicRooms");

            var sala = detectedRooms.First(r => r.Room.Name == "Sala");
            AssertTrue(!sala.IsHydraulic, "Sala permanece nao-hidraulica");
            AssertEqual(RoomType.Unknown, sala.Type, "Sala classificada como Unknown");

            // Nenhum equipamento detectado na Sala
            var salaEquipment = detectedEquipment.Where(e => e.Room.Name == "Sala").ToList();
            AssertEqual(0, salaEquipment.Count,
                "Nenhum equipamento detectado na Sala");

            logger.Info("Sala ignorada corretamente (sem fixtures)");

            // ═══════ ETAPA 4: GERAÇÃO DE RAMAIS ═══════

            PrintSection("PluginFlow_ShouldGenerateBranches_ForHydraulicRoomsOnly");

            var routingService = new BranchRoutingTestService();
            var columns = routingService.CalculateColumns(hydraulicRooms, levels);

            logger.Info($"Colunas criadas: {columns.Count}");

            var (branches, branchResults) = routingService.GenerateBranchesWithResults(
                detectedEquipment, columns);

            logger.Info($"Ramais gerados: {branches.Count}");

            // Esperado:
            // Banheiro: vaso(2) + lavatorio(2) + chuveiro(2) = 6
            // Cozinha: pia(2) = 2
            // Lavanderia: tanque(2) + ralo(1, sem AF) = 3
            // Total = 11
            AssertEqual(11, branches.Count,
                "11 ramais totais (Banheiro:6 + Cozinha:2 + Lavanderia:3)");

            // Verificar esgoto vs AF
            var sewerBranches = branches.Where(b => b.SystemType == ConnectorSystemType.Sewer).ToList();
            var waterBranches = branches.Where(b => b.SystemType == ConnectorSystemType.ColdWater).ToList();

            AssertEqual(6, sewerBranches.Count, "6 ramais de esgoto");
            AssertEqual(5, waterBranches.Count, "5 ramais de agua fria (ralo nao precisa)");

            // Verificar Drain = DN75
            var drainBranch = sewerBranches.FirstOrDefault(b =>
                b.Fixture.FamilyName.Contains("Ralo"));
            if (drainBranch != null)
            {
                AssertApprox(75, drainBranch.Pipe.DiameterMm,
                    "Ralo -> DN75 (regra atualizada)");
            }

            logger.Info($"Esgoto: {sewerBranches.Count}, AF: {waterBranches.Count}");

            // ═══════ ETAPA 5: LOGGING ═══════

            PrintSection("PluginFlow_ShouldWriteExecutionLogs");

            AssertTrue(logger.LogLines.Count > 0,
                "Logs registrados durante execucao");
            AssertTrue(logger.InfoCount > 5,
                $"Mais de 5 INFOs registrados ({logger.InfoCount})");

            // Verificar formato das entradas
            foreach (var line in logger.LogLines.Take(5))
            {
                AssertTrue(TestLoggerService.IsValidFormat(line),
                    "Formato valido: " + line.Substring(0, System.Math.Min(60, line.Length)) + "...");
            }

            // ═══════ ETAPA 6: RESUMO ═══════

            PrintSection("PluginFlow_ShouldReturnExecutionSummary");

            var summary = logger.EndSession();

            AssertTrue(summary.Contains("Teste Integracao Pipeline"),
                "Resumo contem nome da sessao");
            AssertTrue(summary.Contains("INFO:"),
                "Resumo contem contagem de INFO");

            var successCount = branchResults.Count(r => r.Success);
            var failCount = branchResults.Count(r => !r.Success);

            PrintInfo($"=== RESUMO FINAL DA INTEGRACAO ===");
            PrintInfo($"  Rooms analisados: {detectedRooms.Count}");
            PrintInfo($"  Rooms hidraulicos: {hydraulicRooms.Count}");
            PrintInfo($"  Rooms ignorados: {nonHydraulicRooms.Count}");
            PrintInfo($"  Equipamentos: {detectedEquipment.Count}");
            PrintInfo($"  Colunas: {columns.Count}");
            PrintInfo($"  Ramais gerados: {branches.Count}");
            PrintInfo($"  Ramais sucesso: {successCount}");
            PrintInfo($"  Ramais falha: {failCount}");
            PrintInfo($"  Entradas de log: {logger.TotalCount}");

            AssertEqual(detectedEquipment.Count, 6, "Resumo: 6 equipamentos");
            AssertEqual(branches.Count, 11, "Resumo: 11 ramais");

            PrintFooter();
            return FailCount == 0;
        }

        // ═══════ CRIAÇÃO DO PROJETO ═══════

        private MockProject CreateIntegrationProject(MockLevel level)
        {
            var project = new MockProject
            {
                ProjectName = "Projeto de Integracao v2.0",
                Levels = new List<MockLevel> { level }
            };

            // Room 1: Banheiro Suíte
            var banheiro = new MockRoom
            {
                Id = 301, Name = "Banheiro Suíte", Number = "001",
                AreaM2 = 6.5, Level = level,
                CenterPoint = new Point3D(2000, 2000, 0),
                BBoxMin = new Point3D(0, 0, 0),
                BBoxMax = new Point3D(4000, 4000, 3000)
            };
            banheiro.Fixtures.Add(new MockFixture
            {
                Id = 401, FamilyName = "Vaso Sanitário com Caixa Acoplada",
                TypeName = "6 Litros", Category = "Plumbing Fixtures",
                Position = new Point3D(1000, 1000, 0), Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector { Position = new Point3D(1000, 800, 0),
                        Direction = new Point3D(0,-1,0),
                        SystemType = ConnectorSystemType.Sewer, DiameterMm = 100 },
                    new MockConnector { Position = new Point3D(850, 1000, 200),
                        Direction = new Point3D(-1,0,0),
                        SystemType = ConnectorSystemType.ColdWater, DiameterMm = 25 }
                }
            });
            banheiro.Fixtures.Add(new MockFixture
            {
                Id = 402, FamilyName = "Lavatório de Louça",
                TypeName = "Padrão", Category = "Plumbing Fixtures",
                Position = new Point3D(3000, 1000, 850), Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector { Position = new Point3D(3000, 1000, 450),
                        Direction = new Point3D(0,0,-1),
                        SystemType = ConnectorSystemType.Sewer, DiameterMm = 50 },
                    new MockConnector { Position = new Point3D(3000, 900, 850),
                        Direction = new Point3D(0,-1,0),
                        SystemType = ConnectorSystemType.ColdWater, DiameterMm = 25 }
                }
            });
            banheiro.Fixtures.Add(new MockFixture
            {
                Id = 403, FamilyName = "Chuveiro Elétrico",
                TypeName = "220V", Category = "Plumbing Fixtures",
                Position = new Point3D(3000, 3000, 0), Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector { Position = new Point3D(3000, 3000, -50),
                        Direction = new Point3D(0,0,-1),
                        SystemType = ConnectorSystemType.Sewer, DiameterMm = 50 },
                    new MockConnector { Position = new Point3D(3000, 3000, 2100),
                        Direction = new Point3D(0,0,1),
                        SystemType = ConnectorSystemType.ColdWater, DiameterMm = 25 }
                }
            });
            project.Rooms.Add(banheiro);

            // Room 2: Cozinha
            var cozinha = new MockRoom
            {
                Id = 302, Name = "Cozinha", Number = "002",
                AreaM2 = 10.0, Level = level,
                CenterPoint = new Point3D(6000, 2000, 0),
                BBoxMin = new Point3D(4000, 0, 0),
                BBoxMax = new Point3D(8000, 4000, 3000)
            };
            cozinha.Fixtures.Add(new MockFixture
            {
                Id = 411, FamilyName = "Pia de Cozinha Inox",
                TypeName = "1.20m", Category = "Plumbing Fixtures",
                Position = new Point3D(5000, 1000, 850), Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector { Position = new Point3D(5000, 1000, 450),
                        Direction = new Point3D(0,0,-1),
                        SystemType = ConnectorSystemType.Sewer, DiameterMm = 50 },
                    new MockConnector { Position = new Point3D(5000, 900, 850),
                        Direction = new Point3D(0,-1,0),
                        SystemType = ConnectorSystemType.ColdWater, DiameterMm = 25 }
                }
            });
            project.Rooms.Add(cozinha);

            // Room 3: Sala (SEM fixtures)
            project.Rooms.Add(new MockRoom
            {
                Id = 303, Name = "Sala", Number = "003",
                AreaM2 = 20.0, Level = level,
                CenterPoint = new Point3D(5000, 6000, 0),
                BBoxMin = new Point3D(0, 4000, 0),
                BBoxMax = new Point3D(10000, 8000, 3000)
            });

            // Room 4: Lavanderia
            var lavanderia = new MockRoom
            {
                Id = 304, Name = "Lavanderia", Number = "004",
                AreaM2 = 5.0, Level = level,
                CenterPoint = new Point3D(2000, 8000, 0),
                BBoxMin = new Point3D(0, 6000, 0),
                BBoxMax = new Point3D(4000, 10000, 3000)
            };
            lavanderia.Fixtures.Add(new MockFixture
            {
                Id = 421, FamilyName = "Tanque de Lavar Roupa",
                TypeName = "Concreto", Category = "Plumbing Fixtures",
                Position = new Point3D(1000, 8000, 850), Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector { Position = new Point3D(1000, 8000, 450),
                        Direction = new Point3D(0,0,-1),
                        SystemType = ConnectorSystemType.Sewer, DiameterMm = 50 },
                    new MockConnector { Position = new Point3D(1000, 7900, 850),
                        Direction = new Point3D(0,-1,0),
                        SystemType = ConnectorSystemType.ColdWater, DiameterMm = 25 }
                }
            });
            lavanderia.Fixtures.Add(new MockFixture
            {
                Id = 422, FamilyName = "Ralo Seco",
                TypeName = "100mm", Category = "Plumbing Fixtures",
                Position = new Point3D(2000, 7000, 0), Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector { Position = new Point3D(2000, 7000, -50),
                        Direction = new Point3D(0,0,-1),
                        SystemType = ConnectorSystemType.Sewer, DiameterMm = 50 }
                }
            });
            project.Rooms.Add(lavanderia);

            return project;
        }
    }
}
