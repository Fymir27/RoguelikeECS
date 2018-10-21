using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    // helper class for translating entities to strings
    class DescriptionSystem
    {
        public static string GetName(int entity)
        {           
            return EntityManager.GetComponentOfEntity<Components.DescriptionComponent>(entity)?.Name;            
        }

        public static string GetDescription(int entity)
        {
            return EntityManager.GetComponentOfEntity<Components.DescriptionComponent>(entity)?.Description;
        }

        public static string GetDebugInfoEntity(int entity)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Entity " + entity + ", " + GetName(entity));
            var components = EntityManager.GetAllComponentsOfEntity(entity);
            components.ToList().ForEach(component => sb.AppendLine(component.ToString()));
            return sb.ToString();
        }
    }
}
