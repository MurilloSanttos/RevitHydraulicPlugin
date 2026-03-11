namespace RevitHydraulicPlugin.TestEnvironment.Mocks
{
    /// <summary>
    /// Simula um Level (nível) do Revit.
    /// No Revit, Level representa um plano horizontal com uma elevação definida.
    /// </summary>
    public class MockLevel
    {
        /// <summary>
        /// Identificador único do nível (simula ElementId).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome do nível (ex: "Térreo", "1º Pavimento").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Elevação do nível em milímetros, relativa ao ponto base do projeto.
        /// No Revit real, seria em pés.
        /// </summary>
        public double ElevationMm { get; set; }

        public override string ToString()
        {
            return $"Nível: {Name} (Elevação: {ElevationMm}mm)";
        }
    }
}
