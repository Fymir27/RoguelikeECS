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

        [Newtonsoft.Json.JsonConstructor]
        public EquipmentComponent(string weaponName, string armorName)
        { 
            if(weaponName != null && weaponName != "")
            {
                Weapon = GameData.Instance.CreateTemplateItem(weaponName);
            }
            if (armorName != null && armorName != "")
            {
                Armor = GameData.Instance.CreateTemplateItem(armorName);
            }
        }

        public EquipmentComponent() { }
    }
}
