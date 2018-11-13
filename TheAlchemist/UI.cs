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

        public static void Render(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(TextureManager.GetTexture("messageLogBox"), new Vector2(0, Util.WorldHeight), Color.White);
            spriteBatch.DrawString(Util.BigFont, "Message Log", new Vector2(10, Util.WorldHeight + 10), Color.Black);

            spriteBatch.Draw(TextureManager.GetTexture("tooltip"), new Vector2(Util.WorldWidth, 0), Color.White);
            spriteBatch.DrawString(Util.BigFont, "Tooltip", new Vector2(Util.WorldWidth + 10, 10), Color.Black);

            spriteBatch.Draw(TextureManager.GetTexture("inventory"), new Vector2(Util.WorldWidth, 220), InventoryOpen ? Color.Aquamarine : Color.White);
            spriteBatch.DrawString(Util.BigFont, "Inventory", new Vector2(Util.WorldWidth + 10, 220 + 10), Color.Black);
        }
    }
}
