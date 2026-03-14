using RevitHydraulicPlugin.TestEnvironment.Mocks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RevitHydraulicPlugin.TestEnvironment.Services
{
    /// <summary>
    /// Resultado da classificação de um fixture.
    /// </summary>
    public class FixtureClassificationResult
    {
        public FixtureType Type { get; set; } = FixtureType.Unknown;
        public double Confidence { get; set; }
        public string Method { get; set; } = "None";
        public string Reason { get; set; } = "";
    }

    /// <summary>
    /// Serviço de classificação de equipamentos hidráulicos para testes.
    /// Espelha FixtureClassifierService do plugin principal (v2.0).
    /// 
    /// Classificação em 3 camadas:
    ///   1. Nome da família/tipo (patterns PT/EN)
    ///   2. Parâmetros do elemento (categoria)
    ///   3. Conectores MEP (tipo de sistema, diâmetro)
    /// </summary>
    public class FixtureClassifierTestService
    {
        // ── Padrões de nome para cada FixtureType ──

        private static readonly Dictionary<FixtureType, string[]> NamePatterns =
            new Dictionary<FixtureType, string[]>
            {
                {
                    FixtureType.Toilet, new[]
                    {
                        @"\bvaso\b", @"\btoilet\b", @"\bbacia\b",
                        @"\bwc\b", @"\bsanit[aá]rio\b", @"\bcaixa\s*acoplada\b"
                    }
                },
                {
                    FixtureType.Sink, new[]
                    {
                        @"\blavat[oó]rio\b", @"\blavat\b", @"\bbasin\b",
                        @"\bsink\b(?!.*kitchen)(?!.*cozinha)",
                        @"\blava[çc][aã]o\b"
                    }
                },
                {
                    FixtureType.Shower, new[]
                    {
                        @"\bchuveiro\b", @"\bshower\b", @"\bducha\b"
                    }
                },
                {
                    FixtureType.KitchenSink, new[]
                    {
                        @"\bpia\b.*\bcozinha\b", @"\bpia\s*(de\s*)?cozinha\b",
                        @"\bpia\b.*\binox\b", @"\bkitchen\s*sink\b",
                        @"\bpia\b(?!.*tanque)(?!.*lavat)"
                    }
                },
                {
                    FixtureType.LaundrySink, new[]
                    {
                        @"\btanque\b", @"\blaundry\s*sink\b",
                        @"\btanque\s*(de\s*)?lavar\b"
                    }
                },
                {
                    FixtureType.Drain, new[]
                    {
                        @"\bralo\b", @"\bdrain\b", @"\bfloor\s*drain\b",
                        @"\bralo\s*seco\b", @"\bralo\s*sifonado\b"
                    }
                },
                {
                    FixtureType.WashingMachine, new[]
                    {
                        @"\bm[aá]quina\b.*\blavar\b", @"\bwasher\b",
                        @"\bwashing\s*machine\b", @"\blava\s*roupa\b"
                    }
                }
            };

        // ════════════════════════════════════════════════
        //  CLASSIFICAÇÃO COMPLETA (3 camadas)
        // ════════════════════════════════════════════════

        /// <summary>
        /// Classifica um fixture usando nome + categoria + conectores.
        /// </summary>
        public FixtureClassificationResult Classify(MockFixture fixture)
        {
            // Camada 1: Nome
            var nameResult = ClassifyByName(fixture.FamilyName, fixture.TypeName);
            if (nameResult.Confidence >= 0.7)
                return nameResult;

            // Camada 2: Categoria
            var categoryResult = ClassifyByCategory(fixture.Category);
            if (categoryResult.Confidence >= 0.6)
            {
                // Se nome deu parcial, combina
                if (nameResult.Confidence > 0)
                {
                    categoryResult.Confidence += 0.1;
                    categoryResult.Method = "Name+Category";
                }
                return categoryResult;
            }

            // Camada 3: Conectores
            var connectorResult = ClassifyByConnectors(fixture.Connectors);
            if (connectorResult.Confidence >= 0.5)
            {
                connectorResult.Method = "Connector";
                return connectorResult;
            }

            // Combina evidências parciais
            if (nameResult.Type != FixtureType.Unknown)
                return nameResult;
            if (connectorResult.Type != FixtureType.Unknown)
                return connectorResult;

            return new FixtureClassificationResult
            {
                Reason = $"Sem evidencia suficiente para '{fixture.FamilyName}'"
            };
        }

        // ════════════════════════════════════════════════
        //  CAMADA 1: NOME
        // ════════════════════════════════════════════════

        public FixtureClassificationResult ClassifyByName(string familyName, string typeName)
        {
            string combined = $"{familyName ?? ""} {typeName ?? ""}".ToLowerInvariant();
            var result = new FixtureClassificationResult();

            foreach (var kvp in NamePatterns)
            {
                foreach (string pattern in kvp.Value)
                {
                    if (Regex.IsMatch(combined, pattern, RegexOptions.IgnoreCase))
                    {
                        result.Type = kvp.Key;
                        result.Confidence = 0.8;
                        result.Method = "Name";
                        result.Reason = $"'{familyName}' match padrao '{pattern}'";
                        return result;
                    }
                }
            }

            result.Reason = $"Nome '{familyName}' nao reconhecido";
            return result;
        }

        /// <summary>
        /// Versão simplificada que retorna apenas o FixtureType.
        /// </summary>
        public FixtureType ClassifyByNameOnly(string familyName, string typeName)
        {
            return ClassifyByName(familyName, typeName).Type;
        }

        // ════════════════════════════════════════════════
        //  CAMADA 2: CATEGORIA
        // ════════════════════════════════════════════════

        private FixtureClassificationResult ClassifyByCategory(string category)
        {
            var result = new FixtureClassificationResult();
            if (string.IsNullOrWhiteSpace(category)) return result;

            string lower = category.ToLowerInvariant();

            if (lower.Contains("plumbing") || lower.Contains("hidraulic"))
            {
                // Categoria genérica hidráulica, mas sem tipo específico
                result.Confidence = 0.3;
                result.Method = "Category";
                result.Reason = $"Categoria '{category}' indica equipamento hidraulico";
            }

            return result;
        }

        // ════════════════════════════════════════════════
        //  CAMADA 3: CONECTORES
        // ════════════════════════════════════════════════

        private FixtureClassificationResult ClassifyByConnectors(List<MockConnector> connectors)
        {
            var result = new FixtureClassificationResult();
            if (connectors == null || connectors.Count == 0) return result;

            bool hasSewer = connectors.Any(c => c.SystemType == ConnectorSystemType.Sewer);
            bool hasColdWater = connectors.Any(c => c.SystemType == ConnectorSystemType.ColdWater);
            var sewerDiameters = connectors
                .Where(c => c.SystemType == ConnectorSystemType.Sewer)
                .Select(c => c.DiameterMm).ToList();

            // Conector esgoto DN100 → provável vaso sanitário
            if (hasSewer && sewerDiameters.Any(d => d >= 100))
            {
                result.Type = FixtureType.Toilet;
                result.Confidence = 0.7;
                result.Method = "Connector";
                result.Reason = "Conector esgoto DN100+ = provavel Toilet";
                return result;
            }

            // Esgoto + Água Fria → provável lavatório/pia
            if (hasSewer && hasColdWater)
            {
                result.Type = FixtureType.Sink;
                result.Confidence = 0.5;
                result.Method = "Connector";
                result.Reason = "Esgoto + AF = provavel Sink";
                return result;
            }

            // Só esgoto DN50 → provável ralo
            if (hasSewer && !hasColdWater)
            {
                result.Type = FixtureType.Drain;
                result.Confidence = 0.5;
                result.Method = "Connector";
                result.Reason = "So esgoto = provavel Drain";
                return result;
            }

            return result;
        }

        // ════════════════════════════════════════════════
        //  CONVERSÃO LEGACY
        // ════════════════════════════════════════════════

        /// <summary>
        /// Converte FixtureType para EquipmentType legado.
        /// </summary>
        public static EquipmentType ToEquipmentType(FixtureType ft)
        {
            switch (ft)
            {
                case FixtureType.Toilet: return EquipmentType.VasoSanitario;
                case FixtureType.Sink: return EquipmentType.Lavatorio;
                case FixtureType.Shower: return EquipmentType.Chuveiro;
                case FixtureType.KitchenSink: return EquipmentType.Pia;
                case FixtureType.LaundrySink: return EquipmentType.Tanque;
                case FixtureType.Drain: return EquipmentType.Ralo;
                case FixtureType.WashingMachine: return EquipmentType.MaquinaLavar;
                default: return EquipmentType.Outro;
            }
        }
    }
}
