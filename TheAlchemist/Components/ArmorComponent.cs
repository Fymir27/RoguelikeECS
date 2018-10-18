using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class ArmorComponent : Component<ArmorComponent>
    {
        public float FlatMitigation { get; set; }
        public float PercentMitigation { get; set; }
    }
}
