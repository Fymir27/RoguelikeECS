using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    public enum PropertyType
    {
        None,
        Resource,
        Stat,
        Elemental
    }

    public enum Property
    {
        // Ressource
        Health,
        Mana,
        Stamina,
        // Stat
        Str,
        Dex,
        Int,
        // Elemental
        Fire,
        Water,
        Earth,
        Wind
    }

    static class PropertyExtensions
    {
        // defines types of specific properties
        static Dictionary<PropertyType, Property[]> propertyGroups = new Dictionary<PropertyType, Property[]>
        {
            {
                PropertyType.Resource, new Property[]
                {
                    Property.Health, Property.Mana, Property.Stamina
                }
            },
            {
                PropertyType.Stat, new Property[]
                {
                    Property.Str, Property.Dex, Property.Int
                }
            },
            {
                PropertyType.Elemental, new Property[]
                {
                    Property.Fire, Property.Water, Property.Earth, Property.Wind
                }
            }
        };

        public static PropertyType GetPropType(this Property prop)
        {
            foreach (var group in propertyGroups)
            {
                if (group.Value.Contains(prop))
                {
                    return group.Key;
                }
            }
            Log.Warning("Property " + prop.ToString() + " does not have dedicated Property Type!");
            return PropertyType.None;
        }
    }


    public enum MaterialType
    {
        None,
        Potion,
        Plant,
        Mineral
        //...
    }

    class SubstanceComponent : Component<SubstanceComponent>
    {
        // contains actual values of properties      
        public Dictionary<Property, int> Properties { get; set; }

        // determines if properties should be known to the player from the beginning
        public bool PropertiesKnown { get; set; } = false;

        public MaterialType MaterialType { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var property in Properties)
            {
                {
                    sb.AppendLine(String.Format("[{0}; {1}: {2}]", property.Key.GetPropType(), property.Key, property.Value));
                }
            }
            return sb.ToString();
        }

        /*
        interface PropertyGroup
        {
            int[] GetProperties();
        }

        class RessourceProperties : PropertyGroup
        {
            public int[] GetProperties()
            {
                throw new NotImplementedException();
            }

            public enum Property
            {
                Health,
                Mana,
                Stamina
            }

            Dictionary<Property, int> values;

            public int this[Property key]
            {
                get => values[key];
            }           
        }
        */


    }
}
