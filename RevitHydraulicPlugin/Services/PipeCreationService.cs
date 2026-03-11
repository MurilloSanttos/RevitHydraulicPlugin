using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using RevitHydraulicPlugin.Models;
using RevitHydraulicPlugin.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.Services
{
    /// <summary>
    /// Serviço responsável pela criação de tubulações (Pipes) no modelo Revit.
    /// Encapsula todas as chamadas à Revit API para criação de elementos MEP.
    /// 
    /// NOTA: Este serviço NÃO gerencia Transactions — a Transaction deve ser
    /// aberta pelo chamador (Command ou PipelineService) para permitir
    /// controle de commit/rollback no nível correto.
    /// </summary>
    public class PipeCreationService
    {
        private readonly Document _document;

        public PipeCreationService(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// Cria os Pipes de uma coluna hidráulica vertical no modelo Revit.
        /// Cria um segmento de Pipe para cada par de níveis adjacentes.
        /// </summary>
        /// <param name="column">Definição da coluna a criar.</param>
        /// <returns>Lista de ElementIds dos Pipes criados.</returns>
        public List<ElementId> CreateColumnPipes(HydraulicColumn column)
        {
            Logger.Info($"Criando pipes para coluna {column.ColumnId}...");

            var createdIds = new List<ElementId>();

            try
            {
                // Obtém PipeType e PipingSystemType
                var pipeType = GetDefaultPipeType();
                var systemType = GetPipingSystemType(
                    column.SystemType == ColumnSystemType.AguaFria
                        ? "Domestic Cold Water"
                        : "Sanitary");

                if (pipeType == null || systemType == null)
                {
                    Logger.Error($"PipeType ou SystemType não encontrado para coluna {column.ColumnId}");
                    return createdIds;
                }

                double diameterFeet = UnitConversionHelper.MmToFeet(column.DiameterMm);

                // Obtém níveis ordenados por elevação
                var levels = column.LevelIds
                    .Select(id => _document.GetElement(id) as Level)
                    .Where(l => l != null)
                    .OrderBy(l => l.Elevation)
                    .ToList();

                // Cria segmentos verticais entre cada par de níveis
                for (int i = 0; i < levels.Count - 1; i++)
                {
                    var bottomLevel = levels[i];
                    var topLevel = levels[i + 1];

                    XYZ startPoint = new XYZ(
                        column.BasePosition.X,
                        column.BasePosition.Y,
                        bottomLevel.Elevation);

                    XYZ endPoint = new XYZ(
                        column.BasePosition.X,
                        column.BasePosition.Y,
                        topLevel.Elevation);

                    var pipe = Pipe.Create(_document,
                        systemType.Id,
                        pipeType.Id,
                        bottomLevel.Id,
                        startPoint,
                        endPoint);

                    if (pipe != null)
                    {
                        // Define o diâmetro
                        SetPipeDiameter(pipe, diameterFeet);
                        createdIds.Add(pipe.Id);
                    }
                }

                column.CreatedPipeIds = createdIds;
                Logger.Info($"  ✓ Coluna {column.ColumnId}: {createdIds.Count} segmentos criados");
            }
            catch (Exception ex)
            {
                Logger.Error($"Erro ao criar pipes para coluna {column.ColumnId}", ex);
            }

            return createdIds;
        }

        /// <summary>
        /// Cria o Pipe de um ramal de conexão no modelo Revit.
        /// </summary>
        /// <param name="branch">Definição do ramal a criar.</param>
        /// <returns>Lista de ElementIds dos Pipes criados.</returns>
        public List<ElementId> CreateBranchPipes(BranchConnection branch)
        {
            Logger.Info($"Criando pipe para ramal: {branch}...");

            var createdIds = new List<ElementId>();

            try
            {
                var pipeType = GetDefaultPipeType();
                var systemType = GetPipingSystemType(branch.PipeSpec.SystemTypeName);

                if (pipeType == null || systemType == null)
                {
                    Logger.Error($"PipeType ou SystemType não encontrado para ramal");
                    return createdIds;
                }

                // Obtém o nível mais próximo do equipamento
                var level = GetNearestLevel(branch.StartPoint.Z);
                if (level == null)
                {
                    Logger.Error("Nível não encontrado para o ramal");
                    return createdIds;
                }

                double diameterFeet = UnitConversionHelper.MmToFeet(branch.PipeSpec.DiameterMm);

                // Cria o pipe do ramal
                var pipe = Pipe.Create(_document,
                    systemType.Id,
                    pipeType.Id,
                    level.Id,
                    branch.StartPoint,
                    branch.EndPoint);

                if (pipe != null)
                {
                    SetPipeDiameter(pipe, diameterFeet);

                    // Aplica inclinação se for ramal de esgoto
                    if (branch.PipeSpec.SlopePercent > 0)
                    {
                        SetPipeSlope(pipe, branch.PipeSpec.SlopePercent);
                    }

                    createdIds.Add(pipe.Id);
                }

                branch.CreatedPipeIds = createdIds;
                Logger.Info($"  ✓ Ramal criado com sucesso");
            }
            catch (Exception ex)
            {
                Logger.Error($"Erro ao criar pipe para ramal", ex);
            }

            return createdIds;
        }

        /// <summary>
        /// Obtém o PipeType padrão do documento.
        /// </summary>
        private PipeType GetDefaultPipeType()
        {
            return new FilteredElementCollector(_document)
                .OfClass(typeof(PipeType))
                .Cast<PipeType>()
                .FirstOrDefault();
        }

        /// <summary>
        /// Busca um PipingSystemType por nome (ex: "Domestic Cold Water", "Sanitary").
        /// </summary>
        private PipingSystemType GetPipingSystemType(string systemName)
        {
            return new FilteredElementCollector(_document)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .FirstOrDefault(s => s.Name.Contains(systemName)
                    || s.Name.Equals(systemName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Define o diâmetro de um Pipe.
        /// </summary>
        private void SetPipeDiameter(Pipe pipe, double diameterFeet)
        {
            try
            {
                var diamParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (diamParam != null && !diamParam.IsReadOnly)
                {
                    diamParam.Set(diameterFeet);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Não foi possível definir diâmetro do Pipe: {ex.Message}");
            }
        }

        /// <summary>
        /// Define a inclinação (slope) de um Pipe.
        /// </summary>
        private void SetPipeSlope(Pipe pipe, double slopePercent)
        {
            try
            {
                var slopeParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_SLOPE);
                if (slopeParam != null && !slopeParam.IsReadOnly)
                {
                    double slope = UnitConversionHelper.SlopePercentToRevitSlope(slopePercent);
                    slopeParam.Set(slope);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Não foi possível definir inclinação do Pipe: {ex.Message}");
            }
        }

        /// <summary>
        /// Encontra o nível mais próximo de uma elevação Z.
        /// </summary>
        private Level GetNearestLevel(double elevation)
        {
            return new FilteredElementCollector(_document)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => System.Math.Abs(l.Elevation - elevation))
                .FirstOrDefault();
        }
    }
}
