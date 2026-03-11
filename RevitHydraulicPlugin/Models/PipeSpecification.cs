namespace RevitHydraulicPlugin.Models
{
    /// <summary>
    /// Especificações de tubulação para um ramal ou coluna hidráulica.
    /// Centraliza parâmetros que definem como a tubulação será criada no modelo.
    /// </summary>
    public class PipeSpecification
    {
        /// <summary>
        /// Diâmetro nominal da tubulação em milímetros.
        /// </summary>
        public double DiameterMm { get; set; }

        /// <summary>
        /// Inclinação da tubulação em porcentagem (ex: 1.0 = 1%).
        /// Aplicável apenas a ramais de esgoto.
        /// Para água fria, tipicamente é 0.
        /// </summary>
        public double SlopePercent { get; set; }

        /// <summary>
        /// Nome do tipo de sistema do Revit (ex: "Domestic Cold Water", "Sanitary").
        /// Usado para atribuir o PipingSystemType correto.
        /// </summary>
        public string SystemTypeName { get; set; }

        /// <summary>
        /// Nome do tipo de tubo no Revit (ex: "PVC", "Copper").
        /// Usado para selecionar o PipeType correto.
        /// </summary>
        public string PipeTypeName { get; set; }

        /// <summary>
        /// Material padrão da tubulação (para referência/documentação).
        /// </summary>
        public string Material { get; set; }

        public override string ToString()
        {
            return $"Ø{DiameterMm}mm - {Material} - Sistema: {SystemTypeName} - Inclinação: {SlopePercent}%";
        }
    }
}
