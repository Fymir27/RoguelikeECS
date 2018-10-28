using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Input;

namespace TheAlchemist.Systems
{
    using Components;

    class InputSystem
    {
        public event MovementEventHandler MovementEvent;
        public event InteractionHandler InteractionEvent;
        public event ItemPickupHandler PickupItemEvent;

        Keys lastInput;
        int player = 0; // 0 is an invalid entity ID

        public InputSystem()
        {
            player = EntityManager.GetEntitiesWithComponent<PlayerComponent>().FirstOrDefault();
        }

        public void Run()
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
                //TODO: allow holding of key if pressed for long enough
                return;
            }

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
                HandlePlayerInteraction();
                lastInput = Keys.Space;
            }
            else if(keysPressed.Any(item => item == Keys.D1))
            {
                Util.FOV = FOV.Permissive;
                Util.CurrentFloor.CalculateTileVisibility();
            }
            else if (keysPressed.Any(item => item == Keys.D2))
            {
                Util.FOV = FOV.Medium;
                Util.CurrentFloor.CalculateTileVisibility();
            }
            else if (keysPressed.Any(item => item == Keys.D3))
            {
                Util.FOV = FOV.Restricted;
                Util.CurrentFloor.CalculateTileVisibility();
            }
        }

        private void RaiseMovementEvent(int entity, Direction dir)
        {
            MovementEvent?.Invoke(entity, dir);
        }

        // first tries to interact with terrain under player
        // then tries to pick up Item
        private void HandlePlayerInteraction()
        {
            int player = Util.PlayerID;
            var playerPos = EntityManager.GetComponentOfEntity<TransformComponent>(player).Position;

            int terrain = Util.CurrentFloor.GetTerrain(playerPos);

            if (terrain != 0) // is there special terrain?
            {
                var terrainInteraction = EntityManager.GetComponentOfEntity<InteractableComponent>(terrain);

                if (terrainInteraction != null) // is it interactable?
                {
                    InteractionEvent?.Invoke(player, terrain);
                    return;
                }
            }

            bool itemsOnFloor = Util.CurrentFloor.GetItems(playerPos).Count() > 0; // are there items here?
            
            if(!itemsOnFloor)
            {
                Log.Message("No items here to be picked up!");
                return;
            }

            PickupItemEvent?.Invoke(player, playerPos);
        }
    }
}
