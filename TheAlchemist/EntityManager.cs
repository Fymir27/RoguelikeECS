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
            try
            {
                return entitiesWithComponent[Component<T>.TypeID].ToArray();
            }
            catch (KeyNotFoundException)
            {
                Log.Warning("No such Component exists! (" + typeof(T) + ")");
            }
            return new int[0];
        }

        // returns all components of Type T
        public static T[] GetAllComponents<T>() where T : Component<T>
        {        
            try
            {
                return componentsOfType[Component<T>.TypeID].ConvertAll(component => component as T).ToArray();
            }
            catch (KeyNotFoundException)
            {
                Log.Warning("No such Component exists! (" + typeof(T) + ")");
            }
            return new T[0];
        }

        // returns a list of all components attached to specific entity
        public static IComponent[] GetAllComponentsOfEntity(int entityID)
        {
            try
            {
                return componentsOfEntity[entityID].ToArray();
            }
            catch (KeyNotFoundException)
            {
                Log.Error("No such entity! (" + entityID + ")");
                throw;
            }
        }

        public static T GetComponentOfEntity<T>(int entityID) where T : Component<T>
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

        public static int CreateEntity(List<IComponent> components)
        {
            int entityID = CreateEntity();
            AddComponentsToEntity(entityID, components);
            return entityID;            
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

        public static void RemoveEntity(int entityID)
        {
            Log.Message("Entity before removal:");
            Log.Data(Systems.DescriptionSystem.GetDebugInfoEntity(entityID));
            foreach(var key in entitiesWithComponent.Keys)
            {
                entitiesWithComponent[key].Remove(entityID);
            }
            componentsOfEntity[entityID].ForEach(component =>
            {
                if(component.TypeID == TransformComponent.TypeID)
                {
                    var transform = (TransformComponent)component;
                    var pos = transform.Position;
                    var floor = Util.CurrentFloor;

                    if (floor.GetCharacter(pos) == entityID)
                        floor.SetCharacter(pos, 0);
                    else if (floor.GetTerrain(pos) == entityID)
                        floor.SetTerrain(pos, 0);
                    else if (floor.GetItems(pos).Contains(entityID))
                        floor.GetItems(pos); //TODO: remove item!
                }
                componentsOfType[component.TypeID].Remove(component);
            });
            componentsOfEntity.Remove(entityID);
            Dump();
            //Log.Data(ToJson());
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
