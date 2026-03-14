using RevitHydraulicPlugin.Detection;
using RevitHydraulicPlugin.Models;
using System.Collections.Generic;

namespace RevitHydraulicPlugin.Configuration
{
    /// <summary>
    /// Provedor centralizado de regras de dimensionamento de tubulação.
    /// 
    /// VERSÃO 2.0 — Usa FixtureType em vez de EquipmentType para regras,
    /// e organiza as especificações em tabelas declarativas em vez de switch/case.
    /// 
    /// Regras baseadas em normas brasileiras simplificadas (NBR 8160 e NBR 5626).
    /// Em versões futuras, estas regras podem ser carregadas de arquivo JSON/XML.
    /// </summary>
    public static class PipeRuleProvider
    {
        // ════════════════════════════════════════════════
        //  TABELAS DE REGRAS — ESGOTO
        // ════════════════════════════════════════════════

        /// <summary>
        /// Tabela de regras de esgoto indexada por FixtureType.
        /// </summary>
        private static readonly Dictionary<FixtureType, PipeRule> SewerRules =
            new Dictionary<FixtureType, PipeRule>
            {
                // Vaso Sanitário → DN100, inclinação 1%
                {
                    FixtureType.Toilet,
                    new PipeRule
                    {
                        FixtureType = FixtureType.Toilet,
                        DiameterMm = 100,
                        SlopePercent = 1.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 200,
                        MaxLengthMm = 5000,
                        Description = "Ramal de esgoto - Vaso Sanitario"
                    }
                },

                // Lavatório → DN50, inclinação 2%
                {
                    FixtureType.Sink,
                    new PipeRule
                    {
                        FixtureType = FixtureType.Sink,
                        DiameterMm = 50,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 150,
                        MaxLengthMm = 3000,
                        Description = "Ramal de esgoto - Lavatorio"
                    }
                },

                // Chuveiro → DN50, inclinação 2%
                {
                    FixtureType.Shower,
                    new PipeRule
                    {
                        FixtureType = FixtureType.Shower,
                        DiameterMm = 50,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 200,
                        MaxLengthMm = 4000,
                        Description = "Ramal de esgoto - Chuveiro"
                    }
                },

                // Pia de Cozinha → DN50, inclinação 2%
                {
                    FixtureType.KitchenSink,
                    new PipeRule
                    {
                        FixtureType = FixtureType.KitchenSink,
                        DiameterMm = 50,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 200,
                        MaxLengthMm = 4000,
                        Description = "Ramal de esgoto - Pia de Cozinha"
                    }
                },

                // Tanque → DN50, inclinação 2%
                {
                    FixtureType.LaundrySink,
                    new PipeRule
                    {
                        FixtureType = FixtureType.LaundrySink,
                        DiameterMm = 50,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 200,
                        MaxLengthMm = 3000,
                        Description = "Ramal de esgoto - Tanque"
                    }
                },

                // Ralo → DN75, inclinação 2%
                {
                    FixtureType.Drain,
                    new PipeRule
                    {
                        FixtureType = FixtureType.Drain,
                        DiameterMm = 75,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 150,
                        MaxLengthMm = 3000,
                        Description = "Ramal de esgoto - Ralo"
                    }
                },

                // Máquina de Lavar → DN50, inclinação 2%
                {
                    FixtureType.WashingMachine,
                    new PipeRule
                    {
                        FixtureType = FixtureType.WashingMachine,
                        DiameterMm = 50,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 200,
                        MaxLengthMm = 3000,
                        Description = "Ramal de esgoto - Maquina de Lavar"
                    }
                }
            };

        // ════════════════════════════════════════════════
        //  TABELAS DE REGRAS — ÁGUA FRIA
        // ════════════════════════════════════════════════

        /// <summary>
        /// Tabela de regras de água fria indexada por FixtureType.
        /// </summary>
        private static readonly Dictionary<FixtureType, PipeRule> ColdWaterRules =
            new Dictionary<FixtureType, PipeRule>
            {
                {
                    FixtureType.Toilet,
                    new PipeRule
                    {
                        FixtureType = FixtureType.Toilet,
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Agua Fria",
                        MinLengthMm = 200,
                        MaxLengthMm = 5000,
                        Description = "Ramal AF - Vaso Sanitario"
                    }
                },
                {
                    FixtureType.Sink,
                    new PipeRule
                    {
                        FixtureType = FixtureType.Sink,
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Agua Fria",
                        MinLengthMm = 150,
                        MaxLengthMm = 3000,
                        Description = "Ramal AF - Lavatorio"
                    }
                },
                {
                    FixtureType.Shower,
                    new PipeRule
                    {
                        FixtureType = FixtureType.Shower,
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Agua Fria",
                        MinLengthMm = 200,
                        MaxLengthMm = 4000,
                        Description = "Ramal AF - Chuveiro"
                    }
                },
                {
                    FixtureType.KitchenSink,
                    new PipeRule
                    {
                        FixtureType = FixtureType.KitchenSink,
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Agua Fria",
                        MinLengthMm = 200,
                        MaxLengthMm = 4000,
                        Description = "Ramal AF - Pia de Cozinha"
                    }
                },
                {
                    FixtureType.LaundrySink,
                    new PipeRule
                    {
                        FixtureType = FixtureType.LaundrySink,
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Agua Fria",
                        MinLengthMm = 200,
                        MaxLengthMm = 3000,
                        Description = "Ramal AF - Tanque"
                    }
                },
                {
                    FixtureType.WashingMachine,
                    new PipeRule
                    {
                        FixtureType = FixtureType.WashingMachine,
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Agua Fria",
                        MinLengthMm = 200,
                        MaxLengthMm = 3000,
                        Description = "Ramal AF - Maquina de Lavar"
                    }
                }
            };

        // ════════════════════════════════════════════════
        //  REGRA PADRÃO (fallback)
        // ════════════════════════════════════════════════

        private static readonly PipeRule DefaultSewerRule = new PipeRule
        {
            FixtureType = FixtureType.Unknown,
            DiameterMm = 50,
            SlopePercent = 2.0,
            SystemTypeName = "Sanitary",
            PipeTypeName = "PVC",
            Material = "PVC Esgoto",
            MinLengthMm = 150,
            MaxLengthMm = 5000,
            Description = "Ramal de esgoto - Default"
        };

        private static readonly PipeRule DefaultColdWaterRule = new PipeRule
        {
            FixtureType = FixtureType.Unknown,
            DiameterMm = 25,
            SlopePercent = 0,
            SystemTypeName = "Domestic Cold Water",
            PipeTypeName = "PVC",
            Material = "PVC Agua Fria",
            MinLengthMm = 150,
            MaxLengthMm = 5000,
            Description = "Ramal AF - Default"
        };

        // ════════════════════════════════════════════════
        //  API PÚBLICA
        // ════════════════════════════════════════════════

        /// <summary>
        /// Obtém a regra de esgoto para um tipo de fixture.
        /// </summary>
        public static PipeRule GetSewerRule(FixtureType fixtureType)
        {
            return SewerRules.TryGetValue(fixtureType, out var rule) ? rule : DefaultSewerRule;
        }

        /// <summary>
        /// Obtém a regra de água fria para um tipo de fixture.
        /// </summary>
        public static PipeRule GetColdWaterRule(FixtureType fixtureType)
        {
            return ColdWaterRules.TryGetValue(fixtureType, out var rule) ? rule : DefaultColdWaterRule;
        }

        /// <summary>
        /// Converte PipeRule para PipeSpecification (compatibilidade legada).
        /// </summary>
        public static PipeSpecification ToSpecification(PipeRule rule)
        {
            return new PipeSpecification
            {
                DiameterMm = rule.DiameterMm,
                SlopePercent = rule.SlopePercent,
                SystemTypeName = rule.SystemTypeName,
                PipeTypeName = rule.PipeTypeName,
                Material = rule.Material
            };
        }

        /// <summary>
        /// Verifica se um tipo de fixture precisa de ramal de água fria.
        /// Ralos não precisam.
        /// </summary>
        public static bool NeedsColdWater(FixtureType fixtureType)
        {
            return fixtureType != FixtureType.Drain
                && fixtureType != FixtureType.Unknown;
        }

        /// <summary>Diâmetro padrão da coluna de água fria (mm).</summary>
        public static double DefaultColdWaterColumnDiameterMm => 50;

        /// <summary>Diâmetro padrão da coluna de esgoto (mm).</summary>
        public static double DefaultSewerColumnDiameterMm => 100;
    }

    /// <summary>
    /// Regra de dimensionamento para um tipo de fixture.
    /// Versão expandida do PipeSpecification com limites e metadados.
    /// </summary>
    public class PipeRule
    {
        /// <summary>Tipo de fixture associado.</summary>
        public FixtureType FixtureType { get; set; }

        /// <summary>Diâmetro nominal em mm.</summary>
        public double DiameterMm { get; set; }

        /// <summary>Inclinação em %.</summary>
        public double SlopePercent { get; set; }

        /// <summary>Nome do sistema (Sanitary, Domestic Cold Water).</summary>
        public string SystemTypeName { get; set; }

        /// <summary>Tipo de tubo (PVC, Copper, etc.).</summary>
        public string PipeTypeName { get; set; }

        /// <summary>Material da tubulação.</summary>
        public string Material { get; set; }

        /// <summary>Comprimento mínimo do ramal em mm.</summary>
        public double MinLengthMm { get; set; }

        /// <summary>Comprimento máximo do ramal em mm.</summary>
        public double MaxLengthMm { get; set; }

        /// <summary>Descrição da regra.</summary>
        public string Description { get; set; }

        public override string ToString()
        {
            return $"{Description}: D{DiameterMm}mm, incl. {SlopePercent}%, {Material}";
        }
    }
}
