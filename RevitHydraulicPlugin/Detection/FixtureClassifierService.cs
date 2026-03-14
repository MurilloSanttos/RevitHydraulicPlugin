using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using RevitHydraulicPlugin.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RevitHydraulicPlugin.Detection
{
    /// <summary>
    /// Tipos de equipamentos hidráulicos (fixtures) reconhecidos pelo plugin.
    /// 
    /// Nomenclatura alinhada com normas brasileiras e internacionais.
    /// </summary>
    public enum FixtureType
    {
        /// <summary>Vaso sanitário, bacia sanitária (toilet, water closet)</summary>
        Toilet,

        /// <summary>Lavatório, cuba de banheiro (lavatory, wash basin)</summary>
        Sink,

        /// <summary>Chuveiro, ducha (shower)</summary>
        Shower,

        /// <summary>Pia de cozinha (kitchen sink)</summary>
        KitchenSink,

        /// <summary>Tanque de lavar roupa (laundry sink, utility sink)</summary>
        LaundrySink,

        /// <summary>Ralo, ralo sifonado (floor drain, drain)</summary>
        Drain,

        /// <summary>Máquina de lavar roupa (washing machine)</summary>
        WashingMachine,

        /// <summary>Tipo não identificado</summary>
        Unknown
    }

    /// <summary>
    /// Resultado da classificação de um equipamento hidráulico.
    /// Contém o tipo identificado e metadados da classificação.
    /// </summary>
    public class FixtureClassificationResult
    {
        /// <summary>Tipo do equipamento classificado.</summary>
        public FixtureType Type { get; set; } = FixtureType.Unknown;

        /// <summary>Nível de confiança da classificação (0.0 a 1.0).</summary>
        public double Confidence { get; set; }

        /// <summary>Método que determinou a classificação (Name, Parameter, Connector, Combined).</summary>
        public string ClassificationMethod { get; set; } = "None";

        /// <summary>Nome da família original.</summary>
        public string FamilyName { get; set; }

        /// <summary>Nome do tipo original.</summary>
        public string TypeName { get; set; }

        /// <summary>Número de conectores de tubulação encontrados.</summary>
        public int PipingConnectorCount { get; set; }

        /// <summary>Se possui conector de esgoto (Sanitary).</summary>
        public bool HasSanitaryConnector { get; set; }

        /// <summary>Se possui conector de água fria (DomesticColdWater).</summary>
        public bool HasColdWaterConnector { get; set; }

        /// <summary>Se possui conector de água quente (DomesticHotWater).</summary>
        public bool HasHotWaterConnector { get; set; }

        public override string ToString()
        {
            return $"{Type} (confianca: {Confidence:P0}, metodo: {ClassificationMethod})";
        }
    }

    /// <summary>
    /// Serviço de classificação de equipamentos hidráulicos (fixtures).
    /// 
    /// Usa uma estratégia multi-critério em 3 camadas para maximizar
    /// a confiabilidade da classificação:
    /// 
    ///   Camada 1: Análise do nome da família e do tipo (keywords + regex)
    ///   Camada 2: Análise de parâmetros do elemento (BuiltInParameters)
    ///   Camada 3: Análise dos conectores MEP (tipo de sistema, diâmetro)
    /// 
    /// As camadas são combinadas com pesos para gerar um score de confiança.
    /// 
    /// Princípios:
    /// - Sem dependência de nomes hardcoded (usa padrões flexíveis)
    /// - Supora nomes em português, inglês e abreviações
    /// - Fallback inteligente usando conectores quando o nome não é reconhecível
    /// </summary>
    public class FixtureClassifierService
    {
        // ════════════════════════════════════════════════
        //  PADRÕES DE CLASSIFICAÇÃO POR NOME (CAMADA 1)
        // ════════════════════════════════════════════════

        /// <summary>
        /// Mapeamento de padrões regex para cada tipo de fixture.
        /// Suporta nomes em português e inglês, com variações comuns.
        /// Os padrões são avaliados na ordem listada (primeiro match ganha).
        /// </summary>
        private static readonly List<FixturePattern> NamePatterns = new List<FixturePattern>
        {
            // ── Vaso Sanitário (Toilet) ──
            // Match: "vaso sanitário", "bacia sanitária", "toilet", "water closet", "WC"
            new FixturePattern(FixtureType.Toilet, new[]
            {
                @"vaso\s*sanit[aá]ri",
                @"bacia\s*sanit[aá]ri",
                @"\bvaso\b",
                @"\btoilet\b",
                @"\bwater\s*closet\b",
                @"\bw\.?\s*c\.?\b"
            }),

            // ── Pia de Cozinha (KitchenSink) ──
            // DEVE vir antes de Sink genérico para não ser capturada como lavatório.
            // Match: "pia de cozinha", "kitchen sink", "pia inox"
            new FixturePattern(FixtureType.KitchenSink, new[]
            {
                @"pia\s*(de\s*)?cozinha",
                @"kitchen\s*sink",
                @"pia\s*inox",
                @"\bpia\b(?!.*banheiro)"  // "pia" sem "banheiro" no contexto
            }),

            // ── Lavatório / Cuba de Banheiro (Sink) ──
            // Match: "lavatório", "lavatory", "wash basin", "cuba"
            new FixturePattern(FixtureType.Sink, new[]
            {
                @"lavat[oó]ri",
                @"\blavat\b",
                @"\blavatory\b",
                @"\bwash\s*basin\b",
                @"\bbasin\b",
                @"\bcuba\b",
                @"\bsink\b(?!.*kitchen)(?!.*laundry)"  // "sink" sem "kitchen"/"laundry"
            }),

            // ── Chuveiro (Shower) ──
            // Match: "chuveiro", "shower", "ducha"
            new FixturePattern(FixtureType.Shower, new[]
            {
                @"\bchuveiro\b",
                @"\bshower\b",
                @"\bducha\b"
            }),

            // ── Tanque de Lavar (LaundrySink) ──
            // Match: "tanque", "laundry sink", "utility sink"
            new FixturePattern(FixtureType.LaundrySink, new[]
            {
                @"\btanque\b",
                @"laundry\s*sink",
                @"utility\s*sink",
                @"\blaundry\s*tub\b"
            }),

            // ── Ralo (Drain) ──
            // Match: "ralo", "floor drain", "drain", "ralo sifonado"
            new FixturePattern(FixtureType.Drain, new[]
            {
                @"\bralo\b",
                @"floor\s*drain",
                @"ralo\s*sifon",
                @"\bdrain\b"
            }),

            // ── Máquina de Lavar (WashingMachine) ──
            // Match: "máquina de lavar", "washing machine", "washer"
            new FixturePattern(FixtureType.WashingMachine, new[]
            {
                @"m[aá]quina\s*(de\s*)?lavar",
                @"\bwashing\s*machine\b",
                @"\bwasher\b",
                @"\bmaquina\b"
            })
        };

        // ════════════════════════════════════════════════
        //  CLASSIFICAÇÃO PRINCIPAL
        // ════════════════════════════════════════════════

        /// <summary>
        /// Classifica um FamilyInstance como um tipo de fixture hidráulico.
        /// 
        /// Estratégia multi-camada:
        ///   1. Tenta classificar pelo nome da família/tipo (peso: 0.5)
        ///   2. Tenta classificar por parâmetros do elemento (peso: 0.3)
        ///   3. Analisa conectores MEP para refinar/confirmar (peso: 0.2)
        ///   4. Combina os scores para decisão final
        /// </summary>
        /// <param name="instance">FamilyInstance a classificar.</param>
        /// <returns>Resultado da classificação com tipo e confiança.</returns>
        public FixtureClassificationResult Classify(FamilyInstance instance)
        {
            var result = new FixtureClassificationResult
            {
                FamilyName = instance.Symbol?.Family?.Name ?? "",
                TypeName = instance.Symbol?.Name ?? ""
            };

            // ── Camada 1: Classificação por Nome ──
            var nameResult = ClassifyByName(result.FamilyName, result.TypeName);

            // ── Camada 2: Classificação por Parâmetros ──
            var paramResult = ClassifyByParameters(instance);

            // ── Camada 3: Análise de Conectores ──
            var connectorResult = AnalyzeConnectors(instance);

            // Preenche informações de conectores no resultado
            result.PipingConnectorCount = connectorResult.ConnectorCount;
            result.HasSanitaryConnector = connectorResult.HasSanitary;
            result.HasColdWaterConnector = connectorResult.HasColdWater;
            result.HasHotWaterConnector = connectorResult.HasHotWater;

            // ── Decisão Combinada ──
            result = CombineResults(result, nameResult, paramResult, connectorResult);

            // ── Log da classificação ──
            Logger.LogFixtureClassification(
                result.FamilyName,
                result.Type.ToString(),
                result.ClassificationMethod,
                result.PipingConnectorCount);

            return result;
        }

        /// <summary>
        /// Classificação simplificada por nome apenas (para uso sem FamilyInstance).
        /// Útil no ambiente de testes e para classificação rápida.
        /// </summary>
        /// <param name="familyName">Nome da família.</param>
        /// <param name="typeName">Nome do tipo (opcional).</param>
        /// <returns>FixtureType identificado.</returns>
        public FixtureType ClassifyByNameOnly(string familyName, string typeName = "")
        {
            var result = ClassifyByName(familyName, typeName);
            return result.Type;
        }

        // ════════════════════════════════════════════════
        //  CAMADA 1: CLASSIFICAÇÃO POR NOME
        // ════════════════════════════════════════════════

        /// <summary>
        /// Analisa o nome da família e do tipo usando padrões regex.
        /// Normaliza o texto (lowercase, sem acentos desnecessários) antes de comparar.
        /// </summary>
        private NameClassification ClassifyByName(string familyName, string typeName)
        {
            var result = new NameClassification { Type = FixtureType.Unknown, Confidence = 0.0 };

            if (string.IsNullOrWhiteSpace(familyName) && string.IsNullOrWhiteSpace(typeName))
                return result;

            // Combina nome da família + tipo para análise completa
            string combined = $"{familyName} {typeName}".ToLowerInvariant().Trim();

            foreach (var pattern in NamePatterns)
            {
                foreach (string regex in pattern.Patterns)
                {
                    if (Regex.IsMatch(combined, regex, RegexOptions.IgnoreCase))
                    {
                        result.Type = pattern.Type;
                        result.Confidence = 0.8; // Alta confiança quando nome bate
                        result.MatchedPattern = regex;
                        return result;
                    }
                }
            }

            return result;
        }

        // ════════════════════════════════════════════════
        //  CAMADA 2: CLASSIFICAÇÃO POR PARÂMETROS
        // ════════════════════════════════════════════════

        /// <summary>
        /// Analisa parâmetros do FamilyInstance para extrair informações adicionais.
        /// Verifica parâmetros como Description, Type Comments e parâmetros customizados.
        /// </summary>
        private ParameterClassification ClassifyByParameters(FamilyInstance instance)
        {
            var result = new ParameterClassification { Type = FixtureType.Unknown, Confidence = 0.0 };

            // Tenta parâmetros built-in que podem conter descrição do equipamento
            var parametersToCheck = new[]
            {
                BuiltInParameter.ALL_MODEL_DESCRIPTION,
                BuiltInParameter.ALL_MODEL_TYPE_COMMENTS,
                BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS
            };

            foreach (var paramId in parametersToCheck)
            {
                try
                {
                    var param = instance.get_Parameter(paramId);
                    if (param == null) continue;

                    string value = param.AsString();
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    // Tenta classificar o valor do parâmetro como se fosse um nome
                    var nameResult = ClassifyByName(value, "");
                    if (nameResult.Type != FixtureType.Unknown)
                    {
                        result.Type = nameResult.Type;
                        result.Confidence = 0.6; // Confiança média (parâmetro não é tão confiável quanto nome)
                        result.SourceParameter = paramId.ToString();
                        return result;
                    }
                }
                catch
                {
                    // Parâmetro pode não existir no elemento - ignorar
                }
            }

            // Tenta também o parâmetro de Marca (mark)
            try
            {
                var markParam = instance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                if (markParam != null)
                {
                    string mark = markParam.AsString();
                    if (!string.IsNullOrWhiteSpace(mark))
                    {
                        var nameResult = ClassifyByName(mark, "");
                        if (nameResult.Type != FixtureType.Unknown)
                        {
                            result.Type = nameResult.Type;
                            result.Confidence = 0.4; // Baixa confiança para Mark
                            result.SourceParameter = "ALL_MODEL_MARK";
                            return result;
                        }
                    }
                }
            }
            catch { }

            return result;
        }

        // ════════════════════════════════════════════════
        //  CAMADA 3: ANÁLISE DE CONECTORES
        // ════════════════════════════════════════════════

        /// <summary>
        /// Analisa os conectores MEP do equipamento para inferir o tipo.
        /// 
        /// Lógica:
        /// - Somente esgoto (Sanitary) → provável Ralo
        /// - Esgoto + Água Fria → provável Vaso, Lavatório ou Pia
        /// - Esgoto + AF + AQ → provável Chuveiro, Lavatório ou Pia
        /// - Diâmetro esgoto >= 100mm → Vaso Sanitário
        /// - Diâmetro esgoto <= 50mm → Lavatório, Pia, Chuveiro ou Ralo
        /// </summary>
        private ConnectorAnalysis AnalyzeConnectors(FamilyInstance instance)
        {
            var result = new ConnectorAnalysis();

            if (instance?.MEPModel?.ConnectorManager == null)
            {
                result.InferredType = FixtureType.Unknown;
                result.Confidence = 0.0;
                return result;
            }

            foreach (Connector connector in instance.MEPModel.ConnectorManager.Connectors)
            {
                if (connector.Domain != Domain.DomainPiping) continue;

                result.ConnectorCount++;
                double diamMm = UnitConversionHelper.FeetToMm(connector.Radius * 2);

                try
                {
                    var systemType = connector.PipeSystemType;

                    switch (systemType)
                    {
                        case PipeSystemType.Sanitary:
                            result.HasSanitary = true;
                            result.SanitaryDiameterMm = diamMm;
                            break;
                        case PipeSystemType.DomesticColdWater:
                            result.HasColdWater = true;
                            result.ColdWaterDiameterMm = diamMm;
                            break;
                        case PipeSystemType.DomesticHotWater:
                            result.HasHotWater = true;
                            result.HotWaterDiameterMm = diamMm;
                            break;
                    }
                }
                catch
                {
                    // Conector sem PipeSystemType definido
                }
            }

            // ── Inferência por conectores ──

            if (result.ConnectorCount == 0)
            {
                result.InferredType = FixtureType.Unknown;
                result.Confidence = 0.0;
                return result;
            }

            // Esgoto com diâmetro >= 100mm → Vaso Sanitário
            if (result.HasSanitary && result.SanitaryDiameterMm >= 90)
            {
                result.InferredType = FixtureType.Toilet;
                result.Confidence = 0.7;
            }
            // Somente esgoto, sem água → Ralo
            else if (result.HasSanitary && !result.HasColdWater && !result.HasHotWater)
            {
                result.InferredType = FixtureType.Drain;
                result.Confidence = 0.6;
            }
            // Esgoto + AF + AQ → Chuveiro ou Lavatório (ambos usam água quente)
            else if (result.HasSanitary && result.HasColdWater && result.HasHotWater)
            {
                // Chuveiro geralmente tem diâmetro menor de AF
                result.InferredType = FixtureType.Sink; // Default para Lavatório
                result.Confidence = 0.4; // Baixa confiança (pode ser chuveiro ou lavatório)
            }
            // Esgoto + AF (sem AQ) → Vaso ou Pia (equipamentos sem água quente)
            else if (result.HasSanitary && result.HasColdWater)
            {
                result.InferredType = FixtureType.Toilet; // Default
                result.Confidence = 0.3; // Muito baixa (precisa do nome para confirmar)
            }
            else
            {
                result.InferredType = FixtureType.Unknown;
                result.Confidence = 0.1;
            }

            return result;
        }

        // ════════════════════════════════════════════════
        //  COMBINAÇÃO DOS RESULTADOS
        // ════════════════════════════════════════════════

        /// <summary>
        /// Combina os resultados das 3 camadas para a decisão final.
        /// 
        /// Prioridade:
        ///   1. Se nome identificou com alta confiança → usa nome
        ///   2. Se nome não identificou, tenta parâmetros
        ///   3. Se nada funcionou, usa inferência por conectores
        ///   4. Se conector confirma o nome → aumenta confiança
        /// </summary>
        private FixtureClassificationResult CombineResults(
            FixtureClassificationResult result,
            NameClassification nameResult,
            ParameterClassification paramResult,
            ConnectorAnalysis connectorResult)
        {
            // Caso 1: Nome identificou com confiança
            if (nameResult.Type != FixtureType.Unknown && nameResult.Confidence >= 0.5)
            {
                result.Type = nameResult.Type;
                result.Confidence = nameResult.Confidence;
                result.ClassificationMethod = "Name";

                // Bonus: se conector confirma → aumenta confiança
                if (connectorResult.InferredType == nameResult.Type)
                {
                    result.Confidence = System.Math.Min(1.0, result.Confidence + 0.15);
                    result.ClassificationMethod = "Name+Connector";
                }

                return result;
            }

            // Caso 2: Parâmetros identificaram
            if (paramResult.Type != FixtureType.Unknown && paramResult.Confidence >= 0.4)
            {
                result.Type = paramResult.Type;
                result.Confidence = paramResult.Confidence;
                result.ClassificationMethod = "Parameter";

                // Bonus: se conector confirma
                if (connectorResult.InferredType == paramResult.Type)
                {
                    result.Confidence = System.Math.Min(1.0, result.Confidence + 0.2);
                    result.ClassificationMethod = "Parameter+Connector";
                }

                return result;
            }

            // Caso 3: Fallback para conectores
            if (connectorResult.InferredType != FixtureType.Unknown && connectorResult.Confidence >= 0.3)
            {
                result.Type = connectorResult.InferredType;
                result.Confidence = connectorResult.Confidence;
                result.ClassificationMethod = "Connector";
                return result;
            }

            // Caso 4: Não conseguiu classificar
            result.Type = FixtureType.Unknown;
            result.Confidence = 0.0;
            result.ClassificationMethod = "None";
            return result;
        }

        // ════════════════════════════════════════════════
        //  CONVERSÃO FixtureType → EquipmentType (legado)
        // ════════════════════════════════════════════════

        /// <summary>
        /// Converte o novo FixtureType para o EquipmentType existente.
        /// Mantém compatibilidade com o sistema de regras hidráulicas atual.
        /// </summary>
        public static Models.EquipmentType ToEquipmentType(FixtureType fixtureType)
        {
            switch (fixtureType)
            {
                case FixtureType.Toilet: return Models.EquipmentType.VasoSanitario;
                case FixtureType.Sink: return Models.EquipmentType.Lavatorio;
                case FixtureType.Shower: return Models.EquipmentType.Chuveiro;
                case FixtureType.KitchenSink: return Models.EquipmentType.Pia;
                case FixtureType.LaundrySink: return Models.EquipmentType.Tanque;
                case FixtureType.Drain: return Models.EquipmentType.Ralo;
                case FixtureType.WashingMachine: return Models.EquipmentType.MaquinaLavar;
                default: return Models.EquipmentType.Outro;
            }
        }

        // ════════════════════════════════════════════════
        //  CLASSES INTERNAS AUXILIARES
        // ════════════════════════════════════════════════

        /// <summary>Padrão de regex associado a um FixtureType.</summary>
        private class FixturePattern
        {
            public FixtureType Type { get; }
            public string[] Patterns { get; }

            public FixturePattern(FixtureType type, string[] patterns)
            {
                Type = type;
                Patterns = patterns;
            }
        }

        /// <summary>Resultado da classificação por nome (Camada 1).</summary>
        private class NameClassification
        {
            public FixtureType Type { get; set; }
            public double Confidence { get; set; }
            public string MatchedPattern { get; set; }
        }

        /// <summary>Resultado da classificação por parâmetros (Camada 2).</summary>
        private class ParameterClassification
        {
            public FixtureType Type { get; set; }
            public double Confidence { get; set; }
            public string SourceParameter { get; set; }
        }

        /// <summary>Resultado da análise de conectores (Camada 3).</summary>
        private class ConnectorAnalysis
        {
            public int ConnectorCount { get; set; }
            public bool HasSanitary { get; set; }
            public bool HasColdWater { get; set; }
            public bool HasHotWater { get; set; }
            public double SanitaryDiameterMm { get; set; }
            public double ColdWaterDiameterMm { get; set; }
            public double HotWaterDiameterMm { get; set; }
            public FixtureType InferredType { get; set; }
            public double Confidence { get; set; }
        }
    }
}
