namespace RevitHydraulicPlugin.TestEnvironment.Services
{
    /// <summary>
    /// Serviço de regras hidráulicas para testes.
    /// Replica a lógica de HydraulicRules do plugin principal.
    /// 
    /// Centraliza as regras de:
    /// - Diâmetro de tubulação por tipo de equipamento
    /// - Inclinação de ramais de esgoto
    /// - Diâmetro de colunas
    /// </summary>
    public static class HydraulicRulesTestService
    {
        // ===== COLUNAS =====

        /// <summary>
        /// Diâmetro padrão da coluna de água fria (mm).
        /// </summary>
        public static double DefaultColdWaterColumnDiameterMm => 50;

        /// <summary>
        /// Diâmetro padrão da coluna de esgoto (mm).
        /// </summary>
        public static double DefaultSewerColumnDiameterMm => 100;

        // ===== RAMAIS DE ESGOTO =====

        /// <summary>
        /// Retorna especificação de tubulação de ESGOTO para o tipo de equipamento.
        /// </summary>
        public static PipeSpec GetSewerSpec(EquipmentType type)
        {
            switch (type)
            {
                case EquipmentType.VasoSanitario:
                    return new PipeSpec
                    {
                        DiameterMm = 100,
                        SlopePercent = 1.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto"
                    };

                case EquipmentType.Lavatorio:
                case EquipmentType.Chuveiro:
                case EquipmentType.Pia:
                case EquipmentType.Tanque:
                case EquipmentType.Ralo:
                case EquipmentType.MaquinaLavar:
                default:
                    return new PipeSpec
                    {
                        DiameterMm = 50,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto"
                    };
            }
        }

        /// <summary>
        /// Retorna especificação de tubulação de ÁGUA FRIA para o tipo de equipamento.
        /// </summary>
        public static PipeSpec GetColdWaterSpec(EquipmentType type)
        {
            return new PipeSpec
            {
                DiameterMm = 25,
                SlopePercent = 0,
                SystemTypeName = "Domestic Cold Water",
                PipeTypeName = "PVC",
                Material = "PVC Água Fria"
            };
        }

        /// <summary>
        /// Verifica se as regras para um tipo de equipamento estão corretas.
        /// Útil para validação automática nos testes.
        /// </summary>
        public static bool ValidateSewerRule(EquipmentType type,
            double expectedDiameter, double expectedSlope)
        {
            var spec = GetSewerSpec(type);
            return System.Math.Abs(spec.DiameterMm - expectedDiameter) < 0.01
                && System.Math.Abs(spec.SlopePercent - expectedSlope) < 0.01;
        }
    }
}
