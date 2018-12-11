using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TheAlchemist
{
    using Systems;

    static class InputManager
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Command
        {
            None,

            // movement
            MoveN,
            MoveNE,
            MoveE,
            MoveSE,
            MoveS,
            MoveSW,
            MoveW,
            MoveNW,

            // UI
            OpenInv,
            CloseInv,
            EnterLineTargeting, // highlights line to target
            EnterTileTargeting, // only highlights target
            LeaveTargeting,
            ConfirmTarget,

            // Items
            UseItem, // general usage
            ConsumeItem,
            DropItem,
            ThrowItem
        }

        // idea from Kyzrati @GridSageGames (Cogmind)
        // https://old.reddit.com/r/roguelikedev/comments/3g2mcw/faq_friday_18_input_handling/ctu973v/
        [JsonConverter(typeof(StringEnumConverter))]
        public enum CommandDomain
        {
            Exploring, // default/running around
            Examining,
            Targeting,
            Inventory
        }

        static Stack<CommandDomain> domainHistory = new Stack<CommandDomain>();

        // commandToExecute = KeyToCommand[ActiveDomain][PressedKey]
        static Dictionary<CommandDomain, Dictionary<Keys, Command>> keyToCommand = new Dictionary<CommandDomain, Dictionary<Keys, Command>>();
        /* default key bindings:
        {            
            {
                CommandDomain.Exploring, new Dictionary<Keys, Command>()
                {
                    { Keys.Up, Command.MoveN },
                    { Keys.Right, Command.MoveE },
                    { Keys.Down, Command.MoveS },
                    { Keys.Left, Command.MoveW }
                }
            },
            {
                CommandDomain.Inventory, new Dictionary<Keys, Command>()
                {
                    { Keys.I, Command.CloseInv }
                }
            }
            // etc...
        };
        */

        // -- real input related -- //
        static readonly KeyboardState noKeyPressed = new KeyboardState();
        static          KeyboardState prevKeyState = noKeyPressed;

        static readonly int keyHoldDelay = 500; // delay until key registers as beeing held down
        static          int keyHeldDownFor = 0; // milliseconds

        // keyboard mods active
        static bool shift = false;
        static bool ctrl = false;
        static bool alt = false;       

        
        /// <summary>
        /// Checks for user input and executes corresponding command if input is present
        /// </summary>
        /// <param name="gameTime"></param>
        public static void CheckInput(GameTime gameTime)
        {
            KeyboardState curKeyState = Keyboard.GetState();
        
            if(curKeyState == prevKeyState)
            {
                if(curKeyState == noKeyPressed)
                {
                    return;
                }

                keyHeldDownFor += gameTime.ElapsedGameTime.Milliseconds;

                if(keyHeldDownFor < keyHoldDelay)
                {
                    return;
                }              
            }
            else
            {
                prevKeyState = curKeyState;
                keyHeldDownFor = 0;

                shift = curKeyState.IsKeyDown(Keys.LeftShift) || curKeyState.IsKeyDown(Keys.RightShift);
                ctrl = curKeyState.IsKeyDown(Keys.LeftControl) || curKeyState.IsKeyDown(Keys.RightControl);
                alt = curKeyState.IsKeyDown(Keys.LeftAlt);

                //Console.WriteLine("Mods (shift|ctrl|alt): " + shift + "|" + ctrl + "|" + alt);
            }

            if (curKeyState == noKeyPressed)
            {
                return;
            }

            CommandDomain curDomain = domainHistory.Peek();

            if(!keyToCommand.ContainsKey(curDomain))
            {
                Log.Error("Command domain not present: " + curDomain);
                return;
            }

            var keyBindings = keyToCommand[curDomain];

            if(keyBindings == null)
            {
                Log.Error("Key bindings missing for " + curDomain);
                return;
            }

            Command command = Command.None;

            foreach (var keyPressed in curKeyState.GetPressedKeys())
            {
                // TODO: implement modifiers 
                if(keyBindings.ContainsKey(keyPressed))
                {
                    command = keyBindings[keyPressed];
                    break;
                }
            }

            if(command == Command.None)
            {
                // TODO: remove
                UISystem.Message("What?");
                return;
            }

            ExecuteCommand(command);
        }

        static void ExecuteCommand(Command command)
        {
            switch(command)
            {
                default:
                    Log.Error("Command not implemented! " + command);
                    break;
            }
        }

        public static void BindKey(CommandDomain domain, Keys key, Command command)
        {
            if(!keyToCommand.ContainsKey(domain))
            {
                keyToCommand.Add(domain, new Dictionary<Keys, Command>());
            }

            var keyBinding = keyToCommand[domain];

            if(keyBinding.ContainsKey(key))
            {
                Log.Message("Rebinding: " + key + " from " + keyBinding[key] + " to " + command);
                keyBinding.Remove(key);
            }

            keyBinding.Add(key, command);
        }

        public static void EnterDomain(CommandDomain domain)
        {
            if(domainHistory.Count > 0)
            {
                if(domainHistory.Peek() == domain)
                {
                    return; // never enter the same domain we are in!
                }
            }
            domainHistory.Push(domain);
        }

        public static void LeaveCurrentDomain()
        {
            if(domainHistory.Count > 1)
            {
                domainHistory.Pop();
            }
            else
            {
                Log.Warning("Already at lowest level of domain history!");
            }
        }

        public static string GetSerializedKeyBindings()
        {
            return Util.SerializeObject(keyToCommand, true);
        }

        public static void LoadKeyBindings(string Json)
        {
            keyToCommand = Util.DeserializeObject <Dictionary<CommandDomain, Dictionary<Keys, Command>>> (Json);
        }
    }
}
