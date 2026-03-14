using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace RevitHydraulicPlugin.UI
{
    /// <summary>
    /// Constrói a interface na Ribbon do Revit para o plugin hidráulico.
    /// 
    /// Estrutura criada:
    ///   Tab: "Hydraulic Tools"
    ///     Panel: "Hydraulic Automation"
    ///       • Preview Hydraulic Layout  (PreviewCommand)
    ///       • Generate Hydraulic Layout (GenerateCommand)
    ///     Panel: "Individual Steps"
    ///       • Detect Rooms
    ///       • Identify Equipment
    ///       • Create Columns
    ///       • Generate Branches
    ///       • Full Pipeline
    /// 
    /// Chamado pelo App.OnStartup para registrar a interface.
    /// </summary>
    public static class RibbonBuilder
    {
        private const string TabName = "Hydraulic Tools";
        private const string MainPanelName = "Hydraulic Automation";
        private const string StepsPanelName = "Individual Steps";

        /// <summary>
        /// Cria a aba, painéis e botões na Ribbon do Revit.
        /// </summary>
        /// <param name="application">Instância do UIControlledApplication do Revit.</param>
        public static void Build(UIControlledApplication application)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            // ── Criar aba na Ribbon ──
            try
            {
                application.CreateRibbonTab(TabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Tab já existe (não deve acontecer, mas é seguro)
            }

            // ════════════════════════════════════════════════
            //  PAINEL PRINCIPAL — Hydraulic Automation
            // ════════════════════════════════════════════════

            RibbonPanel mainPanel = application.CreateRibbonPanel(TabName, MainPanelName);

            // ── Botão 1: Preview ──
            var previewButton = new PushButtonData(
                name: "PreviewHydraulicLayout",
                text: "Preview\nHydraulic Layout",
                assemblyName: assemblyPath,
                className: "RevitHydraulicPlugin.UI.PreviewCommand")
            {
                ToolTip = "Analyze the model and preview the hydraulic layout " +
                          "that would be generated.\n\n" +
                          "No changes are made to the model.",
                LongDescription =
                    "Executes the full analysis pipeline:\n" +
                    "• Detect hydraulic rooms\n" +
                    "• Identify plumbing fixtures\n" +
                    "• Calculate column positions\n" +
                    "• Calculate branch routes\n\n" +
                    "Shows a summary with room counts, fixture types, " +
                    "branches to generate, and any warnings.\n\n" +
                    "The model is NOT modified."
            };

            // Ícone do Preview (letra P azul)
            previewButton.Image = CreateLetterIcon("P", 16);
            previewButton.LargeImage = CreateLetterIcon("P", 32);

            mainPanel.AddItem(previewButton);

            // ── Separador ──
            mainPanel.AddSeparator();

            // ── Botão 2: Generate ──
            var generateButton = new PushButtonData(
                name: "GenerateHydraulicLayout",
                text: "Generate\nHydraulic Layout",
                assemblyName: assemblyPath,
                className: "RevitHydraulicPlugin.UI.GenerateCommand")
            {
                ToolTip = "Generate the complete hydraulic layout with confirmation.\n\n" +
                          "Shows a preview before making any changes.",
                LongDescription =
                    "Complete workflow:\n" +
                    "1. Analyzes the model (same as Preview)\n" +
                    "2. Shows a confirmation dialog with summary\n" +
                    "3. Creates hydraulic columns\n" +
                    "4. Creates branch connections\n\n" +
                    "You can Undo with Ctrl+Z after generation.\n\n" +
                    "WARNING: This will modify the model."
            };

            // Ícone do Generate (letra G verde)
            generateButton.Image = CreateLetterIcon("G", 16);
            generateButton.LargeImage = CreateLetterIcon("G", 32);

            mainPanel.AddItem(generateButton);

            // ════════════════════════════════════════════════
            //  PAINEL SECUNDÁRIO — Individual Steps
            // ════════════════════════════════════════════════

            RibbonPanel stepsPanel = application.CreateRibbonPanel(TabName, StepsPanelName);

            // Detect Rooms
            AddStepButton(stepsPanel, assemblyPath,
                "DetectRooms", "Detect\nRooms",
                "RevitHydraulicPlugin.Commands.DetectRoomsCommand",
                "Detect hydraulic rooms in the model.",
                "D");

            // Identify Equipment
            AddStepButton(stepsPanel, assemblyPath,
                "IdentifyEquipment", "Identify\nEquipment",
                "RevitHydraulicPlugin.Commands.IdentifyEquipmentCommand",
                "Identify plumbing fixtures in hydraulic rooms.",
                "E");

            // Create Columns
            AddStepButton(stepsPanel, assemblyPath,
                "CreateColumns", "Create\nColumns",
                "RevitHydraulicPlugin.Commands.CreateColumnsCommand",
                "Create hydraulic columns through all levels.",
                "C");

            // Generate Branches
            AddStepButton(stepsPanel, assemblyPath,
                "GenerateBranches", "Generate\nBranches",
                "RevitHydraulicPlugin.Commands.GenerateBranchesCommand",
                "Generate branch connections from fixtures to columns.",
                "B");

            stepsPanel.AddSeparator();

            // Full Pipeline
            AddStepButton(stepsPanel, assemblyPath,
                "FullPipeline", "Full\nPipeline",
                "RevitHydraulicPlugin.Commands.RunFullPipelineCommand",
                "Run the complete hydraulic automation pipeline.",
                "F");
        }

        // ════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════

        /// <summary>
        /// Adiciona um botão de etapa individual ao painel.
        /// </summary>
        private static void AddStepButton(
            RibbonPanel panel, string assemblyPath,
            string name, string text, string className,
            string tooltip, string iconLetter)
        {
            var buttonData = new PushButtonData(name, text, assemblyPath, className)
            {
                ToolTip = tooltip
            };

            buttonData.Image = CreateLetterIcon(iconLetter, 16);
            buttonData.LargeImage = CreateLetterIcon(iconLetter, 32);

            panel.AddItem(buttonData);
        }

        /// <summary>
        /// Cria um ícone simples com uma letra centralizada.
        /// Usa WPF para gerar o BitmapSource programaticamente.
        /// 
        /// Em produção, substitua por ícones .png reais na pasta Resources.
        /// </summary>
        private static BitmapSource CreateLetterIcon(string letter, int size)
        {
            try
            {
                // Tenta carregar ícone do diretório de recursos
                string iconPath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
                    "Resources",
                    $"icon_{letter.ToLower()}_{size}.png");

                if (File.Exists(iconPath))
                {
                    var uri = new Uri(iconPath, UriKind.Absolute);
                    return new BitmapImage(uri);
                }
            }
            catch
            {
                // Fallback: ícone gerado programaticamente
            }

            // Gera ícone programaticamente com WPF
            return GenerateLetterBitmap(letter, size);
        }

        /// <summary>
        /// Gera um BitmapSource com uma letra em um fundo colorido.
        /// </summary>
        private static BitmapSource GenerateLetterBitmap(string letter, int size)
        {
            // Cria pixel data simples (ícone sólido com contraste)
            // Em produção, use ícones desenhados profissionalmente.
            int stride = size * 4; // 4 bytes por pixel (BGRA)
            byte[] pixels = new byte[size * stride];

            // Cor de fundo baseada na letra
            byte r, g, b;
            switch (letter.ToUpper())
            {
                case "P": r = 66; g = 133; b = 244; break;    // Azul (Preview)
                case "G": r = 52; g = 168; b = 83; break;     // Verde (Generate)
                case "D": r = 251; g = 188; b = 4; break;     // Amarelo (Detect)
                case "E": r = 234; g = 67; b = 53; break;     // Vermelho (Equipment)
                case "C": r = 155; g = 89; b = 182; break;    // Roxo (Columns)
                case "B": r = 255; g = 152; b = 0; break;     // Laranja (Branches)
                case "F": r = 0; g = 150; b = 136; break;     // Teal (Full)
                default: r = 128; g = 128; b = 128; break;    // Cinza
            }

            // Preenche todos os pixels com a cor de fundo
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = b;     // Blue
                pixels[i + 1] = g; // Green
                pixels[i + 2] = r; // Red
                pixels[i + 3] = 255; // Alpha
            }

            return BitmapSource.Create(
                size, size, 96, 96,
                System.Windows.Media.PixelFormats.Bgra32,
                null, pixels, stride);
        }
    }
}
