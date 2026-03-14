using RevitHydraulicPlugin.TestEnvironment.Mocks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RevitHydraulicPlugin.TestEnvironment.Services
{
    // ════════════════════════════════════════════════
    //  ENUMS — Espelham o plugin principal v2.0
    // ════════════════════════════════════════════════

    /// <summary>
    /// Tipos de ambientes hidráulicos reconhecidos (v2.0).
    /// Espelha RevitHydraulicPlugin.Models.RoomType.
    /// </summary>
    public enum RoomType
    {
        Bathroom,
        Lavatory,
        Kitchen,
        Laundry,
        ServiceArea,
        Pantry,
        SuiteBathroom,
        AccessibleBathroom,
        Unknown,

        // Aliases legados
        Banheiro = Bathroom,
        Lavabo = Lavatory,
        Cozinha = Kitchen,
        AreaDeServico = ServiceArea,
        Lavanderia = Laundry,
        Outro = Unknown
    }

    /// <summary>
    /// Tipos de equipamentos hidráulicos legados.
    /// Espelha RevitHydraulicPlugin.Models.EquipmentType.
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
    /// Tipos de fixtures hidráulicos (v2.0).
    /// Espelha RevitHydraulicPlugin.Detection.FixtureType.
    /// </summary>
    public enum FixtureType
    {
        Toilet,
        Sink,
        Shower,
        KitchenSink,
        LaundrySink,
        Drain,
        WashingMachine,
        Unknown
    }

    // ════════════════════════════════════════════════
    //  MODELOS DE RESULTADO
    // ════════════════════════════════════════════════

    /// <summary>
    /// Resultado da detecção/classificação de um ambiente.
    /// </summary>
    public class DetectedRoom
    {
        public MockRoom Room { get; set; }
        public RoomType Type { get; set; }
        public bool IsHydraulic { get; set; }
        public double Confidence { get; set; }
        public string Method { get; set; } = "None";
        public string Reason { get; set; } = "";

        public override string ToString()
        {
            if (!IsHydraulic) return $"'{Room.Name}' -> NAO HIDRAULICO";
            return $"'{Room.Name}' -> {Type} (conf: {Confidence:P0}, metodo: {Method})";
        }
    }

    /// <summary>
    /// Resultado da detecção de um equipamento.
    /// </summary>
    public class DetectedEquipment
    {
        public MockFixture Fixture { get; set; }
        public EquipmentType Type { get; set; }
        public FixtureType FixtureType { get; set; }
        public MockRoom Room { get; set; }
        public double Confidence { get; set; }
        public string Method { get; set; }

        public override string ToString()
        {
            return $"{FixtureType}: {Fixture.FamilyName} em {Room.Name} ({Confidence:P0})";
        }
    }

    /// <summary>
    /// Especificação de tubulação para testes.
    /// </summary>
    public class PipeSpec
    {
        public double DiameterMm { get; set; }
        public double SlopePercent { get; set; }
        public string SystemTypeName { get; set; }
        public string PipeTypeName { get; set; }
        public string Material { get; set; }
        public double MinLengthMm { get; set; }
        public double MaxLengthMm { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return $"D{DiameterMm}mm {Material} | Incl: {SlopePercent}% | {SystemTypeName}";
        }
    }

    // ════════════════════════════════════════════════
    //  SERVIÇO DE DETECÇÃO DE ROOMS (v2.0)
    // ════════════════════════════════════════════════

    /// <summary>
    /// Serviço de detecção de ambientes para testes.
    /// Usa RoomClassifierTestService para classificação multi-critério.
    /// </summary>
    public class RoomDetectionTestService
    {
        private readonly RoomClassifierTestService _classifier = new RoomClassifierTestService();

        /// <summary>
        /// Fase 1: Detecta rooms por nome.
        /// </summary>
        public List<DetectedRoom> DetectRooms(MockProject project)
        {
            var results = new List<DetectedRoom>();

            foreach (var room in project.Rooms)
            {
                var analysis = _classifier.ClassifyByName(room.Name);

                results.Add(new DetectedRoom
                {
                    Room = room,
                    Type = analysis.Type,
                    IsHydraulic = analysis.IsHydraulic,
                    Confidence = analysis.Confidence,
                    Method = analysis.Method,
                    Reason = analysis.Reason
                });
            }

            return results;
        }

        /// <summary>
        /// Fase 2: Reclassifica rooms usando fixtures.
        /// </summary>
        public void ReclassifyWithFixtures(List<DetectedRoom> rooms)
        {
            foreach (var room in rooms)
            {
                if (room.Room.Fixtures.Count == 0) continue;

                var fixtureTypes = new List<FixtureType>();
                var fixtureClassifier = new FixtureClassifierTestService();

                foreach (var fixture in room.Room.Fixtures)
                {
                    var ft = fixtureClassifier.ClassifyByNameOnly(fixture.FamilyName, fixture.TypeName);
                    if (ft != FixtureType.Unknown)
                        fixtureTypes.Add(ft);
                }

                if (fixtureTypes.Count == 0) continue;

                var reclassification = _classifier.ClassifyWithFixtures(room.Room.Name, fixtureTypes);

                if (reclassification.Confidence > room.Confidence)
                {
                    room.Type = reclassification.Type;
                    room.IsHydraulic = reclassification.IsHydraulic;
                    room.Confidence = reclassification.Confidence;
                    room.Method = reclassification.Method;
                    room.Reason = reclassification.Reason;
                }
            }
        }
    }
}
