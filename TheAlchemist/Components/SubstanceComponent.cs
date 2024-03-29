﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
        Nature,
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
                    Property.Fire, Property.Water, Property.Nature, Property.Wind
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

        public static Stat ToStat(this Property prop)
        {          
            switch (prop)
            {
                // base stats
                case Property.Str: return Stat.Strength;
                case Property.Dex: return Stat.Dexterity;
                case Property.Int: return Stat.Intelligence;

                // Resistance / Affinity
                case Property.Fire: return Stat.Fire;
                case Property.Water: return Stat.Water;
                case Property.Nature: return Stat.Nature;
                case Property.Wind: return Stat.Wind;

                default:
                    Log.Error(String.Format("Could not convert Property {0} to Stat!", prop.ToString()));
                    return Stat.Strength;
            }
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

        public override bool Equals(object obj)
        {
            if(!base.Equals(obj))
            {
                return false;
            }

            SubstanceComponent other = obj as SubstanceComponent;

            if(this.MaterialType != other.MaterialType)
            {
                return false;
            }

            if(this.Properties.Count != other.Properties.Count)
            {
                return false;
            }

            foreach (var prop in other.Properties.Keys)
            {
                if(!this.Properties.Keys.Contains(prop) ||
                   this.Properties[prop] != other.Properties[prop])
                {
                    return false;
                }
            }

            return true;
        }

        [JsonConstructor]
        public SubstanceComponent(MaterialType materialType, Dictionary<Property, int> properties = null, bool propertiesKnown = false, 
            bool generateRandomly = false, int count = 1, int min = 1, int max = 99)
        {
            MaterialType = materialType;
            if (properties == null)
                properties = new Dictionary<Property, int>();
            if (generateRandomly)
                properties = Systems.ItemSystem.GenerateRandomProperties(count, min, max);
            Properties = properties;
            PropertiesKnown = propertiesKnown;
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
