using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class WeaponComponent : Component<WeaponComponent>
    {
        public float Damage { get; set; }
    }
}
