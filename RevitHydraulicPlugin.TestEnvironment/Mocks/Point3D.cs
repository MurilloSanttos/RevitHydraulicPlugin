using System;

namespace RevitHydraulicPlugin.TestEnvironment.Mocks
{
    /// <summary>
    /// Substituto do Autodesk.Revit.DB.XYZ para o ambiente de testes.
    /// Representa um ponto ou vetor no espaço 3D com coordenadas em milímetros.
    /// 
    /// No Revit real, XYZ usa pés (feet) como unidade interna.
    /// Aqui usamos milímetros para simplificar a leitura dos testes.
    /// </summary>
    public class Point3D
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Calcula a distância horizontal (no plano XY) até outro ponto.
        /// </summary>
        public double HorizontalDistanceTo(Point3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Calcula a distância 3D total até outro ponto.
        /// </summary>
        public double DistanceTo(Point3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Retorna um novo ponto com offset aplicado.
        /// </summary>
        public Point3D Offset(double dx, double dy, double dz)
        {
            return new Point3D(X + dx, Y + dy, Z + dz);
        }

        /// <summary>
        /// Retorna um novo ponto com Z alterado (projeção em elevação).
        /// </summary>
        public Point3D AtElevation(double z)
        {
            return new Point3D(X, Y, z);
        }

        public override string ToString()
        {
            return $"({X:F1}, {Y:F1}, {Z:F1})";
        }

        public override bool Equals(object obj)
        {
            if (obj is Point3D other)
            {
                return Math.Abs(X - other.X) < 0.001
                    && Math.Abs(Y - other.Y) < 0.001
                    && Math.Abs(Z - other.Z) < 0.001;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }
    }
}
