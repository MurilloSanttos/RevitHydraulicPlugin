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

        /// <summary>
        /// Nível de confiança da classificação (0.0 a 1.0).
        /// Valores altos indicam forte evidência hidráulica.
        /// </summary>
        public double ClassificationConfidence { get; set; }

        /// <summary>
        /// Método usado para classificação (Name, Fixture, Combined).
        /// </summary>
        public string ClassificationMethod { get; set; } = "None";

        /// <summary>
        /// Área do ambiente em metros quadrados.
        /// </summary>
        public double AreaSqM { get; set; }

        public override string ToString()
        {
            return $"{RoomName} ({Type}) - Nível: {LevelName} - Equipamentos: {Equipment.Count}";
        }
    }

    /// <summary>
    /// Tipos de ambientes hidráulicos reconhecidos pelo plugin.
    /// 
    /// VERSÃO 2.0 — Expandido com tipos específicos para classificação
    /// mais precisa e regras diferenciadas por ambiente.
    /// </summary>
    public enum RoomType
    {
        /// <summary>Banheiro padrão (social, suíte simples)</summary>
        Bathroom,

        /// <summary>Lavabo (banheiro sem chuveiro, apenas vaso + lavatório)</summary>
        Lavatory,

        /// <summary>Cozinha</summary>
        Kitchen,

        /// <summary>Lavanderia</summary>
        Laundry,

        /// <summary>Área de serviço</summary>
        ServiceArea,

        /// <summary>Copa (cozinha pequena / espaço com pia)</summary>
        Pantry,

        /// <summary>Banheiro de suíte</summary>
        SuiteBathroom,

        /// <summary>Banheiro PCD (acessível)</summary>
        AccessibleBathroom,

        /// <summary>Ambiente não classificado ou sem evidência hidráulica</summary>
        Unknown,

        // ── Mapeamento legado (para compatibilidade com código existente) ──

        /// <summary>[Legado] Equivale a Bathroom</summary>
        Banheiro = Bathroom,

        /// <summary>[Legado] Equivale a Lavatory</summary>
        Lavabo = Lavatory,

        /// <summary>[Legado] Equivale a Kitchen</summary>
        Cozinha = Kitchen,

        /// <summary>[Legado] Equivale a ServiceArea</summary>
        AreaDeServico = ServiceArea,

        /// <summary>[Legado] Equivale a Laundry</summary>
        Lavanderia = Laundry,

        /// <summary>[Legado] Equivale a Unknown</summary>
        Outro = Unknown
    }
}
