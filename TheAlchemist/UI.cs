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

    /// <summary>
    /// represents a part of the UI
    /// Depends on UI.Graphics and fonts in Util already initialized
    /// </summary>
    struct UIWindow
    {
        public string Name;
        public readonly NineSlicedSprite Background;       
        public List<string> Content;

        public static readonly int Padding = 10;

        public UIWindow(string name)
        {           
            Name = name;
            Background = null;
            Content = new List<string>();
        }

        public UIWindow(string name, Rectangle bounds, string backgroundTexture = "UIBox") : this(name)
        {
            if (UI.Graphics == null)
            {
                throw new Exception("UI.Graphics not initialized before creating UIWindow!");
            }

            Background = new NineSlicedSprite(backgroundTexture, bounds, UI.Graphics);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Background.Draw(spriteBatch);

            var anchor = Background.Rect.Location;

            var drawPos = new Vector2(anchor.X + Padding, anchor.Y + Padding);

            // Draw Name
            spriteBatch.DrawString(Util.BigFont, Name, drawPos, Color.Black);
            drawPos.Y += Util.BigFont.LineSpacing + Padding;

            foreach (var line in Content)
            {             
                spriteBatch.DrawString(Util.DefaultFont, line, drawPos, Color.Black);
                drawPos.Y += Util.DefaultFont.LineSpacing;
            }
        }
    }

    // special entity for UI element access
    static class UI
    {
        public static bool InventoryOpen { get; set; } = false;
        public static int InventoryCursorPosition { get; set; } = 1;
        public static int InventoryColumnLength { get; set; } = 25;

        public static bool CraftingMode = false;

        public static int MessageLogLineCount { get; set; } = 11;
        public static string[] MessageLog { get; set; } = new string[MessageLogLineCount];

        public static UIWindow CraftingWindow;

        public static GraphicsDevice Graphics { get; private set; }

        public static void Init(GraphicsDevice graphics)
        {
            UI.Graphics = graphics;

            int margin = 25;
            var bounds = new Rectangle(margin, margin, Util.WorldViewPixelWidth - margin * 2, Util.WorldViewPixelHeight - margin * 2);
            CraftingWindow = new UIWindow("Crafting", bounds);            
        }

        public static void Render(SpriteBatch spriteBatch)
        {
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------
            spriteBatch.Draw(TextureManager.GetTexture("messageLogBox"), new Vector2(0, Util.WorldViewPixelHeight), Color.White);
            //spriteBatch.DrawString(Util.BigFont, "Message Log", new Vector2(10, Util.WorldHeight + 10), Color.Black);
           
            string messageLogString = "";
            for (int i = 0; i < MessageLogLineCount; i++)
            {
                messageLogString += MessageLog[i] + '\n';
            }
            spriteBatch.DrawString(Util.DefaultFont, messageLogString, new Vector2(10, Util.WorldViewPixelHeight + 10), Color.Black);

            // ------------------------------------------------------------------------------------------------------------------------------------------------------------
            spriteBatch.Draw(TextureManager.GetTexture("inventory"), new Vector2(Util.WorldViewPixelWidth, 220), InventoryOpen ? Color.Aquamarine : Color.White);
            spriteBatch.DrawString(Util.BigFont, "Inventory", new Vector2(Util.WorldViewPixelWidth + 10, 220 + 10), Color.Black);

            var items = EntityManager.GetComponent<InventoryComponent>(Util.PlayerID).Items;

            int counter = 1;

            var itemsFirstHalf = items.Take(InventoryColumnLength);
            var itemsSecondHalf = items.Skip(InventoryColumnLength);

            SyncInventoryCursor();

            string itemStringLeftCol = "";
            foreach (var item in itemsFirstHalf)
            {
                if (counter == InventoryCursorPosition)
                    itemStringLeftCol += "# ";
                itemStringLeftCol += counter++ + ": " + DescriptionSystem.GetName(item) + " x" + EntityManager.GetComponent<ItemComponent>(item).Count + '\n';
            }

            spriteBatch.DrawString(Util.DefaultFont, itemStringLeftCol, new Vector2(Util.WorldViewPixelWidth + 20, 220 + 40), Color.Black);

            string itemStringRightCol = "";
            foreach (var item in itemsSecondHalf)
            {
                if (counter == InventoryCursorPosition)
                    itemStringRightCol += "# ";
                itemStringRightCol += counter++ + ": " + DescriptionSystem.GetName(item) + " x" + EntityManager.GetComponent<ItemComponent>(item).Count + '\n';
            }

            spriteBatch.DrawString(Util.DefaultFont, itemStringRightCol, new Vector2(Util.WorldViewPixelWidth + 20 + 240, 220 + 40), Color.Black);

            // ------------------------------------------------------------------------------------------------------------------------------------------------------------
            spriteBatch.Draw(TextureManager.GetTexture("tooltip"), new Vector2(Util.WorldViewPixelWidth, 0), Color.White);


            InputManager.CommandDomain curDomain = InputManager.Instance.GetCurrentDomain();

            string name = "";
            string description = "";

            if (curDomain == InputManager.CommandDomain.Exploring)
            {
                name = "Player (Turns: " + Util.TurnCount + ")";
                description = DescriptionSystem.GetCharacterTooltip(Util.PlayerID);
                //description = "HP: " + EntityManager.GetComponent<HealthComponent>(Util.PlayerID).GetString();
            }
            else if (curDomain == InputManager.CommandDomain.Inventory || curDomain == InputManager.CommandDomain.Crafting)
            {
                if (items.Count == 0)
                {
                    name = "Your inventory is empty!";
                    description = "";
                }
                else
                {
                    int item = items[InventoryCursorPosition - 1];
                    name = DescriptionSystem.GetName(item);
                    description = DescriptionSystem.GetItemTooltip(item);
                }
            }
            else if (curDomain == InputManager.CommandDomain.Targeting)
            {
                var pos = EntityManager.GetComponent<TransformComponent>(Util.TargetIndicatorID).Position;
                int character = Util.CurrentFloor.GetCharacter(pos);
                int item = Util.CurrentFloor.GetFirstItem(pos);
                int structure = Util.CurrentFloor.GetStructure(pos);
                int terrain = Util.CurrentFloor.GetTerrain(pos);

                int descrEntity = 0;

                if (character != 0)
                {
                    descrEntity = character;
                }
                else if (item != 0)
                {
                    descrEntity = item;
                }
                else if(structure != 0)
                {
                    descrEntity = structure;
                }
                else if (terrain != 0)
                {
                    descrEntity = terrain;
                }

                if (descrEntity == 0)
                {
                    name = "Floor";
                    description = "There's nothing there!";
                }
                else
                {
                    var descriptionC = EntityManager.GetComponent<DescriptionComponent>(descrEntity);
                    if (descriptionC == null)
                    {
                        name = "???";
                        description = "???";
                    }
                    else
                    {
                        name = descriptionC.Name;
                        description = descriptionC.Description;
                    }
                }
            }

            // draw name
            spriteBatch.DrawString(Util.BigFont, name, new Vector2(Util.WorldViewPixelWidth + 10, 10), Color.Black);

            // split description into multiple lines
            int rowLength = 40;
            int cur = 0;
            int limit = 55;
            while (cur + rowLength < description.Length)
            {
                if (limit-- == 0)
                {
                    return;
                }
                //Console.WriteLine(cur + "|" + description.Length);
                int newlPos = description.IndexOf('\n', cur, rowLength);
                if (newlPos >= 0)
                {
                    cur = newlPos + 1;
                    continue;
                }

                int insertHere = description.LastIndexOf(' ', cur + rowLength, rowLength);

                if (insertHere == -1)
                {
                    insertHere = cur + rowLength; // no space found? just cut off the word, lol
                }
                else
                {
                    insertHere++; // insert after space
                }

                description = description.Insert(insertHere, "\n");
                cur = insertHere + 1;
            }

            // draw description text
            spriteBatch.DrawString(Util.MonospaceFont, description, new Vector2(Util.WorldViewPixelWidth + 10, 40), Color.Black);
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------

            // draw crafting window
            if (CraftingMode)
            {
                CraftingWindow.Content.Clear();

                CraftingWindow.Content.Add("(press 'R' to reset and 'Enter' to craft)");
                CraftingWindow.Content.Add("");
                CraftingWindow.Content.Add("Current Recipe:");

                foreach (var ingredientName in CraftingSystem.Instance.GetIngredientNames())
                {
                    CraftingWindow.Content.Add("- " + ingredientName);
                }

                CraftingWindow.Draw(spriteBatch);
            }
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------
        }

        public static void SyncInventoryCursor()
        {
            int itemCount = Util.GetPlayerInventory().Items.Count();
            if (InventoryCursorPosition < 1)
                InventoryCursorPosition = 1;
            else if (InventoryCursorPosition > itemCount)
                InventoryCursorPosition = itemCount;
        }
    }
}
