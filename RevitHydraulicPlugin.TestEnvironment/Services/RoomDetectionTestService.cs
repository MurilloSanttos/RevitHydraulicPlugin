using RevitHydraulicPlugin.TestEnvironment.Mocks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RevitHydraulicPlugin.TestEnvironment.Services
{
    /// <summary>
    /// Tipos de ambientes hidráulicos reconhecidos.
    /// Espelha RevitHydraulicPlugin.Models.RoomType do plugin principal.
    /// </summary>
    public enum RoomType
    {
        Banheiro,
        Lavabo,
        Cozinha,
        AreaDeServico,
        Lavanderia,
        Outro
    }

    /// <summary>
    /// Tipos de equipamentos hidráulicos reconhecidos.
    /// Espelha RevitHydraulicPlugin.Models.EquipmentType do plugin principal.
    /// </summary>
    public enum EquipmentType
    {
        VasoSanitario,
        Lavatorio,
        Chuveiro,
        Pia,
        Tanque,
        Ralo,
        MaquinaLavar,
        Outro
    }

    /// <summary>
    /// Resultado da detecção de um ambiente hidráulico.
    /// Contém o MockRoom original e sua classificação.
    /// </summary>
    public class DetectedRoom
    {
        public MockRoom Room { get; set; }
        public RoomType Type { get; set; }
        public bool IsHydraulic { get; set; }

        public override string ToString()
        {
            return $"'{Room.Name}' → {(IsHydraulic ? Type.ToString() : "NÃO HIDRÁULICO")}";
        }
    }

    /// <summary>
    /// Resultado da detecção de um equipamento hidráulico.
    /// Contém o MockFixture original e sua classificação.
    /// </summary>
    public class DetectedEquipment
    {
        public MockFixture Fixture { get; set; }
        public EquipmentType Type { get; set; }
        public MockRoom Room { get; set; }

        public override string ToString()
        {
            return $"{Type}: {Fixture.FamilyName} em {Room.Name}";
        }
    }

    /// <summary>
    /// Especificação de tubulação para um ramal ou coluna.
    /// Espelha RevitHydraulicPlugin.Models.PipeSpecification do plugin principal,
    /// mas sem dependências do Revit.
    /// </summary>
    public class PipeSpec
    {
        public double DiameterMm { get; set; }
        public double SlopePercent { get; set; }
        public string SystemTypeName { get; set; }
        public string PipeTypeName { get; set; }
        public string Material { get; set; }

        public override string ToString()
        {
            return $"Ø{DiameterMm}mm {Material} | Inclinação: {SlopePercent}% | Sistema: {SystemTypeName}";
        }
    }

    /// <summary>
    /// Serviço de detecção de ambientes hidráulicos.
    /// Replica a lógica de RoomDetectionService + RoomClassification do plugin principal.
    /// 
    /// USA A MESMA LÓGICA DE REGEX do plugin para garantir que os testes
    /// validam exatamente o mesmo comportamento.
    /// </summary>
    public class RoomDetectionTestService
    {
        /// <summary>
        /// Padrões de regex para cada tipo de ambiente.
        /// IDÊNTICOS aos de RevitHydraulicPlugin.Configuration.RoomClassification.
        /// </summary>
        private static readonly Dictionary<RoomType, List<string>> RoomPatterns =
            new Dictionary<RoomType, List<string>>
            {
                {
                    RoomType.Banheiro, new List<string>
                    {
                        @"banheiro", @"wc", @"sanitário", @"sanitario",
                        @"banho", @"bath", @"bathroom", @"toilet"
                    }
                },
                {
                    RoomType.Lavabo, new List<string>
                    {
                        @"lavabo", @"powder\s*room", @"half\s*bath"
                    }
                },
                {
                    RoomType.Cozinha, new List<string>
                    {
                        @"cozinha", @"kitchen", @"copa"
                    }
                },
                {
                    RoomType.AreaDeServico, new List<string>
                    {
                        @"[aá]rea\s*(de\s*)?servi[cç]o", @"service\s*area",
                        @"\ba\.?\s*s\.\b", @"utilit"
                    }
                },
                {
                    RoomType.Lavanderia, new List<string>
                    {
                        @"lavanderia", @"laundry"
                    }
                }
            };

        /// <summary>
        /// Classifica um nome de ambiente como hidráulico ou não.
        /// </summary>
        public bool TryClassify(string roomName, out RoomType roomType)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                roomType = RoomType.Outro;
                return false;
            }

            string normalized = roomName.Trim().ToLowerInvariant();

            foreach (var kvp in RoomPatterns)
            {
                foreach (string pattern in kvp.Value)
                {
                    if (Regex.IsMatch(normalized, pattern, RegexOptions.IgnoreCase))
                    {
                        roomType = kvp.Key;
                        return true;
                    }
                }
            }

            roomType = RoomType.Outro;
            return false;
        }

        /// <summary>
        /// Analisa todos os Rooms de um projeto e retorna a lista de detecções
        /// (incluindo os não-hidráulicos, para fins de relatório no teste).
        /// </summary>
        public List<DetectedRoom> DetectRooms(MockProject project)
        {
            var results = new List<DetectedRoom>();

            foreach (var room in project.Rooms)
            {
                bool isHydraulic = TryClassify(room.Name, out RoomType roomType);

                results.Add(new DetectedRoom
                {
                    Room = room,
                    Type = roomType,
                    IsHydraulic = isHydraulic
                });
            }

            return results;
        }
    }
}
