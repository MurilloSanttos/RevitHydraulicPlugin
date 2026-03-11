using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitHydraulicPlugin.Models
{
    /// <summary>
    /// Representa uma coluna hidráulica vertical no projeto.
    /// Colunas atravessam múltiplos níveis, servindo como tronco principal
    /// para conexão dos ramais de cada pavimento.
    /// </summary>
    public class HydraulicColumn
    {
        /// <summary>
        /// Identificador único da coluna (para referência interna).
        /// </summary>
        public string ColumnId { get; set; }

        /// <summary>
        /// Tipo de sistema da coluna.
        /// </summary>
        public ColumnSystemType SystemType { get; set; }

        /// <summary>
        /// Posição XY da coluna no plano horizontal.
        /// A coordenada Z varia conforme os níveis.
        /// </summary>
        public XYZ BasePosition { get; set; }

        /// <summary>
        /// Lista dos ElementIds dos níveis (Levels) que a coluna atravessa.
        /// Ordenados do mais baixo ao mais alto.
        /// </summary>
        public List<ElementId> LevelIds { get; set; } = new List<ElementId>();

        /// <summary>
        /// Diâmetro da coluna em milímetros.
        /// </summary>
        public double DiameterMm { get; set; }

        /// <summary>
        /// ElementIds dos Pipes criados no modelo Revit para esta coluna.
        /// Populado após a criação no modelo.
        /// </summary>
        public List<ElementId> CreatedPipeIds { get; set; } = new List<ElementId>();

        /// <summary>
        /// Referência ao ambiente hidráulico associado (para posicionamento).
        /// </summary>
        public ElementId AssociatedRoomId { get; set; }

        public override string ToString()
        {
            return $"Coluna {ColumnId} ({SystemType}) - Diâmetro: {DiameterMm}mm - Níveis: {LevelIds.Count}";
        }
    }

    /// <summary>
    /// Tipos de sistema para colunas hidráulicas.
    /// </summary>
    public enum ColumnSystemType
    {
        /// <summary>Coluna de água fria.</summary>
        AguaFria,

        /// <summary>Coluna de esgoto sanitário.</summary>
        Esgoto,

        /// <summary>Coluna de água quente (futura expansão).</summary>
        AguaQuente,

        /// <summary>Coluna de águas pluviais (futura expansão).</summary>
        Pluvial
    }
}
