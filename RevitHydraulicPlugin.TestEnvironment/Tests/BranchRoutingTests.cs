using RevitHydraulicPlugin.TestEnvironment.Mocks;
using RevitHydraulicPlugin.TestEnvironment.Services;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.TestEnvironment.Tests
{
    /// <summary>
    /// TESTE 4 — Geração de Ramais Simulados
    /// 
    /// Cenário:
    /// Cria um edifício residencial completo usando MockProjectFactory.
    /// Executa o pipeline completo: detecção → equipamentos → colunas → ramais.
    /// Verifica se os ramais são gerados corretamente.
    /// </summary>
    public class BranchRoutingTests : BaseTest
    {
        public override string TestName => "Geração de Ramais";
        public override string Description => "Valida pipeline completo: detecção → ramais";

        public override bool Run()
        {
            PrintHeader();

            // Cria projeto completo de teste
            var project = MockProjectFactory.CreateResidentialBuilding();

            PrintInfo($"Projeto: {project.ProjectName}");
            PrintInfo($"Níveis: {project.Levels.Count}");
            PrintInfo($"Rooms: {project.Rooms.Count}");

            // === ETAPA 1: Detecção de Ambientes ===
            PrintSection("ETAPA 1 — Detecção de Ambientes:");
            var roomService = new RoomDetectionTestService();
            var detectedRooms = roomService.DetectRooms(project);
            var hydraulicRooms = detectedRooms.Where(r => r.IsHydraulic).ToList();

            foreach (var r in detectedRooms)
            {
                string marker = r.IsHydraulic ? "✓" : "✗";
                PrintInfo($"  {marker} {r}");
            }

            AssertTrue(hydraulicRooms.Count > 0,
                $"Ambientes hidráulicos detectados: {hydraulicRooms.Count}");

            // === ETAPA 2: Identificação de Equipamentos ===
            PrintSection("ETAPA 2 — Identificação de Equipamentos:");
            var equipService = new EquipmentDetectionTestService();
            var equipment = equipService.DetectEquipment(detectedRooms);

            foreach (var e in equipment)
            {
                PrintInfo($"  {e.Type}: {e.Fixture.FamilyName} ({e.Room.Name})");
            }

            AssertTrue(equipment.Count > 0,
                $"Equipamentos detectados: {equipment.Count}");

            // === ETAPA 3: Cálculo de Colunas ===
            PrintSection("ETAPA 3 — Cálculo de Colunas:");
            var routingService = new BranchRoutingTestService();
            var columns = routingService.CalculateColumns(hydraulicRooms, project.Levels);

            foreach (var col in columns)
            {
                PrintInfo($"  {col}");
                foreach (var pipe in col.Pipes)
                {
                    PrintInfo($"    └→ {pipe}");
                }
            }

            AssertTrue(columns.Count > 0, $"Colunas calculadas: {columns.Count}");

            // Verifica que há colunas de ambos os tipos
            var coldWaterCols = columns.Where(c => c.SystemType == ConnectorSystemType.ColdWater).ToList();
            var sewerCols = columns.Where(c => c.SystemType == ConnectorSystemType.Sewer).ToList();

            AssertTrue(coldWaterCols.Count > 0,
                $"Colunas de Água Fria: {coldWaterCols.Count}");
            AssertTrue(sewerCols.Count > 0,
                $"Colunas de Esgoto: {sewerCols.Count}");

            // Verifica que colunas têm segmentos verticais
            foreach (var col in columns)
            {
                AssertTrue(col.Pipes.Count == project.Levels.Count - 1,
                    $"Coluna {col.ColumnId}: {col.Pipes.Count} segmentos (esperado: {project.Levels.Count - 1})");

                // Verifica que segmentos são verticais
                foreach (var pipe in col.Pipes)
                {
                    AssertTrue(pipe.IsVertical,
                        $"Coluna {col.ColumnId}: segmento é vertical");
                }
            }

            // === ETAPA 4: Geração de Ramais ===
            PrintSection("ETAPA 4 — Geração de Ramais:");
            var branches = routingService.GenerateBranches(equipment, columns);

            foreach (var b in branches)
            {
                PrintInfo($"  {b}");
            }

            AssertTrue(branches.Count > 0,
                $"Ramais gerados: {branches.Count}");

            // Verifica ramais de esgoto
            var sewerBranches = branches.Where(b => b.SystemType == ConnectorSystemType.Sewer).ToList();
            AssertTrue(sewerBranches.Count == equipment.Count,
                $"Todos os equipamentos têm ramal de esgoto: {sewerBranches.Count}/{equipment.Count}");

            // Verifica ramais de água fria (exceto ralos)
            var coldWaterBranches = branches.Where(b => b.SystemType == ConnectorSystemType.ColdWater).ToList();
            int equipWithoutRalo = equipment.Count(e => e.Type != EquipmentType.Ralo);
            AssertTrue(coldWaterBranches.Count == equipWithoutRalo,
                $"Ramais AF (sem ralos): {coldWaterBranches.Count}/{equipWithoutRalo}");

            // Verifica inclinação nos ramais de esgoto
            PrintSection("Verificando inclinações de esgoto:");
            foreach (var branch in sewerBranches)
            {
                var equipType = equipment.First(e =>
                    e.Fixture.Id == branch.Fixture.Id).Type;

                if (equipType == EquipmentType.VasoSanitario)
                {
                    AssertApprox(100, branch.Pipe.DiameterMm,
                        $"Ramal esgoto vaso → Ø100mm");
                    AssertApprox(1.0, branch.Pipe.SlopePercent,
                        $"Ramal esgoto vaso → 1%");
                }
                else
                {
                    AssertApprox(50, branch.Pipe.DiameterMm,
                        $"Ramal esgoto {equipType} → Ø50mm");
                    AssertApprox(2.0, branch.Pipe.SlopePercent,
                        $"Ramal esgoto {equipType} → 2%");
                }
            }

            // Verifica que ramais de água fria têm inclinação zero
            PrintSection("Verificando ramais de água fria:");
            foreach (var branch in coldWaterBranches)
            {
                AssertApprox(0, branch.Pipe.SlopePercent,
                    $"Ramal AF {branch.Fixture.FamilyName} → 0% inclinação");
                AssertApprox(25, branch.Pipe.DiameterMm,
                    $"Ramal AF {branch.Fixture.FamilyName} → Ø25mm");
            }

            // === ESTATÍSTICAS FINAIS ===
            PrintSection("Estatísticas do Pipeline:");
            int totalPipes = columns.Sum(c => c.Pipes.Count) + branches.Count;
            PrintInfo($"  Total de Pipes gerados: {totalPipes}");
            PrintInfo($"    Segmentos de coluna: {columns.Sum(c => c.Pipes.Count)}");
            PrintInfo($"    Ramais: {branches.Count}");
            PrintInfo($"      Esgoto: {sewerBranches.Count}");
            PrintInfo($"      Água Fria: {coldWaterBranches.Count}");

            PrintFooter();
            return FailCount == 0;
        }
    }
}
