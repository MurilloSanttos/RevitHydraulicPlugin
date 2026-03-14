using RevitHydraulicPlugin.Detection;
using RevitHydraulicPlugin.Models;
using RevitHydraulicPlugin.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RevitHydraulicPlugin.Configuration
{
    /// <summary>
    /// Resultado da análise e classificação de um ambiente.
    /// </summary>
    public class RoomAnalysisResult
    {
        /// <summary>Tipo do ambiente classificado.</summary>
        public RoomType Type { get; set; } = RoomType.Unknown;

        /// <summary>Confiança da classificação (0.0 a 1.0).</summary>
        public double Confidence { get; set; }

        /// <summary>Método que determinou a classificação.</summary>
        public string Method { get; set; } = "None";

        /// <summary>Se o ambiente foi classificado como hidráulico.</summary>
        public bool IsHydraulic => Type != RoomType.Unknown;

        /// <summary>Justificativa da classificação (para log/debug).</summary>
        public string Reason { get; set; } = "";
    }

    /// <summary>
    /// Serviço de classificação de ambientes hidráulicos.
    /// 
    /// VERSÃO 2.0 — Classificação multi-critério em 3 camadas:
    /// 
    ///   Camada 1: Análise do nome do Room (regex, normalização, abreviações)
    ///   Camada 2: Análise de fixtures presentes no ambiente
    ///   Camada 3: Combinação dos critérios para decisão final
    /// 
    /// Regras de decisão:
    /// - Nome hidráulico conhecido → classifica com alta confiança
    /// - Nome genérico MAS com fixtures → classifica pelo tipo de fixture
    /// - Nome genérico SEM fixtures → Unknown (não hidráulico)
    /// - Fixtures confirmam nome → aumenta confiança
    /// 
    /// Substitui o RoomClassification estático com lógica mais robusta.
    /// </summary>
    public class RoomClassifierService
    {
        // ════════════════════════════════════════════════
        //  PADRÕES DE NOME (CAMADA 1)
        // ════════════════════════════════════════════════

        /// <summary>
        /// Padrões regex para cada tipo de ambiente.
        /// Avaliados na ordem listada. Suportam PT-BR e EN com variações.
        /// </summary>
        private static readonly List<RoomNamePattern> NamePatterns = new List<RoomNamePattern>
        {
            // ── Banheiro PCD (deve vir ANTES de Banheiro genérico) ──
            new RoomNamePattern(RoomType.AccessibleBathroom, new[]
            {
                @"banheiro\s*(pcd|pne|acess[ií]vel|deficiente)",
                @"(pcd|pne|acess[ií]vel)\s*banheiro",
                @"wc\s*(pcd|pne|acess[ií]vel)",
                @"accessible\s*bath",
                @"ada\s*bath"
            }),

            // ── Banheiro Suíte (deve vir ANTES de Banheiro genérico) ──
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

            // ── Banheiro genérico ──
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

            // ── Lavabo ──
            new RoomNamePattern(RoomType.Lavatory, new[]
            {
                @"\blavabo\b",
                @"\bpowder\s*room\b",
                @"\bhalf\s*bath\b",
                @"\bguest\s*bath\b"
            }),

            // ── Copa (deve vir ANTES de Cozinha, pois "copa/cozinha" é cozinha) ──
            new RoomNamePattern(RoomType.Pantry, new[]
            {
                @"\bcopa\b(?!\s*/\s*cozinha)(?!\s*cozinha)",
                @"\bpantry\b",
                @"\bkitchenette\b",
                @"\bcop\.\b"
            }),

            // ── Cozinha ──
            new RoomNamePattern(RoomType.Kitchen, new[]
            {
                @"\bcozinha\b",
                @"\bkitchen\b",
                @"\bcopa\s*/\s*cozinha",
                @"\bcopa\s*cozinha",
                @"\bcoz\.\b"
            }),

            // ── Área de Serviço ──
            new RoomNamePattern(RoomType.ServiceArea, new[]
            {
                @"[aá]rea\s*(de\s*)?servi[cç]o",
                @"\ba\.?\s*s\.\b",
                @"\bservice\s*area\b",
                @"\butility\s*room\b",
                @"\butility\b"
            }),

            // ── Lavanderia ──
            new RoomNamePattern(RoomType.Laundry, new[]
            {
                @"\blavanderia\b",
                @"\blaundry\b",
                @"\bwash\s*room\b(?!.*bath)"
            })
        };

        // ════════════════════════════════════════════════
        //  CLASSIFICAÇÃO PRINCIPAL
        // ════════════════════════════════════════════════

        /// <summary>
        /// Classifica um ambiente usando apenas o nome (sem fixtures).
        /// Útil na primeira passada, antes de detectar equipamentos.
        /// </summary>
        /// <param name="roomName">Nome do Room.</param>
        /// <returns>Resultado da classificação.</returns>
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
                        result.Reason = $"Nome '{roomName}' match com padrao '{regex}'";
                        return result;
                    }
                }
            }

            result.Reason = $"Nome '{roomName}' nao reconhecido";
            return result;
        }

        /// <summary>
        /// Classifica um ambiente usando o nome E a lista de fixtures presentes.
        /// Esta é a classificação completa multi-critério.
        /// </summary>
        /// <param name="roomName">Nome do Room.</param>
        /// <param name="fixtures">Lista de FixtureType presentes no ambiente.</param>
        /// <returns>Resultado da classificação com confiança combinada.</returns>
        public RoomAnalysisResult ClassifyWithFixtures(
            string roomName,
            List<FixtureType> fixtures)
        {
            // ── Camada 1: Classificação por Nome ──
            var nameResult = ClassifyByName(roomName);

            // ── Camada 2: Análise de Fixtures ──
            var fixtureResult = AnalyzeFixtures(fixtures);

            // ── Camada 3: Combinação ──
            return CombineResults(roomName, nameResult, fixtureResult, fixtures);
        }

        // ════════════════════════════════════════════════
        //  CAMADA 2: ANÁLISE DE FIXTURES
        // ════════════════════════════════════════════════

        /// <summary>
        /// Analisa a combinação de fixtures para inferir o tipo de ambiente.
        /// 
        /// Heurísticas:
        /// - Toilet + Sink/Shower → Banheiro
        /// - Toilet + Sink (sem chuveiro) → Lavabo
        /// - KitchenSink → Cozinha
        /// - LaundrySink/WashingMachine → Lavanderia/Área de Serviço
        /// - Somente Drain → Área de Serviço
        /// </summary>
        private RoomAnalysisResult AnalyzeFixtures(List<FixtureType> fixtures)
        {
            var result = new RoomAnalysisResult();

            if (fixtures == null || fixtures.Count == 0)
            {
                result.Reason = "Nenhum fixture presente";
                return result;
            }

            bool hasToilet = fixtures.Contains(FixtureType.Toilet);
            bool hasSink = fixtures.Contains(FixtureType.Sink);
            bool hasShower = fixtures.Contains(FixtureType.Shower);
            bool hasKitchenSink = fixtures.Contains(FixtureType.KitchenSink);
            bool hasLaundrySink = fixtures.Contains(FixtureType.LaundrySink);
            bool hasWasher = fixtures.Contains(FixtureType.WashingMachine);
            bool hasDrain = fixtures.Contains(FixtureType.Drain);

            // Banheiro: toilet + (sink ou shower)
            if (hasToilet && (hasSink || hasShower))
            {
                result.Type = RoomType.Bathroom;
                result.Confidence = 0.9;
                result.Method = "Fixture";
                result.Reason = "Toilet + Sink/Shower = Banheiro";
                return result;
            }

            // Lavabo: toilet + sink, SEM chuveiro
            if (hasToilet && hasSink && !hasShower)
            {
                result.Type = RoomType.Lavatory;
                result.Confidence = 0.85;
                result.Method = "Fixture";
                result.Reason = "Toilet + Sink (sem shower) = Lavabo";
                return result;
            }

            // Apenas vaso (pode ser banheiro simples)
            if (hasToilet)
            {
                result.Type = RoomType.Bathroom;
                result.Confidence = 0.7;
                result.Method = "Fixture";
                result.Reason = "Toilet presente = provavel Banheiro";
                return result;
            }

            // Cozinha: pia de cozinha
            if (hasKitchenSink)
            {
                result.Type = RoomType.Kitchen;
                result.Confidence = 0.85;
                result.Method = "Fixture";
                result.Reason = "KitchenSink presente = Cozinha";
                return result;
            }

            // Lavanderia: tanque ou máquina de lavar
            if (hasLaundrySink || hasWasher)
            {
                result.Type = RoomType.Laundry;
                result.Confidence = 0.8;
                result.Method = "Fixture";
                result.Reason = "LaundrySink/WashingMachine = Lavanderia";
                return result;
            }

            // Apenas chuveiro (pode ser banheiro)
            if (hasShower)
            {
                result.Type = RoomType.Bathroom;
                result.Confidence = 0.65;
                result.Method = "Fixture";
                result.Reason = "Shower presente = provavel Banheiro";
                return result;
            }

            // Apenas sink genérico (pode ser lavabo ou copa)
            if (hasSink)
            {
                result.Type = RoomType.Pantry;
                result.Confidence = 0.5;
                result.Method = "Fixture";
                result.Reason = "Sink generico = provavel Copa/Lavabo";
                return result;
            }

            // Apenas ralo (área de serviço)
            if (hasDrain)
            {
                result.Type = RoomType.ServiceArea;
                result.Confidence = 0.4;
                result.Method = "Fixture";
                result.Reason = "Drain = provavel Area de Servico";
                return result;
            }

            result.Reason = "Fixtures insuficientes para classificar";
            return result;
        }

        // ════════════════════════════════════════════════
        //  CAMADA 3: COMBINAÇÃO
        // ════════════════════════════════════════════════

        /// <summary>
        /// Combina resultado da análise por nome e por fixtures.
        /// 
        /// Prioridades:
        ///   1. Nome forte + fixtures confirmam → alta confiança
        ///   2. Nome forte + sem fixtures → confiança do nome
        ///   3. Nome desconhecido + fixtures fortes → usa fixtures
        ///   4. Nome desconhecido + sem fixtures → Unknown
        /// </summary>
        private RoomAnalysisResult CombineResults(
            string roomName,
            RoomAnalysisResult nameResult,
            RoomAnalysisResult fixtureResult,
            List<FixtureType> fixtures)
        {
            var result = new RoomAnalysisResult();
            int fixtureCount = fixtures?.Count ?? 0;

            // Caso 1: Nome identificou E fixtures confirmam
            if (nameResult.IsHydraulic && fixtureResult.IsHydraulic)
            {
                // Se concordam no tipo → alta confiança
                if (AreCompatibleTypes(nameResult.Type, fixtureResult.Type))
                {
                    result.Type = nameResult.Type; // Nome tem precedência
                    result.Confidence = System.Math.Min(1.0, nameResult.Confidence + 0.15);
                    result.Method = "Name+Fixture";
                    result.Reason = $"Nome ({nameResult.Type}) confirmado por fixtures ({fixtureCount} fixtures)";
                }
                else
                {
                    // Discordam — nome tem precedência, mas confiança reduzida
                    result.Type = nameResult.Type;
                    result.Confidence = nameResult.Confidence * 0.9;
                    result.Method = "Name (fixture diverge)";
                    result.Reason = $"Nome ({nameResult.Type}) vs fixtures ({fixtureResult.Type}) — usado nome";
                }

                // Refinamento: "Banheiro" com nome de suíte → SuiteBathroom
                result.Type = RefineRoomType(result.Type, roomName, fixtures);
                return result;
            }

            // Caso 2: Nome identificou, sem fixtures
            if (nameResult.IsHydraulic && !fixtureResult.IsHydraulic)
            {
                result.Type = nameResult.Type;
                result.Confidence = nameResult.Confidence;
                result.Method = "Name";
                result.Reason = $"Classificado por nome '{roomName}' (sem fixtures detectados)";

                Logger.Debug($"[ROOM-CLASSIFY] '{roomName}' classificado por nome sem fixtures — "
                    + "fixtures serao analisados apos deteccao de equipamentos");
                return result;
            }

            // Caso 3: Nome desconhecido, mas fixtures indicam ambiente hidráulico
            if (!nameResult.IsHydraulic && fixtureResult.IsHydraulic)
            {
                result.Type = fixtureResult.Type;
                result.Confidence = fixtureResult.Confidence;
                result.Method = "Fixture";
                result.Reason = $"Nome '{roomName}' nao reconhecido, mas {fixtureCount} fixture(s) indicam {fixtureResult.Type}";

                Logger.Warning($"[ROOM-CLASSIFY] '{roomName}' nao reconhecido por nome, "
                    + $"mas fixtures indicam {fixtureResult.Type}");
                return result;
            }

            // Caso 4: Nada identificou
            result.Type = RoomType.Unknown;
            result.Confidence = 0.0;
            result.Method = "None";
            result.Reason = $"Nome '{roomName}' nao reconhecido e sem fixtures hidraulicos";
            return result;
        }

        // ════════════════════════════════════════════════
        //  REFINAMENTO
        // ════════════════════════════════════════════════

        /// <summary>
        /// Refina o tipo do ambiente baseado em padrões específicos no nome.
        /// Ex: "Banheiro Suíte" → SuiteBathroom
        /// </summary>
        private RoomType RefineRoomType(RoomType baseType, string roomName, List<FixtureType> fixtures)
        {
            if (baseType != RoomType.Bathroom) return baseType;

            string lower = (roomName ?? "").ToLowerInvariant();

            // Verifica se é suíte
            if (Regex.IsMatch(lower, @"su[ií]te|master|ensuite|en\s*suite"))
                return RoomType.SuiteBathroom;

            // Verifica se é PCD
            if (Regex.IsMatch(lower, @"pcd|pne|acess[ií]vel|deficiente|ada"))
                return RoomType.AccessibleBathroom;

            // Verifica se é lavabo (banheiro sem chuveiro)
            if (fixtures != null
                && fixtures.Contains(FixtureType.Toilet)
                && fixtures.Contains(FixtureType.Sink)
                && !fixtures.Contains(FixtureType.Shower)
                && Regex.IsMatch(lower, @"lavabo|half\s*bath|powder"))
            {
                return RoomType.Lavatory;
            }

            return baseType;
        }

        /// <summary>
        /// Verifica se dois RoomTypes são compatíveis (mesmo grupo funcional).
        /// Ex: Bathroom e SuiteBathroom são compatíveis.
        /// </summary>
        private bool AreCompatibleTypes(RoomType a, RoomType b)
        {
            if (a == b) return true;

            // Banheiros são compatíveis entre si
            var bathroomGroup = new HashSet<RoomType>
            {
                RoomType.Bathroom, RoomType.SuiteBathroom,
                RoomType.AccessibleBathroom, RoomType.Lavatory
            };

            if (bathroomGroup.Contains(a) && bathroomGroup.Contains(b))
                return true;

            // Cozinha e Copa são compatíveis
            if ((a == RoomType.Kitchen || a == RoomType.Pantry)
                && (b == RoomType.Kitchen || b == RoomType.Pantry))
                return true;

            // Lavanderia e Área de Serviço são compatíveis
            if ((a == RoomType.Laundry || a == RoomType.ServiceArea)
                && (b == RoomType.Laundry || b == RoomType.ServiceArea))
                return true;

            return false;
        }

        // ════════════════════════════════════════════════
        //  COMPATIBILIDADE LEGADA
        // ════════════════════════════════════════════════

        /// <summary>
        /// Compatibilidade com o RoomClassification.TryClassify estático anterior.
        /// Mantém a interface antiga funcionando.
        /// </summary>
        public bool TryClassify(string roomName, out RoomType roomType)
        {
            var result = ClassifyByName(roomName);
            roomType = result.Type;
            return result.IsHydraulic;
        }

        /// <summary>
        /// Verifica se um nome indica ambiente hidráulico.
        /// </summary>
        public bool IsHydraulicRoom(string roomName)
        {
            return ClassifyByName(roomName).IsHydraulic;
        }

        // ════════════════════════════════════════════════
        //  CLASSE INTERNA
        // ════════════════════════════════════════════════

        /// <summary>Padrão de nome para um tipo de ambiente.</summary>
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
    }
}
