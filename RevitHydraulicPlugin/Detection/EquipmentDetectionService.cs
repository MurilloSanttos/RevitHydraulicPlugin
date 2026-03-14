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
    /// VERSÃO 2.0 — Agora utiliza o FixtureClassifierService para classificação
    /// multi-critério (nome + parâmetros + conectores) em vez de depender
    /// exclusivamente de keywords no nome da família.
    /// 
    /// Fluxo:
    /// 1. Coleta FamilyInstances das categorias MEP relevantes
    /// 2. Para cada instância, usa FixtureClassifierService para classificar
    /// 3. Verifica se o equipamento está dentro de algum HydraulicRoom
    /// 4. Extrai conectores e atribui especificações hidráulicas
    /// 5. Registra logs detalhados de cada etapa
    /// </summary>
    public class EquipmentDetectionService
    {
        private readonly Document _document;
        private readonly FixtureClassifierService _classifier;

        /// <summary>
        /// Categorias do Revit que contêm equipamentos hidráulicos.
        /// </summary>
        private static readonly BuiltInCategory[] HydraulicCategories = new[]
        {
            BuiltInCategory.OST_PlumbingFixtures,    // Aparelhos sanitários (vaso, pia, lavatório)
            BuiltInCategory.OST_MechanicalEquipment, // Equipamentos mecânicos (aquecedores, etc.)
            BuiltInCategory.OST_GenericModel          // Modelos genéricos (ralos, acessórios)
        };

        public EquipmentDetectionService(Document document)
        {
            _document = document;
            _classifier = new FixtureClassifierService();
        }

        /// <summary>
        /// Detecta equipamentos hidráulicos em todos os ambientes fornecidos.
        /// Utiliza o FixtureClassifierService para classificação robusta.
        /// </summary>
        /// <param name="rooms">Lista de ambientes hidráulicos detectados.</param>
        /// <returns>Lista completa de equipamentos hidráulicos encontrados.</returns>
        public List<HydraulicEquipment> DetectEquipment(List<HydraulicRoom> rooms)
        {
            using (Logger.MeasureTime("Deteccao de Equipamentos"))
            {
                Logger.Info("[EQUIP] Iniciando deteccao de equipamentos hidraulicos...");
                Logger.Info($"[EQUIP] Ambientes a analisar: {rooms.Count}");

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

                    Logger.Debug($"[EQUIP] Categoria {category}: {instances.Count} instancias");
                    plumbingInstances.AddRange(instances);
                }

                Logger.Info($"[EQUIP] Total de FamilyInstances MEP encontradas: {plumbingInstances.Count}");

                int classifiedCount = 0;
                int skippedGenericCount = 0;
                int skippedNoLocationCount = 0;
                int skippedNoRoomCount = 0;

                foreach (var instance in plumbingInstances)
                {
                    // ── Classificação via FixtureClassifierService ──
                    var classification = _classifier.Classify(instance);

                    // Ignora equipamentos Unknown da categoria GenericModel
                    // (para evitar falsos positivos com modelos genéricos)
                    if (classification.Type == FixtureType.Unknown)
                    {
                        var genericModelId = new ElementId(BuiltInCategory.OST_GenericModel);
                        if (instance.Category.Id.Equals(genericModelId))
                        {
                            skippedGenericCount++;
                            Logger.Debug($"[EQUIP] Ignorado (GenericModel desconhecido): '{classification.FamilyName}'");
                            continue;
                        }
                    }

                    // Converte para EquipmentType (compatibilidade com sistema existente)
                    EquipmentType equipmentType = FixtureClassifierService.ToEquipmentType(classification.Type);

                    // Obtém posição do equipamento
                    var location = instance.Location as LocationPoint;
                    if (location == null)
                    {
                        skippedNoLocationCount++;
                        Logger.Debug($"[EQUIP] Ignorado (sem LocationPoint): '{classification.FamilyName}'");
                        continue;
                    }

                    XYZ position = location.Point;

                    // Encontra o Room ao qual pertence
                    var associatedRoom = FindRoomForEquipment(position, rooms);
                    if (associatedRoom == null)
                    {
                        skippedNoRoomCount++;
                        Logger.Debug($"[EQUIP] Fora de ambientes hidraulicos: '{classification.FamilyName}' em ({position.X:F2}, {position.Y:F2})");
                        continue;
                    }

                    // Extrai conectores de tubulação
                    var connectors = ConnectorHelper.GetPipingConnectors(instance);

                    // Cria o modelo de equipamento
                    var equipment = new HydraulicEquipment
                    {
                        ElementId = instance.Id,
                        FamilyName = classification.FamilyName,
                        TypeName = classification.TypeName,
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
                    classifiedCount++;

                    // Log detalhado da detecção
                    Logger.LogFixtureDetected(
                        classification.FamilyName,
                        $"{classification.Type} ({classification.Confidence:P0}, {classification.ClassificationMethod})",
                        associatedRoom.RoomName);
                }

                // ── Relatório de detecção ──
                Logger.Info($"[EQUIP] === Resumo da Deteccao ===");
                Logger.Info($"[EQUIP]   Classificados: {classifiedCount}");
                Logger.Info($"[EQUIP]   Ignorados (GenericModel): {skippedGenericCount}");
                Logger.Info($"[EQUIP]   Ignorados (sem posicao): {skippedNoLocationCount}");
                Logger.Info($"[EQUIP]   Ignorados (fora de Rooms): {skippedNoRoomCount}");
                Logger.Info($"[EQUIP] Total detectados: {allEquipment.Count}");

                // Log por Room
                foreach (var room in rooms.Where(r => r.Equipment.Count > 0))
                {
                    Logger.Info($"[EQUIP]   {room.RoomName}: {room.Equipment.Count} equipamentos");
                    foreach (var eq in room.Equipment)
                    {
                        Logger.Debug($"[EQUIP]     - {eq.Type}: {eq.FamilyName}");
                    }
                }

                return allEquipment;
            }
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
