using RevitHydraulicPlugin.TestEnvironment.Mocks;
using System.Collections.Generic;

namespace RevitHydraulicPlugin.TestEnvironment.Services
{
    /// <summary>
    /// Serviço de identificação de equipamentos hidráulicos para testes.
    /// Replica a lógica de EquipmentDetectionService do plugin principal.
    /// </summary>
    public class EquipmentDetectionTestService
    {
        /// <summary>
        /// Mapeamento de palavras-chave para tipos de equipamento.
        /// IDÊNTICO ao de RevitHydraulicPlugin.Detection.EquipmentDetectionService.
        /// </summary>
        private static readonly Dictionary<string, EquipmentType> EquipmentKeywords =
            new Dictionary<string, EquipmentType>
            {
                { "vaso", EquipmentType.VasoSanitario },
                { "toilet", EquipmentType.VasoSanitario },
                { "bacia", EquipmentType.VasoSanitario },
                { "lavatorio", EquipmentType.Lavatorio },
                { "lavatório", EquipmentType.Lavatorio },
                { "lavat", EquipmentType.Lavatorio },
                { "sink", EquipmentType.Lavatorio },
                { "basin", EquipmentType.Lavatorio },
                { "chuveiro", EquipmentType.Chuveiro },
                { "shower", EquipmentType.Chuveiro },
                { "ducha", EquipmentType.Chuveiro },
                { "pia", EquipmentType.Pia },
                { "kitchen sink", EquipmentType.Pia },
                { "tanque", EquipmentType.Tanque },
                { "laundry", EquipmentType.Tanque },
                { "ralo", EquipmentType.Ralo },
                { "drain", EquipmentType.Ralo },
                { "floor drain", EquipmentType.Ralo },
                { "máquina", EquipmentType.MaquinaLavar },
                { "maquina", EquipmentType.MaquinaLavar },
                { "washer", EquipmentType.MaquinaLavar },
                { "washing", EquipmentType.MaquinaLavar }
            };

        /// <summary>
        /// Classifica um equipamento pelo nome da família e tipo.
        /// </summary>
        public EquipmentType ClassifyEquipment(string familyName, string typeName)
        {
            string combined = $"{familyName} {typeName}".ToLowerInvariant();

            foreach (var kvp in EquipmentKeywords)
            {
                if (combined.Contains(kvp.Key))
                {
                    return kvp.Value;
                }
            }

            return EquipmentType.Outro;
        }

        /// <summary>
        /// Identifica todos os equipamentos nos ambientes hidráulicos detectados.
        /// </summary>
        public List<DetectedEquipment> DetectEquipment(
            List<DetectedRoom> detectedRooms)
        {
            var results = new List<DetectedEquipment>();

            foreach (var detected in detectedRooms)
            {
                // Processa apenas ambientes hidráulicos
                if (!detected.IsHydraulic) continue;

                foreach (var fixture in detected.Room.Fixtures)
                {
                    var equipmentType = ClassifyEquipment(
                        fixture.FamilyName, fixture.TypeName);

                    results.Add(new DetectedEquipment
                    {
                        Fixture = fixture,
                        Type = equipmentType,
                        Room = detected.Room
                    });
                }
            }

            return results;
        }
    }
}
