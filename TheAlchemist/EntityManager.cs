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
        // maps from component Type ID to List of entities that have that specific component attached
        static Dictionary<int, List<int>> entitiesWithComponent = new Dictionary<int, List<int>>();

        // maps from entity ID to a list of components it has
        static Dictionary<int, List<IComponent>> entities = new Dictionary<int, List<IComponent>>();

        // running counter for entiy IDs (ID 0 is unused)
        static int entityIDCounter = 1;

        // returns an array of entity IDs that have a specific component attached
        public static int[] GetEntitiesWithComponent(int componentTypeID)
        {
            Console.WriteLine("EntityManger::GetEntitiesWithComponent called with componentTypeID = " + componentTypeID);           
            return entitiesWithComponent[componentTypeID].ToArray();
        }

        // returns a list of all components attached to specific entity
        public static IComponent[] GetAllComponentsOfEntity(int entityID)
        {
            return entities[entityID].ToArray();
        }

        
        public static IComponent GetComponentOfEntity(int entityID, int componentTypeID)
        {
            foreach(var component in entities[entityID])
            {
                if(component.TypeID == componentTypeID)
                {
                    return component;
                }
            }
            return null;
        }

        public static IComponent GetComponentOfEntity<T>(int entityID) where T : Component<T>
        {
            var componentTypeID = Component<T>.TypeID;
            foreach (var component in entities[entityID])
            {
                if (component.TypeID == componentTypeID)
                {
                    return component;
                }
            }
            return null;
        }

        // creates new Entity with an empty list of components and returns its ID
        public static int createEntity()
        {
            int entityID = entityIDCounter++;

            entities.Add(entityID, new List<IComponent>());
            
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
            int componentType = component.TypeID;

            if (!entitiesWithComponent.ContainsKey(componentType))
            {
                entitiesWithComponent.Add(componentType, new List<int>());
            }

            entitiesWithComponent[componentType].Add(entityID);
            entities[entityID].Add(component);
        }

        // debug
        public static void Dump()
        {
            Console.WriteLine("----- DEBUG -----");

            Console.WriteLine("Entities: ");
            foreach(var key in entities.Keys)
            {
                Console.Write(key + ": [");
                foreach (var item in entities[key])
                {
                    Console.Write(item.TypeID + ", ");
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

            Console.WriteLine("----- DEBUG -----");
        }
    }
}
