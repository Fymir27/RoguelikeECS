using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;


    class RenderSystem
    {
        public void Run(SpriteBatch spriteBatch)
        {
            var renderedComponents = EntityManager
                .GetAllComponents<RenderableComponent>()
                .Where(component => component.Visible)
                .ToArray();

            foreach (var item in renderedComponents)
            {
                spriteBatch.Draw(TextureManager.GetTexture(item.Texture), item.Position, Color.White);
            }
        }
    }
}
