using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class WeaponComponent : Component<WeaponComponent>
    {
        public List<Systems.DamageRange> Damages { get; set; }
        public List<Systems.StatScaling> Scalings { get; set; } = new List<Systems.StatScaling>();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Systems.DescriptionSystem.GetName(EntityID));
            sb.Append(": ");
            foreach (var dmg in Damages)
            {
                sb.Append(dmg.ToString());
            }
            sb.Append(" (");
            foreach (var scaling in Scalings)
            {
                sb.Append(scaling.Stat.ToString()).Append(",");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(")");
            return sb.ToString();
        }
    }
}
