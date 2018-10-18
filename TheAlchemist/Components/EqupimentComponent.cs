using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class EquipmentComponent : Component<EquipmentComponent>
    {
        public int Weapon { get; set; } // entityID
        public int Armor { get; set; }  // entityID
    }
}
