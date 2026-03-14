using RevitHydraulicPlugin.TestEnvironment.Mocks;
using System.Collections.Generic;

namespace RevitHydraulicPlugin.TestEnvironment.Services
{
    /// <summary>
    /// Serviço de detecção de equipamentos hidráulicos para testes (v2.0).
    /// Usa FixtureClassifierTestService para classificação multi-critério.
    /// </summary>
    public class EquipmentDetectionTestService
    {
        private readonly FixtureClassifierTestService _classifier =
            new FixtureClassifierTestService();

        /// <summary>
        /// Classifica um equipamento e retorna FixtureType + EquipmentType.
        /// </summary>
        public DetectedEquipment ClassifyFixture(MockFixture fixture, MockRoom room)
        {
            var result = _classifier.Classify(fixture);

            return new DetectedEquipment
            {
                Fixture = fixture,
                FixtureType = result.Type,
                Type = FixtureClassifierTestService.ToEquipmentType(result.Type),
                Room = room,
                Confidence = result.Confidence,
                Method = result.Method
            };
        }

        /// <summary>
        /// Classificação legada por nome (para compatibilidade com testes existentes).
        /// </summary>
        public EquipmentType ClassifyEquipment(string familyName, string typeName)
        {
            var ft = _classifier.ClassifyByNameOnly(familyName, typeName);
            return FixtureClassifierTestService.ToEquipmentType(ft);
        }

        /// <summary>
        /// Identifica equipamentos em ambientes hidráulicos.
        /// </summary>
        public List<DetectedEquipment> DetectEquipment(List<DetectedRoom> detectedRooms)
        {
            var results = new List<DetectedEquipment>();

            foreach (var detected in detectedRooms)
            {
                if (!detected.IsHydraulic) continue;

                foreach (var fixture in detected.Room.Fixtures)
                {
                    var equip = ClassifyFixture(fixture, detected.Room);
                    results.Add(equip);
                }
            }

            return results;
        }
    }
}
