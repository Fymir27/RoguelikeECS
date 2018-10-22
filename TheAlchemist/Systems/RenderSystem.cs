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
            var renderedSprites = EntityManager
                .GetAllComponents<RenderableSpriteComponent>()
                .Where(component => component.Visible);

            foreach(var sprite in renderedSprites)
            {
                spriteBatch.Draw(TextureManager.GetTexture(sprite.Texture), sprite.Position, Color.White);
            }

            var renderedTexts = EntityManager
                .GetAllComponents<RenderableTextComponent>()
                .Where(text => text.Visible);

            foreach (var text in renderedTexts)
            {
                spriteBatch.DrawString(text.Font, text.Text, text.Position, Color.Black);
            }
        }
    }
}
