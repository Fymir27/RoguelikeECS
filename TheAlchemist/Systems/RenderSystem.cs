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
            //renderedSprites = 
            EntityManager
                .GetAllComponents<RenderableSpriteComponent>()
                .Where(component => component.Visible)
                .ToList()
                .ForEach(sprite => spriteBatch.Draw(TextureManager.GetTexture(sprite.Texture), sprite.Position, Color.White));

            //renderedText 
            EntityManager
                .GetAllComponents<RenderableTextComponent>()
                .Where(text => text.Visible)
                .ToList()
                .ForEach(text => spriteBatch.DrawString(text.Font, text.Text, text.Position, Color.Black));
        }
    }
}
