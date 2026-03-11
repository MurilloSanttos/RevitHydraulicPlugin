using Autodesk.Revit.DB;
using RevitHydraulicPlugin.Detection;
using RevitHydraulicPlugin.Models;
using RevitHydraulicPlugin.Routing;
using RevitHydraulicPlugin.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RevitHydraulicPlugin.Services
{
    /// <summary>
    /// Serviço central que orquestra o pipeline completo de automação hidráulica.
    /// Coordena a execução sequencial de todas as etapas:
    /// 
    /// 1. Detecção de ambientes hidráulicos
    /// 2. Identificação de equipamentos
    /// 3. Cálculo de colunas hidráulicas
    /// 4. Cálculo de ramais de conexão
    /// 5. Criação de tubulações no modelo
    /// 
    /// Cada etapa pode ser executada independentemente ou como parte do pipeline completo.
    /// </summary>
    public class HydraulicPipelineService
    {
        private readonly Document _document;

        // Serviços injetados (composição)
        private readonly RoomDetectionService _roomDetection;
        private readonly EquipmentDetectionService _equipmentDetection;
        private readonly ColumnRoutingService _columnRouting;
        private readonly BranchRoutingService _branchRouting;
        private readonly PipeCreationService _pipeCreation;

        // Estado intermediário do pipeline
        private List<HydraulicRoom> _detectedRooms;
        private List<HydraulicEquipment> _detectedEquipment;
        private List<HydraulicColumn> _calculatedColumns;
        private List<BranchConnection> _calculatedBranches;

        /// <summary>
        /// Inicializa o pipeline com todos os serviços necessários.
        /// </summary>
        public HydraulicPipelineService(Document document)
        {
            _document = document;

            _roomDetection = new RoomDetectionService(document);
            _equipmentDetection = new EquipmentDetectionService(document);
            _columnRouting = new ColumnRoutingService(document);
            _branchRouting = new BranchRoutingService(document);
            _pipeCreation = new PipeCreationService(document);
        }

        /// <summary>
        /// Executa o pipeline completo de automação hidráulica.
        /// Todas as modificações no modelo são feitas dentro de uma única Transaction.
        /// </summary>
        /// <returns>Resultado resumido da execução.</returns>
        public PipelineResult RunFullPipeline()
        {
            Logger.Info("========================================");
            Logger.Info("INICIANDO PIPELINE HIDRÁULICO COMPLETO");
            Logger.Info("========================================");

            var result = new PipelineResult();

            // Etapa 1: Detecção (somente leitura, sem Transaction)
            _detectedRooms = _roomDetection.DetectHydraulicRooms();
            result.RoomsDetected = _detectedRooms.Count;

            if (_detectedRooms.Count == 0)
            {
                result.Success = false;
                result.Message = "Nenhum ambiente hidráulico detectado no modelo.";
                Logger.Warning(result.Message);
                return result;
            }

            // Etapa 2: Identificação (somente leitura, sem Transaction)
            _detectedEquipment = _equipmentDetection.DetectEquipment(_detectedRooms);
            result.EquipmentDetected = _detectedEquipment.Count;

            if (_detectedEquipment.Count == 0)
            {
                result.Success = false;
                result.Message = "Nenhum equipamento hidráulico detectado nos ambientes.";
                Logger.Warning(result.Message);
                return result;
            }

            // Etapa 3: Cálculo de colunas (sem Transaction)
            _calculatedColumns = _columnRouting.CalculateColumns(_detectedRooms);
            result.ColumnsCalculated = _calculatedColumns.Count;

            // Etapa 4: Cálculo de ramais (sem Transaction)
            _calculatedBranches = _branchRouting.CalculateBranches(_detectedRooms, _calculatedColumns);
            result.BranchesCalculated = _calculatedBranches.Count;

            // Etapa 5: Criação no modelo (REQUER Transaction)
            using (var transaction = new Transaction(_document, "Criar Instalações Hidráulicas"))
            {
                transaction.Start();

                try
                {
                    // Criar colunas
                    foreach (var column in _calculatedColumns)
                    {
                        _pipeCreation.CreateColumnPipes(column);
                    }
                    result.ColumnsCreated = _calculatedColumns
                        .Sum(c => c.CreatedPipeIds.Count);

                    // Criar ramais
                    foreach (var branch in _calculatedBranches)
                    {
                        _pipeCreation.CreateBranchPipes(branch);
                    }
                    result.BranchesCreated = _calculatedBranches
                        .Sum(b => b.CreatedPipeIds.Count);

                    transaction.Commit();
                    result.Success = true;
                    result.Message = "Pipeline executado com sucesso!";
                }
                catch (System.Exception ex)
                {
                    transaction.RollBack();
                    result.Success = false;
                    result.Message = $"Erro durante a criação: {ex.Message}";
                    Logger.Error("Erro no pipeline - Transaction rolled back", ex);
                }
            }

            Logger.Info("========================================");
            Logger.Info($"PIPELINE FINALIZADO: {result.Message}");
            Logger.Info("========================================");

            return result;
        }

        /// <summary>
        /// Executa apenas a etapa de detecção de ambientes.
        /// </summary>
        public List<HydraulicRoom> RunDetection()
        {
            _detectedRooms = _roomDetection.DetectHydraulicRooms();
            return _detectedRooms;
        }

        /// <summary>
        /// Executa apenas a etapa de identificação de equipamentos.
        /// Requer que RunDetection tenha sido executado antes.
        /// </summary>
        public List<HydraulicEquipment> RunEquipmentIdentification()
        {
            if (_detectedRooms == null || _detectedRooms.Count == 0)
            {
                _detectedRooms = _roomDetection.DetectHydraulicRooms();
            }

            _detectedEquipment = _equipmentDetection.DetectEquipment(_detectedRooms);
            return _detectedEquipment;
        }

        /// <summary>
        /// Retorna os Rooms detectados (acesso ao estado intermediário).
        /// </summary>
        public List<HydraulicRoom> GetDetectedRooms() => _detectedRooms;

        /// <summary>
        /// Retorna os equipamentos detectados (acesso ao estado intermediário).
        /// </summary>
        public List<HydraulicEquipment> GetDetectedEquipment() => _detectedEquipment;

        /// <summary>
        /// Retorna as colunas calculadas (acesso ao estado intermediário).
        /// </summary>
        public List<HydraulicColumn> GetCalculatedColumns() => _calculatedColumns;

        /// <summary>
        /// Retorna os ramais calculados (acesso ao estado intermediário).
        /// </summary>
        public List<BranchConnection> GetCalculatedBranches() => _calculatedBranches;
    }

    /// <summary>
    /// Resultado da execução do pipeline hidráulico.
    /// Contém contadores e status para relatório ao usuário.
    /// </summary>
    public class PipelineResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int RoomsDetected { get; set; }
        public int EquipmentDetected { get; set; }
        public int ColumnsCalculated { get; set; }
        public int BranchesCalculated { get; set; }
        public int ColumnsCreated { get; set; }
        public int BranchesCreated { get; set; }

        /// <summary>
        /// Gera um relatório formatado para exibição ao usuário.
        /// </summary>
        public string ToReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("  RELATÓRIO - AUTOMAÇÃO HIDRÁULICA");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"  Status: {(Success ? "✓ SUCESSO" : "✗ FALHA")}");
            sb.AppendLine($"  Mensagem: {Message}");
            sb.AppendLine();
            sb.AppendLine("  DETECÇÃO:");
            sb.AppendLine($"    Ambientes detectados: {RoomsDetected}");
            sb.AppendLine($"    Equipamentos detectados: {EquipmentDetected}");
            sb.AppendLine();
            sb.AppendLine("  CRIAÇÃO:");
            sb.AppendLine($"    Colunas calculadas: {ColumnsCalculated}");
            sb.AppendLine($"    Segmentos de coluna criados: {ColumnsCreated}");
            sb.AppendLine($"    Ramais calculados: {BranchesCalculated}");
            sb.AppendLine($"    Ramais criados: {BranchesCreated}");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            return sb.ToString();
        }
    }
}
