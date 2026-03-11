namespace RevitHydraulicPlugin.TestEnvironment.Mocks
{
    /// <summary>
    /// Simula um Connector MEP do Revit.
    /// No Revit, Connectors são pontos de conexão em FamilyInstances
    /// que permitem ligação de tubulações, dutos, etc.
    /// </summary>
    public class MockConnector
    {
        /// <summary>
        /// Posição do conector no espaço 3D (milímetros).
        /// </summary>
        public Point3D Position { get; set; }

        /// <summary>
        /// Direção do conector (vetor unitário indicando para onde aponta).
        /// </summary>
        public Point3D Direction { get; set; }

        /// <summary>
        /// Tipo de sistema do conector.
        /// </summary>
        public ConnectorSystemType SystemType { get; set; }

        /// <summary>
        /// Diâmetro nominal do conector em milímetros.
        /// </summary>
        public double DiameterMm { get; set; }

        public override string ToString()
        {
            return $"Conector {SystemType} Ø{DiameterMm}mm em {Position}";
        }
    }

    /// <summary>
    /// Tipos de sistema de conectores hidráulicos.
    /// Corresponde aos PipeSystemType do Revit.
    /// </summary>
    public enum ConnectorSystemType
    {
        /// <summary>Água fria (Domestic Cold Water).</summary>
        ColdWater,

        /// <summary>Água quente (Domestic Hot Water).</summary>
        HotWater,

        /// <summary>Esgoto sanitário (Sanitary).</summary>
        Sewer,

        /// <summary>Esgoto pluvial (Storm).</summary>
        Storm
    }
}
