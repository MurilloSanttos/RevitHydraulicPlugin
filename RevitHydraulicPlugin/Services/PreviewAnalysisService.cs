using Autodesk.Revit.DB;
using RevitHydraulicPlugin.Detection;
using RevitHydraulicPlugin.Models;
using RevitHydraulicPlugin.Routing;
using RevitHydraulicPlugin.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RevitHydraulicPlugin.Services
{
    /// <summary>
    /// Serviço de análise para modo Preview.
    /// 
    /// Executa todas as etapas de detecção e cálculo SEM modificar o modelo.
    /// Produz um PreviewResult com métricas, contagens e avisos para o usuário.
    /// 
    /// Reutiliza os mesmos serviços do pipeline principal para garantir
    /// que o preview reflita exatamente o que será gerado.
    /// </summary>
    public class PreviewAnalysisService
    {
        private readonly Document _document;

        // Serviços de análise (somente leitura)
        private readonly RoomDetectionService _roomDetection;
        private readonly EquipmentDetectionService _equipmentDetection;
        private readonly ColumnRoutingService _columnRouting;
        private readonly BranchRoutingService _branchRouting;

        // Estado intermediário (preservado para uso posterior)
        private List<HydraulicRoom> _detectedRooms;
        private List<HydraulicEquipment> _detectedEquipment;
        private List<HydraulicColumn> _calculatedColumns;
        private List<BranchConnection> _calculatedBranches;

        public PreviewAnalysisService(Document document)
        {
            _document = document;

            _roomDetection = new RoomDetectionService(document);
            _equipmentDetection = new EquipmentDetectionService(document);
            _columnRouting = new ColumnRoutingService(document);
            _branchRouting = new BranchRoutingService(document);
        }

        // ════════════════════════════════════════════════
        //  ANÁLISE PRINCIPAL
        // ════════════════════════════════════════════════

        /// <summary>
        /// Executa a análise completa do modelo e retorna o resultado.
        /// NÃO modifica o modelo Revit (nenhuma Transaction é aberta).
        /// </summary>
        public PreviewResult RunAnalysis()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new PreviewResult();

            Logger.Info("[PREVIEW] Iniciando analise de preview...");

            try
            {
                // ── Etapa 1: Detecção de ambientes ──
                using (Logger.MeasureTime("Preview - Deteccao de Ambientes"))
                {
                    _detectedRooms = _roomDetection.DetectHydraulicRooms();
                }

                // Contar total de rooms no modelo
                result.TotalRoomsAnalyzed = CountAllRooms();
                result.HydraulicRoomsDetected = _detectedRooms.Count;

                Logger.Info($"[PREVIEW] Rooms analisados: {result.TotalRoomsAnalyzed}");
                Logger.Info($"[PREVIEW] Rooms hidraulicos: {result.HydraulicRoomsDetected}");

                if (_detectedRooms.Count == 0)
                {
                    result.AnalysisSuccessful = true;
                    result.Warnings.Add("Nenhum ambiente hidraulico detectado no modelo.");
                    Logger.Warning("[PREVIEW] Nenhum ambiente hidraulico detectado.");
                    stopwatch.Stop();
                    result.AnalysisTimeMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                // Contagem por tipo de room
                foreach (var room in _detectedRooms)
                {
                    string typeName = room.Type.ToString();
                    if (result.RoomTypeCounts.ContainsKey(typeName))
                        result.RoomTypeCounts[typeName]++;
                    else
                        result.RoomTypeCounts[typeName] = 1;
                }

                // ── Etapa 2: Detecção de equipamentos ──
                using (Logger.MeasureTime("Preview - Deteccao de Equipamentos"))
                {
                    _detectedEquipment = _equipmentDetection.DetectEquipment(_detectedRooms);
                }

                result.FixturesDetected = _detectedEquipment.Count;
                Logger.Info($"[PREVIEW] Fixtures detectados: {result.FixturesDetected}");

                // Contagem por tipo de fixture
                foreach (var equip in _detectedEquipment)
                {
                    string typeName = TranslateEquipmentType(equip.Type);
                    if (result.FixtureTypeCounts.ContainsKey(typeName))
                        result.FixtureTypeCounts[typeName]++;
                    else
                        result.FixtureTypeCounts[typeName] = 1;
                }

                if (_detectedEquipment.Count == 0)
                {
                    result.Warnings.Add("Nenhum equipamento hidraulico detectado nos ambientes.");
                    Logger.Warning("[PREVIEW] Nenhum equipamento detectado.");
                }

                // ── Etapa 2.5: Reclassificação com fixtures ──
                _detectedRooms = _roomDetection.ReclassifyWithFixtures(_detectedRooms);

                // ── Etapa 3: Cálculo de colunas ──
                using (Logger.MeasureTime("Preview - Calculo de Colunas"))
                {
                    _calculatedColumns = _columnRouting.CalculateColumns(_detectedRooms);
                }

                result.ColumnsToGenerate = _calculatedColumns.Count;
                Logger.Info($"[PREVIEW] Colunas calculadas: {result.ColumnsToGenerate}");

                // ── Etapa 4: Cálculo de ramais ──
                using (Logger.MeasureTime("Preview - Calculo de Ramais"))
                {
                    _calculatedBranches = _branchRouting.CalculateBranches(_detectedRooms, _calculatedColumns);
                }

                result.BranchesToGenerate = _calculatedBranches.Count;
                Logger.Info($"[PREVIEW] Ramais calculados: {result.BranchesToGenerate}");

                // ── Detalhes por room ──
                BuildRoomDetails(result);

                // ── Gerar avisos ──
                GenerateWarnings(result);

                result.AnalysisSuccessful = true;
                Logger.Info("[PREVIEW] Analise de preview concluida com sucesso.");
            }
            catch (System.Exception ex)
            {
                result.AnalysisSuccessful = false;
                result.ErrorMessage = ex.Message;
                Logger.Error("[PREVIEW] Erro durante analise de preview", ex);
            }

            stopwatch.Stop();
            result.AnalysisTimeMs = stopwatch.ElapsedMilliseconds;
            Logger.Info($"[PREVIEW] Tempo de analise: {result.AnalysisTimeMs}ms");

            return result;
        }

        // ════════════════════════════════════════════════
        //  ACESSO AO ESTADO (para GenerateCommand)
        // ════════════════════════════════════════════════

        /// <summary>
        /// Retorna os rooms detectados durante a análise.
        /// </summary>
        public List<HydraulicRoom> GetDetectedRooms() => _detectedRooms;

        /// <summary>
        /// Retorna os equipamentos detectados durante a análise.
        /// </summary>
        public List<HydraulicEquipment> GetDetectedEquipment() => _detectedEquipment;

        /// <summary>
        /// Retorna as colunas calculadas durante a análise.
        /// </summary>
        public List<HydraulicColumn> GetCalculatedColumns() => _calculatedColumns;

        /// <summary>
        /// Retorna os ramais calculados durante a análise.
        /// </summary>
        public List<BranchConnection> GetCalculatedBranches() => _calculatedBranches;

        // ════════════════════════════════════════════════
        //  MÉTODOS PRIVADOS
        // ════════════════════════════════════════════════

        /// <summary>
        /// Conta todos os Rooms do modelo (inclusive não-hidráulicos).
        /// </summary>
        private int CountAllRooms()
        {
            try
            {
                var collector = new FilteredElementCollector(_document)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType();

                return collector.GetElementCount();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Preenche os detalhes por room no resultado.
        /// </summary>
        private void BuildRoomDetails(PreviewResult result)
        {
            foreach (var room in _detectedRooms)
            {
                int fixtureCount = room.Equipment?.Count ?? 0;

                // Contar ramais cujo Equipment pertence a este room
                int branchCount = 0;
                if (_calculatedBranches != null)
                {
                    branchCount = _calculatedBranches
                        .Count(b => b.Equipment?.RoomId == room.RoomId);
                }

                result.HydraulicRooms.Add(new RoomPreviewInfo
                {
                    RoomName = room.RoomName,
                    RoomNumber = room.RoomNumber,
                    LevelName = room.LevelName,
                    ClassifiedType = room.Type.ToString(),
                    Confidence = room.ClassificationConfidence,
                    FixtureCount = fixtureCount,
                    BranchCount = branchCount
                });
            }
        }

        /// <summary>
        /// Gera avisos baseados nos resultados da análise.
        /// </summary>
        private void GenerateWarnings(PreviewResult result)
        {
            // Fixtures sem conectores válidos
            int fixturesWithoutConnectors = _detectedEquipment
                .Count(e => e.Connectors == null || e.Connectors.Count == 0);
            if (fixturesWithoutConnectors > 0)
            {
                result.Warnings.Add(
                    $"{fixturesWithoutConnectors} fixture(s) sem conectores validos.");
            }

            // Rooms classificados como Unknown
            int unknownRooms = _detectedRooms
                .Count(r => r.Type == RoomType.Unknown);
            if (unknownRooms > 0)
            {
                result.Warnings.Add(
                    $"{unknownRooms} room(s) classificado(s) como Unknown.");
            }

            // Rooms sem equipamentos
            int roomsWithoutEquipment = _detectedRooms
                .Count(r => r.Equipment == null || r.Equipment.Count == 0);
            if (roomsWithoutEquipment > 0)
            {
                result.Warnings.Add(
                    $"{roomsWithoutEquipment} room(s) hidraulico(s) sem equipamentos detectados.");
            }

            // Rooms com baixa confiança
            int lowConfidenceRooms = _detectedRooms
                .Count(r => r.ClassificationConfidence < 0.5 && r.ClassificationConfidence > 0);
            if (lowConfidenceRooms > 0)
            {
                result.Warnings.Add(
                    $"{lowConfidenceRooms} room(s) com confianca de classificacao baixa.");
            }

            // Nenhum ramal gerado
            if (result.FixturesDetected > 0 && result.BranchesToGenerate == 0)
            {
                result.Warnings.Add(
                    "Fixtures detectados mas nenhum ramal calculado. Verificar colunas.");
            }
        }

        /// <summary>
        /// Traduz EquipmentType para nomes legíveis para o preview.
        /// </summary>
        private string TranslateEquipmentType(EquipmentType type)
        {
            switch (type)
            {
                case EquipmentType.VasoSanitario: return "Toilets";
                case EquipmentType.Lavatorio: return "Sinks";
                case EquipmentType.Chuveiro: return "Showers";
                case EquipmentType.Pia: return "Kitchen Sinks";
                case EquipmentType.Tanque: return "Laundry Sinks";
                case EquipmentType.Ralo: return "Drains";
                case EquipmentType.MaquinaLavar: return "Washing Machines";
                default: return "Other";
            }
        }
    }
}
