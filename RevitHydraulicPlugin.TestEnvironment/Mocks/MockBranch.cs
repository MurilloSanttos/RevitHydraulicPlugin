namespace RevitHydraulicPlugin.TestEnvironment.Mocks
{
    /// <summary>
    /// Simula um ramal hidráulico conectando um equipamento a uma coluna.
    /// </summary>
    public class MockBranch
    {
        /// <summary>
        /// Equipamento de origem.
        /// </summary>
        public MockFixture Fixture { get; set; }

        /// <summary>
        /// Coluna de destino.
        /// </summary>
        public MockColumn TargetColumn { get; set; }

        /// <summary>
        /// Pipe criado para este ramal.
        /// </summary>
        public MockPipe Pipe { get; set; }

        /// <summary>
        /// Tipo de sistema do ramal.
        /// </summary>
        public ConnectorSystemType SystemType { get; set; }

        public override string ToString()
        {
            string systemName = SystemType == ConnectorSystemType.ColdWater ? "AF" : "ES";
            return $"Ramal [{systemName}] {Fixture?.FamilyName} → Coluna {TargetColumn?.ColumnId} | {Pipe}";
        }
    }
}
