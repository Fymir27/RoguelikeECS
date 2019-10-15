using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    /// <summary>
    /// This class keeps track of how much an entity knows about
    /// the different properties of a substance
    /// </summary>
    class SubstanceKnowledgeComponent : Component<SubstanceKnowledgeComponent>
    {
        /// <summary>
        /// Knowledge about specific properties
        /// </summary>
        public Dictionary<Property, int> PropertyKnowledge { get; set; }

        /// <summary>
        /// Knowledge about groups of properties
        /// </summary>
        public Dictionary<PropertyType, int> TypeKnowledge { get; set; }

        /// <summary>
        /// default ctor that intitializes all values to 0
        /// </summary>
        public SubstanceKnowledgeComponent()
        {
            PropertyKnowledge = new Dictionary<Property, int>();
            // loops through all the values of the enum
            foreach (var prop in (Property[])Enum.GetValues(typeof(Property)))
            {
                PropertyKnowledge.Add(prop, 0);
            }

            TypeKnowledge = new Dictionary<PropertyType, int>();
            // loops through all the values of the enum
            foreach (var type in (PropertyType[])Enum.GetValues(typeof(PropertyType)))
            {
                TypeKnowledge.Add(type, 0);
            }
        }
    }
}
