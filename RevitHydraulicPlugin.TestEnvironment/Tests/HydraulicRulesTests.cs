using RevitHydraulicPlugin.TestEnvironment.Services;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// TESTE 3 — Aplicação de Regras Hidráulicas
    /// 
    /// Valida se o sistema aplica corretamente as regras de dimensionamento:
    /// 
    /// Vaso sanitário → 100mm, 1% inclinação
    /// Lavatório      → 50mm,  2% inclinação
    /// Chuveiro       → 50mm,  2% inclinação
    /// Pia            → 50mm,  2% inclinação
    /// Tanque         → 50mm,  2% inclinação
    /// Ralo           → 50mm,  2% inclinação
    /// 
    /// Água fria (todos): 25mm, 0% inclinação (horizontal)
    /// </summary>
    public class HydraulicRulesTests : BaseTest
    {
        public override string TestName => "Regras Hidráulicas";
        public override string Description => "Valida diâmetros e inclinações por equipamento";

        public override bool Run()
        {
            PrintHeader();

            // === REGRAS DE ESGOTO ===
            PrintSection("Regras de ESGOTO:");

            // Vaso sanitário: 100mm, 1%
            var vasoSewer = HydraulicRulesTestService.GetSewerSpec(EquipmentType.VasoSanitario);
            PrintInfo($"Vaso Sanitário: {vasoSewer}");
            AssertApprox(100, vasoSewer.DiameterMm, "Vaso → Ø100mm");
            AssertApprox(1.0, vasoSewer.SlopePercent, "Vaso → 1% inclinação");
            AssertEqual("Sanitary", vasoSewer.SystemTypeName, "Vaso → Sistema Sanitary");
            AssertEqual("PVC Esgoto", vasoSewer.Material, "Vaso → Material PVC Esgoto");

            // Lavatório: 50mm, 2%
            var lavSewer = HydraulicRulesTestService.GetSewerSpec(EquipmentType.Lavatorio);
            PrintInfo($"Lavatório: {lavSewer}");
            AssertApprox(50, lavSewer.DiameterMm, "Lavatório → Ø50mm");
            AssertApprox(2.0, lavSewer.SlopePercent, "Lavatório → 2% inclinação");

            // Chuveiro: 50mm, 2%
            var chuvSewer = HydraulicRulesTestService.GetSewerSpec(EquipmentType.Chuveiro);
            PrintInfo($"Chuveiro: {chuvSewer}");
            AssertApprox(50, chuvSewer.DiameterMm, "Chuveiro → Ø50mm");
            AssertApprox(2.0, chuvSewer.SlopePercent, "Chuveiro → 2% inclinação");

            // Pia: 50mm, 2%
            var piaSewer = HydraulicRulesTestService.GetSewerSpec(EquipmentType.Pia);
            PrintInfo($"Pia: {piaSewer}");
            AssertApprox(50, piaSewer.DiameterMm, "Pia → Ø50mm");
            AssertApprox(2.0, piaSewer.SlopePercent, "Pia → 2% inclinação");

            // Tanque: 50mm, 2%
            var tanqueSewer = HydraulicRulesTestService.GetSewerSpec(EquipmentType.Tanque);
            PrintInfo($"Tanque: {tanqueSewer}");
            AssertApprox(50, tanqueSewer.DiameterMm, "Tanque → Ø50mm");
            AssertApprox(2.0, tanqueSewer.SlopePercent, "Tanque → 2% inclinação");

            // Ralo: 50mm, 2%
            var raloSewer = HydraulicRulesTestService.GetSewerSpec(EquipmentType.Ralo);
            PrintInfo($"Ralo: {raloSewer}");
            AssertApprox(50, raloSewer.DiameterMm, "Ralo → Ø50mm");
            AssertApprox(2.0, raloSewer.SlopePercent, "Ralo → 2% inclinação");

            // === REGRAS DE ÁGUA FRIA ===
            PrintSection("Regras de ÁGUA FRIA:");

            var vasoWater = HydraulicRulesTestService.GetColdWaterSpec(EquipmentType.VasoSanitario);
            PrintInfo($"Vaso Sanitário: {vasoWater}");
            AssertApprox(25, vasoWater.DiameterMm, "Vaso (AF) → Ø25mm");
            AssertApprox(0, vasoWater.SlopePercent, "Vaso (AF) → 0% inclinação");
            AssertEqual("Domestic Cold Water", vasoWater.SystemTypeName, "Vaso (AF) → Sistema Cold Water");
            AssertEqual("PVC Água Fria", vasoWater.Material, "Vaso (AF) → Material PVC Água Fria");

            var lavWater = HydraulicRulesTestService.GetColdWaterSpec(EquipmentType.Lavatorio);
            AssertApprox(25, lavWater.DiameterMm, "Lavatório (AF) → Ø25mm");
            AssertApprox(0, lavWater.SlopePercent, "Lavatório (AF) → 0% inclinação");

            var chuvWater = HydraulicRulesTestService.GetColdWaterSpec(EquipmentType.Chuveiro);
            AssertApprox(25, chuvWater.DiameterMm, "Chuveiro (AF) → Ø25mm");
            AssertApprox(0, chuvWater.SlopePercent, "Chuveiro (AF) → 0% inclinação");

            // === COLUNAS ===
            PrintSection("Diâmetros de COLUNAS:");

            AssertApprox(50, HydraulicRulesTestService.DefaultColdWaterColumnDiameterMm,
                "Coluna Água Fria → Ø50mm");
            AssertApprox(100, HydraulicRulesTestService.DefaultSewerColumnDiameterMm,
                "Coluna Esgoto → Ø100mm");

            // === VALIDAÇÃO CRUZADA ===
            PrintSection("Validação cruzada com método ValidateSewerRule:");

            AssertTrue(HydraulicRulesTestService.ValidateSewerRule(
                EquipmentType.VasoSanitario, 100, 1.0),
                "Validação OK: VasoSanitario (100mm, 1%)");

            AssertTrue(HydraulicRulesTestService.ValidateSewerRule(
                EquipmentType.Lavatorio, 50, 2.0),
                "Validação OK: Lavatorio (50mm, 2%)");

            AssertTrue(!HydraulicRulesTestService.ValidateSewerRule(
                EquipmentType.VasoSanitario, 50, 2.0),
                "Validação REJEITA: VasoSanitario com valores errados");

            PrintFooter();
            return FailCount == 0;
        }
    }
}
