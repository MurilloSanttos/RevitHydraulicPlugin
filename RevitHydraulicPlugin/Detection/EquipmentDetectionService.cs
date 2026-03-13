using Autodesk.Revit.DB;
using RevitHydraulicPlugin.Configuration;
using RevitHydraulicPlugin.Models;
using RevitHydraulicPlugin.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace RevitHydraulicPlugin.Detection
{
    /// <summary>
    /// Serviço responsável por detectar equipamentos hidráulicos dentro dos
    /// ambientes identificados pelo RoomDetectionService.
    /// 
    /// Analisa FamilyInstances de categorias MEP (Plumbing Fixtures, etc.)
    /// e associa cada equipamento ao Room em que está posicionado.
    /// </summary>
    public class EquipmentDetectionService
    {
        private readonly Document _document;

        /// <summary>
        /// Categorias do Revit que contêm equipamentos hidráulicos.
        /// </summary>
        private static readonly BuiltInCategory[] HydraulicCategories = new[]
        {
            BuiltInCategory.OST_PlumbingFixtures,    // Aparelhos sanitários (vaso, pia, lavatório)
            BuiltInCategory.OST_MechanicalEquipment, // Equipamentos mecânicos (aquecedores, etc.)
            BuiltInCategory.OST_GenericModel          // Modelos genéricos (ralos, acessórios)
        };

        /// <summary>
        /// Mapeamento de palavras-chave no nome da família para tipo de equipamento.
        /// Case-insensitive. A primeira correspondência encontrada é usada.
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

        public EquipmentDetectionService(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// Detecta equipamentos hidráulicos em todos os ambientes fornecidos.
        /// 
        /// Fluxo:
        /// 1. Coleta todas as FamilyInstances das categorias MEP relevantes.
        /// 2. Para cada instância, verifica se está dentro de algum HydraulicRoom.
        /// 3. Classifica o tipo de equipamento pelo nome da família.
        /// 4. Extrai conectores de tubulação.
        /// 5. Atribui especificação de tubulação via HydraulicRules.
        /// 6. Adiciona o equipamento à lista do Room correspondente.
        /// </summary>
        /// <param name="rooms">Lista de ambientes hidráulicos detectados.</param>
        /// <returns>Lista completa de equipamentos hidráulicos encontrados.</returns>
        public List<HydraulicEquipment> DetectEquipment(List<HydraulicRoom> rooms)
        {
            Logger.Info("Iniciando detecção de equipamentos hidráulicos...");

            var allEquipment = new List<HydraulicEquipment>();

            // Coleta FamilyInstances das categorias hidráulicas
            var plumbingInstances = new List<FamilyInstance>();
            foreach (var category in HydraulicCategories)
            {
                var instances = new FilteredElementCollector(_document)
                    .OfCategory(category)
                    .WhereElementIsNotElementType()
                    .OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>()
                    .ToList();

                plumbingInstances.AddRange(instances);
            }

            Logger.Info($"Total de FamilyInstances MEP encontradas: {plumbingInstances.Count}");

            foreach (var instance in plumbingInstances)
            {
                string familyName = instance.Symbol?.Family?.Name ?? "";
                string typeName = instance.Symbol?.Name ?? "";

                // Classifica o tipo de equipamento
                EquipmentType equipmentType = ClassifyEquipment(familyName, typeName);

                // Ignora equipamentos não reconhecidos da categoria GenericModel
                // (para evitar falsos positivos)
                // NOTA: Usa comparação de ElementId direta em vez de IntegerValue
                // para compatibilidade com Revit 2024, 2025 e 2026.
                var genericModelId = new ElementId(BuiltInCategory.OST_GenericModel);
                if (equipmentType == EquipmentType.Outro
                    && instance.Category.Id.Equals(genericModelId))
                {
                    continue;
                }

                // Obtém posição do equipamento
                var location = instance.Location as LocationPoint;
                if (location == null) continue;

                XYZ position = location.Point;

                // Encontra o Room ao qual pertence
                var associatedRoom = FindRoomForEquipment(position, rooms);
                if (associatedRoom == null)
                {
                    Logger.Debug($"  ✗ Equipamento '{familyName}' não está em nenhum ambiente hidráulico");
                    continue;
                }

                // Extrai conectores de tubulação
                var connectors = ConnectorHelper.GetPipingConnectors(instance);

                // Cria o modelo de equipamento
                var equipment = new HydraulicEquipment
                {
                    ElementId = instance.Id,
                    FamilyName = familyName,
                    TypeName = typeName,
                    Type = equipmentType,
                    Position = position,
                    RoomId = associatedRoom.RoomId,
                    LevelId = instance.LevelId,
                    Connectors = connectors,
                    PipeSpec = HydraulicRules.GetSewerSpec(equipmentType)
                };

                // Adiciona ao Room e à lista geral
                associatedRoom.Equipment.Add(equipment);
                allEquipment.Add(equipment);

                Logger.Info($"  ✓ Detectado: {equipment}");
            }

            Logger.Info($"Total de equipamentos hidráulicos detectados: {allEquipment.Count}");
            return allEquipment;
        }

        /// <summary>
        /// Classifica um equipamento pelo nome da família e do tipo.
        /// </summary>
        private EquipmentType ClassifyEquipment(string familyName, string typeName)
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
        /// Encontra o HydraulicRoom em que um ponto está localizado.
        /// Usa verificação via BoundingBox do Room.
        /// </summary>
        private HydraulicRoom FindRoomForEquipment(XYZ point, List<HydraulicRoom> rooms)
        {
            foreach (var room in rooms)
            {
                if (room.BoundingBox != null
                    && RevitGeometryHelper.IsPointInsideBBoxXY(point, room.BoundingBox))
                {
                    return room;
                }
            }

            return null;
        }
    }
}
