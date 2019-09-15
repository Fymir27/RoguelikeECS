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
                if (typeof(StatComponent).IsInstanceOfType(component))
                {
                    continue; // doesnt work!
                }
                sb.AppendLine(component.GetType().ToString());

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
        /// <param name="entity">ID of entity</param>
        /// <returns>tooltip as string</returns>
        public static string GetCharacterTooltip(int entity)
        {
            StringBuilder tooltip = new StringBuilder();

            int maxBars = 25;

            tooltip.Append("Health ");

            var hp = EntityManager.GetComponent<HealthComponent>(entity);

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

            var stats = EntityManager.GetComponent<StatComponent>(entity);

            if (stats != null)
            {
                tooltip.Append("Strength:     " + stats[Stat.Strength] + '\n');
                tooltip.Append("Dexterity:    " + stats[Stat.Dexterity] + '\n');
                tooltip.Append("Intelligence: " + stats[Stat.Intelligence] + '\n');
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
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            sb.Append(new string('=', cur));
            sb.Append(new string(' ', max - cur));
            sb.Append(']');
            return sb.ToString();
        }
    }
}
