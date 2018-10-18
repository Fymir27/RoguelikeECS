using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class HealthComponent : Component<HealthComponent>
    {
        public float Amount { get; set; }
        public float Max { get; set; }
        public float Regeneration { get; set; }
    }
}
