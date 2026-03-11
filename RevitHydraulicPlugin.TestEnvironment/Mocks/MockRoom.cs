using System.Collections.Generic;

namespace RevitHydraulicPlugin.TestEnvironment.Mocks
{
    /// <summary>
    /// Simula um Room (ambiente) do Revit.
    /// No Revit, Rooms são elementos que representam espaços fechados
    /// delimitados por paredes, com propriedades como nome, número e área.
    /// </summary>
    public class MockRoom
    {
        /// <summary>
        /// Identificador único (simula ElementId).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome do ambiente (ex: "Banheiro", "Cozinha").
        /// Corresponde ao parâmetro ROOM_NAME no Revit.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Número do ambiente (ex: "101", "102").
        /// Corresponde ao parâmetro ROOM_NUMBER no Revit.
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// Área do ambiente em metros quadrados.
        /// No Revit real seria em pés quadrados (internal units).
        /// </summary>
        public double AreaM2 { get; set; }

        /// <summary>
        /// Nível em que o Room se encontra.
        /// </summary>
        public MockLevel Level { get; set; }

        /// <summary>
        /// Ponto central do ambiente (centróide aproximado) em milímetros.
        /// </summary>
        public Point3D CenterPoint { get; set; }

        /// <summary>
        /// Ponto mínimo do BoundingBox (canto inferior esquerdo) em milímetros.
        /// </summary>
        public Point3D BBoxMin { get; set; }

        /// <summary>
        /// Ponto máximo do BoundingBox (canto superior direito) em milímetros.
        /// </summary>
        public Point3D BBoxMax { get; set; }

        /// <summary>
        /// Equipamentos hidráulicos dentro deste ambiente.
        /// </summary>
        public List<MockFixture> Fixtures { get; set; } = new List<MockFixture>();

        public override string ToString()
        {
            return $"Room '{Name}' nº{Number} ({AreaM2:F1}m²) - Nível: {Level?.Name} - Equipamentos: {Fixtures.Count}";
        }
    }
}
