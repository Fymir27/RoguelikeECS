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
    }
}
