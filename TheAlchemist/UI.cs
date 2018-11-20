using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheAlchemist
{
    using Components;
    using Systems;

    // special entity for UI element access
    static class UI
    {
        public static bool InventoryOpen { get; set; } = false;
        public static int InventoryCursorPosition { get; set; } = 1;
        public static int InventoryColumnLength { get; set; } = 25;

        // TODO: save older messages
        public static int MessageLogLineCount { get; set; } = 10;
        public static string[] MessageLog { get; set; } = new string[MessageLogLineCount];

        public static void Render(SpriteBatch spriteBatch)
        {
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------
            spriteBatch.Draw(TextureManager.GetTexture("messageLogBox"), new Vector2(0, Util.WorldHeight), Color.White);
            //spriteBatch.DrawString(Util.BigFont, "Message Log", new Vector2(10, Util.WorldHeight + 10), Color.Black);

            string messageLogString = "";
            for (int i = 0; i < MessageLogLineCount; i++)
            {
                messageLogString += MessageLog[i] + '\n';
            }
            spriteBatch.DrawString(Util.DefaultFont, messageLogString, new Vector2(10, Util.WorldHeight + 10), Color.Black);

            // ------------------------------------------------------------------------------------------------------------------------------------------------------------
            spriteBatch.Draw(TextureManager.GetTexture("tooltip"), new Vector2(Util.WorldWidth, 0), Color.White);
            spriteBatch.DrawString(Util.BigFont, "Tooltip", new Vector2(Util.WorldWidth + 10, 10), Color.Black);
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------
            spriteBatch.Draw(TextureManager.GetTexture("inventory"), new Vector2(Util.WorldWidth, 220), InventoryOpen ? Color.Aquamarine : Color.White);
            spriteBatch.DrawString(Util.BigFont, "Inventory", new Vector2(Util.WorldWidth + 10, 220 + 10), Color.Black);

            var items = EntityManager.GetComponentOfEntity<InventoryComponent>(Util.PlayerID).Items;

            int counter = 1;

            var itemsFirstHalf = items.Take(InventoryColumnLength);
            var itemsSecondHalf = items.Skip(InventoryColumnLength);

            string itemStringLeftCol = "";
            foreach(var item in itemsFirstHalf)
            {
                if (counter == InventoryCursorPosition)
                    itemStringLeftCol += "# ";
                itemStringLeftCol += counter++ + ": " + DescriptionSystem.GetName(item) + " x" + EntityManager.GetComponentOfEntity<ItemComponent>(item).Count + '\n';
            }

            spriteBatch.DrawString(Util.DefaultFont, itemStringLeftCol, new Vector2(Util.WorldWidth + 20, 220 + 40), Color.Black);

            string itemStringRightCol = "";
            foreach (var item in itemsSecondHalf)
            {
                if (counter == InventoryCursorPosition)
                    itemStringRightCol += "# ";
                itemStringRightCol += counter++ + ": " + DescriptionSystem.GetName(item) + " x" + EntityManager.GetComponentOfEntity<ItemComponent>(item).Count + '\n';
            }

            spriteBatch.DrawString(Util.DefaultFont, itemStringRightCol, new Vector2(Util.WorldWidth + 20 + 240, 220 + 40), Color.Black);
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------
        }


    }
}
