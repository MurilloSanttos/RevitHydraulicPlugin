using System.Collections.Generic;

namespace RevitHydraulicPlugin.TestEnvironment.Services
{
    /// <summary>
    /// Serviço de regras hidráulicas para testes (v2.0).
    /// Espelha PipeRuleProvider + HydraulicRules do plugin principal.
    /// 
    /// Regras baseadas em normas brasileiras simplificadas (NBR 8160/5626).
    /// </summary>
    public static class HydraulicRulesTestService
    {
        // ===== COLUNAS =====

        public static double DefaultColdWaterColumnDiameterMm => 50;
        public static double DefaultSewerColumnDiameterMm => 100;

        // ===== TABELA DE REGRAS — ESGOTO (por FixtureType) =====

        private static readonly Dictionary<FixtureType, PipeSpec> SewerRules =
            new Dictionary<FixtureType, PipeSpec>
            {
                {
                    FixtureType.Toilet, new PipeSpec
                    {
                        DiameterMm = 100, SlopePercent = 1.0,
                        SystemTypeName = "Sanitary", PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 200, MaxLengthMm = 5000,
                        Description = "Ramal esgoto - Vaso Sanitario"
                    }
                },
                {
                    FixtureType.Sink, new PipeSpec
                    {
                        DiameterMm = 50, SlopePercent = 2.0,
                        SystemTypeName = "Sanitary", PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 150, MaxLengthMm = 3000,
                        Description = "Ramal esgoto - Lavatorio"
                    }
                },
                {
                    FixtureType.Shower, new PipeSpec
                    {
                        DiameterMm = 50, SlopePercent = 2.0,
                        SystemTypeName = "Sanitary", PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 200, MaxLengthMm = 4000,
                        Description = "Ramal esgoto - Chuveiro"
                    }
                },
                {
                    FixtureType.KitchenSink, new PipeSpec
                    {
                        DiameterMm = 50, SlopePercent = 2.0,
                        SystemTypeName = "Sanitary", PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 200, MaxLengthMm = 4000,
                        Description = "Ramal esgoto - Pia de Cozinha"
                    }
                },
                {
                    FixtureType.LaundrySink, new PipeSpec
                    {
                        DiameterMm = 50, SlopePercent = 2.0,
                        SystemTypeName = "Sanitary", PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 200, MaxLengthMm = 3000,
                        Description = "Ramal esgoto - Tanque"
                    }
                },
                {
                    FixtureType.Drain, new PipeSpec
                    {
                        DiameterMm = 75, SlopePercent = 2.0,
                        SystemTypeName = "Sanitary", PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 150, MaxLengthMm = 3000,
                        Description = "Ramal esgoto - Ralo"
                    }
                },
                {
                    FixtureType.WashingMachine, new PipeSpec
                    {
                        DiameterMm = 50, SlopePercent = 2.0,
                        SystemTypeName = "Sanitary", PipeTypeName = "PVC",
                        Material = "PVC Esgoto",
                        MinLengthMm = 200, MaxLengthMm = 3000,
                        Description = "Ramal esgoto - Maquina de Lavar"
                    }
                }
            };

        // ===== TABELA DE REGRAS — ÁGUA FRIA =====

        private static readonly PipeSpec DefaultColdWaterSpec = new PipeSpec
        {
            DiameterMm = 25, SlopePercent = 0,
            SystemTypeName = "Domestic Cold Water", PipeTypeName = "PVC",
            Material = "PVC Agua Fria",
            MinLengthMm = 150, MaxLengthMm = 5000,
            Description = "Ramal AF - Default"
        };

        private static readonly PipeSpec DefaultSewerSpec = new PipeSpec
        {
            DiameterMm = 50, SlopePercent = 2.0,
            SystemTypeName = "Sanitary", PipeTypeName = "PVC",
            Material = "PVC Esgoto",
            MinLengthMm = 150, MaxLengthMm = 5000,
            Description = "Ramal esgoto - Default"
        };

        // ===== API PÚBLICA =====

        /// <summary>
        /// Regra de esgoto por FixtureType.
        /// </summary>
        public static PipeSpec GetSewerRule(FixtureType fixtureType)
        {
            return SewerRules.TryGetValue(fixtureType, out var spec) ? spec : DefaultSewerSpec;
        }

        /// <summary>
        /// Regra de água fria (DN25 para todos).
        /// </summary>
        public static PipeSpec GetColdWaterRule(FixtureType fixtureType)
        {
            return DefaultColdWaterSpec;
        }

        /// <summary>
        /// Verifica se fixture precisa de água fria.
        /// </summary>
        public static bool NeedsColdWater(FixtureType fixtureType)
        {
            return fixtureType != FixtureType.Drain
                && fixtureType != FixtureType.Unknown;
        }

        // ===== COMPATIBILIDADE LEGADA (EquipmentType) =====

        public static PipeSpec GetSewerSpec(EquipmentType type)
        {
            var ft = EquipmentToFixtureType(type);
            return GetSewerRule(ft);
        }

        public static PipeSpec GetColdWaterSpec(EquipmentType type)
        {
            return DefaultColdWaterSpec;
        }

        public static bool ValidateSewerRule(FixtureType type,
            double expectedDiameter, double expectedSlope)
        {
            var spec = GetSewerRule(type);
            return System.Math.Abs(spec.DiameterMm - expectedDiameter) < 0.01
                && System.Math.Abs(spec.SlopePercent - expectedSlope) < 0.01;
        }

        private static FixtureType EquipmentToFixtureType(EquipmentType type)
        {
            switch (type)
            {
                case EquipmentType.VasoSanitario: return FixtureType.Toilet;
                case EquipmentType.Lavatorio: return FixtureType.Sink;
                case EquipmentType.Chuveiro: return FixtureType.Shower;
                case EquipmentType.Pia: return FixtureType.KitchenSink;
                case EquipmentType.Tanque: return FixtureType.LaundrySink;
                case EquipmentType.Ralo: return FixtureType.Drain;
                case EquipmentType.MaquinaLavar: return FixtureType.WashingMachine;
                default: return FixtureType.Unknown;
            }
        }
    }
}
