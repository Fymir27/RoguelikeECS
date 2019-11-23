using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class ArmorComponent : Component<ArmorComponent>
    {
        public int FlatMitigation { get; set; }
        public int PercentMitigation { get; set; }

        public override string ToString()
        {
            return String.Format("{0}: {1}%+{2}", Systems.DescriptionSystem.GetName(EntityID), PercentMitigation, FlatMitigation);
        }
    }
}
