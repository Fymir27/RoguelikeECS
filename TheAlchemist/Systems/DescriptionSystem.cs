using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    // helper class for DescriptionComponents
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
    }
}
