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
        public bool DisplayName;       
        public int Padding;

        public readonly List<string> Content;
        public readonly NineSlicedSprite Background;
        public SpriteFont NameFont;
        public SpriteFont ContentFont;

        public UIWindow(string name, bool displayName, int padding = 10)
        {           
            Name = name;
            DisplayName = displayName;
            Padding = padding;
         
            Content = new List<string>();
            Background = null;
            NameFont = Util.BigFont;
            ContentFont = Util.DefaultFont;
        }

        public UIWindow(string name, Rectangle bounds, bool displayName = true, string backgroundTexture = "UIBox") : this(name, displayName)
        {
            if (UI.Graphics == null)
            {
                throw new Exception("UI.Graphics not initialized before creating UIWindow!");
            }

            Background = new NineSlicedSprite(backgroundTexture, bounds, UI.Graphics);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Draw(spriteBatch, Color.White);
        }

        public void Draw(SpriteBatch spriteBatch, Color tint)
        {
            Background.Draw(spriteBatch, tint);

            var anchor = Background.Rect.Location;

            var drawPos = new Vector2(anchor.X + Padding, anchor.Y + Padding);

            // Draw Name
            spriteBatch.DrawString(NameFont, Name, drawPos, Color.Black);
            drawPos.Y += NameFont.LineSpacing + Padding;

            foreach (var line in Content)
            {             
                spriteBatch.DrawString(ContentFont, line, drawPos, Color.Black);
                drawPos.Y += ContentFont.LineSpacing;
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

        public static int MessageLogLineCount { get; set; } = 9;
        public static string[] MessageLog { get; set; } = new string[MessageLogLineCount];

        public static UIWindow PopUpWindow;
        public static UIWindow TooltipWindow;
        public static UIWindow InventoryWindow;
        public static UIWindow MessageLogWindow;

        public static GraphicsDevice Graphics { get; private set; }

        public static void Init(GraphicsDevice graphics)
        {
            UI.Graphics = graphics;

            int margin = 25;
            PopUpWindow = new UIWindow("Crafting", new Rectangle(margin, margin, Util.WorldViewPixelWidth - margin * 2, Util.WorldViewPixelHeight - margin * 2));

            TooltipWindow = new UIWindow("Tooltip", new Rectangle(Util.WorldViewPixelWidth, 0, Util.ScreenWidth - Util.WorldViewPixelWidth, 220));
            TooltipWindow.ContentFont = Util.MonospaceFont;

            InventoryWindow = new UIWindow("Inventory", new Rectangle(Util.WorldViewPixelWidth, 220, Util.ScreenWidth - Util.WorldViewPixelWidth, Util.ScreenHeight - 220));

            MessageLogWindow = new UIWindow("Log", new Rectangle(0, Util.WorldViewPixelHeight, Util.WorldViewPixelWidth, Util.ScreenHeight - Util.WorldViewPixelHeight));
        }

        public static void Render(SpriteBatch spriteBatch)
        {
            StringBuilder sb = new StringBuilder();
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------
            //spriteBatch.Draw(TextureManager.GetTexture("messageLogBox"), new Vector2(0, Util.WorldViewPixelHeight), Color.White);
            //spriteBatch.DrawString(Util.BigFont, "Message Log", new Vector2(10, Util.WorldHeight + 10), Color.Black);

            string messageLogString = "";
            for (int i = 0; i < MessageLogLineCount; i++)
            {
                messageLogString += MessageLog[i] + '\n';
            }
            //spriteBatch.DrawString(Util.DefaultFont, messageLogString, new Vector2(10, Util.WorldViewPixelHeight + 10), Color.Black);

            MessageLogWindow.Content.Clear();
            MessageLogWindow.Content.Add(messageLogString);
            MessageLogWindow.Draw(spriteBatch);

            // ------------------------------------------------------------------------------------------------------------------------------------------------------------
            //spriteBatch.Draw(TextureManager.GetTexture("inventory"), new Vector2(Util.WorldViewPixelWidth, 220), InventoryOpen ? Color.Aquamarine : Color.White);
            //spriteBatch.DrawString(Util.BigFont, "Inventory", new Vector2(Util.WorldViewPixelWidth + 10, 220 + 10), Color.Black);
            InventoryWindow.Content.Clear();

            var weapon = CombatSystem.GetEquippedWeapon(Util.PlayerID);

            if (weapon != null)
            {
                InventoryWindow.Content.Add(weapon.ToString());
            }

            var armor = CombatSystem.GetEquippedArmor(Util.PlayerID);

            if(armor != null)
            {
                InventoryWindow.Content.Add(armor.ToString());
            }

            InventoryWindow.Content.Add("");

            var items = EntityManager.GetComponent<InventoryComponent>(Util.PlayerID).Items;
            SyncInventoryCursor();

            for (int i = 0; i < items.Count; i++)
            {
                int item = items[i];
                bool selected = i == InventoryCursorPosition - 1;
                var itemC = EntityManager.GetComponent<ItemComponent>(item);
                InventoryWindow.Content.Add(String.Format("{0} {1} x{2}", selected ? "#" : "  ", DescriptionSystem.GetName(item), itemC.Count));
            }                  

            InventoryWindow.Draw(spriteBatch, InventoryOpen ? Color.CornflowerBlue : Color.White);

            // ------------------------------------------------------------------------------------------------------------------------------------------------------------
            //spriteBatch.Draw(TextureManager.GetTexture("tooltip"), new Vector2(Util.WorldViewPixelWidth, 0), Color.White);


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
            //spriteBatch.DrawString(Util.BigFont, name, new Vector2(Util.WorldViewPixelWidth + 10, 10), Color.Black);

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
            //spriteBatch.DrawString(Util.MonospaceFont, description, new Vector2(Util.WorldViewPixelWidth + 10, 40), Color.Black);
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------

            TooltipWindow.Name = name;
            TooltipWindow.Content.Clear();

            TooltipWindow.Content.Add(description);

            TooltipWindow.Draw(spriteBatch);

            // draw crafting window
            if (CraftingMode)
            {
                PopUpWindow.Name = "Crafting";
                PopUpWindow.DisplayName = true;

                PopUpWindow.Content.Clear();
                PopUpWindow.Content.Add("(press 'R' to reset and 'Enter' to craft)");
                PopUpWindow.Content.Add("");
                PopUpWindow.Content.Add("Current Recipe:");

                foreach (var ingredientName in CraftingSystem.Instance.GetIngredientNames())
                {
                    PopUpWindow.Content.Add("- " + ingredientName);
                }

                PopUpWindow.Draw(spriteBatch);
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
