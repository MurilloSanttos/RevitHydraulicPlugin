using System.Collections.Generic;

namespace RevitHydraulicPlugin.Models
{
    /// <summary>
    /// Resultado da análise de preview do plugin hidráulico.
    /// Contém todas as métricas e avisos sem modificar o modelo.
    /// 
    /// Gerado por PreviewAnalysisService.
    /// Formatado por PreviewFormatter.
    /// Exibido por PreviewCommand / GenerateCommand.
    /// </summary>
    public class PreviewResult
    {
        // ════════════════════════════════════════════════
        //  CONTADORES
        // ════════════════════════════════════════════════

        /// <summary>
        /// Total de Rooms analisados no modelo (hidráulicos + não-hidráulicos).
        /// </summary>
        public int TotalRoomsAnalyzed { get; set; }

        /// <summary>
        /// Total de Rooms classificados como hidráulicos.
        /// </summary>
        public int HydraulicRoomsDetected { get; set; }

        /// <summary>
        /// Total de fixtures (equipamentos) detectados nos rooms hidráulicos.
        /// </summary>
        public int FixturesDetected { get; set; }

        /// <summary>
        /// Quantidade de colunas que seriam geradas.
        /// </summary>
        public int ColumnsToGenerate { get; set; }

        /// <summary>
        /// Quantidade de ramais que seriam gerados.
        /// </summary>
        public int BranchesToGenerate { get; set; }

        // ════════════════════════════════════════════════
        //  DETALHES POR TIPO
        // ════════════════════════════════════════════════

        /// <summary>
        /// Contagem de fixtures por tipo de equipamento.
        /// Chave: nome legível do tipo (ex: "Toilets", "Sinks").
        /// Valor: quantidade encontrada.
        /// </summary>
        public Dictionary<string, int> FixtureTypeCounts { get; set; }
            = new Dictionary<string, int>();

        /// <summary>
        /// Contagem de rooms por tipo de ambiente.
        /// Chave: nome legível do tipo (ex: "Bathroom", "Kitchen").
        /// Valor: quantidade encontrada.
        /// </summary>
        public Dictionary<string, int> RoomTypeCounts { get; set; }
            = new Dictionary<string, int>();

        // ════════════════════════════════════════════════
        //  DETALHES DOS ROOMS
        // ════════════════════════════════════════════════

        /// <summary>
        /// Lista de rooms hidráulicos detectados com informações de preview.
        /// </summary>
        public List<RoomPreviewInfo> HydraulicRooms { get; set; }
            = new List<RoomPreviewInfo>();

        // ════════════════════════════════════════════════
        //  AVISOS E ALERTAS
        // ════════════════════════════════════════════════

        /// <summary>
        /// Lista de avisos gerados durante a análise.
        /// Usados para informar o usuário sobre possíveis inconsistências.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        // ════════════════════════════════════════════════
        //  STATUS
        // ════════════════════════════════════════════════

        /// <summary>
        /// Se a análise foi concluída com sucesso.
        /// </summary>
        public bool AnalysisSuccessful { get; set; }

        /// <summary>
        /// Mensagem de erro caso a análise tenha falhado.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Tempo de execução da análise em milissegundos.
        /// </summary>
        public long AnalysisTimeMs { get; set; }
    }

    /// <summary>
    /// Informações de preview para um room específico.
    /// </summary>
    public class RoomPreviewInfo
    {
        /// <summary>Nome do room.</summary>
        public string RoomName { get; set; }

        /// <summary>Número do room.</summary>
        public string RoomNumber { get; set; }

        /// <summary>Nível do room.</summary>
        public string LevelName { get; set; }

        /// <summary>Tipo classificado (ex: "Bathroom", "Kitchen").</summary>
        public string ClassifiedType { get; set; }

        /// <summary>Confiança da classificação (0.0 a 1.0).</summary>
        public double Confidence { get; set; }

        /// <summary>Número de fixtures no room.</summary>
        public int FixtureCount { get; set; }

        /// <summary>Número de ramais que seriam gerados para este room.</summary>
        public int BranchCount { get; set; }

        public override string ToString()
        {
            return $"{RoomName} ({ClassifiedType}) - {FixtureCount} fixtures, {BranchCount} branches";
        }
    }
}
