using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace TheAlchemist
{
    using Components;
    using Systems;

    // special entity for UI element access
    static class UI
    {
        public static int UIEntityID { get; set; } = 0;

        public static RenderableSpriteComponent InventoryBackground { get; set; } = null;
        public static RenderableTextComponent InventoryText { get; set; } = null;

        public static RenderableSpriteComponent MessageLogBackground { get; set; } = null;
        public static RenderableTextComponent MessageLogText { get; set; } = null;

        public static RenderableTextComponent PlayerHealthText { get; set; } = null;

        public static void Init()
        {
            int lowerFloorBorder = Util.TileSize * Util.CurrentFloor.Height;
            int rightFloorBorder = Util.TileSize * Util.CurrentFloor.Width;

            InventoryBackground = new RenderableSpriteComponent() { Position = new Vector2(rightFloorBorder + 10, 0), Texture = "inventory" };
            InventoryText = new RenderableTextComponent()
            {
                Position = new Vector2(rightFloorBorder + 20, 30),
                Font = Util.SmallFont,
                GetTextFrom = () =>
                {
                    IEnumerable<int> items = EntityManager.GetComponentOfEntity<InventoryComponent>(Util.PlayerID).Items;
                    string itemString = "";
                    int counter = 1;
                    foreach (var item in items)
                    {
                        itemString += counter++ + ": " + DescriptionSystem.GetName(item) + " x" + EntityManager.GetComponentOfEntity<ItemComponent>(item).Count + '\n';
                    }
                    return itemString;
                }
            };

            MessageLogBackground = new RenderableSpriteComponent() { Position = new Vector2(0, lowerFloorBorder + 10), Texture = "box" };
            MessageLogText = new RenderableTextComponent() { Position = new Vector2(10, lowerFloorBorder + 85), Text = @"Welcome to <The Alchemist>!" };

            PlayerHealthText = new RenderableTextComponent()
            {
                Position = new Vector2(rightFloorBorder + 10, 0),
                Text = ""  //GetTextFrom = () => "Player HP: " + EntityManager.GetComponentOfEntity<HealthComponent>(Util.PlayerID).GetString()
            };

            UIEntityID = EntityManager.CreateEntity(new List<IComponent>()
            {
                new DescriptionComponent() { Name = "UI", Description = "Displays stuff you probably want to know!"},
                InventoryBackground, InventoryText,
                MessageLogBackground, MessageLogText,
                PlayerHealthText
            });
            
        }
    }
}
