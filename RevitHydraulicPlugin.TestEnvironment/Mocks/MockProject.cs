using System.Collections.Generic;

namespace RevitHydraulicPlugin.TestEnvironment.Mocks
{
    /// <summary>
    /// Simula um projeto completo do Revit (Document).
    /// Contém todos os elementos que normalmente estariam no modelo:
    /// níveis, ambientes e equipamentos.
    /// 
    /// Para cada cenário de teste, um MockProject diferente é montado
    /// com os dados necessários.
    /// </summary>
    public class MockProject
    {
        /// <summary>
        /// Nome do projeto (para identificação no console).
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Níveis do projeto.
        /// </summary>
        public List<MockLevel> Levels { get; set; } = new List<MockLevel>();

        /// <summary>
        /// Ambientes (Rooms) do projeto.
        /// </summary>
        public List<MockRoom> Rooms { get; set; } = new List<MockRoom>();

        public override string ToString()
        {
            return $"Projeto '{ProjectName}' - Níveis: {Levels.Count} - Rooms: {Rooms.Count}";
        }
    }

    /// <summary>
    /// Factory para criar projetos de teste pré-configurados.
    /// Centraliza a criação de cenários de teste consistentes.
    /// </summary>
    public static class MockProjectFactory
    {
        private static int _nextId = 1000;
        private static int NextId() => _nextId++;

        /// <summary>
        /// Cria um projeto de edifício residencial de 3 pavimentos
        /// com ambientes e equipamentos típicos.
        /// </summary>
        public static MockProject CreateResidentialBuilding()
        {
            // === NÍVEIS ===
            var terreo = new MockLevel { Id = NextId(), Name = "Térreo", ElevationMm = 0 };
            var pav1 = new MockLevel { Id = NextId(), Name = "1º Pavimento", ElevationMm = 3000 };
            var pav2 = new MockLevel { Id = NextId(), Name = "2º Pavimento", ElevationMm = 6000 };
            var cobertura = new MockLevel { Id = NextId(), Name = "Cobertura", ElevationMm = 9000 };

            var project = new MockProject
            {
                ProjectName = "Edifício Residencial 3 Pavimentos",
                Levels = new List<MockLevel> { terreo, pav1, pav2, cobertura }
            };

            // === TÉRREO ===
            // Sala (NÃO hidráulica)
            project.Rooms.Add(new MockRoom
            {
                Id = NextId(), Name = "Sala", Number = "001",
                AreaM2 = 25.0, Level = terreo,
                CenterPoint = new Point3D(5000, 3000, 0),
                BBoxMin = new Point3D(2000, 1000, 0),
                BBoxMax = new Point3D(8000, 5000, 3000)
            });

            // Cozinha (hidráulica)
            var cozinhaTérreo = new MockRoom
            {
                Id = NextId(), Name = "Cozinha", Number = "002",
                AreaM2 = 12.0, Level = terreo,
                CenterPoint = new Point3D(2000, 8000, 0),
                BBoxMin = new Point3D(0, 6000, 0),
                BBoxMax = new Point3D(4000, 10000, 3000)
            };
            cozinhaTérreo.Fixtures.Add(CreatePia(new Point3D(1000, 8000, 850), terreo));
            cozinhaTérreo.Fixtures.Add(CreateRalo(new Point3D(2000, 7000, 0), terreo));
            project.Rooms.Add(cozinhaTérreo);

            // Quarto (NÃO hidráulica)
            project.Rooms.Add(new MockRoom
            {
                Id = NextId(), Name = "Quarto", Number = "003",
                AreaM2 = 15.0, Level = terreo,
                CenterPoint = new Point3D(8000, 8000, 0),
                BBoxMin = new Point3D(6000, 6000, 0),
                BBoxMax = new Point3D(10000, 10000, 3000)
            });

            // === 1º PAVIMENTO ===
            // Banheiro (hidráulica)
            var banheiroPav1 = new MockRoom
            {
                Id = NextId(), Name = "Banheiro", Number = "101",
                AreaM2 = 5.0, Level = pav1,
                CenterPoint = new Point3D(2000, 2000, 3000),
                BBoxMin = new Point3D(0, 0, 3000),
                BBoxMax = new Point3D(4000, 4000, 6000)
            };
            banheiroPav1.Fixtures.Add(CreateVasoSanitario(new Point3D(1000, 1000, 3000), pav1));
            banheiroPav1.Fixtures.Add(CreateLavatorio(new Point3D(3000, 1000, 3850), pav1));
            banheiroPav1.Fixtures.Add(CreateChuveiro(new Point3D(3000, 3000, 3000), pav1));
            project.Rooms.Add(banheiroPav1);

            // Quarto (NÃO hidráulica)
            project.Rooms.Add(new MockRoom
            {
                Id = NextId(), Name = "Quarto 1", Number = "102",
                AreaM2 = 16.0, Level = pav1,
                CenterPoint = new Point3D(8000, 2000, 3000),
                BBoxMin = new Point3D(5000, 0, 3000),
                BBoxMax = new Point3D(11000, 4000, 6000)
            });

            // Lavanderia (hidráulica)
            var lavanderiaPav1 = new MockRoom
            {
                Id = NextId(), Name = "Lavanderia", Number = "103",
                AreaM2 = 6.0, Level = pav1,
                CenterPoint = new Point3D(2000, 8000, 3000),
                BBoxMin = new Point3D(0, 6000, 3000),
                BBoxMax = new Point3D(4000, 10000, 6000)
            };
            lavanderiaPav1.Fixtures.Add(CreateTanque(new Point3D(1000, 8000, 3850), pav1));
            lavanderiaPav1.Fixtures.Add(CreateRalo(new Point3D(2000, 7000, 3000), pav1));
            project.Rooms.Add(lavanderiaPav1);

            // === 2º PAVIMENTO ===
            // Banheiro Suíte (hidráulica)
            var banheiroSuite = new MockRoom
            {
                Id = NextId(), Name = "Banheiro Suíte", Number = "201",
                AreaM2 = 8.0, Level = pav2,
                CenterPoint = new Point3D(2000, 2000, 6000),
                BBoxMin = new Point3D(0, 0, 6000),
                BBoxMax = new Point3D(4000, 4000, 9000)
            };
            banheiroSuite.Fixtures.Add(CreateVasoSanitario(new Point3D(1000, 1000, 6000), pav2));
            banheiroSuite.Fixtures.Add(CreateLavatorio(new Point3D(3000, 1000, 6850), pav2));
            banheiroSuite.Fixtures.Add(CreateChuveiro(new Point3D(3000, 3000, 6000), pav2));
            project.Rooms.Add(banheiroSuite);

            // Lavabo (hidráulica)
            var lavaboPav2 = new MockRoom
            {
                Id = NextId(), Name = "Lavabo", Number = "202",
                AreaM2 = 3.0, Level = pav2,
                CenterPoint = new Point3D(8000, 2000, 6000),
                BBoxMin = new Point3D(6000, 0, 6000),
                BBoxMax = new Point3D(10000, 4000, 9000)
            };
            lavaboPav2.Fixtures.Add(CreateVasoSanitario(new Point3D(7000, 1000, 6000), pav2));
            lavaboPav2.Fixtures.Add(CreateLavatorio(new Point3D(9000, 1000, 6850), pav2));
            project.Rooms.Add(lavaboPav2);

            // Escritório (NÃO hidráulica)
            project.Rooms.Add(new MockRoom
            {
                Id = NextId(), Name = "Escritório", Number = "203",
                AreaM2 = 20.0, Level = pav2,
                CenterPoint = new Point3D(5000, 8000, 6000),
                BBoxMin = new Point3D(0, 6000, 6000),
                BBoxMax = new Point3D(10000, 10000, 9000)
            });

            return project;
        }

        // ===== FACTORY METHODS PARA EQUIPAMENTOS =====

        private static MockFixture CreateVasoSanitario(Point3D position, MockLevel level)
        {
            return new MockFixture
            {
                Id = NextId(),
                FamilyName = "Vaso Sanitário com Caixa Acoplada",
                TypeName = "6 Litros",
                Category = "Plumbing Fixtures",
                Position = position,
                Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector
                    {
                        Position = position.Offset(0, -200, 0),
                        Direction = new Point3D(0, -1, 0),
                        SystemType = ConnectorSystemType.Sewer,
                        DiameterMm = 100
                    },
                    new MockConnector
                    {
                        Position = position.Offset(-150, 0, 200),
                        Direction = new Point3D(-1, 0, 0),
                        SystemType = ConnectorSystemType.ColdWater,
                        DiameterMm = 25
                    }
                }
            };
        }

        private static MockFixture CreateLavatorio(Point3D position, MockLevel level)
        {
            return new MockFixture
            {
                Id = NextId(),
                FamilyName = "Lavatório de Louça",
                TypeName = "Padrão",
                Category = "Plumbing Fixtures",
                Position = position,
                Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector
                    {
                        Position = position.Offset(0, 0, -400),
                        Direction = new Point3D(0, 0, -1),
                        SystemType = ConnectorSystemType.Sewer,
                        DiameterMm = 50
                    },
                    new MockConnector
                    {
                        Position = position.Offset(0, -100, 0),
                        Direction = new Point3D(0, -1, 0),
                        SystemType = ConnectorSystemType.ColdWater,
                        DiameterMm = 25
                    }
                }
            };
        }

        private static MockFixture CreateChuveiro(Point3D position, MockLevel level)
        {
            return new MockFixture
            {
                Id = NextId(),
                FamilyName = "Chuveiro Elétrico",
                TypeName = "220V",
                Category = "Plumbing Fixtures",
                Position = position,
                Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector
                    {
                        Position = position.Offset(0, 0, -50),
                        Direction = new Point3D(0, 0, -1),
                        SystemType = ConnectorSystemType.Sewer,
                        DiameterMm = 50
                    },
                    new MockConnector
                    {
                        Position = position.Offset(0, 0, 2100),
                        Direction = new Point3D(0, 0, 1),
                        SystemType = ConnectorSystemType.ColdWater,
                        DiameterMm = 25
                    }
                }
            };
        }

        private static MockFixture CreatePia(Point3D position, MockLevel level)
        {
            return new MockFixture
            {
                Id = NextId(),
                FamilyName = "Pia de Cozinha Inox",
                TypeName = "1.20m",
                Category = "Plumbing Fixtures",
                Position = position,
                Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector
                    {
                        Position = position.Offset(0, 0, -400),
                        Direction = new Point3D(0, 0, -1),
                        SystemType = ConnectorSystemType.Sewer,
                        DiameterMm = 50
                    },
                    new MockConnector
                    {
                        Position = position.Offset(0, -100, 0),
                        Direction = new Point3D(0, -1, 0),
                        SystemType = ConnectorSystemType.ColdWater,
                        DiameterMm = 25
                    }
                }
            };
        }

        private static MockFixture CreateTanque(Point3D position, MockLevel level)
        {
            return new MockFixture
            {
                Id = NextId(),
                FamilyName = "Tanque de Lavar Roupa",
                TypeName = "Concreto",
                Category = "Plumbing Fixtures",
                Position = position,
                Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector
                    {
                        Position = position.Offset(0, 0, -400),
                        Direction = new Point3D(0, 0, -1),
                        SystemType = ConnectorSystemType.Sewer,
                        DiameterMm = 50
                    },
                    new MockConnector
                    {
                        Position = position.Offset(0, -100, 0),
                        Direction = new Point3D(0, -1, 0),
                        SystemType = ConnectorSystemType.ColdWater,
                        DiameterMm = 25
                    }
                }
            };
        }

        private static MockFixture CreateRalo(Point3D position, MockLevel level)
        {
            return new MockFixture
            {
                Id = NextId(),
                FamilyName = "Ralo Seco",
                TypeName = "100mm",
                Category = "Plumbing Fixtures",
                Position = position,
                Level = level,
                Connectors = new List<MockConnector>
                {
                    new MockConnector
                    {
                        Position = position.Offset(0, 0, -50),
                        Direction = new Point3D(0, 0, -1),
                        SystemType = ConnectorSystemType.Sewer,
                        DiameterMm = 50
                    }
                }
            };
        }
    }
}
