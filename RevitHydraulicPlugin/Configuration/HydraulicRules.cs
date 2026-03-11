using RevitHydraulicPlugin.Models;
using System.Collections.Generic;

namespace RevitHydraulicPlugin.Configuration
{
    /// <summary>
    /// Define as regras hidráulicas para dimensionamento de ramais e colunas.
    /// Todas as regras são configuráveis e centralizadas aqui para facilitar ajustes.
    /// 
    /// IMPORTANTE: Estas são regras simplificadas para a versão 1 do plugin.
    /// Em versões futuras, poderão ser lidas de arquivo externo (JSON/XML).
    /// </summary>
    public static class HydraulicRules
    {
        /// <summary>
        /// Retorna a especificação de tubulação de ESGOTO para o tipo de equipamento.
        /// Define diâmetro e inclinação conforme norma simplificada.
        /// </summary>
        public static PipeSpecification GetSewerSpec(EquipmentType equipmentType)
        {
            switch (equipmentType)
            {
                case EquipmentType.VasoSanitario:
                    return new PipeSpecification
                    {
                        DiameterMm = 100,
                        SlopePercent = 1.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto"
                    };

                case EquipmentType.Lavatorio:
                    return new PipeSpecification
                    {
                        DiameterMm = 50,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto"
                    };

                case EquipmentType.Chuveiro:
                    return new PipeSpecification
                    {
                        DiameterMm = 50,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto"
                    };

                case EquipmentType.Pia:
                    return new PipeSpecification
                    {
                        DiameterMm = 50,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto"
                    };

                case EquipmentType.Tanque:
                    return new PipeSpecification
                    {
                        DiameterMm = 50,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto"
                    };

                case EquipmentType.Ralo:
                    return new PipeSpecification
                    {
                        DiameterMm = 50,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto"
                    };

                case EquipmentType.MaquinaLavar:
                    return new PipeSpecification
                    {
                        DiameterMm = 50,
                        SlopePercent = 2.0,
                        SystemTypeName = "Sanitary",
                        PipeTypeName = "PVC",
                        Material = "PVC Esgoto"
                    };

                default:
                    return new PipeSpecification
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
        /// Retorna a especificação de tubulação de ÁGUA FRIA para o tipo de equipamento.
        /// </summary>
        public static PipeSpecification GetColdWaterSpec(EquipmentType equipmentType)
        {
            switch (equipmentType)
            {
                case EquipmentType.VasoSanitario:
                    return new PipeSpecification
                    {
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Água Fria"
                    };

                case EquipmentType.Lavatorio:
                    return new PipeSpecification
                    {
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Água Fria"
                    };

                case EquipmentType.Chuveiro:
                    return new PipeSpecification
                    {
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Água Fria"
                    };

                case EquipmentType.Pia:
                    return new PipeSpecification
                    {
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Água Fria"
                    };

                case EquipmentType.Tanque:
                    return new PipeSpecification
                    {
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Água Fria"
                    };

                case EquipmentType.MaquinaLavar:
                    return new PipeSpecification
                    {
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Água Fria"
                    };

                default:
                    return new PipeSpecification
                    {
                        DiameterMm = 25,
                        SlopePercent = 0,
                        SystemTypeName = "Domestic Cold Water",
                        PipeTypeName = "PVC",
                        Material = "PVC Água Fria"
                    };
            }
        }

        /// <summary>
        /// Diâmetro padrão da coluna de água fria (mm).
        /// </summary>
        public static double DefaultColdWaterColumnDiameterMm => 50;

        /// <summary>
        /// Diâmetro padrão da coluna de esgoto (mm).
        /// </summary>
        public static double DefaultSewerColumnDiameterMm => 100;
    }
}
