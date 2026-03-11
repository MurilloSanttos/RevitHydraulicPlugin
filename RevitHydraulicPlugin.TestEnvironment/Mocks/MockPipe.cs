namespace RevitHydraulicPlugin.TestEnvironment.Mocks
{
    /// <summary>
    /// Simula um Pipe (tubulação) do Revit.
    /// No Revit, Pipe é criado a partir de dois pontos, um PipeType e um SystemType.
    /// </summary>
    public class MockPipe
    {
        /// <summary>
        /// Identificador único (simula ElementId).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ponto de início da tubulação em milímetros.
        /// </summary>
        public Point3D StartPoint { get; set; }

        /// <summary>
        /// Ponto final da tubulação em milímetros.
        /// </summary>
        public Point3D EndPoint { get; set; }

        /// <summary>
        /// Diâmetro nominal em milímetros.
        /// </summary>
        public double DiameterMm { get; set; }

        /// <summary>
        /// Tipo de sistema (ex: "Sanitary", "Domestic Cold Water").
        /// </summary>
        public string SystemType { get; set; }

        /// <summary>
        /// Material da tubulação.
        /// </summary>
        public string Material { get; set; }

        /// <summary>
        /// Inclinação em porcentagem (ex: 2.0 = 2%).
        /// Zero para tubulações horizontais de água fria.
        /// </summary>
        public double SlopePercent { get; set; }

        /// <summary>
        /// Comprimento calculado automaticamente (em mm).
        /// </summary>
        public double LengthMm
        {
            get
            {
                if (StartPoint == null || EndPoint == null) return 0;
                return StartPoint.DistanceTo(EndPoint);
            }
        }

        /// <summary>
        /// Indica se a tubulação é vertical (coluna) ou horizontal (ramal).
        /// </summary>
        public bool IsVertical
        {
            get
            {
                if (StartPoint == null || EndPoint == null) return false;
                return System.Math.Abs(StartPoint.X - EndPoint.X) < 0.1
                    && System.Math.Abs(StartPoint.Y - EndPoint.Y) < 0.1;
            }
        }

        public override string ToString()
        {
            string orientation = IsVertical ? "VERTICAL" : "HORIZONTAL";
            return $"Pipe [{orientation}] Ø{DiameterMm}mm {SystemType} | " +
                   $"{StartPoint} → {EndPoint} | Comprimento: {LengthMm:F1}mm | " +
                   $"Inclinação: {SlopePercent}%";
        }
    }
}
