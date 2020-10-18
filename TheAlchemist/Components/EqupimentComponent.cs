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

        //[Newtonsoft.Json.JsonConstructor]
        public EquipmentComponent(string weaponName, string armorName)
        { 
            if(weaponName != null && weaponName != "")
            {
                Weapon = GameData.Instance.CreateItem(weaponName);
            }
            if (armorName != null && armorName != "")
            {
                Armor = GameData.Instance.CreateItem(armorName);
            }
        }

        [Newtonsoft.Json.JsonConstructor]
        public EquipmentComponent(string weaponName, string armorName, List<IComponent> weaponComponents, List<IComponent> armorComponents) : this(weaponName, armorName)
        {
            if(weaponComponents != null && weaponComponents.Count > 0)
            {
                Weapon = EntityManager.CreateEntity(weaponComponents, EntityType.Item);
            }
            if(armorComponents != null && armorComponents.Count > 0)
            {
                Armor = EntityManager.CreateEntity(armorComponents, EntityType.Item);
            }
        }

        public EquipmentComponent() { }
    }
}
