using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitHydraulicPlugin.Models
{
    /// <summary>
    /// Representa um ambiente hidráulico detectado no modelo Revit.
    /// Encapsula informações do Room relevantes para o projeto hidráulico.
    /// </summary>
    public class HydraulicRoom
    {
        /// <summary>
        /// ElementId do Room no modelo Revit.
        /// </summary>
        public ElementId RoomId { get; set; }

        /// <summary>
        /// Nome do ambiente (ex: "Banheiro", "Cozinha").
        /// </summary>
        public string RoomName { get; set; }

        /// <summary>
        /// Número do ambiente, se definido no modelo.
        /// </summary>
        public string RoomNumber { get; set; }

        /// <summary>
        /// Nível (Level) em que o Room se encontra.
        /// </summary>
        public ElementId LevelId { get; set; }

        /// <summary>
        /// Nome do nível para exibição.
        /// </summary>
        public string LevelName { get; set; }

        /// <summary>
        /// Ponto central aproximado do ambiente (centróide do BoundingBox).
        /// Usado como referência para posicionamento de colunas.
        /// </summary>
        public XYZ CenterPoint { get; set; }

        /// <summary>
        /// BoundingBox do ambiente para cálculos geométricos.
        /// </summary>
        public BoundingBoxXYZ BoundingBox { get; set; }

        /// <summary>
        /// Lista de equipamentos hidráulicos encontrados neste ambiente.
        /// Populada pela etapa de identificação de equipamentos.
        /// </summary>
        public List<HydraulicEquipment> Equipment { get; set; } = new List<HydraulicEquipment>();

        /// <summary>
        /// Classificação do tipo de ambiente hidráulico.
        /// </summary>
        public RoomType Type { get; set; }

        public override string ToString()
        {
            return $"{RoomName} ({Type}) - Nível: {LevelName} - Equipamentos: {Equipment.Count}";
        }
    }

    /// <summary>
    /// Tipos de ambientes hidráulicos reconhecidos pelo plugin.
    /// </summary>
    public enum RoomType
    {
        Banheiro,
        Lavabo,
        Cozinha,
        AreaDeServico,
        Lavanderia,
        Outro
    }
}
