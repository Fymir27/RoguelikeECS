﻿using Microsoft.Xna.Framework;
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
        GraphicsDevice graphics;
        SpriteBatch spriteBatch;

        public bool FogOfWarEnabled = true;

        public RenderSystem(GraphicsDevice graphics)
        {
            this.graphics = graphics;
            spriteBatch = new SpriteBatch(graphics);
        }

        public void RenderWorld(RenderTarget2D renderTarget)
        {
            graphics.SetRenderTarget(renderTarget);           
          
            spriteBatch.Begin(); //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 
            graphics.Clear(Color.White);

            // seen by player
            var seenPositions = Util.CurrentFloor.GetSeenPositions();

            var renderedSprites = EntityManager
                .GetAllComponentsOfType<RenderableSpriteComponent>()
                .Where(component => component.Visible);

            // order sprite by Layer to determine what to draw first
            var orderedSprites = renderedSprites.OrderBy(sprite => sprite.Layer);

            foreach (var sprite in orderedSprites)
            {
                var transform = EntityManager.GetComponent<TransformComponent>(sprite.EntityID);

                if (EntityManager.GetComponent<NPCComponent>(sprite.EntityID) != null &&
                    !seenPositions.Any(pos => pos == transform.Position))
                {
                    continue; // only render npcs on seen positions
                }
                spriteBatch.Draw(TextureManager.GetTexture(sprite.Texture), sprite.Position, sprite.Tint);              
            }

            // mask hidden and discovered tiles
            if (!FogOfWarEnabled) goto SKIPFOW;

            for (int y = 0; y < Util.CurrentFloor.Height; y++)
            {
                for (int x = 0; x < Util.CurrentFloor.Width; x++)
                {
                    var pos = new Vector2(x, y);
                    if (!seenPositions.Any(seenPos => seenPos == pos))
                    {
                        if (Util.CurrentFloor.IsDiscovered(pos))
                        {
                            spriteBatch.Draw(TextureManager.GetTexture("square"), Util.WorldToScreenPosition(pos), new Color(Color.Black, 0.7f));                          
                        }
                        else
                        {
                            spriteBatch.Draw(TextureManager.GetTexture("square"), Util.WorldToScreenPosition(pos), Color.Black);
                        }
                    }

                }
            }

            SKIPFOW:

            /* no rendered texts at the moment!
            var renderedTexts = EntityManager
               .GetAllComponents<RenderableTextComponent>()
               .Where(text => text.Visible);

            foreach (var text in renderedTexts)
            {
                spriteBatch.DrawString(text.Font, text.Text, text.Position, Color.Black);
            }
            */

            spriteBatch.End(); //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 

            graphics.SetRenderTarget(null);
        }
    }
}
