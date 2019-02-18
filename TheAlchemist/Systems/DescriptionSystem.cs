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
                foreach (var item in component.GetType().GetProperties())
                {
                    //if (item.Name == "EntityID")
                    //continue;
                    sb.AppendLine("[ " + item.Name + ": " + item.GetValue(component) + " ]");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static string GetCharacterTooltip(int entity)
        {
            string tooltip = "";

            tooltip += "Health  ";

            var hp = EntityManager.GetComponent<HealthComponent>(entity);

            if (hp != null)
            {
                float relativeAmount = hp.Amount / hp.Max;
                int maxBars = 30;
                int curBars = (int)Math.Round(maxBars * relativeAmount);
                tooltip += new string('#', curBars);
            }
            else
            {
                tooltip += "???";
            }

            tooltip += '\n';

            // TODO: Mana

            var stats = EntityManager.GetComponent<StatComponent>(entity);

            if (stats != null)
            {
                tooltip += "Strength:     " + stats[Stat.Strength] + '\n';
                tooltip += "Dexterity:    " + stats[Stat.Dexterity] + '\n';
                tooltip += "Intelligence: " + stats[Stat.Intelligence] + '\n';
            }

            return tooltip;
        }
    }
}
