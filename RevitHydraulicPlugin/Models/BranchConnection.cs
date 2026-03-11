using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitHydraulicPlugin.Models
{
    /// <summary>
    /// Representa uma conexão de ramal entre um equipamento hidráulico e uma coluna.
    /// Contém todas as informações necessárias para criar a tubulação no modelo Revit.
    /// </summary>
    public class BranchConnection
    {
        /// <summary>
        /// Equipamento de origem da conexão.
        /// </summary>
        public HydraulicEquipment Equipment { get; set; }

        /// <summary>
        /// Coluna hidráulica de destino.
        /// </summary>
        public HydraulicColumn TargetColumn { get; set; }

        /// <summary>
        /// Ponto de partida do ramal (saída do equipamento).
        /// </summary>
        public XYZ StartPoint { get; set; }

        /// <summary>
        /// Ponto de chegada do ramal (entrada na coluna).
        /// </summary>
        public XYZ EndPoint { get; set; }

        /// <summary>
        /// Pontos intermediários da rota, se houver desvios.
        /// </summary>
        public List<XYZ> IntermediatePoints { get; set; } = new List<XYZ>();

        /// <summary>
        /// Especificação da tubulação para este ramal.
        /// </summary>
        public PipeSpecification PipeSpec { get; set; }

        /// <summary>
        /// ElementIds dos Pipes criados no modelo Revit para este ramal.
        /// Populado após a criação no modelo.
        /// </summary>
        public List<ElementId> CreatedPipeIds { get; set; } = new List<ElementId>();

        public override string ToString()
        {
            return $"Ramal: {Equipment?.Type} → Coluna {TargetColumn?.ColumnId} " +
                   $"({PipeSpec?.DiameterMm}mm, inclinação {PipeSpec?.SlopePercent}%)";
        }
    }
}
