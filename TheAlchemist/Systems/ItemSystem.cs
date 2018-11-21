using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;


namespace TheAlchemist.Systems
{
    using Components;
    using Components.ItemComponents;

    public delegate void ItemPickupHandler(int character, Vector2 position);
    public delegate void ItemUsedHandler(int character, int item);

    class ItemSystem
    {
        public event PlayerPromptHandler PlayerPromptEvent;

        public void PickUpItem(int character, Vector2 position)
        {
            var inventory = EntityManager.GetComponentOfEntity<InventoryComponent>(character);

            if (inventory == null)
            {
                Log.Warning("Character does not have an inventory! -> " + DescriptionSystem.GetNameWithID(character));
                return;
            }

            if (inventory.Full)
            {
                UISystem.Message("Inventory is full! -> " + DescriptionSystem.GetNameWithID(character));
                return;
            }
        

            int item = Util.CurrentFloor.GetFirstItem(position);
            UISystem.Message(DescriptionSystem.GetNameWithID(character) + " picked up " + DescriptionSystem.GetNameWithID(item));

            Util.CurrentFloor.RemoveItem(position, item);
            EntityManager.GetComponentOfEntity<RenderableSpriteComponent>(item).Visible = false;

            inventory.Items.Add(item);

           // Log.Data(DescriptionSystem.GetDebugInfoEntity(character));

            Util.TurnOver(character);
        }

        public void UseItem(int character, int item)
        {
            //Console.WriteLine("ItemSystem.UseItem");
            //Log.Message(DescriptionSystem.GetNameWithID(character) + " used " + DescriptionSystem.GetNameWithID(item));
            
            //Log.Data(DescriptionSystem.GetDebugInfoEntity(item));
            IEnumerable<UsableComponent> usableComponents = EntityManager.GetAllComponentsOfEntity(item).Where(x => x.TypeID == UsableComponent.TypeID).Cast<UsableComponent>();
            

            if(!usableComponents.Any())
            {
                UISystem.Message(DescriptionSystem.GetNameWithID(item) + " is not usable!");
                return;
            }

            string options = "";
            List<Keys> keys = new List<Keys>();
            List<Action> callbacks = new List<Action>();

            foreach(var component in usableComponents)
            {
                options += component.Action + " | ";
                keys.Add(component.Key);
                callbacks.Add(() => UISystem.Message(component.Action));
            }

            options = options.Substring(0, options.Length - 3);

            UISystem.Message(DescriptionSystem.GetNameWithID(character) + " used " + DescriptionSystem.GetNameWithID(item) + ". Options: " + options);

            PlayerPromptEvent?.Invoke(keys.ToArray(), callbacks.ToArray());

            Util.TurnOver(character);
        }

        
    }
}
