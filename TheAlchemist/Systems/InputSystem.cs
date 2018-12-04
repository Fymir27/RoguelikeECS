using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace TheAlchemist.Systems
{
    using Components;
    using Systems;

    // waits for player to input one of the keys and invokes callback with the index of the key chosen
    public delegate void PlayerPromptHandler(Keys[] keys, Action<int> callback);

    class InputSystem
    {
        public event MovementEventHandler MovementEvent;
        public event InteractionHandler InteractionEvent;

        public event ItemPickupHandler PickupItemEvent;
        public event ItemUsedHandler UsedItemEvent;

        public event InventoryToggledHandler InventoryToggledEvent;

        // keys that the player is prompted to press
        Keys[] promptKeys;
        // corresponding callback if key is pressed
        Action<int> callback;

        Keys lastInput;
        int sameKeyHeldFor = 0; // milliseconds for how long key has been held down

        int player = 0; // 0 is an invalid entity ID
        

        public InputSystem()
        {
            player = EntityManager.GetEntitiesWithComponent<PlayerComponent>().FirstOrDefault();
        }

        public void Run(GameTime gameTime)
        {
            KeyboardState keyboard = Keyboard.GetState();
            Keys[] keysPressed = keyboard.GetPressedKeys();


            if (keysPressed.Length == 0)
            {                
                lastInput = Keys.None;
                return;
            }

            if(keysPressed.Any(item => item == lastInput))
            {
                sameKeyHeldFor += gameTime.ElapsedGameTime.Milliseconds;

                if (sameKeyHeldFor < 500)
                {
                    return;
                }
            }
            else
            {
                sameKeyHeldFor = 0;
            }

            // check if player was just prompted for input
            if (promptKeys != null)
            {
                for (int i = 0; i < promptKeys.Length; i++)
                {
                    if (keysPressed.Contains(promptKeys[i]))
                    {
                        promptKeys = null;
                        callback.Invoke(i);
                        break;
                    }
                }
                // don't check for normal input until prompt is resolved
                return;
            }

            // check for general input
            if(keysPressed.Any(item => item == Keys.Up))
            {
                lastInput = Keys.Up;
                RaiseMovementEvent(player, Direction.North);
            }
            else if (keysPressed.Any(item => item == Keys.Right))
            {
                lastInput = Keys.Right;
                RaiseMovementEvent(player, Direction.East);
            }
            else if (keysPressed.Any(item => item == Keys.Down))
            {
                lastInput = Keys.Down;
                RaiseMovementEvent(player, Direction.South);
            }
            else if (keysPressed.Any(item => item == Keys.Left))
            {
                lastInput = Keys.Left;
                RaiseMovementEvent(player, Direction.West);
            }
            else if(keysPressed.Contains(Keys.Space))
            {
                HandleSpacePressed();
                lastInput = Keys.Space;
            }
            else if(keysPressed.Contains(Keys.I))
            {
                InventoryToggledEvent?.Invoke();
                lastInput = Keys.I;
            }
        }

        private void RaiseMovementEvent(int entity, Direction dir)
        {
            MovementEvent?.Invoke(entity, dir);
        }

        // This function tries to do the following things in order:
        // tries to use an item if inventory is open
        // tries to interact with terrain under player
        // tries to pick up Item
        private void HandleSpacePressed()
        {
            int player = Util.PlayerID;

            if (UI.InventoryOpen)
            {
                //Console.WriteLine("Space trigger item use!");
                //Console.WriteLine("UsedItemEvent null? " + UsedItemEvent == null);
                UsedItemEvent?.Invoke(player, EntityManager.GetComponent<InventoryComponent>(player).Items[UI.InventoryCursorPosition - 1]);
                return;
            }
            
            var playerPos = EntityManager.GetComponent<TransformComponent>(player).Position;

            int terrain = Util.CurrentFloor.GetTerrain(playerPos);

            if (terrain != 0) // is there special terrain?
            {
                var terrainInteraction = EntityManager.GetComponent<InteractableComponent>(terrain);

                if (terrainInteraction != null) // is it interactable?
                {
                    InteractionEvent?.Invoke(player, terrain);
                    return;
                }
            }

            bool itemsOnFloor = Util.CurrentFloor.GetItems(playerPos).Count() > 0; // are there items here?
            
            if(!itemsOnFloor)
            {
                UISystem.Message("No items here to be picked up!");
                return;
            }

            PickupItemEvent?.Invoke(player, playerPos);
        }

        public void HandlePlayerPrompt(Keys[] keys, Action<int> callback)
        {
            promptKeys = keys;
            this.callback = callback;
        }
    }
}
