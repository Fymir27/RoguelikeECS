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

        public static string ToJson()
        {
            return JsonConvert.SerializeObject(new EntityManager(), Formatting.Indented, new JsonSerializerSettings() {
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });

        }

        public static void InitFromJson(string json)
        {
            //Dump();
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            JsonConvert.DeserializeObject<EntityManager>(json, settings);
            //Dump();
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

        // returns an array of entity IDs that have a specific component attached
        public static int[] GetEntitiesWithComponent<T>() where T : Component<T>
        {
            return entitiesWithComponent[Component<T>.TypeID].ToArray();
        }

        // returns all components of Type T
        public static T[] GetAllComponents<T>() where T : Component<T>
        {
            return componentsOfType[Component<T>.TypeID].ConvertAll(component => component as T).ToArray();
        }

        // returns a list of all components attached to specific entity
        public static IComponent[] GetAllComponentsOfEntity(int entityID)
        {
            return componentsOfEntity[entityID].ToArray();
        }

        public static T GetComponentOfEntity<T>(int entityID) where T : Component<T>
        {           
            return componentsOfEntity[entityID].FirstOrDefault(x => x.TypeID == Component<T>.TypeID) as T;
        }

        // creates new Entity with an empty list of components and returns its ID
        public static int createEntity()
        {
            int entityID = entityIDCounter++;

            componentsOfEntity.Add(entityID, new List<IComponent>());
            
            return entityID;
        }

        public static int createEntity(List<IComponent> components)
        {
            int entityID = createEntity();
            addComponentsToEntity(entityID, components);
            return entityID;            
        }

        // adds multiple components to an entity
        public static void addComponentsToEntity(int entityID, List<IComponent> components)
        {
            foreach (var component in components)
            {
                addComponentToEntity(entityID, component);
            }
        }

        // adds a single component to an entity
        public static void addComponentToEntity(int entityID, IComponent component)
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
