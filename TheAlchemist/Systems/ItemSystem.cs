using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TheAlchemist.Systems
{
    using Components;

    public delegate void ItemPickupHandler(int character, Vector2 position);
    public delegate void ItemUseHandler(int character, int item);

    class ItemSystem
    {
        public void PickUpItem(int character, Vector2 position)
        {
            var inventory = EntityManager.GetComponentOfEntity<InventoryComponent>(character);

            if(inventory == null)
            {
                Log.Warning("Character does not have an inventory! -> " + DescriptionSystem.GetNameWithID(character));
                return;
            }

            if (inventory.Full)
            {
                Log.Message("Invetory is full! -> " + DescriptionSystem.GetNameWithID(character));
                return;
            }
        

            int item = Util.CurrentFloor.GetFirstItem(position);
            Log.Message(DescriptionSystem.GetNameWithID(character) + " picked up " + DescriptionSystem.GetNameWithID(item));

            Util.CurrentFloor.RemoveItem(position, item);
            EntityManager.GetComponentOfEntity<RenderableSpriteComponent>(item).Visible = false;

            inventory.Items.Add(item);

           // Log.Data(DescriptionSystem.GetDebugInfoEntity(character));

            Util.TurnOver(character);
        }

        public void UseItem(int character, int item)
        {
            Log.Message(DescriptionSystem.GetNameWithID(character) + " used " + DescriptionSystem.GetNameWithID(item));
            Util.TurnOver(character);
        }
    }
}
