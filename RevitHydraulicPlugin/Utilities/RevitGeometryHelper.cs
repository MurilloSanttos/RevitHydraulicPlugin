using Autodesk.Revit.DB;

namespace RevitHydraulicPlugin.Utilities
{
    /// <summary>
    /// Funções auxiliares para cálculos geométricos com elementos do Revit.
    /// </summary>
    public static class RevitGeometryHelper
    {
        /// <summary>
        /// Calcula o ponto central (centróide) de um BoundingBoxXYZ.
        /// Utilizado para encontrar o centro aproximado de um Room.
        /// </summary>
        /// <param name="bbox">BoundingBox do elemento.</param>
        /// <returns>Ponto XYZ central, ou null se o BoundingBox for inválido.</returns>
        public static XYZ GetCentroid(BoundingBoxXYZ bbox)
        {
            if (bbox == null) return null;

            return new XYZ(
                (bbox.Min.X + bbox.Max.X) / 2.0,
                (bbox.Min.Y + bbox.Max.Y) / 2.0,
                (bbox.Min.Z + bbox.Max.Z) / 2.0);
        }

        /// <summary>
        /// Calcula o centróide no plano horizontal (XY), usando Z do ponto mínimo.
        /// Útil para posicionamento de colunas, que precisam da posição XY
        /// mas com Z no nível do piso.
        /// </summary>
        public static XYZ GetHorizontalCentroid(BoundingBoxXYZ bbox)
        {
            if (bbox == null) return null;

            return new XYZ(
                (bbox.Min.X + bbox.Max.X) / 2.0,
                (bbox.Min.Y + bbox.Max.Y) / 2.0,
                bbox.Min.Z);
        }

        /// <summary>
        /// Calcula a distância horizontal (no plano XY) entre dois pontos.
        /// Ignora a coordenada Z.
        /// </summary>
        public static double HorizontalDistance(XYZ point1, XYZ point2)
        {
            double dx = point1.X - point2.X;
            double dy = point1.Y - point2.Y;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Projeta um ponto no plano horizontal com um valor Z específico.
        /// </summary>
        public static XYZ ProjectToElevation(XYZ point, double z)
        {
            return new XYZ(point.X, point.Y, z);
        }

        /// <summary>
        /// Verifica se um ponto está dentro de um BoundingBox (no plano XY).
        /// </summary>
        public static bool IsPointInsideBBoxXY(XYZ point, BoundingBoxXYZ bbox)
        {
            if (point == null || bbox == null) return false;

            return point.X >= bbox.Min.X && point.X <= bbox.Max.X
                && point.Y >= bbox.Min.Y && point.Y <= bbox.Max.Y;
        }

        /// <summary>
        /// Cria um ponto com offset a partir de um ponto base.
        /// Offset em pés (unidade interna do Revit).
        /// </summary>
        public static XYZ OffsetPoint(XYZ basePoint, double dx, double dy, double dz)
        {
            return new XYZ(basePoint.X + dx, basePoint.Y + dy, basePoint.Z + dz);
        }
    }
}
