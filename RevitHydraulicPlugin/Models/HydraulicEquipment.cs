using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitHydraulicPlugin.Models
{
    /// <summary>
    /// Representa um equipamento hidráulico detectado no modelo Revit.
    /// Armazena informações essenciais para roteamento de tubulações.
    /// </summary>
    public class HydraulicEquipment
    {
        /// <summary>
        /// ElementId do equipamento (FamilyInstance) no modelo Revit.
        /// </summary>
        public ElementId ElementId { get; set; }

        /// <summary>
        /// Nome da família do equipamento (ex: "Vaso Sanitário Padrão").
        /// </summary>
        public string FamilyName { get; set; }

        /// <summary>
        /// Nome do tipo da família (ex: "Tipo A - 6L").
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Classificação do tipo de equipamento hidráulico.
        /// </summary>
        public EquipmentType Type { get; set; }

        /// <summary>
        /// Posição do equipamento no espaço 3D (ponto de inserção).
        /// Unidade: pés (unidade interna do Revit).
        /// </summary>
        public XYZ Position { get; set; }

        /// <summary>
        /// ElementId do Room em que o equipamento está inserido.
        /// </summary>
        public ElementId RoomId { get; set; }

        /// <summary>
        /// ElementId do nível (Level) do equipamento.
        /// </summary>
        public ElementId LevelId { get; set; }

        /// <summary>
        /// Lista de informações dos conectores hidráulicos disponíveis no equipamento.
        /// </summary>
        public List<ConnectorInfo> Connectors { get; set; } = new List<ConnectorInfo>();

        /// <summary>
        /// Especificação de tubulação recomendada para este equipamento.
        /// Obtida a partir de HydraulicRules.
        /// </summary>
        public PipeSpecification PipeSpec { get; set; }

        public override string ToString()
        {
            return $"{Type} - {FamilyName} ({TypeName}) em ({Position?.X:F2}, {Position?.Y:F2}, {Position?.Z:F2})";
        }
    }

    /// <summary>
    /// Tipos de equipamentos hidráulicos reconhecidos pelo plugin.
    /// </summary>
    public enum EquipmentType
    {
        VasoSanitario,
        Lavatorio,
        Chuveiro,
        Pia,
        Tanque,
        Ralo,
        MaquinaLavar,
        Outro
    }

    /// <summary>
    /// Informações simplificadas de um conector MEP de um equipamento.
    /// </summary>
    public class ConnectorInfo
    {
        /// <summary>
        /// Ponto de origem do conector no espaço 3D.
        /// </summary>
        public XYZ Origin { get; set; }

        /// <summary>
        /// Domínio do conector (Piping, HVAC, Electrical, etc.).
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Tipo de sistema (SupplyHydronicReturn, DomesticColdWater, etc.).
        /// </summary>
        public string SystemType { get; set; }

        /// <summary>
        /// Diâmetro nominal do conector em milímetros.
        /// </summary>
        public double DiameterMm { get; set; }
    }
}
