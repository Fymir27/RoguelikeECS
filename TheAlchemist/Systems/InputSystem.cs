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
                //return;
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
        }

        private void RaiseMovementEvent(int entity, Direction dir)
        {
            MovementEvent?.Invoke(entity, dir);
        }
    }
}
