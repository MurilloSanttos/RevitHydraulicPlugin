using System.Collections.Generic;

namespace RevitHydraulicPlugin.TestEnvironment.Mocks
{
    /// <summary>
    /// Simula um equipamento hidráulico (FamilyInstance) do Revit.
    /// No Revit, equipamentos como vasos sanitários, lavatórios e chuveiros
    /// são FamilyInstances da categoria PlumbingFixtures.
    /// </summary>
    public class MockFixture
    {
        /// <summary>
        /// Identificador único (simula ElementId).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome da família (ex: "Vaso Sanitário Padrão").
        /// Corresponde a FamilyInstance.Symbol.Family.Name no Revit.
        /// </summary>
        public string FamilyName { get; set; }

        /// <summary>
        /// Nome do tipo (ex: "6 Litros").
        /// Corresponde a FamilyInstance.Symbol.Name no Revit.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Categoria do equipamento (ex: "Plumbing Fixtures").
        /// Corresponde a FamilyInstance.Category.Name no Revit.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Posição do equipamento no espaço 3D (milímetros).
        /// Corresponde a LocationPoint.Point no Revit.
        /// </summary>
        public Point3D Position { get; set; }

        /// <summary>
        /// Conectores hidráulicos do equipamento.
        /// </summary>
        public List<MockConnector> Connectors { get; set; } = new List<MockConnector>();

        /// <summary>
        /// Nível em que o equipamento está.
        /// </summary>
        public MockLevel Level { get; set; }

        public override string ToString()
        {
            return $"{FamilyName} ({TypeName}) em {Position} - Conectores: {Connectors.Count}";
        }
    }
}
