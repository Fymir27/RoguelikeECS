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

    public delegate void ItemPickupHandler(int character);
    public delegate void ItemUsedHandler(int character, int item);

    class ItemSystem
    {
        public event PlayerPromptHandler PlayerPromptEvent;
        public event InventoryToggledHandler InventoryToggledEvent;
        public event TargetModeToggledHandler TargetModeToggledEvent;
        public event WaitForConfirmationHandler WaitForConfirmationEvent;

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

        public void PickUpItem(int character)
        {
            Vector2 position = EntityManager.GetComponent<TransformComponent>(character).Position;
            int pickupItemID = Util.CurrentFloor.GetFirstItem(position);

            if(pickupItemID == 0)
            {
                UISystem.Message("Nothing here to be picked up!");
                return;
            }

            var inventory = EntityManager.GetComponent<InventoryComponent>(character);

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

            // post message to player
            UISystem.Message(DescriptionSystem.GetNameWithID(character) + " picked up " + DescriptionSystem.GetNameWithID(pickupItemID));

            Util.CurrentFloor.RemoveItem(position, pickupItemID);

            var pickupComponentTypeIDs = EntityManager.GetComponentTypeIDs(pickupItemID);

            bool match = false; // can item can be stacked here

            // start looking through every item in inventory
            // to find out if picked up item can be stacked
            foreach (var invItemID in inventory.Items)
            {
                var invItemInfo = EntityManager.GetComponent<ItemComponent>(invItemID);

                // if already stacked to max, jump straight to next
                if (invItemInfo.Count == invItemInfo.MaxCount)
                {
                    match = false;
                    continue;
                }

                var invItemComponentIDs = EntityManager.GetComponentTypeIDs(invItemID);

                // check if both items have the exact same components attached
                match = invItemComponentIDs.All(pickupComponentTypeIDs.Contains) && invItemComponentIDs.Count() == pickupComponentTypeIDs.Count();             

                if(match)
                {                  
                    var pickedUpItemInfo = EntityManager.GetComponent<ItemComponent>(pickupItemID);

                    // cumulative count doesnt exceed max -> just increase count 
                    // in inventory and remove picked up item
                    if(invItemInfo.Count + pickedUpItemInfo.Count <= invItemInfo.MaxCount)
                    {
                        invItemInfo.Count += pickedUpItemInfo.Count;
                        EntityManager.RemoveEntity(pickupItemID);
                    }

                    // cumulative count exceeds max ->
                    // stack up to max, rest becomes new stack
                    else
                    {
                        pickedUpItemInfo.Count -= invItemInfo.MaxCount - invItemInfo.Count; // remove difference used to max out inventory item
                        invItemInfo.Count = invItemInfo.MaxCount;
                        inventory.Items.Add(pickupItemID);
                    }
                    break;
                }              
            }

            if (!match)
            {
                inventory.Items.Add(pickupItemID);
            }

            Util.TurnOver(character);
        }

        public void UseItem(int character, int item)
        {
            //Console.WriteLine("ItemSystem.UseItem");
            //Log.Message(DescriptionSystem.GetNameWithID(character) + " used " + DescriptionSystem.GetNameWithID(item));
            
            //Log.Data(DescriptionSystem.GetDebugInfoEntity(item));
            usableComponents = EntityManager.GetComponents(item).Where(x => x.TypeID == UsableComponent.TypeID).Cast<UsableComponent>();
            

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

            int itemID = consumable.EntityID;
            var itemComponent = EntityManager.GetComponent<ItemComponent>(itemID);

            if(itemComponent.Count > 1)
            {
                itemComponent.Count--;
            }
            else
            {
                RemoveFromInventory(itemID, character);
            }
        }

        public void ThrowItem(int character, ThrowableComponent throwable)
        {
            UISystem.Message(DescriptionSystem.GetNameWithID(character) + " throws " + DescriptionSystem.GetNameWithID(throwable.EntityID));
            
            if(TargetModeToggledEvent == null)
            {
                Log.Error("TargetModeToggledEvent is null!");
                throw new NullReferenceException("TargetModeToggledEvent is null!");
            }

            TargetModeToggledEvent.Invoke();
            WaitForConfirmationEvent?.Invoke(() => ThrowItemConfirmed(character, throwable));
        }

        private void ThrowItemConfirmed(int character, ThrowableComponent throwable)
        {
            UISystem.Message(DescriptionSystem.GetNameWithID(character) + " throws " + DescriptionSystem.GetNameWithID(throwable.EntityID));
            TargetModeToggledEvent.Invoke();
            var pos = EntityManager.GetComponent<TransformComponent>(Util.TargetIndicatorID).Position;

            int itemID = throwable.EntityID;

            RemoveFromInventory(itemID, character);

            Util.CurrentFloor.PlaceItem(pos, itemID);
        }

        public void DropItem(int character, DroppableComponent droppable)
        {
            UISystem.Message(DescriptionSystem.GetNameWithID(character) + " drops " + DescriptionSystem.GetNameWithID(droppable.EntityID));

            int itemID = droppable.EntityID;

            RemoveFromInventory(itemID, character);

            var transform = EntityManager.GetComponent<TransformComponent>(character);

            Util.CurrentFloor.PlaceItem(transform.Position, itemID);          
        }

        public void RemoveFromInventory(int item, int character)
        {
            var inventory = EntityManager.GetComponent<InventoryComponent>(character);

            if (inventory == null)
            {
                Log.Error("Could not remove " + DescriptionSystem.GetNameWithID(item) + " from inventory!");
                Log.Error(DescriptionSystem.GetNameWithID(character) + " has no inventory!");
                return;
            }

            inventory.Items.Remove(item);

            UI.InventoryCursorPosition = 1;
        }
    }
}
