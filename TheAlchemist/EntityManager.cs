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

        // returns serialized state of entity manager as string
        public static string ToJson()
        {
            return JsonConvert.SerializeObject(new EntityManager(), Formatting.Indented, new JsonSerializerSettings() {
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
            try
            {
                return entitiesWithComponent[Component<T>.TypeID];
            }
            catch (KeyNotFoundException)
            {
                Log.Warning("No entity with such Component exists! (" + typeof(T) + ")");
                return new List<int>();
            }
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
            AddComponentsToEntity(entityID, components);
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
        public static void AddComponentsToEntity(int entityID, List<IComponent> components)
        {
            foreach (var component in components)
            {
                AddComponentToEntity(entityID, component);
            }
        }

        // adds a single component to an entity
        public static void AddComponentToEntity(int entityID, IComponent component)
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

        // "deletes" an entity
        public static void RemoveEntity(int entityID)
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
            catch(KeyNotFoundException)
            {
                Log.Error("No such entity! (" + entityID + ")");
                throw;
            }
        }

        // debug
        public static void Dump()
        {
            Console.WriteLine("----- DEBUG -----");

            Console.WriteLine("Entities: ");
            foreach(var key in componentsOfEntity.Keys)
            {
                Console.Write(key + ": [");
                foreach (var item in componentsOfEntity[key])
                {
                    Console.Write(item.ComponentID + ", ");
                }
                Console.WriteLine("]");
            }

            Console.WriteLine("EntitiesWithComponent ");
            foreach (var key in entitiesWithComponent.Keys)
            {
                Console.Write(key + ": [");
                foreach (var item in entitiesWithComponent[key])
                {
                    Console.Write(item + ", ");
                }
                Console.WriteLine("]");
            }

            Console.WriteLine("ComponentsOfType ");
            foreach (var key in componentsOfType.Keys)
            {
                Console.Write(key + ": [");
                foreach (var item in componentsOfType[key])
                {
                    Console.Write(item.ComponentID + ", ");
                }
                Console.WriteLine("]");
            }

            Console.WriteLine("----- DEBUG -----");
        }
    }
}
