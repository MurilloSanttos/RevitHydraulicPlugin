using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RevitHydraulicPlugin.TestEnvironment.Services
{
    /// <summary>
    /// Resultado da análise de classificação de um room.
    /// </summary>
    public class RoomAnalysisResult
    {
        public RoomType Type { get; set; } = RoomType.Unknown;
        public double Confidence { get; set; }
        public string Method { get; set; } = "None";
        public bool IsHydraulic => Type != RoomType.Unknown;
        public string Reason { get; set; } = "";
    }

    /// <summary>
    /// Serviço de classificação de ambientes hidráulicos para testes.
    /// Espelha RoomClassifierService do plugin principal (v2.0).
    /// 
    /// Classificação multi-critério:
    ///   Camada 1: Nome do Room (regex PT/EN)
    ///   Camada 2: Fixtures presentes no ambiente
    ///   Camada 3: Combinação dos critérios
    /// </summary>
    public class RoomClassifierTestService
    {
        // ── Padrões de nome ──

        private class RoomNamePattern
        {
            public RoomType Type { get; }
            public string[] Patterns { get; }
            public RoomNamePattern(RoomType type, string[] patterns)
            {
                Type = type;
                Patterns = patterns;
            }
        }

        private static readonly List<RoomNamePattern> NamePatterns = new List<RoomNamePattern>
        {
            // Banheiro PCD (antes de Banheiro genérico)
            new RoomNamePattern(RoomType.AccessibleBathroom, new[]
            {
                @"banheiro\s*(pcd|pne|acess[ií]vel|deficiente)",
                @"(pcd|pne|acess[ií]vel)\s*banheiro",
                @"wc\s*(pcd|pne|acess[ií]vel)",
                @"accessible\s*bath",
                @"ada\s*bath"
            }),

            // Banheiro Suíte (antes de Banheiro genérico)
            new RoomNamePattern(RoomType.SuiteBathroom, new[]
            {
                @"banheiro\s*su[ií]te",
                @"su[ií]te\s*banheiro",
                @"banho\s*su[ií]te",
                @"wc\s*su[ií]te",
                @"master\s*bath",
                @"ensuite",
                @"en\s*suite"
            }),

            // Banheiro genérico
            new RoomNamePattern(RoomType.Bathroom, new[]
            {
                @"\bbanheiro\b",
                @"\bbanho\b",
                @"\bwc\b",
                @"\bbwc\b",
                @"\bsanit[aá]rio\b",
                @"\bbathroom\b",
                @"\btoilet\b(?!.*kitchen)",
                @"\brest\s*room\b",
                @"\bwash\s*room\b"
            }),

            // Lavabo
            new RoomNamePattern(RoomType.Lavatory, new[]
            {
                @"\blavabo\b",
                @"\bpowder\s*room\b",
                @"\bhalf\s*bath\b",
                @"\bguest\s*bath\b"
            }),

            // Copa (antes de Cozinha)
            new RoomNamePattern(RoomType.Pantry, new[]
            {
                @"\bcopa\b(?!\s*/\s*cozinha)(?!\s*cozinha)",
                @"\bpantry\b",
                @"\bkitchenette\b"
            }),

            // Cozinha
            new RoomNamePattern(RoomType.Kitchen, new[]
            {
                @"\bcozinha\b",
                @"\bkitchen\b",
                @"\bcopa\s*/\s*cozinha",
                @"\bcopa\s*cozinha"
            }),

            // Área de Serviço
            new RoomNamePattern(RoomType.ServiceArea, new[]
            {
                @"[aá]rea\s*(de\s*)?servi[cç]o",
                @"\ba\.?\s*s\.\b",
                @"\bservice\s*area\b",
                @"\butility\s*room\b",
                @"\butility\b"
            }),

            // Lavanderia
            new RoomNamePattern(RoomType.Laundry, new[]
            {
                @"\blavanderia\b",
                @"\blaundry\b"
            })
        };

        // ════════════════════════════════════════════════
        //  CLASSIFICAÇÃO POR NOME (CAMADA 1)
        // ════════════════════════════════════════════════

        public RoomAnalysisResult ClassifyByName(string roomName)
        {
            var result = new RoomAnalysisResult();

            if (string.IsNullOrWhiteSpace(roomName))
            {
                result.Reason = "Nome vazio";
                return result;
            }

            string normalized = roomName.Trim().ToLowerInvariant();

            foreach (var pattern in NamePatterns)
            {
                foreach (string regex in pattern.Patterns)
                {
                    if (Regex.IsMatch(normalized, regex, RegexOptions.IgnoreCase))
                    {
                        result.Type = pattern.Type;
                        result.Confidence = 0.8;
                        result.Method = "Name";
                        result.Reason = $"Nome '{roomName}' match padrao '{regex}'";
                        return result;
                    }
                }
            }

            result.Reason = $"Nome '{roomName}' nao reconhecido";
            return result;
        }

        // ════════════════════════════════════════════════
        //  CLASSIFICAÇÃO COM FIXTURES (CAMADA 2+3)
        // ════════════════════════════════════════════════

        public RoomAnalysisResult ClassifyWithFixtures(string roomName, List<FixtureType> fixtures)
        {
            var nameResult = ClassifyByName(roomName);
            var fixtureResult = AnalyzeFixtures(fixtures);

            return CombineResults(roomName, nameResult, fixtureResult, fixtures);
        }

        // ── Análise de Fixtures ──

        private RoomAnalysisResult AnalyzeFixtures(List<FixtureType> fixtures)
        {
            var result = new RoomAnalysisResult();
            if (fixtures == null || fixtures.Count == 0)
            {
                result.Reason = "Sem fixtures";
                return result;
            }

            bool hasToilet = fixtures.Contains(FixtureType.Toilet);
            bool hasSink = fixtures.Contains(FixtureType.Sink);
            bool hasShower = fixtures.Contains(FixtureType.Shower);
            bool hasKitchenSink = fixtures.Contains(FixtureType.KitchenSink);
            bool hasLaundrySink = fixtures.Contains(FixtureType.LaundrySink);
            bool hasWasher = fixtures.Contains(FixtureType.WashingMachine);
            bool hasDrain = fixtures.Contains(FixtureType.Drain);

            if (hasToilet && (hasSink || hasShower))
            {
                result.Type = RoomType.Bathroom;
                result.Confidence = 0.9;
                result.Method = "Fixture";
                result.Reason = "Toilet + Sink/Shower = Banheiro";
                return result;
            }

            if (hasToilet && hasSink && !hasShower)
            {
                result.Type = RoomType.Lavatory;
                result.Confidence = 0.85;
                result.Method = "Fixture";
                result.Reason = "Toilet + Sink (sem shower) = Lavabo";
                return result;
            }

            if (hasToilet)
            {
                result.Type = RoomType.Bathroom;
                result.Confidence = 0.7;
                result.Method = "Fixture";
                result.Reason = "Toilet = provavel Banheiro";
                return result;
            }

            if (hasKitchenSink)
            {
                result.Type = RoomType.Kitchen;
                result.Confidence = 0.85;
                result.Method = "Fixture";
                result.Reason = "KitchenSink = Cozinha";
                return result;
            }

            if (hasLaundrySink || hasWasher)
            {
                result.Type = RoomType.Laundry;
                result.Confidence = 0.8;
                result.Method = "Fixture";
                result.Reason = "LaundrySink/WashingMachine = Lavanderia";
                return result;
            }

            if (hasShower)
            {
                result.Type = RoomType.Bathroom;
                result.Confidence = 0.65;
                result.Method = "Fixture";
                result.Reason = "Shower = provavel Banheiro";
                return result;
            }

            if (hasSink)
            {
                result.Type = RoomType.Pantry;
                result.Confidence = 0.5;
                result.Method = "Fixture";
                result.Reason = "Sink generico = provavel Copa";
                return result;
            }

            if (hasDrain)
            {
                result.Type = RoomType.ServiceArea;
                result.Confidence = 0.4;
                result.Method = "Fixture";
                result.Reason = "Drain = provavel Area de Servico";
                return result;
            }

            result.Reason = "Fixtures insuficientes";
            return result;
        }

        // ── Combinação ──

        private RoomAnalysisResult CombineResults(string roomName,
            RoomAnalysisResult nameResult, RoomAnalysisResult fixtureResult,
            List<FixtureType> fixtures)
        {
            var result = new RoomAnalysisResult();

            // Caso 1: Nome + fixtures
            if (nameResult.IsHydraulic && fixtureResult.IsHydraulic)
            {
                result.Type = nameResult.Type;
                result.Confidence = System.Math.Min(1.0, nameResult.Confidence + 0.15);
                result.Method = "Name+Fixture";
                result.Reason = $"Nome ({nameResult.Type}) confirmado por fixtures";

                result.Type = RefineRoomType(result.Type, roomName, fixtures);
                return result;
            }

            // Caso 2: Só nome
            if (nameResult.IsHydraulic)
            {
                result.Type = nameResult.Type;
                result.Confidence = nameResult.Confidence;
                result.Method = "Name";
                result.Reason = nameResult.Reason;
                return result;
            }

            // Caso 3: Só fixtures
            if (fixtureResult.IsHydraulic)
            {
                result.Type = fixtureResult.Type;
                result.Confidence = fixtureResult.Confidence;
                result.Method = "Fixture";
                result.Reason = fixtureResult.Reason;
                return result;
            }

            // Caso 4: Nada
            result.Reason = "Sem evidencia hidraulica";
            return result;
        }

        private RoomType RefineRoomType(RoomType baseType, string roomName, List<FixtureType> fixtures)
        {
            if (baseType != RoomType.Bathroom) return baseType;

            string lower = (roomName ?? "").ToLowerInvariant();
            if (Regex.IsMatch(lower, @"su[ií]te|master|ensuite"))
                return RoomType.SuiteBathroom;
            if (Regex.IsMatch(lower, @"pcd|pne|acess[ií]vel|deficiente|ada"))
                return RoomType.AccessibleBathroom;

            return baseType;
        }

        // ── Compatibilidade ──

        public bool TryClassify(string roomName, out RoomType roomType)
        {
            var result = ClassifyByName(roomName);
            roomType = result.Type;
            return result.IsHydraulic;
        }
    }
}
