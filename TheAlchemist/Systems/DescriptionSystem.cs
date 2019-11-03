using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;

    // helper class for translating entities to strings
    class DescriptionSystem
    {
        public static string GetName(int entity)
        {
            return EntityManager.GetComponent<Components.DescriptionComponent>(entity)?.Name;
        }

        public static string GetNameWithID(int entity)
        {
            return EntityManager.GetComponent<Components.DescriptionComponent>(entity)?.Name + " (ID " + entity + ")";
        }

        public static string GetDescription(int entity)
        {
            return EntityManager.GetComponent<Components.DescriptionComponent>(entity)?.Description;
        }

        public static string GetSpecialMessage(int entity, DescriptionComponent.MessageType type)
        {
            string message = "";
            var messages = EntityManager.GetComponent<DescriptionComponent>(entity)?.SpecialMessages;
            messages?.TryGetValue(type, out message);
            return message;
        }

        public static string GetDebugInfoEntity(int entity)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Entity " + entity + ", " + GetName(entity));
            var components = EntityManager.GetComponents(entity);
            foreach (var component in components)
            {
                sb.AppendLine(component.GetType().ToString());
                if (typeof(StatComponent).IsInstanceOfType(component))
                {
                    sb.AppendLine("NOT IMPLEMENTED");
                    continue; // doesnt work!
                }

                switch (component)
                {
                    case StatComponent stats:
                        sb.AppendLine("NOT IMPLEMENTED");
                        continue;

                    case SubstanceComponent substance:
                        sb.AppendLine(substance.ToString());
                        continue;

                    default:
                        break;
                }


                foreach (var propertyInfo in component.GetType().GetProperties())
                {
                    if (propertyInfo == null)
                    {
                        sb.AppendLine("[ NULL ]");
                        continue;
                    }

                    sb.Append("[ " + propertyInfo.Name + ": ");

                    //if (item.Name == "EntityID")
                    //continue;

                    if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetInterfaces().Any(t => t.GetGenericTypeDefinition() == typeof(ICollection<>)))
                    {
                        //var genericType = item.PropertyType.GetGenericTypeDefinition();
                        //sb.AppendLine("Interfaces: " + Util.GetStringFromEnumerable(genericType.GetInterfaces()));
                        //sb.AppendLine("typeof(ICollection<>): " + typeof(ICollection<>));
                        //sb.AppendLine("match?: " + genericType.GetInterfaces().Any(t => t.GetGenericTypeDefinition() == typeof(ICollection<>)));

                        var value = propertyInfo.GetValue(component);

                        if (value == null)
                        {
                            sb.AppendLine("NULL ]");
                            continue;
                        }

                        sb.AppendLine(Util.GetStringFromCollection(value as System.Collections.ICollection));
                    }
                    else
                    {
                        sb.AppendLine(propertyInfo.GetValue(component) + " ]");
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// returns the characters tooltip as a string
        /// only displayed correctly if using monspace font
        /// </summary>
        /// <param name="characterID">ID of entity</param>
        /// <returns>tooltip as string</returns>
        public static string GetCharacterTooltip(int characterID)
        {
            StringBuilder tooltip = new StringBuilder();

            int maxBars = 25;

            tooltip.Append("Health ");

            var hp = EntityManager.GetComponent<HealthComponent>(characterID);

            if (hp != null)
            {
                float relativeAmount = hp.Amount / hp.Max;
                int curBars = (int)Math.Round(maxBars * relativeAmount);
                tooltip.Append(GetAsciiBar(curBars, maxBars));
            }
            else
            {
                tooltip.Append("???");
            }

            tooltip.Append('\n');

            // TODO: Mana
            tooltip.Append("Mana   ");
            tooltip.Append(GetAsciiBar(maxBars, maxBars));
            tooltip.Append('\n');

            var stats = EntityManager.GetComponent<StatComponent>(characterID);

            if (stats != null)
            {
                tooltip.Append("Strength:     " + stats.BaseStats[BaseStat.Strength] + '\n');
                tooltip.Append("Dexterity:    " + stats.BaseStats[BaseStat.Dexterity] + '\n');
                tooltip.Append("Intelligence: " + stats.BaseStats[BaseStat.Intelligence] + '\n');
            }

            return tooltip.ToString();
        }

        /// <summary>
        /// Creates Ascii bar like this: [======   ]
        /// </summary>
        /// <param name="cur">current amount (count of symbols to display)</param>
        /// <param name="max">maximum amount (count of symbols to display)</param>
        /// <returns></returns>
        public static string GetAsciiBar(int cur, int max)
        {
            cur = Util.Clamp(cur, 0, max);
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            sb.Append(new string('=', cur));
            sb.Append(new string(' ', max - cur));
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// returns the item's tooltip as a string
        /// substance information will depend on players substance knowledge
        /// </summary>
        /// <param name="itemID">ID of entity</param>
        /// <returns>tooltip as string</returns>
        public static string GetItemTooltip(int itemID)
        {
            StringBuilder sb = new StringBuilder();

            // normal description
            var descriptionC = EntityManager.GetComponent<DescriptionComponent>(itemID);

            if (descriptionC == null)
            {
                sb.AppendLine("No description available...");
            }
            else
            {
                sb.AppendLine(descriptionC.Description);
            }

            // item properties
            var substance = EntityManager.GetComponent<SubstanceComponent>(itemID);
            var substanceKnowledge = EntityManager.GetComponent<SubstanceKnowledgeComponent>(Util.PlayerID);

            if (substance == null)
            {
                return sb.ToString();
            }

            sb.AppendLine();

            foreach (var prop in substance.Properties)
            {
                // compute total knowledge by adding property and propertyType knowledge
                int knowledge = substanceKnowledge.PropertyKnowledge[prop.Key] + substanceKnowledge.TypeKnowledge[prop.Key.GetPropType()];

                if (knowledge >= 66 || substance.PropertiesKnown)
                {
                    sb.Append("- ")
                        .Append(prop.Key.ToString())
                        .Append(": ")
                        .AppendLine(prop.Value.ToString());
                }
                else if (knowledge >= 33)
                {
                    sb.Append("- ")
                        .Append(prop.Key.ToString())
                        .AppendLine(": ???");
                }
                else
                {
                    sb.AppendLine("- Unknown Property");
                }
            }

            return sb.ToString();
        }
    }
}
