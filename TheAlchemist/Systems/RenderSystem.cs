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
        GraphicsDevice graphics;
        SpriteBatch spriteBatch;

        public bool FogOfWarEnabled = true;

        Position min; // top left world position thats beeing rendered / thats on screen

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

            var renderedTerrain = new List<RenderableSpriteComponent>();
            var renderedItems = new List<RenderableSpriteComponent>();
            var renderedCharacters = new List<RenderableSpriteComponent>();
            var renderedDarkness = new List<RenderableSpriteComponent>();

            Position playerPos = Util.GetPlayerPos();
            Floor floor = Util.CurrentFloor;

            // camera capped to floor
            int minX = Math.Max(0, Math.Min(floor.Width - Util.WorldViewTileWidth, playerPos.X - Util.WorldViewTileWidth / 2));
            int minY = Math.Max(0, Math.Min(floor.Height - Util.WorldViewTileHeight, playerPos.Y - Util.WorldViewTileHeight / 2));

            // camera uncapped
            //int minX = playerPos.X - Util.WorldViewTileWidth / 2;
            //int minY = playerPos.Y - Util.WorldViewTileHeight / 2;

            min = new Position(minX, minY);

            for (int y = 0; y < Util.WorldViewTileHeight; y++)
            {
                for (int x = 0; x < Util.WorldViewTileWidth; x++)
                {
                    var relScreenPos = new Position(x, y);
                    var worldPos = min + relScreenPos;

                    Tile tile = floor.GetTile(worldPos);

                    if (tile == null || !tile.Discovered)
                    {
                        // black square
                        renderedDarkness.Add(new RenderableSpriteComponent()
                        {
                            Texture = "square",
                            Position = Util.WorldToScreenPosition(relScreenPos),
                            Tint = Color.Black,
                        });

                        if (FogOfWarEnabled)
                            continue; // everything else is hidden
                    }

                    if (tile.Terrain != 0)
                    {
                        var sprite = EntityManager.GetComponent<RenderableSpriteComponent>(tile.Terrain);

                        if (sprite != null && sprite.Visible)
                        {
                            renderedTerrain.Add(sprite);
                        }
                    }

                    if (!seenPositions.Contains(worldPos))
                    {
                        // grey square
                        renderedDarkness.Add(new RenderableSpriteComponent()
                        {
                            Texture = "square",
                            Position = Util.WorldToScreenPosition(relScreenPos),
                            Tint = new Color(Color.Black, 0.7f)
                        });

                        if (FogOfWarEnabled)
                            continue; // hide everything else
                    }

                    if (tile.Items != null && tile.Items.Count > 0)
                    {
                        var sprite = EntityManager.GetComponent<RenderableSpriteComponent>(tile.Items.Last());

                        if (sprite != null && sprite.Visible)
                        {
                            renderedItems.Add(sprite);
                        }
                    }

                    if (tile.Character != 0)
                    {
                        //Log.Data(DescriptionSystem.GetDebugInfoEntity(tile.Character));
                        var sprite = EntityManager.GetComponent<RenderableSpriteComponent>(tile.Character);

                        if (sprite != null && sprite.Visible)
                        {
                            renderedCharacters.Add(sprite);
                        }
                    }
                }
            }

            renderedTerrain.ForEach(DrawSprite);
            renderedItems.ForEach(DrawSprite);
            renderedCharacters.ForEach(DrawSprite);

            if (FogOfWarEnabled)
                renderedDarkness.ForEach(DrawSprite);


            /*

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

            */

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

        void DrawSprite(RenderableSpriteComponent sprite)
        {
            if (sprite.EntityID != 0)
            {
                sprite.Position = Util.WorldToScreenPosition(EntityManager.GetComponent<TransformComponent>(sprite.EntityID).Position - min);
                //Log.Message("Drawing sprite of entity at: " + sprite.Position);
            }
            spriteBatch.Draw(TextureManager.GetTexture(sprite.Texture), sprite.Position, sprite.Tint);
        }
    }
}
