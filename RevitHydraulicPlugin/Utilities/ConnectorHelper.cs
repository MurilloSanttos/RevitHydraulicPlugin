using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using RevitHydraulicPlugin.Models;
using System.Collections.Generic;

namespace RevitHydraulicPlugin.Utilities
{
    /// <summary>
    /// Funções auxiliares para trabalhar com conectores MEP (Mechanical/Electrical/Plumbing)
    /// de FamilyInstances no Revit.
    /// </summary>
    public static class ConnectorHelper
    {
        /// <summary>
        /// Extrai todos os conectores de um FamilyInstance e retorna como lista de ConnectorInfo.
        /// Filtra apenas os conectores do domínio de tubulação (Piping).
        /// </summary>
        /// <param name="familyInstance">Instância da família a inspecionar.</param>
        /// <returns>Lista de ConnectorInfo com informações dos conectores de tubulação.</returns>
        public static List<ConnectorInfo> GetPipingConnectors(FamilyInstance familyInstance)
        {
            var connectors = new List<ConnectorInfo>();

            if (familyInstance == null) return connectors;

            // Obtém o ConnectorManager do MEP
            var connectorManager = familyInstance.MEPModel?.ConnectorManager;
            if (connectorManager == null) return connectors;

            foreach (Connector connector in connectorManager.Connectors)
            {
                // Filtra apenas conectores de tubulação
                if (connector.Domain != Domain.DomainPiping) continue;

                var connectorInfo = new ConnectorInfo
                {
                    Origin = connector.Origin,
                    Domain = connector.Domain.ToString(),
                    DiameterMm = UnitConversionHelper.FeetToMm(connector.Radius * 2)
                };

                // Tenta obter o tipo de sistema do conector
                try
                {
                    connectorInfo.SystemType = connector.PipeSystemType.ToString();
                }
                catch
                {
                    connectorInfo.SystemType = "Unknown";
                }

                connectors.Add(connectorInfo);
            }

            return connectors;
        }

        /// <summary>
        /// Verifica se um FamilyInstance possui conectores de tubulação.
        /// Usado como filtro rápido para identificar equipamentos MEP.
        /// </summary>
        public static bool HasPipingConnectors(FamilyInstance familyInstance)
        {
            if (familyInstance?.MEPModel?.ConnectorManager == null) return false;

            foreach (Connector connector in familyInstance.MEPModel.ConnectorManager.Connectors)
            {
                if (connector.Domain == Domain.DomainPiping)
                    return true;
            }

            return false;
        }
    }
}
