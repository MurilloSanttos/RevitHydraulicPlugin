using RevitHydraulicPlugin.Models;
using System.Linq;
using System.Text;

namespace RevitHydraulicPlugin.Services
{
    /// <summary>
    /// Formata PreviewResult em texto legível para exibição.
    /// 
    /// Produz diferentes formatos:
    /// - FormatForDialog:  texto compacto para TaskDialog do Revit
    /// - FormatDetailed:   relatório detalhado com informações por room
    /// - FormatSummary:    resumo de uma linha para status bar
    /// </summary>
    public static class PreviewFormatter
    {
        // ════════════════════════════════════════════════
        //  FORMATO PRINCIPAL — TASKDIALOG
        // ════════════════════════════════════════════════

        /// <summary>
        /// Formata o resultado para exibição em TaskDialog do Revit.
        /// Texto compacto e organizado para leitura rápida.
        /// </summary>
        public static string FormatForDialog(PreviewResult result)
        {
            if (!result.AnalysisSuccessful)
            {
                return $"Analysis failed:\n{result.ErrorMessage}";
            }

            var sb = new StringBuilder();

            // ── Seção 1: Rooms ──
            sb.AppendLine("═══ ROOMS ═══");
            sb.AppendLine($"Rooms analyzed:           {result.TotalRoomsAnalyzed}");
            sb.AppendLine($"Hydraulic rooms detected: {result.HydraulicRoomsDetected}");
            sb.AppendLine();

            // Detalhes por tipo de room
            if (result.RoomTypeCounts.Count > 0)
            {
                foreach (var kvp in result.RoomTypeCounts.OrderByDescending(x => x.Value))
                {
                    sb.AppendLine($"  • {kvp.Key}: {kvp.Value}");
                }
                sb.AppendLine();
            }

            // ── Seção 2: Fixtures ──
            sb.AppendLine("═══ FIXTURES ═══");
            sb.AppendLine($"Total fixtures detected: {result.FixturesDetected}");
            sb.AppendLine();

            if (result.FixtureTypeCounts.Count > 0)
            {
                foreach (var kvp in result.FixtureTypeCounts.OrderByDescending(x => x.Value))
                {
                    sb.AppendLine($"  • {kvp.Key}: {kvp.Value}");
                }
                sb.AppendLine();
            }

            // ── Seção 3: Geração ──
            sb.AppendLine("═══ GENERATION ═══");
            sb.AppendLine($"Columns to generate: {result.ColumnsToGenerate}");
            sb.AppendLine($"Branches to generate: {result.BranchesToGenerate}");
            sb.AppendLine();

            // ── Seção 4: Avisos ──
            if (result.Warnings.Count > 0)
            {
                sb.AppendLine("═══ WARNINGS ═══");
                foreach (var warning in result.Warnings)
                {
                    sb.AppendLine($"  ⚠ {warning}");
                }
                sb.AppendLine();
            }

            // ── Rodapé ──
            sb.AppendLine($"Analysis time: {result.AnalysisTimeMs}ms");

            return sb.ToString();
        }

        // ════════════════════════════════════════════════
        //  FORMATO DETALHADO — EXPANDED CONTENT
        // ════════════════════════════════════════════════

        /// <summary>
        /// Formata relatório detalhado com informações por room.
        /// Usado no ExpandedContent do TaskDialog.
        /// </summary>
        public static string FormatDetailed(PreviewResult result)
        {
            if (!result.AnalysisSuccessful)
                return "Analysis failed. Check logs for details.";

            if (result.HydraulicRooms.Count == 0)
                return "No hydraulic rooms to detail.";

            var sb = new StringBuilder();

            sb.AppendLine("═══ ROOM DETAILS ═══");
            sb.AppendLine();

            foreach (var room in result.HydraulicRooms)
            {
                sb.AppendLine($"  {room.RoomName} (#{room.RoomNumber})");
                sb.AppendLine($"    Level: {room.LevelName}");
                sb.AppendLine($"    Type: {room.ClassifiedType} ({room.Confidence:P0} confidence)");
                sb.AppendLine($"    Fixtures: {room.FixtureCount}");
                sb.AppendLine($"    Branches: {room.BranchCount}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ════════════════════════════════════════════════
        //  FORMATO RESUMO — UMA LINHA
        // ════════════════════════════════════════════════

        /// <summary>
        /// Formata resumo compacto de uma linha.
        /// Útil para status ou mensagem rápida.
        /// </summary>
        public static string FormatSummary(PreviewResult result)
        {
            if (!result.AnalysisSuccessful)
                return $"Analysis failed: {result.ErrorMessage}";

            return $"{result.HydraulicRoomsDetected} hydraulic rooms, " +
                   $"{result.FixturesDetected} fixtures, " +
                   $"{result.BranchesToGenerate} branches to generate" +
                   (result.Warnings.Count > 0 ? $" ({result.Warnings.Count} warnings)" : "");
        }

        // ════════════════════════════════════════════════
        //  FORMATO CONFIRMAÇÃO — PARA GENERATE
        // ════════════════════════════════════════════════

        /// <summary>
        /// Formata texto de confirmação para o comando Generate.
        /// Inclui resumo + pergunta de confirmação.
        /// </summary>
        public static string FormatConfirmation(PreviewResult result)
        {
            var sb = new StringBuilder();

            sb.AppendLine("The following changes will be made to the model:");
            sb.AppendLine();
            sb.AppendLine($"  • {result.ColumnsToGenerate} hydraulic columns");
            sb.AppendLine($"  • {result.BranchesToGenerate} branch connections");
            sb.AppendLine();
            sb.AppendLine($"Based on analysis of:");
            sb.AppendLine($"  • {result.HydraulicRoomsDetected} hydraulic rooms");
            sb.AppendLine($"  • {result.FixturesDetected} fixtures detected");
            sb.AppendLine();

            if (result.Warnings.Count > 0)
            {
                sb.AppendLine("⚠ Warnings:");
                foreach (var warning in result.Warnings)
                {
                    sb.AppendLine($"  • {warning}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("Do you want to proceed with the generation?");

            return sb.ToString();
        }
    }
}
