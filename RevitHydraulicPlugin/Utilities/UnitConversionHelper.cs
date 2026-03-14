using Autodesk.Revit.DB;

namespace RevitHydraulicPlugin.Utilities
{
    /// <summary>
    /// Utilitários para conversão de unidades entre o sistema interno do Revit (pés)
    /// e unidades métricas (milímetros), que são o padrão em projetos hidráulicos brasileiros.
    /// 
    /// O Revit utiliza internamente a unidade "pés" (feet) para todas as medidas de comprimento.
    /// 1 pé = 304.8 milímetros.
    /// </summary>
    public static class UnitConversionHelper
    {
        /// <summary>
        /// Fator de conversão: 1 pé = 304.8 mm.
        /// </summary>
        private const double FeetToMmFactor = 304.8;

        /// <summary>
        /// Converte milímetros para pés (unidade interna do Revit).
        /// </summary>
        /// <param name="mm">Valor em milímetros.</param>
        /// <returns>Valor em pés.</returns>
        public static double MmToFeet(double mm)
        {
            return mm / FeetToMmFactor;
        }

        /// <summary>
        /// Converte pés (unidade interna do Revit) para milímetros.
        /// </summary>
        /// <param name="feet">Valor em pés.</param>
        /// <returns>Valor em milímetros.</returns>
        public static double FeetToMm(double feet)
        {
            return feet * FeetToMmFactor;
        }

        /// <summary>
        /// Converte milímetros para metros.
        /// </summary>
        public static double MmToMeters(double mm)
        {
            return mm / 1000.0;
        }

        /// <summary>
        /// Converte metros para milímetros.
        /// </summary>
        public static double MetersToMm(double meters)
        {
            return meters * 1000.0;
        }

        /// <summary>
        /// Converte inclinação em porcentagem para valor de slope do Revit.
        /// No Revit, slope é definido como rise/run (sem unidade).
        /// Exemplo: 2% = 0.02 (2cm de queda a cada 100cm de distância horizontal).
        /// </summary>
        /// <param name="slopePercent">Inclinação em porcentagem.</param>
        /// <returns>Slope como fração decimal.</returns>
        public static double SlopePercentToRevitSlope(double slopePercent)
        {
            return slopePercent / 100.0;
        }

        /// <summary>
        /// Converte um XYZ de pés para milímetros (todas as coordenadas).
        /// </summary>
        public static XYZ ConvertPointToMm(XYZ pointInFeet)
        {
            return new XYZ(
                FeetToMm(pointInFeet.X),
                FeetToMm(pointInFeet.Y),
                FeetToMm(pointInFeet.Z));
        }

        /// <summary>
        /// Converte um XYZ de milímetros para pés (todas as coordenadas).
        /// </summary>
        public static XYZ ConvertPointToFeet(XYZ pointInMm)
        {
            return new XYZ(
                MmToFeet(pointInMm.X),
                MmToFeet(pointInMm.Y),
                MmToFeet(pointInMm.Z));
        }

        /// <summary>
        /// Converte pés quadrados para metros quadrados.
        /// Usado para converter áreas de Rooms (Revit usa sq ft internamente).
        /// 1 sq ft = 0.092903 sq m
        /// </summary>
        public static double SqFeetToSqM(double sqFeet)
        {
            return sqFeet * 0.092903;
        }

        /// <summary>
        /// Converte pés para metros.
        /// </summary>
        public static double FeetToMeters(double feet)
        {
            return feet * 0.3048;
        }
    }
}
