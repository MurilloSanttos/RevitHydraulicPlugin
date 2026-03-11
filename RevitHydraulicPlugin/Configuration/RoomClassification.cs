using RevitHydraulicPlugin.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RevitHydraulicPlugin.Configuration
{
    /// <summary>
    /// Classifica ambientes (Rooms) como hidráulicos baseado em padrões de nome.
    /// Os padrões são definidos como expressões regulares case-insensitive,
    /// permitindo variações de grafia e abreviações comuns.
    /// </summary>
    public static class RoomClassification
    {
        /// <summary>
        /// Mapeamento de RoomType para padrões de nome (regex).
        /// Cada entrada contém uma lista de padrões que identificam aquele tipo de ambiente.
        /// </summary>
        private static readonly Dictionary<RoomType, List<string>> RoomPatterns =
            new Dictionary<RoomType, List<string>>
            {
                {
                    RoomType.Banheiro, new List<string>
                    {
                        @"banheiro",
                        @"wc",
                        @"sanitário",
                        @"sanitario",
                        @"banho",
                        @"bath",
                        @"bathroom",
                        @"toilet"
                    }
                },
                {
                    RoomType.Lavabo, new List<string>
                    {
                        @"lavabo",
                        @"powder\s*room",
                        @"half\s*bath"
                    }
                },
                {
                    RoomType.Cozinha, new List<string>
                    {
                        @"cozinha",
                        @"kitchen",
                        @"copa"
                    }
                },
                {
                    RoomType.AreaDeServico, new List<string>
                    {
                        @"[aá]rea\s*(de\s*)?servi[cç]o",
                        @"service\s*area",
                        @"\ba\.?\s*s\.\b",
                        @"utilit"
                    }
                },
                {
                    RoomType.Lavanderia, new List<string>
                    {
                        @"lavanderia",
                        @"laundry"
                    }
                }
            };

        /// <summary>
        /// Tenta classificar um nome de ambiente como um tipo hidráulico.
        /// Retorna true se for reconhecido, atribuindo o RoomType correspondente.
        /// </summary>
        /// <param name="roomName">Nome do ambiente a classificar.</param>
        /// <param name="roomType">Tipo de ambiente identificado (saída).</param>
        /// <returns>True se o ambiente foi classificado com sucesso.</returns>
        public static bool TryClassify(string roomName, out RoomType roomType)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                roomType = RoomType.Outro;
                return false;
            }

            string normalizedName = roomName.Trim().ToLowerInvariant();

            foreach (var kvp in RoomPatterns)
            {
                foreach (string pattern in kvp.Value)
                {
                    if (Regex.IsMatch(normalizedName, pattern, RegexOptions.IgnoreCase))
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
        /// Verifica se um nome de ambiente é um ambiente hidráulico reconhecido.
        /// </summary>
        public static bool IsHydraulicRoom(string roomName)
        {
            return TryClassify(roomName, out _);
        }

        /// <summary>
        /// Retorna todos os padrões registrados para uso em diagnóstico/log.
        /// </summary>
        public static Dictionary<RoomType, List<string>> GetAllPatterns()
        {
            return new Dictionary<RoomType, List<string>>(RoomPatterns);
        }
    }
}
