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
    using Components;

    /// <summary>
    /// used as Singleton
    /// </summary>
    class InputManager
    {
        public static readonly InputManager Instance = new InputManager();

        public int ControlledEntity = 0;

        public event MovementEventHandler MovementEvent;
        public event ItemPickupHandler PickupItemEvent;

        public event UpdateTargetLineHandler UpdateTargetLineEvent;

        public event InventoryToggledHandler InventoryToggledEvent;
        public event InventoryCursorMovedHandler InventoryCursorMovedEvent;
        public event ItemUsedHandler ItemUsedEvent;



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

            PickupItem,

            // UI
            ToggleInv,
            MoveInvCursorUp,
            MoveInvCursorRight,
            MoveInvCursorDown,
            MoveInvCursorLeft,

            ToggleTargeting,
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

        Stack<CommandDomain> domainHistory = new Stack<CommandDomain>();

        // commandToExecute = KeyToCommand[ActiveDomain][PressedKey]
        Dictionary<CommandDomain, Dictionary<Keys, Command>> keyToCommand = new Dictionary<CommandDomain, Dictionary<Keys, Command>>();
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
        KeyboardState prevKeyState = noKeyPressed;

        readonly int keyHoldDelay = 500; // delay until key registers as beeing held down
        int keyHeldDownFor = 0; // milliseconds

        // keyboard mods active
        bool shift = false;
        bool ctrl = false;
        bool alt = false;


        /// <summary>
        /// Checks for user input and executes corresponding command if input is present
        /// </summary>
        /// <param name="gameTime"></param>
        public void CheckInput(GameTime gameTime)
        {
            KeyboardState curKeyState = Keyboard.GetState();

            if (curKeyState == prevKeyState)
            {
                if (curKeyState == noKeyPressed)
                {
                    return;
                }

                keyHeldDownFor += gameTime.ElapsedGameTime.Milliseconds;

                if (keyHeldDownFor < keyHoldDelay)
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

            if (!keyToCommand.ContainsKey(curDomain))
            {
                Log.Error("Command domain not present: " + curDomain);
                return;
            }

            var keyBindings = keyToCommand[curDomain];

            if (keyBindings == null)
            {
                Log.Error("Key bindings missing for " + curDomain);
                return;
            }

            Command command = Command.None;

            foreach (var keyPressed in curKeyState.GetPressedKeys())
            {
                // TODO: implement modifiers 
                if (keyBindings.ContainsKey(keyPressed))
                {
                    command = keyBindings[keyPressed];
                    break;
                }
            }

            if (command == Command.None)
            {
                // TODO: remove
                UISystem.Message("What?");
                return;
            }

            ExecuteCommand(command);
        }

        public void ExecuteCommand(Command command)
        {
            switch (command)
            {
                case Command.None:
                    Log.Error("Command \"None\" cannot be executed!");
                    break;

                // Movement -----------------------
                case Command.MoveN:
                    MoveEntity(Direction.North);
                    break;
                case Command.MoveNE:
                    break;
                case Command.MoveE:
                    MoveEntity(Direction.East);
                    break;
                case Command.MoveSE:
                    break;
                case Command.MoveS:
                    MoveEntity(Direction.South);
                    break;
                case Command.MoveSW:
                    break;
                case Command.MoveW:
                    MoveEntity(Direction.West);
                    break;
                case Command.MoveNW:
                    break;
                // -------------------------------

                case Command.PickupItem:
                    PickUpItem();
                    break;

                case Command.ToggleInv:
                    ToggleInventory();
                    break;
                case Command.MoveInvCursorUp:
                    MoveInventoryCursor(Direction.North);
                    break;
                case Command.MoveInvCursorRight:
                    MoveInventoryCursor(Direction.East);
                    break;
                case Command.MoveInvCursorDown:
                    MoveInventoryCursor(Direction.South);
                    break;
                case Command.MoveInvCursorLeft:
                    MoveInventoryCursor(Direction.West);
                    break;

                case Command.ToggleTargeting:
                    ToggleTargetMode();
                    break;
                case Command.ConfirmTarget:
                    break;
                case Command.UseItem:
                    UseItem();
                    break;
                case Command.ConsumeItem:
                    break;
                case Command.DropItem:
                    break;
                case Command.ThrowItem:
                    break;

                default:
                    Log.Error("Command not implemented! " + command);
                    break;
            }
        }

        public void BindKey(CommandDomain domain, Keys key, Command command)
        {
            if (!keyToCommand.ContainsKey(domain))
            {
                keyToCommand.Add(domain, new Dictionary<Keys, Command>());
            }

            var keyBinding = keyToCommand[domain];

            if (keyBinding.ContainsKey(key))
            {
                Log.Message("Rebinding: " + key + " from " + keyBinding[key] + " to " + command);
                keyBinding.Remove(key);
            }

            keyBinding.Add(key, command);
        }

        public void EnterDomain(CommandDomain domain)
        {
            if (domainHistory.Count > 0)
            {
                if (domainHistory.Peek() == domain)
                {
                    Log.Warning("Trying to enter same command domain again!");
                    return; // never enter the same domain we are in!
                }
            }
            domainHistory.Push(domain);
        }

        public void LeaveCurrentDomain()
        {
            if (domainHistory.Count > 1)
            {
                domainHistory.Pop();
            }
            else
            {
                Log.Warning("Already at lowest level of domain history!");
            }
        }

        public string GetSerializedKeyBindings()
        {
            return Util.SerializeObject(keyToCommand, true);
        }

        public void LoadKeyBindings(string Json)
        {
            keyToCommand = Util.DeserializeObject<Dictionary<CommandDomain, Dictionary<Keys, Command>>>(Json);

            if (keyToCommand == null)
            {
                Log.Error("Failed to load keybindings!");
                keyToCommand = new Dictionary<CommandDomain, Dictionary<Keys, Command>>();
            }
        }

        /// <summary>
        /// Sends movement event with ControlledEntity
        /// </summary>
        /// <param name="dir">Direction of movement</param>
        private void MoveEntity(Direction dir)
        {
            MovementEvent?.Invoke(ControlledEntity, dir);
        }

        /// <summary>
        /// opens/closes Inventory
        /// </summary>
        private void ToggleInventory()
        {
            InventoryToggledEvent?.Invoke();

            if (UI.InventoryOpen)
            {
                EnterDomain(CommandDomain.Inventory);
            }
            else
            {
                LeaveCurrentDomain();
            }
        }

        private void MoveInventoryCursor(Direction dir)
        {
            InventoryCursorMovedEvent?.Invoke(dir);
        }

        private void PickUpItem()
        {
            PickupItemEvent?.Invoke(ControlledEntity);
        }

        private void UseItem()
        {
            int cursorPos = UI.InventoryCursorPosition;
            var item = EntityManager.GetComponent<InventoryComponent>(ControlledEntity).Items[cursorPos - 1];
            ItemUsedEvent?.Invoke(ControlledEntity, item);
            // TODO: ToggleInventory();
            LeaveCurrentDomain();
        }

        public void ToggleTargetMode()
        {
            if (domainHistory.Peek() == CommandDomain.Targeting)
            {               
                ControlledEntity = Util.PlayerID;
                EntityManager.RemoveAllComponentsOfType(Util.TargetIndicatorID, RenderableSpriteComponent.TypeID);
                LeaveCurrentDomain();
            }
            else
            {
                ControlledEntity = Util.TargetIndicatorID;
                var playerPos = EntityManager.GetComponent<TransformComponent>(Util.PlayerID).Position;
                EntityManager.GetComponent<TransformComponent>(Util.TargetIndicatorID).Position = playerPos;
                UpdateTargetLineEvent?.Invoke();
                EnterDomain(CommandDomain.Targeting);
            }
        }
    }
}
