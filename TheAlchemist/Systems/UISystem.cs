using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;
    using Systems;

    public delegate void InventoryToggledHandler();
    public delegate void InventoryCursorMovedHandler(Direction dir);

    class UISystem
    {
        public void HandleInventoryToggled()
        {
            UI.InventoryOpen = !UI.InventoryOpen;
        }

        // MovementEventHandler
        public void HandleInventoryCursorMoved(Direction dir)
        {
            if (!UI.InventoryOpen)
            {
                return;
            }

            var inventory = EntityManager.GetComponent<InventoryComponent>(Util.PlayerID);

            if (inventory == null)
            {
                Log.Error("No player inventory found!");
                throw new NullReferenceException("No player inventory found!");
            }

            //Log.Message("Inventory cursor moved " + dir);

            int newCursorPosition = UI.InventoryCursorPosition;
            int colLength = UI.InventoryColumnLength;

            switch (dir)
            {
                case Direction.North:
                    if (newCursorPosition > 1)
                        newCursorPosition--;
                    break;

                case Direction.East:
                    newCursorPosition += colLength;
                    break;

                case Direction.South:
                    newCursorPosition++;
                    break;

                case Direction.West:
                    newCursorPosition -= colLength;
                    break;

                default:
                    break;
            }

            if (newCursorPosition >= 1 && newCursorPosition <= inventory.Items.Count)
            {
                UI.InventoryCursorPosition = newCursorPosition;
            }

        }

        // posts a message to the Message Log
        public static void Message(string message)
        {
            for (int i = 0; i < UI.MessageLogLineCount - 1; i++)
            {
                UI.MessageLog[i] = UI.MessageLog[i + 1];
            }

            UI.MessageLog[UI.MessageLogLineCount - 1] = message;
        }
    }
}
