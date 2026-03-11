using System.Collections.Generic;

namespace RevitHydraulicPlugin.TestEnvironment.Mocks
{
    /// <summary>
    /// Simula uma coluna hidráulica vertical.
    /// Agrupa os MockPipes verticais criados para representar a coluna.
    /// </summary>
    public class MockColumn
    {
        /// <summary>
        /// Identificador da coluna (ex: "CAF-01", "CES-01").
        /// </summary>
        public string ColumnId { get; set; }

        /// <summary>
        /// Tipo de sistema da coluna.
        /// </summary>
        public ConnectorSystemType SystemType { get; set; }

        /// <summary>
        /// Posição XY da coluna no plano horizontal (mm).
        /// </summary>
        public Point3D BasePosition { get; set; }

        /// <summary>
        /// Diâmetro da coluna em milímetros.
        /// </summary>
        public double DiameterMm { get; set; }

        /// <summary>
        /// Níveis que a coluna atravessa.
        /// </summary>
        public List<MockLevel> Levels { get; set; } = new List<MockLevel>();

        /// <summary>
        /// Pipes criados para esta coluna (um por par de níveis adjacentes).
        /// </summary>
        public List<MockPipe> Pipes { get; set; } = new List<MockPipe>();

        /// <summary>
        /// Referência ao Room associado (para posicionamento).
        /// </summary>
        public MockRoom AssociatedRoom { get; set; }

        public override string ToString()
        {
            string systemName = SystemType == ConnectorSystemType.ColdWater ? "Água Fria" : "Esgoto";
            return $"Coluna {ColumnId} ({systemName}) Ø{DiameterMm}mm em {BasePosition} - " +
                   $"Níveis: {Levels.Count} - Segmentos: {Pipes.Count}";
        }
    }
}
