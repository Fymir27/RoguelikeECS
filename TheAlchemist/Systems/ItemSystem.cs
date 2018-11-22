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
        public event InventoryToggledHandler InventoryToggledEvent;

        public event HealthGainedHandler HealthGainedEvent;
        public event HealthLostHandler HealthLostEvent;

        public enum ItemEffectType
        {
            RestoreHealth,
            LoseHealth
        }

        public struct ItemEffectDescription
        {
            public ItemEffectType Type;
            public float[] Values;
        }
    
        int itemInUse = 0;
        IEnumerable<UsableComponent> usableComponents;

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
            usableComponents = EntityManager.GetAllComponentsOfEntity(item).Where(x => x.TypeID == UsableComponent.TypeID).Cast<UsableComponent>();
            

            if(!usableComponents.Any())
            {
                UISystem.Message(DescriptionSystem.GetNameWithID(item) + " is not usable!");
                return;
            }

            itemInUse = item;

            // choose a random use action for npcs
            if(character != Util.PlayerID)
            {
                int i = Game.Random.Next(usableComponents.Count());
                var chosenComponent = usableComponents.ElementAt(i);
                chosenComponent.Handler.Invoke(this, character);
                Util.TurnOver(character);
                return;
            }

            // present the player with options of what to do with the item
            string options = "";
            List<Keys> keys = new List<Keys>();

            foreach(var component in usableComponents)
            {
                options += component.Action + ", ";
                keys.Add(component.Key);
            }

            // remove exta ", " at the end
            options = options.Substring(0, options.Length - 2); 

            UISystem.Message("How do you want to use that item? (" + options + ")");

            // set up callback when buttons are pressed
            PlayerPromptEvent?.Invoke(keys.ToArray(), ChooseOption);
        }

        // callback that gets invoked when player 
        // chooses what to do with the item
        public void ChooseOption(int i)
        {
            var chosenComponent = usableComponents.ElementAt(i);
            //UISystem.Message("You chose: " + chosenComponent.Action);

            // invoke the specific handler of the component
            chosenComponent.Handler.Invoke(this, Util.PlayerID);

            // close inventory
            InventoryToggledEvent?.Invoke();
            Util.TurnOver(Util.PlayerID);
        }

        public void ConsumeItem(int character, ConsumableComponent consumable)
        {
            UISystem.Message(DescriptionSystem.GetNameWithID(character) + " consumes " + DescriptionSystem.GetNameWithID(consumable.EntityID));
            foreach (var effect in consumable.Effects)
            {
                switch (effect.Type)
                {
                    case ItemEffectType.RestoreHealth:
                        HealthGainedEvent?.Invoke(character, effect.Values[0]);
                        break;

                    case ItemEffectType.LoseHealth:
                        HealthLostEvent?.Invoke(character, effect.Values[0]);
                        break;

                    default:
                        break;
                }
            }
        }

        public void ThrowItem(int character, ThrowableComponent throwable)
        {
            UISystem.Message(DescriptionSystem.GetNameWithID(character) + " throws " + DescriptionSystem.GetNameWithID(throwable.EntityID));
        }

        public void DropItem(int character, DroppableComponent droppable)
        {
            UISystem.Message(DescriptionSystem.GetNameWithID(character) + " drops " + DescriptionSystem.GetNameWithID(droppable.EntityID));
        }
    }
}
