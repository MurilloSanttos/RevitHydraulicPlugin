using RevitHydraulicPlugin.TestEnvironment.Services;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// TESTE — Regras Hidráulicas (v2.0)
    /// 
    /// Valida PipeRuleProvider/HydraulicRulesTestService:
    /// - Diâmetros por FixtureType
    /// - Inclinações por tipo de sistema
    /// - Regra de Drain -> 75mm
    /// - Água fria para todos exceto Drain
    /// </summary>
    public class HydraulicRulesTests : BaseTest
    {
        public override string TestName => "Regras Hidraulicas v2.0";
        public override string Description => "Valida diametros, inclinacoes e regras por FixtureType";

        public override bool Run()
        {
            PrintHeader();

            Test_ToiletRule();
            Test_SinkRule();
            Test_ShowerRule();
            Test_KitchenSinkRule();
            Test_LaundrySinkRule();
            Test_DrainRule();
            Test_WashingMachineRule();
            Test_ColdWaterRules();
            Test_NeedsColdWater();
            Test_DefaultFallback();

            PrintFooter();
            return FailCount == 0;
        }

        private void Test_ToiletRule()
        {
            PrintSection("BranchRouting_ShouldApplyToiletRule");

            var spec = HydraulicRulesTestService.GetSewerRule(FixtureType.Toilet);
            AssertApprox(100, spec.DiameterMm, "Toilet -> DN100");
            AssertApprox(1.0, spec.SlopePercent, "Toilet -> 1% inclinacao");
            AssertEqual("Sanitary", spec.SystemTypeName, "Toilet -> Sanitary");
            AssertEqual("PVC", spec.PipeTypeName, "Toilet -> PVC");
        }

        private void Test_SinkRule()
        {
            PrintSection("BranchRouting_ShouldApplySinkRule");

            var spec = HydraulicRulesTestService.GetSewerRule(FixtureType.Sink);
            AssertApprox(50, spec.DiameterMm, "Sink -> DN50");
            AssertApprox(2.0, spec.SlopePercent, "Sink -> 2% inclinacao");
        }

        private void Test_ShowerRule()
        {
            PrintSection("BranchRouting_ShouldApplyShowerRule");

            var spec = HydraulicRulesTestService.GetSewerRule(FixtureType.Shower);
            AssertApprox(50, spec.DiameterMm, "Shower -> DN50");
            AssertApprox(2.0, spec.SlopePercent, "Shower -> 2% inclinacao");
        }

        private void Test_KitchenSinkRule()
        {
            PrintSection("BranchRouting_ShouldApplyKitchenSinkRule");

            var spec = HydraulicRulesTestService.GetSewerRule(FixtureType.KitchenSink);
            AssertApprox(50, spec.DiameterMm, "KitchenSink -> DN50");
            AssertApprox(2.0, spec.SlopePercent, "KitchenSink -> 2%");
        }

        private void Test_LaundrySinkRule()
        {
            PrintSection("BranchRouting_ShouldApplyLaundrySinkRule");

            var spec = HydraulicRulesTestService.GetSewerRule(FixtureType.LaundrySink);
            AssertApprox(50, spec.DiameterMm, "LaundrySink -> DN50");
            AssertApprox(2.0, spec.SlopePercent, "LaundrySink -> 2%");
        }

        private void Test_DrainRule()
        {
            PrintSection("BranchRouting_ShouldApplyDrainRule");

            var spec = HydraulicRulesTestService.GetSewerRule(FixtureType.Drain);
            AssertApprox(75, spec.DiameterMm, "Drain -> DN75 (atualizado)");
            AssertApprox(2.0, spec.SlopePercent, "Drain -> 2%");
        }

        private void Test_WashingMachineRule()
        {
            PrintSection("WashingMachine regra");

            var spec = HydraulicRulesTestService.GetSewerRule(FixtureType.WashingMachine);
            AssertApprox(50, spec.DiameterMm, "WashingMachine -> DN50");
            AssertApprox(2.0, spec.SlopePercent, "WashingMachine -> 2%");
        }

        private void Test_ColdWaterRules()
        {
            PrintSection("Regras Agua Fria");

            var fixtures = new[]
            {
                FixtureType.Toilet, FixtureType.Sink, FixtureType.Shower,
                FixtureType.KitchenSink, FixtureType.LaundrySink
            };

            foreach (var ft in fixtures)
            {
                var spec = HydraulicRulesTestService.GetColdWaterRule(ft);
                AssertApprox(25, spec.DiameterMm, $"AF {ft} -> DN25");
                AssertApprox(0, spec.SlopePercent, $"AF {ft} -> 0% inclinacao");
                AssertEqual("Domestic Cold Water", spec.SystemTypeName,
                    $"AF {ft} -> Domestic Cold Water");
            }
        }

        private void Test_NeedsColdWater()
        {
            PrintSection("NeedsColdWater");

            AssertTrue(HydraulicRulesTestService.NeedsColdWater(FixtureType.Toilet),
                "Toilet precisa AF");
            AssertTrue(HydraulicRulesTestService.NeedsColdWater(FixtureType.Sink),
                "Sink precisa AF");
            AssertTrue(HydraulicRulesTestService.NeedsColdWater(FixtureType.Shower),
                "Shower precisa AF");
            AssertTrue(!HydraulicRulesTestService.NeedsColdWater(FixtureType.Drain),
                "Drain NAO precisa AF");
            AssertTrue(!HydraulicRulesTestService.NeedsColdWater(FixtureType.Unknown),
                "Unknown NAO precisa AF");
        }

        private void Test_DefaultFallback()
        {
            PrintSection("Default Fallback");

            var spec = HydraulicRulesTestService.GetSewerRule(FixtureType.Unknown);
            AssertApprox(50, spec.DiameterMm, "Unknown -> DN50 (fallback)");
            AssertApprox(2.0, spec.SlopePercent, "Unknown -> 2% (fallback)");
        }
    }
}
