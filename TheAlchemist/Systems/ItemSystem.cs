using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TheAlchemist.Systems
{
    public delegate void ItemPickupHandler(int character, Vector2 position);
    public delegate void ItemUseHandler(int character, int item);

    class ItemSystem
    {
        public void PickUpItem(int character, Vector2 position)
        {
            int item = Util.CurrentFloor.GetItems(position).First();
            Log.Message(DescriptionSystem.GetNameWithID(character) + " picked up " + DescriptionSystem.GetNameWithID(item));
        }

        public void UseItem(int character, int item)
        {
            Log.Message(DescriptionSystem.GetNameWithID(character) + " used " + DescriptionSystem.GetNameWithID(item));
        }
    }
}
