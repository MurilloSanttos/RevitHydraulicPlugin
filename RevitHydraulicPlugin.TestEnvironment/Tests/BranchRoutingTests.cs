using RevitHydraulicPlugin.TestEnvironment.Mocks;
using RevitHydraulicPlugin.TestEnvironment.Services;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// TESTE — Geração de Ramais Hidráulicos (v2.0)
    /// 
    /// Cenários:
    /// 1. Banheiro (vaso + lavatório + chuveiro)
    /// 2. Cozinha (pia)
    /// 3. Lavanderia (tanque + ralo)
    /// 4. Sem coluna disponível
    /// 5. Rota ortogonal
    /// </summary>
    public class BranchRoutingTests : BaseTest
    {
        public override string TestName => "Geracao de Ramais v2.0";
        public override string Description => "Valida roteamento ortogonal e regras por fixture";

        public override bool Run()
        {
            PrintHeader();

            Test_Scenario1_Bathroom();
            Test_Scenario2_Kitchen();
            Test_Scenario3_Laundry();
            Test_Scenario4_NoColumn();
            Test_OrthogonalPath();
            Test_DrainDoesNotGenerateWaterBranch();

            PrintFooter();
            return FailCount == 0;
        }

        // ═══════ CENÁRIO 1: BANHEIRO ═══════

        private void Test_Scenario1_Bathroom()
        {
            PrintSection("Cenario 1: Banheiro (vaso + lavatorio + chuveiro)");

            var level = new MockLevel { Id = 1, Name = "Terreo", ElevationMm = 0 };
            var room = CreateBathroomRoom(level);
            var (equipment, columns) = SetupScenario(room, level);

            var routingService = new BranchRoutingTestService();
            var (branches, results) = routingService.GenerateBranchesWithResults(equipment, columns);

            // 3 fixtures: vaso, lavatório, chuveiro
            // Cada um gera: 1 esgoto + 1 AF = 6 ramais
            PrintInfo($"Ramais gerados: {branches.Count}");
            PrintInfo($"Resultados: {results.Count}");

            AssertEqual(6, branches.Count,
                "3 fixtures x 2 sistemas = 6 ramais");

            // Verifica regras aplicadas
            var toiletSewer = results.First(r =>
                r.AppliedRule?.Description?.Contains("Vaso") == true
                && r.AppliedRule?.SystemTypeName == "Sanitary");

            AssertTrue(toiletSewer.Success, "Ramal esgoto do vaso gerado");
            AssertApprox(100, toiletSewer.AppliedRule.DiameterMm,
                "Vaso -> DN100");
            AssertApprox(1.0, toiletSewer.AppliedRule.SlopePercent,
                "Vaso -> 1%");

            var sinkSewer = results.First(r =>
                r.AppliedRule?.Description?.Contains("Lavatorio") == true
                && r.AppliedRule?.SystemTypeName == "Sanitary");

            AssertTrue(sinkSewer.Success, "Ramal esgoto do lavatorio gerado");
            AssertApprox(50, sinkSewer.AppliedRule.DiameterMm,
                "Lavatorio -> DN50");

            // Verificar que todas as pipes de esgoto têm inclinação
            var sewerBranches = branches.Where(b => b.SystemType == ConnectorSystemType.Sewer).ToList();
            foreach (var b in sewerBranches)
            {
                AssertTrue(b.Pipe.SlopePercent > 0,
                    $"Esgoto {b.Fixture.FamilyName}: inclinacao > 0");
            }

            // Verificar que AF não tem inclinação
            var afBranches = branches.Where(b => b.SystemType == ConnectorSystemType.ColdWater).ToList();
            foreach (var b in afBranches)
            {
                AssertApprox(0, b.Pipe.SlopePercent,
                    $"AF {b.Fixture.FamilyName}: inclinacao = 0");
            }
        }

        // ═══════ CENÁRIO 2: COZINHA ═══════

        private void Test_Scenario2_Kitchen()
        {
            PrintSection("Cenario 2: Cozinha (pia de cozinha)");

            var level = new MockLevel { Id = 1, Name = "Terreo", ElevationMm = 0 };
            var room = CreateKitchenRoom(level);
            var (equipment, columns) = SetupScenario(room, level);

            var routingService = new BranchRoutingTestService();
            var branches = routingService.GenerateBranches(equipment, columns);

            // 1 pia: 1 esgoto + 1 AF = 2 ramais
            AssertEqual(2, branches.Count,
                "Pia de cozinha gera 2 ramais (esgoto + AF)");

            var sewerBranch = branches.First(b => b.SystemType == ConnectorSystemType.Sewer);
            AssertApprox(50, sewerBranch.Pipe.DiameterMm,
                "KitchenSink esgoto -> DN50");
        }

        // ═══════ CENÁRIO 3: LAVANDERIA ═══════

        private void Test_Scenario3_Laundry()
        {
            PrintSection("Cenario 3: Lavanderia (tanque + ralo)");

            var level = new MockLevel { Id = 1, Name = "Terreo", ElevationMm = 0 };
            var room = CreateLaundryRoom(level);
            var (equipment, columns) = SetupScenario(room, level);

            var routingService = new BranchRoutingTestService();
            var branches = routingService.GenerateBranches(equipment, columns);

            // Tanque: 1 esgoto + 1 AF = 2
            // Ralo: 1 esgoto (sem AF) = 1
            // Total = 3
            AssertEqual(3, branches.Count,
                "Tanque (2) + Ralo (1) = 3 ramais");

            var drainBranch = branches.First(b =>
                b.Fixture.FamilyName.Contains("Ralo")
                && b.SystemType == ConnectorSystemType.Sewer);

            AssertApprox(75, drainBranch.Pipe.DiameterMm,
                "Ralo esgoto -> DN75");
        }

        // ═══════ CENÁRIO 4: SEM COLUNA ═══════

        private void Test_Scenario4_NoColumn()
        {
            PrintSection("Cenario 4: Sem coluna disponivel");

            var level = new MockLevel { Id = 1, Name = "Terreo", ElevationMm = 0 };
            var room = CreateBathroomRoom(level);

            var fixtureClassifier = new FixtureClassifierTestService();
            var equipment = new List<DetectedEquipment>();
            foreach (var fixture in room.Fixtures)
            {
                var ft = fixtureClassifier.ClassifyByNameOnly(fixture.FamilyName, fixture.TypeName);
                equipment.Add(new DetectedEquipment
                {
                    Fixture = fixture,
                    FixtureType = ft,
                    Type = FixtureClassifierTestService.ToEquipmentType(ft),
                    Room = room
                });
            }

            var noColumns = new List<MockColumn>(); // Vazio

            var routingService = new BranchRoutingTestService();
            var (branches, results) = routingService.GenerateBranchesWithResults(equipment, noColumns);

            AssertEqual(0, branches.Count,
                "Sem colunas -> 0 ramais gerados");

            var failures = results.Where(r => !r.Success).ToList();
            AssertTrue(failures.Count > 0,
                "Deve reportar falhas quando nao ha colunas");

            foreach (var f in failures)
            {
                AssertTrue(!string.IsNullOrEmpty(f.FailureReason),
                    $"Falha tem motivo: '{f.FailureReason}'");
            }
        }

        // ═══════ ORTOGONALIDADE ═══════

        private void Test_OrthogonalPath()
        {
            PrintSection("BranchRouting_ShouldGenerateOrthogonalPath");

            var level = new MockLevel { Id = 1, Name = "Terreo", ElevationMm = 0 };
            var room = CreateBathroomRoom(level);
            var (equipment, columns) = SetupScenario(room, level);

            var routingService = new BranchRoutingTestService();
            var branches = routingService.GenerateBranches(equipment, columns);

            // Verifica que caminhos são ortogonais:
            // Start.Z e End.Z devem diferir (esgoto tem queda)
            // Start.Z == End.Z para AF
            var sewerBranch = branches.First(b => b.SystemType == ConnectorSystemType.Sewer);
            var afBranch = branches.First(b => b.SystemType == ConnectorSystemType.ColdWater);

            AssertTrue(sewerBranch.Pipe.StartPoint.Z > sewerBranch.Pipe.EndPoint.Z,
                "Esgoto: ponto final mais baixo (queda por inclinacao)");
            AssertApprox(afBranch.Pipe.StartPoint.Z, afBranch.Pipe.EndPoint.Z,
                "AF: sem queda (horizontal)", 0.1);
        }

        // ═══════ RALO NÃO GERA AF ═══════

        private void Test_DrainDoesNotGenerateWaterBranch()
        {
            PrintSection("Drain nao gera ramal AF");

            var level = new MockLevel { Id = 1, Name = "Terreo", ElevationMm = 0 };
            var ralo = MockProjectFactory.CreateResidentialBuilding().Rooms
                .SelectMany(r => r.Fixtures)
                .FirstOrDefault(f => f.FamilyName.Contains("Ralo"));

            if (ralo == null)
            {
                PrintInfo("SKIP: Nenhum ralo encontrado no MockProject");
                return;
            }

            var needsWater = HydraulicRulesTestService.NeedsColdWater(FixtureType.Drain);
            AssertTrue(!needsWater,
                "Drain (Ralo) NAO precisa de agua fria");
        }

        // ═══════ HELPERS ═══════

        private MockRoom CreateBathroomRoom(MockLevel level)
        {
            var room = new MockRoom
            {
                Id = 100, Name = "Banheiro Suite", Number = "101",
                AreaM2 = 6.0, Level = level,
                CenterPoint = new Point3D(2000, 2000, 0),
                BBoxMin = new Point3D(0, 0, 0),
                BBoxMax = new Point3D(4000, 4000, 3000)
            };
            room.Fixtures.Add(new MockFixture
            {
                Id = 201, FamilyName = "Vaso Sanitário com Caixa Acoplada",
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
            room.Fixtures.Add(new MockFixture
            {
                Id = 202, FamilyName = "Lavatório de Louça",
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
            room.Fixtures.Add(new MockFixture
            {
                Id = 203, FamilyName = "Chuveiro Elétrico",
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
            return room;
        }

        private MockRoom CreateKitchenRoom(MockLevel level)
        {
            var room = new MockRoom
            {
                Id = 110, Name = "Cozinha", Number = "102",
                AreaM2 = 12.0, Level = level,
                CenterPoint = new Point3D(6000, 2000, 0),
                BBoxMin = new Point3D(4000, 0, 0),
                BBoxMax = new Point3D(8000, 4000, 3000)
            };
            room.Fixtures.Add(new MockFixture
            {
                Id = 211, FamilyName = "Pia de Cozinha Inox",
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
            return room;
        }

        private MockRoom CreateLaundryRoom(MockLevel level)
        {
            var room = new MockRoom
            {
                Id = 120, Name = "Lavanderia", Number = "103",
                AreaM2 = 6.0, Level = level,
                CenterPoint = new Point3D(2000, 6000, 0),
                BBoxMin = new Point3D(0, 4000, 0),
                BBoxMax = new Point3D(4000, 8000, 3000)
            };
            room.Fixtures.Add(new MockFixture
            {
                Id = 221, FamilyName = "Tanque de Lavar Roupa",
                TypeName = "Concreto", Category = "Plumbing Fixtures",
                Position = new Point3D(1000, 6000, 850), Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector { Position = new Point3D(1000, 6000, 450),
                        Direction = new Point3D(0,0,-1),
                        SystemType = ConnectorSystemType.Sewer, DiameterMm = 50 },
                    new MockConnector { Position = new Point3D(1000, 5900, 850),
                        Direction = new Point3D(0,-1,0),
                        SystemType = ConnectorSystemType.ColdWater, DiameterMm = 25 }
                }
            });
            room.Fixtures.Add(new MockFixture
            {
                Id = 222, FamilyName = "Ralo Seco",
                TypeName = "100mm", Category = "Plumbing Fixtures",
                Position = new Point3D(2000, 5000, 0), Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector { Position = new Point3D(2000, 5000, -50),
                        Direction = new Point3D(0,0,-1),
                        SystemType = ConnectorSystemType.Sewer, DiameterMm = 50 }
                }
            });
            return room;
        }

        private (List<DetectedEquipment> equipment, List<MockColumn> columns)
            SetupScenario(MockRoom room, MockLevel level)
        {
            var fixtureClassifier = new FixtureClassifierTestService();
            var equipment = new List<DetectedEquipment>();

            foreach (var fixture in room.Fixtures)
            {
                var ft = fixtureClassifier.ClassifyByNameOnly(fixture.FamilyName, fixture.TypeName);
                equipment.Add(new DetectedEquipment
                {
                    Fixture = fixture,
                    FixtureType = ft,
                    Type = FixtureClassifierTestService.ToEquipmentType(ft),
                    Room = room
                });
            }

            var detectedRoom = new DetectedRoom
            {
                Room = room, Type = RoomType.Bathroom, IsHydraulic = true
            };

            var levels = new List<MockLevel> { level, new MockLevel { Id = 2, Name = "1 Pav", ElevationMm = 3000 } };
            var routingService = new BranchRoutingTestService();
            var columns = routingService.CalculateColumns(
                new List<DetectedRoom> { detectedRoom }, levels);

            return (equipment, columns);
        }
    }
}
