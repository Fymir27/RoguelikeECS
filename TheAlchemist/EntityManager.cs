using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TheAlchemist.Components;

namespace TheAlchemist
{
    // keeps track of all entities and their components attached
    // an entity is just an int (ID) and is not a concrete object
    class EntityManager
    {
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

        // creates new Entity with an empty list of components and returns its ID
        public static int CreateEntity()
        {
            int entityID = entityIDCounter++;

            componentsOfEntity.Add(entityID, new List<IComponent>());

            return entityID;
        }

        // creates new Entity with list of components and returns its ID
        public static int CreateEntity(List<IComponent> components)
        {
            int entityID = CreateEntity();
            AddComponents(entityID, components);
            return entityID;
        }

        // creates new Entity from json template
        // expects string that contains a list of components
        public static int CreateEntity(string json)
        {
            var components = Util.DeserializeObject<List<IComponent>>(json);
            return CreateEntity(components);
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
                var componentsToRemove = GetComponents(entityID).Where(c => c.TypeID == typeID);
                if (componentsToRemove.Count() == 0)
                {
                    Log.Warning("No components of type " + componentsOfType[typeID][0].GetType().ToString() + " on " + Systems.DescriptionSystem.GetNameWithID(entityID));
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

                foreach (var component in componentsOfEntity[entityID])
                {
                    if (component.TypeID == TransformComponent.TypeID)
                    {
                        var transform = (TransformComponent)component;
                        var pos = transform.Position;
                        var floor = Util.CurrentFloor;

                        // check if entity was character/tile/item
                        if (floor.GetCharacter(pos) == entityID)
                            floor.RemoveCharacter(pos);
                        else if (floor.GetTerrain(pos) == entityID)
                            floor.RemoveTerrain(pos);
                        else if (floor.GetItems(pos).Contains(entityID))
                            floor.RemoveItem(pos, entityID);
                    }
                    componentsOfType[component.TypeID].Remove(component);
                }

                componentsOfEntity.Remove(entityID);
            }
            catch (KeyNotFoundException)
            {
                Log.Error("No such entity! (" + entityID + ")");
                throw;
            }
        }

        // debug
        public static void Dump()
        {
            string s = "";

            Log.Message("----- Dump of all entities: -----");

            Log.Message("Entities: ");
            foreach (var key in componentsOfEntity.Keys)
            {
                s += key + ": [";
                foreach (var item in componentsOfEntity[key])
                {
                    s += item.ComponentID + ", ";
                }
                s += "]\n";
            }
            Log.Data(s);

            s = "";
            Log.Message("EntitiesWithComponent ");
            foreach (var key in entitiesWithComponent.Keys)
            {
                s += key + ": [";
                foreach (var item in entitiesWithComponent[key])
                {
                    s += item + ", ";
                }
                s += "]\n";
            }
            Log.Data(s);

            s = "";
            Log.Message("ComponentsOfType ");
            foreach (var key in componentsOfType.Keys)
            {
                s += key + ": [";
                foreach (var item in componentsOfType[key])
                {
                    s += item.ComponentID + ", ";
                }
                s += "]\n";
            }
            Log.Data(s);

            Log.Message("----- End of dump -----");
        }
    }
}
