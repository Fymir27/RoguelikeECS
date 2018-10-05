using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Input;

namespace TheAlchemist.Systems
{
    static class InputSystem
    {
        static Keys lastInput;
        public static void CheckInput()
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
                //send Movement event
            }
        }
    }
}
