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
        public event InteractionHandler InteractionEvent;

        public event ItemPickupHandler PickupItemEvent;
        public event InventoryToggledHandler InventoryToggledEvent;
        public event InventoryCursorMovedHandler InventoryCursorMovedEvent;
        public event ItemUsedHandler ItemUsedEvent;
        public event ItemConsumedHandler ItemConsumedEvent;

        public event AddCraftingIngredientHandler AddItemAsIngredientEvent;
        public event ResetCraftingHandler ResetCraftingEvent;
        public event CraftItemHandler CraftItemEvent;

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
            Wait,

            Interact,

            // UI
            ToggleInv,
            MoveInvCursorUp,
            MoveInvCursorRight,
            MoveInvCursorDown,
            MoveInvCursorLeft,

            ToggleTargeting,
            ConfirmTarget,

            // Items
            UseItem, // general usage -> just prints specific item usage, unless there is only one usage Type
            ConsumeItem,
            DropItem,
            ThrowItem,

            // Crafting
            ToggleCrafting,
            AddIngredient,
            ResetCrafting,
            CraftItem
        }

        // idea from Kyzrati @GridSageGames (Cogmind)
        // https://old.reddit.com/r/roguelikedev/comments/3g2mcw/faq_friday_18_input_handling/ctu973v/
        [JsonConverter(typeof(StringEnumConverter))]
        public enum CommandDomain
        {
            Exploring, // default/running around
            Examining,
            Targeting,
            Inventory,
            Crafting
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

        // function that gets called when user selects target
        Action<Position> targetConfirmationCallback = null;

        // remembers the current command that's supposed to be executed next
        Command curCommand;

        /// <summary>
        /// Checks for user input and executes corresponding command if input is present
        /// </summary>
        /// <param name="gameTime"></param>
        /// <returns>wether input/command is available</returns>
        public bool CheckInput(GameTime gameTime)
        {
            KeyboardState curKeyState = Keyboard.GetState();

            if (curKeyState == prevKeyState)
            {
                if (curKeyState == noKeyPressed)
                {
                    return false;
                }

                keyHeldDownFor += gameTime.ElapsedGameTime.Milliseconds;

                if (keyHeldDownFor < keyHoldDelay)
                {
                    return false;
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
                return false;
            }

            CommandDomain curDomain = domainHistory.Peek();

            if (!keyToCommand.ContainsKey(curDomain))
            {
                Log.Error("Command domain not present: " + curDomain);
                return false;
            }

            var keyBindings = keyToCommand[curDomain];

            if (keyBindings == null)
            {
                Log.Error("Key bindings missing for " + curDomain);
                return false;
            }

            Command command = Command.None;

            foreach (var keyPressed in curKeyState.GetPressedKeys())
            {
                if (keyPressed == Keys.F1)
                {
                    EntityManager.Dump();
                    continue;
                }

                // TODO: implement modifiers 
                if (keyBindings.ContainsKey(keyPressed))
                {
                    command = keyBindings[keyPressed];
                    break;
                }
            }

            if (command == Command.None)
            {
                UISystem.Message("Unkown Command!");
                return false;
            }

            //ExecuteCommand(command);
            curCommand = command;
            return true;
        }

        /// <summary>
        /// returns wether any key is pressed down
        /// </summary>
        /// <returns>true if at least one key is pressed</returns>
        public bool AnyKey()
        {
            return Keyboard.GetState().GetPressedKeys().Length > 0;
        }

        /// <summary>
        /// checks wether a specific key is down
        /// </summary>
        /// <param name="key">key to check</param>
        /// <returns>true if key is down</returns>
        public bool CheckKey(Keys key)
        {
            return Keyboard.GetState().IsKeyDown(key);
        }

        public void ExecuteCurrentCommand()
        {
            ExecuteCommand(curCommand);
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
                case Command.Wait:
                    if (!Util.PlayerTurnOver)
                    {
                        Util.TurnOver(Util.PlayerID);
                    }
                    break;
                // -------------------------------

                case Command.Interact:
                    Interact();
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
                    ConfirmTarget();
                    break;
                case Command.UseItem:
                    UseItem(ItemUsage.None);
                    break;
                case Command.ConsumeItem:
                    UseItem(ItemUsage.Consume);
                    break;
                case Command.DropItem:
                    break;
                case Command.ThrowItem:
                    UseItem(ItemUsage.Throw);
                    break;

                case Command.ToggleCrafting:
                    ToggleCrafting();
                    break;
                case Command.AddIngredient:
                    AddItemAsCraftingIngredient();
                    break;
                case Command.ResetCrafting:
                    ResetCraftingEvent?.Invoke();
                    break;
                case Command.CraftItem:
                    CraftItemEvent?.Invoke();
                    ToggleCrafting();
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

        public CommandDomain GetCurrentDomain()
        {
            return domainHistory.Peek();
        }

        public void EnterDomain(CommandDomain domain)
        {
            //Log.Message("Entered domain " + domain.ToString());
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
            //Log.Message("Left domain " + GetCurrentDomain().ToString());
            if (domainHistory.Count > 1)
            {
                domainHistory.Pop();
            }
            else
            {
                Log.Warning("Already at lowest level of domain history!");
            }
        }

        /// <summary>
        /// Returns the key for a specific command (first one found)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        public Keys GetKeybinding(Command command, CommandDomain domain)
        {
            Dictionary<Keys, Command> keybindings;
            keyToCommand.TryGetValue(domain, out keybindings);
            if (keybindings == null)
            {
                Log.Warning("Domain empty! " + domain);
                return Keys.None;
            }

            bool commandDefined = keybindings.ContainsValue(command);

            if (!commandDefined)
            {
                Log.Warning("Command not defined! " + command);
                return Keys.None;
            }

            return keybindings.FirstOrDefault(x => x.Value == command).Key;
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
                UISystem.Message("Failed to load keybindings!");
                keyToCommand = new Dictionary<CommandDomain, Dictionary<Keys, Command>>() { { CommandDomain.Exploring, new Dictionary<Keys, Command>() } };
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

        private void Interact()
        {
            var tile = Util.CurrentFloor.GetTile(Util.GetPlayerPos());

            if (tile.Items != null && tile.Items.Count > 0)
            {
                PickupItemEvent?.Invoke(ControlledEntity);
                return;
            }

            if (tile.Structure != 0)
            {
                var interactableStructure = EntityManager.GetComponent<InteractableComponent>(tile.Structure);

                if (interactableStructure != null)
                {
                    InteractionEvent?.Invoke(Util.PlayerID, tile.Structure);
                    return;
                }
            }
        }

        /// <summary>
        /// checks if item is usable
        /// prints problems to player if any
        /// </summary>
        /// <returns> wether item is usable </returns>
        private bool ItemUsable(int item, ItemUsage usage)
        {
            if (item == 0)
            {
                UISystem.Message("Your inventory is empty!");
                return false;
            }

            var usableItem = EntityManager.GetComponent<UsableItemComponent>(item);

            if (usableItem == null)
            {
                UISystem.Message("You can't use that!");
                return false;
            }

            if(!usableItem.Usages.Contains(usage))
            {
                UISystem.Message("Item usage:");
                foreach (var action in usableItem.Usages)
                {
                    Keys key = Keys.None;
                    switch (action)
                    {
                        case ItemUsage.Consume:
                            key = GetKeybinding(Command.ConsumeItem, CommandDomain.Inventory);
                            break;

                        case ItemUsage.Throw:
                            key = GetKeybinding(Command.ThrowItem, CommandDomain.Inventory);
                            break;
                    }
                    UISystem.Message(key + " -> " + action);
                }
                return false;
            }
            return true;
        }
        

        private void UseItem(ItemUsage usage)
        {
            var item = Util.GetCurrentItem();

            if(!ItemUsable(item, usage))
            {
                return;
            }

            var usableItem = EntityManager.GetComponent<UsableItemComponent>(item);

            ItemUsedEvent?.Invoke(ControlledEntity, item, usage);
        }

        private void ConsumeItem()
        {
            var item = Util.GetCurrentItem();

            if (item == 0)
            {
                return;
            }

            var consumable = EntityManager.GetComponent<ConsumableComponent>(item);

            if (consumable == null)
            {
                UISystem.Message("I can't consume that!");
                return;
            }

            ItemConsumedEvent?.Invoke(ControlledEntity, item);
        }

        private void ThrowItem()
        {

        }

        /// <summary>
        /// turns target mode on/off
        /// </summary>
        public void ToggleTargetMode()
        {
            if (Util.TargetIndicatorID == 0) // lazy init
            {
                // create target indicator
                Util.TargetIndicatorID = EntityManager.CreateEntity(new List<IComponent>()
                {
                    new TransformComponent() { Position = new Position(1, 1) },
                }, EntityType.None);
            }

            if (domainHistory.Peek() == CommandDomain.Targeting)
            {
                ControlledEntity = Util.PlayerID;
                LeaveCurrentDomain();
            }
            else
            {
                ControlledEntity = Util.TargetIndicatorID;
                var playerPos = EntityManager.GetComponent<TransformComponent>(Util.PlayerID).Position;
                EntityManager.GetComponent<TransformComponent>(Util.TargetIndicatorID).Position = playerPos;
                EnterDomain(CommandDomain.Targeting);
            }
        }

        /// <summary>
        /// initiates target mode while registering a callback
        /// to invoke when target has been selected
        /// </summary>
        /// <param name="callback">Recieves target position when confirmed</param>
        public void InitiateTargeting(Action<Position> callback)
        {
            targetConfirmationCallback = callback;
            ToggleTargetMode();
        }

        /// <summary>
        /// passes target position to callback,
        /// resets callback to null
        /// and turns off target mode
        /// </summary>
        public void ConfirmTarget()
        {
            var targetPos = EntityManager.GetComponent<TransformComponent>(Util.TargetIndicatorID).Position;
            targetConfirmationCallback?.Invoke(targetPos);
            targetConfirmationCallback = null;
            ToggleTargetMode();
        }

        private void ToggleCrafting()
        {
            if (UI.CraftingMode)
            {
                ResetCraftingEvent?.Invoke();
                UI.CraftingMode = false;
                LeaveCurrentDomain();
                // leave inventory open
            }
            else
            {
                if (GetCurrentDomain() != CommandDomain.Inventory)
                {
                    ToggleInventory();
                }
                UI.CraftingMode = true;
                EnterDomain(CommandDomain.Crafting);
            }
        }

        public void AddItemAsCraftingIngredient()
        {
            int item = Util.GetCurrentItem();
            if (item == 0)
            {
                UISystem.Message("Your inventory is empty!");
                return;
            }
            AddItemAsIngredientEvent?.Invoke(item);
        }
    }
}
