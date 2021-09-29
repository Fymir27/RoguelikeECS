using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheAlchemist.Components;

namespace TheAlchemist
{
    public enum EntityType
    {
        None,
        All,
        Terrain,
        Structure,
        Item,
        Character
    }

    // keeps track of all entities and their components attached
    // an entity is just an int (ID) and is not a concrete object
    class EntityManager
    {
        [JsonProperty]
        static Dictionary<int, EntityType> entityType = new Dictionary<int, EntityType>();

        [JsonProperty]
        // maps from component Type ID to List of entities that have that specific component attached
        static Dictionary<int, List<int>> entitiesWithComponent = new Dictionary<int, List<int>>();

        [JsonProperty]
        // maps from entity ID to a list of components it has
        static Dictionary<int, List<IComponent>> componentsOfEntity = new Dictionary<int, List<IComponent>>();

        [JsonProperty]
        // maps from component Type ID to list of components of that type
        static Dictionary<int, List<IComponent>> componentsOfType = new Dictionary<int, List<IComponent>>();

        // List of entities to be removed when possible
        static List<int> toBeRemoved = new List<int>();

        // returns serialized state of entity manager as string
        public static string ToJson()
        {
            return JsonConvert.SerializeObject(new EntityManager(), Formatting.Indented, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });

        }

        // deserializes entity manager from string
        public static void InitFromJson(string json)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            JsonConvert.DeserializeObject<EntityManager>(json, settings);
        }

        // only used for debugging!
        public static void Reset()
        {
            entitiesWithComponent = new Dictionary<int, List<int>>();
            componentsOfEntity = new Dictionary<int, List<IComponent>>();
            componentsOfType = new Dictionary<int, List<IComponent>>();
        }

        // running counter for entiy IDs (ID 0 is unused)
        static int entityIDCounter = 1;

        // returns entities that have a specific type of component attached
        public static IEnumerable<int> GetEntitiesWithComponent<T>() where T : Component<T>
        {

            entitiesWithComponent.TryGetValue(Component<T>.TypeID, out List<int> res);
            return res != null ? res : new List<int>();
        }

        // returns all components of specific type
        public static IEnumerable<T> GetAllComponentsOfType<T>() where T : Component<T>
        {
            try
            {
                return componentsOfType[Component<T>.TypeID].Cast<T>();
            }
            catch (KeyNotFoundException)
            {
                Log.Warning("No such Component exists! (" + typeof(T) + ")");
                return Enumerable.Empty<T>();
            }
        }

        // returns all components attached to specific entity
        public static IEnumerable<IComponent> GetComponents(int entityID)
        {
            try
            {
                return componentsOfEntity[entityID];
            }
            catch (KeyNotFoundException)
            {
                Log.Error("No such entity! (" + entityID + ")");
                throw;
            }
        }

        // returns all component typeIDs of entity
        public static IEnumerable<int> GetComponentTypeIDs(int entityID)
        {
            return GetComponents(entityID)
                .Aggregate(new List<int>(), (list, item) =>
                {
                    list.Add(item.TypeID);
                    return list;
                });
        }

        public static T GetComponent<T>(int entityID) where T : Component<T>
        {
            try
            {
                return componentsOfEntity[entityID].FirstOrDefault(x => x.TypeID == Component<T>.TypeID) as T;
            }
            catch (KeyNotFoundException)
            {
                Log.Error("No such entity! (" + entityID + ")");
                throw;
            }
        }

        public static EntityType GetEntityType(int entityID)
        {
            try
            {
                return entityType[entityID];
            }
            catch (KeyNotFoundException)
            {
                Log.Error("No such entity! (" + entityID + ")");
                throw;
            }
        }

        // creates new Entity with an empty list of components and returns its ID
        public static int CreateEntity(EntityType type)
        {
            int entityID = entityIDCounter++;

            componentsOfEntity.Add(entityID, new List<IComponent>());

            entityType[entityID] = type;

            return entityID;
        }

        // creates new Entity with list of components and returns its ID
        public static int CreateEntity(List<IComponent> components, EntityType type)
        {
            int entityID = CreateEntity(type);
            AddComponents(entityID, components);
            return entityID;
        }

        // creates new Entity from json template
        // expects string that contains a list of components
        public static int CreateEntity(string json, EntityType type)
        {
            var components = Util.DeserializeObject<List<IComponent>>(json);
            if (components == null)
            {
                Log.Error("Failed to load components of entity from JSON!");
                Log.Data(json);
                components = new List<IComponent>();
            }
            return CreateEntity(components, type);
        }

        // adds multiple components to an entity
        public static void AddComponents(int entityID, List<IComponent> components)
        {
            foreach (var component in components)
            {
                AddComponent(entityID, component);
            }
        }

        // adds a single component to an entity
        public static void AddComponent(int entityID, IComponent component)
        {
            component.EntityID = entityID;
            componentsOfEntity[entityID].Add(component);

            int componentType = component.TypeID;

            if (!entitiesWithComponent.ContainsKey(componentType))
            {
                entitiesWithComponent.Add(componentType, new List<int>());
            }
            entitiesWithComponent[componentType].Add(entityID);

            if (!componentsOfType.ContainsKey(componentType))
            {
                componentsOfType.Add(componentType, new List<IComponent>());
            }
            componentsOfType[componentType].Add(component);
        }

        // removes a single specific component from entity
        public static void RemoveComponent(int entityID, IComponent component)
        {
            try
            {
                componentsOfEntity[entityID].Remove(component);
                componentsOfType[component.TypeID].Remove(component);
                var componentsOfSameType = componentsOfEntity[entityID].Where(c => c.TypeID == component.TypeID);
                if (componentsOfSameType.Count() == 0)
                {
                    entitiesWithComponent[component.TypeID].Remove(entityID);
                }
            }
            catch (KeyNotFoundException)
            {
                Log.Error("Failed to remove " + component + " from entity " + entityID + "!");
            }
        }

        // removes all components of specific type from entity
        public static void RemoveAllComponentsOfType(int entityID, int typeID)
        {
            try
            {
                var componentsToRemove = GetComponents(entityID).Where(c => c.TypeID == typeID).ToArray();
                if (!componentsToRemove.Any())
                {
                    Log.Warning("No components of type " + componentsOfType[typeID][0].GetType() + " on " + Systems.DescriptionSystem.GetNameWithID(entityID));
                    Log.Data(Systems.DescriptionSystem.GetDebugInfoEntity(entityID));
                    return;
                }
                componentsOfType[typeID].RemoveAll(componentsToRemove.Contains);
                componentsOfEntity[entityID].RemoveAll(componentsToRemove.Contains);
                entitiesWithComponent[typeID].Remove(entityID);
            }
            catch (KeyNotFoundException)
            {
                Log.Error("Failed to remove components of type " + typeID + " from entity " + entityID + "!");
            }
        }

        // marks entity for later deletion
        public static void RemoveEntity(int entityID)
        {
            if(entityID == Util.PlayerID)
            {
                // never fully remove player or everything breaks
                return;
            }

            if (toBeRemoved.Contains(entityID))
            {
                return;
            }
            toBeRemoved.Add(entityID);
        }

        // deletes all entities that got previously marked for deletion and resets list
        public static void CleanUpEntities()
        {
            foreach (int entity in toBeRemoved)
            {
                DeleteEntity(entity);
            }
            toBeRemoved.Clear();
        }

        // "deletes" an entity
        static void DeleteEntity(int entityID)
        {
            try
            {
                foreach (var key in entitiesWithComponent.Keys)
                {
                    entitiesWithComponent[key].Remove(entityID);
                }

                var floor = Util.CurrentFloor;
                TransformComponent transform = null;

                foreach (var component in componentsOfEntity[entityID])
                {
                    if (component.TypeID == TransformComponent.TypeID)
                    {
                        transform = (TransformComponent)component;                       
                    }
                    componentsOfType[component.TypeID].Remove(component);
                }

                componentsOfEntity.Remove(entityID);

                if (transform != null && !floor.IsOutOfBounds(transform.Position))
                {
                    var pos = transform.Position;
                    var tile = floor.GetTile(pos);

                    switch (entityType[entityID])
                    {
                        case EntityType.None:
                            break;

                        case EntityType.All:
                            break;

                        case EntityType.Terrain:
                            if (tile.Terrain == entityID)
                                tile.Terrain = 0;
                            break;

                        case EntityType.Structure:
                            if (tile.Structure == entityID)
                                tile.Structure = 0;
                            break;

                        case EntityType.Item:
                            if (tile.Items.Contains(entityID))
                                tile.Items.Remove(entityID);
                            break;

                        case EntityType.Character:
                            if (tile.Character == entityID)
                                tile.Character = 0;
                            break;
                    }
                }

                entityType.Remove(entityID);
            }
            catch (KeyNotFoundException)
            {
                Log.Error("No such entity! (" + entityID + ")");
                throw;
            }
        }

        // debug
        public static void Dump(bool ignoreTerrain = true)
        {
            StringBuilder s = new StringBuilder();          

            Log.Message("----- Dump of all entities: -----");

            Log.Message("Entities: ");
            foreach (var key in componentsOfEntity.Keys)
            {
                if (ignoreTerrain && entityType[key] == EntityType.Terrain)
                    continue;

                s.Append(Systems.DescriptionSystem.GetNameWithID(key) + "[" + entityType[key].ToString() + "]: [");
                foreach (var item in componentsOfEntity[key])
                {
                    s.Append(item.ComponentID + ", ");
                }
                s.Append("]\n");
            }
            Log.Data(s.ToString());

            s.Clear();

            Log.Message("ComponentTypes:");
            foreach (var typeID in componentsOfType.Keys)
            {
                s.AppendLine(typeID + " -> " + componentsOfType[typeID].ElementAt(0).GetType().ToString());
            }
            Log.Data(s.ToString());

            s.Clear();

            Log.Message("EntitiesWithComponent ");
            foreach (var key in entitiesWithComponent.Keys)
            {
                s.Append(key + ": [");
                foreach (var item in entitiesWithComponent[key])
                {
                    s.Append(item + ", ");
                }
                s.Append("]\n");
            }
            Log.Data(s.ToString());      

            s.Clear();

            Log.Message("ComponentsOfType ");
            foreach (var key in componentsOfType.Keys)
            {
                s.Append(key + ": [");
                foreach (var item in componentsOfType[key])
                {
                    s.Append(item.ComponentID + ", ");
                }
                s.Append("]\n");
            }
            Log.Data(s.ToString());

            Log.Message("----- End of dump -----");
        }
    }
}
