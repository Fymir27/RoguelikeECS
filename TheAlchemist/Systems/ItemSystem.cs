﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TheAlchemist.Systems
{
    using Components;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ItemUsage
    {
        Consume,
        Throw
    }

    public delegate void ItemPickupHandler(int character);
    public delegate void ItemUsedHandler(int character, int item, ItemUsage usage);
    public delegate void ItemConsumedHandler(int character, int item);

    class ItemSystem
    {
        public event HealthGainedHandler HealthGainedEvent;
        public event HealthLostHandler HealthLostEvent;
        public event StatChangedHandler StatChangedEvent;

        [JsonConverter(typeof(StringEnumConverter))]
        public enum EffectType
        {
            // Ressources
            Health,
            Mana,

            // Stats
            Str,
            Dex,
            Int,

            // Elemental
            Ice,
            Fire
        }

        public enum ItemEffectType
        {
            RestoreHealth,
            LoseHealth
        }

        public void PickUpItem(int character)
        {
            Position position = EntityManager.GetComponent<TransformComponent>(character).Position;
            int pickupItemID = Util.CurrentFloor.GetFirstItem(position);

            if (pickupItemID == 0)
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

            #region ItemStacking
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

                if (match)
                {
                    var pickedUpItemInfo = EntityManager.GetComponent<ItemComponent>(pickupItemID);

                    // cumulative count doesnt exceed max -> just increase count 
                    // in inventory and remove picked up item
                    if (invItemInfo.Count + pickedUpItemInfo.Count <= invItemInfo.MaxCount)
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
            #endregion

            Util.TurnOver(character);
        }

        public void UseItem(int character, int item, ItemUsage usage)
        {
            var usableItem = EntityManager.GetComponent<UsableItemComponent>(item);

            if (usableItem == null)
            {
                UISystem.Message("You can't use that!");
                return;
            }

            if (!usableItem.Usages.Contains(usage))
            {
                UISystem.Message("You can't use that like that!");
                return;
            }

            UISystem.Message(DescriptionSystem.GetNameWithID(character) + " uses " + DescriptionSystem.GetNameWithID(item) + " -> " + usage);

            switch (usage)
            {
                case ItemUsage.Consume:
                    ConsumeItem(character, item);
                    break;

                case ItemUsage.Throw:
                    InputManager.Instance.InitiateTargeting((pos) => ThrowItem(character, item, pos));
                    //UISystem.Message("Throwing items is not yet implemented");
                    break;
            }
        }

        public void DecreaseItemCount(int character, int item)
        {
            var itemComponent = EntityManager.GetComponent<ItemComponent>(item);

            if (itemComponent.Count > 1)
            {
                itemComponent.Count--;
            }
            else
            {
                RemoveFromInventory(item, character);
            }
        }

        public void ConsumeItem(int character, int item)
        {
            UISystem.Message(String.Format("{0} consumes {1}", DescriptionSystem.GetNameWithID(character), DescriptionSystem.GetNameWithID(item)));

            var substance = EntityManager.GetComponent<SubstanceComponent>(item);

            Dictionary<Property, int> properties = null;

            if (substance == null)
            {
                Log.Warning("Consumable Item does not have SubstanceComponent attached!");
            }
            else
            {
                properties = substance.Properties;
            }

            if (properties == null || properties.Count == 0)
            {
                UISystem.Message("That had no effect...");
                DecreaseItemCount(character, item);
                Util.TurnOver(character);
                return;
            }

            foreach (var prop in properties)
            {
                switch (prop.Key)
                {
                    // --- Resource --- //
                    case Property.Health:
                        if (prop.Value < 0)
                            HealthLostEvent?.Invoke(character, -prop.Value);
                        else
                            HealthGainedEvent?.Invoke(character, prop.Value);
                        break;

                    // --- Stat --- //
                    case Property.Str:
                    case Property.Dex:
                    case Property.Int:
                        Stat stat = GetStatFromItemProperty(prop.Key);
                        StatChangedEvent?.Invoke(character, stat, prop.Value, prop.Value * 2);
                        break;

                    default:
                        UISystem.Message(String.Format("Item effect not implemented: {0} ({1})", prop.Key.ToString(), prop.Value));
                        break;
                }
            }
            //var itemEffects = EntityManager.GetComponent<UsableItemComponent>(item).Effects;

            //foreach (var effect in itemEffects)
            //{
            //    switch (effect.Type)
            //    {
            //        case EffectType.Health:
            //            if (effect.Harmful)
            //                HealthLostEvent?.Invoke(character, effect.Potency * 0.5f);
            //            else
            //                HealthGainedEvent?.Invoke(character, effect.Potency * 0.5f);
            //            break;

            //        case EffectType.Str:
            //        case EffectType.Dex:
            //        case EffectType.Int:
            //            int amount = (int)Math.Round(effect.Potency * 0.1f) * Util.Sign(!effect.Harmful);
            //            Stat stat = GetStatFromEffectType(effect.Type);
            //            int duration = 7 * (int)Math.Round(effect.Potency * 0.1f);
            //            StatChangedEvent?.Invoke(character, stat, amount, duration);
            //            break;

            //        default:
            //            UISystem.Message("Consume: " + effect.Type + " not implemented!");
            //            break;
            //    }
            //}

            DecreaseItemCount(character, item);
            Util.TurnOver(character);
        }

        public void ThrowItem(int character, int item, Position pos)
        {
            //UISystem.Message(DescriptionSystem.GetNameWithID(character) + " throws " + DescriptionSystem.GetNameWithID(item) + " at " + pos);
            UISystem.Message(DescriptionSystem.GetName(character) + " throws " + DescriptionSystem.GetName(item));

            var usableComponent = EntityManager.GetComponent<UsableItemComponent>(item);
            int targetCharacter = Util.CurrentFloor.GetCharacter(pos);
            int targetTerrain = Util.CurrentFloor.GetTerrain(pos);

            if (targetCharacter == 0)
            {
                if (usableComponent.BreakOnThrow)
                {
                    UISystem.Message("The item breaks!");
                }
                else
                {
                    bool solid = false;

                    var collider = EntityManager.GetComponent<ColliderComponent>(targetTerrain);
                    if (collider != null)
                    {
                        solid = collider.Solid;
                    }

                    if (solid)
                    {
                        // TODO: bounce off wall
                        UISystem.Message("You can't throw that there!");
                        return;
                    }

                    Util.CurrentFloor.PlaceItem(pos, item);
                }
            }
            else
            {
                UISystem.Message("It hits " + DescriptionSystem.GetName(targetCharacter) + "!");
                foreach (var effect in usableComponent.Effects)
                {
                    switch (effect.Type)
                    {
                        case EffectType.Health:
                            if (effect.Harmful)
                                HealthLostEvent?.Invoke(targetCharacter, effect.Potency * 0.2f);
                            else
                                HealthGainedEvent?.Invoke(targetCharacter, effect.Potency * 0.2f);
                            break;

                        case EffectType.Str:
                        case EffectType.Dex:
                        case EffectType.Int:
                            int amount = (int)Math.Round(effect.Potency * 0.1f * 0.5f) * Util.Sign(!effect.Harmful);
                            Stat stat = GetStatFromEffectType(effect.Type);
                            int duration = 7 * (int)Math.Round(effect.Potency * 0.1f * 0.5f);
                            StatChangedEvent?.Invoke(targetCharacter, stat, amount, duration);
                            break;

                        default:
                            UISystem.Message("Throw: " + effect.Type + " not implemented!");
                            return;
                    }
                }
            }

            DecreaseItemCount(character, item);
            Util.TurnOver(character);
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

            EntityManager.RemoveEntity(item);

            UI.InventoryCursorPosition = 1;
        }

        private Stat GetStatFromEffectType(EffectType effect)
        {
            switch (effect)
            {
                case EffectType.Str: return Stat.Strength;

                case EffectType.Dex: return Stat.Dexterity;

                case EffectType.Int: return Stat.Intelligence;

                default:
                    Log.Error("Can't get Stat from this Item Effect Type: " + effect);
                    return Stat.Strength;
            }
        }

        private Stat GetStatFromItemProperty(Property prop)
        {
            switch (prop)
            {
                case Property.Str: return Stat.Strength;
                case Property.Dex: return Stat.Dexterity;
                case Property.Int: return Stat.Intelligence;
                default: throw new ArgumentException("Can't get Stat from this Item Property: " + prop.ToString(), "prop");
            }
        }
    }
}
