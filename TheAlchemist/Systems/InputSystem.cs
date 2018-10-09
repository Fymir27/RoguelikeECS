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
            player = EntityManager.GetEntitiesWithComponent(PlayerComponent.TypeID)[0];
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
                Console.WriteLine("Up pressed");
                RaiseMovementEvent(player, Direction.North);
            }
        }

        private void RaiseMovementEvent(int entity, Direction dir)
        {
            MovementEvent?.Invoke(entity, dir);
        }
    }
}
