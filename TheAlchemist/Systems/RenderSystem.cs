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
        public bool FogOfWarEnabled = true;

        GraphicsDevice graphics;
        SpriteBatch spriteBatch;

        Position min; // top left world position thats beeing rendered / thats on screen

        RenderableSpriteComponent blackSquare;
        RenderableSpriteComponent greySquare;
        RenderableSpriteComponent floorSprite;
        RenderableSpriteComponent targetLine;
        RenderableSpriteComponent targetIndicator;

        public RenderSystem(GraphicsDevice graphics)
        {
            this.graphics = graphics;
            spriteBatch = new SpriteBatch(graphics);

            blackSquare = new RenderableSpriteComponent()
            {
                Texture = "square",
                Tint = Color.Black
            };

            greySquare = new RenderableSpriteComponent()
            {
                Texture = "square",
                Tint = new Color(Color.Black, 0.7f)
            };

            targetLine = new RenderableSpriteComponent()
            {
                Texture = "square",
                Tint = new Color(Color.Gold, 0.7f),
            };

            targetIndicator = new RenderableSpriteComponent()
            {
                Texture = "targetIndicator"
            };
        }

        public void RenderWorld(RenderTarget2D renderTarget)
        {
            floorSprite = new RenderableSpriteComponent()
            {
                Texture = Util.CurrentFloor.FloorTexture
            };

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

                    if (FogOfWarEnabled && !tile.Discovered)
                    {
                        DrawSprite(blackSquare, relScreenPos);
                        continue;
                    }

                    if (tile.Terrain == 0)
                    {
                        DrawSprite(floorSprite, relScreenPos);
                    }
                    else
                    {
                        var sprite = EntityManager.GetComponent<RenderableSpriteComponent>(tile.Terrain);

                        if (sprite != null && sprite.Visible)
                        {
                            DrawSprite(sprite, relScreenPos);
                        }
                    }

                    if(tile.Structure != 0)
                    {
                        var sprite = EntityManager.GetComponent<RenderableSpriteComponent>(tile.Structure);

                        if (sprite != null && sprite.Visible)
                        {
                            DrawSprite(sprite, relScreenPos);
                        }
                    }

                    if (FogOfWarEnabled && !seenPositions.Contains(worldPos))
                    {
                        DrawSprite(greySquare, relScreenPos);
                        continue;
                    }

                    if (tile.Items != null && tile.Items.Count > 0)
                    {
                        var sprite = EntityManager.GetComponent<RenderableSpriteComponent>(tile.Items.Last());

                        if (sprite != null && sprite.Visible)
                        {
                            DrawSprite(sprite, relScreenPos);
                        }
                    }

                    if (tile.Character != 0)
                    {
                        //Log.Data(DescriptionSystem.GetDebugInfoEntity(tile.Character));
                        var sprite = EntityManager.GetComponent<RenderableSpriteComponent>(tile.Character);

                        if (sprite != null && sprite.Visible)
                        {
                            var multiTileC = EntityManager.GetComponent<MultiTileComponent>(tile.Character);

                            if (multiTileC == null)
                            {
                                DrawSprite(sprite, relScreenPos);
                            }
                            else
                            {
                                var relAnchor = multiTileC.Anchor - min;
                                DrawSpriteMultiTile(sprite, relScreenPos, relAnchor, multiTileC.FlippedHorizontally, multiTileC.Width);
                            }
                        }
                    }
                }
            }

            // draw target line if necessary
            if (InputManager.Instance.GetCurrentDomain() == InputManager.CommandDomain.Targeting)
            {
                var targetPos = EntityManager.GetComponent<TransformComponent>(Util.TargetIndicatorID).Position;
                var line = Util.CurrentFloor.GetLine(playerPos, targetPos, true);

                if (line.Count > 1)
                {
                    line.RemoveAt(0); // don't draw square on player unless targeting self
                }

                foreach (var pos in line)
                {
                    DrawSprite(targetLine, pos - min);
                }

                DrawSprite(targetIndicator, targetPos - min);

            }

            spriteBatch.End(); //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 

            graphics.SetRenderTarget(null);
        }

        /// <summary>
        /// Draws sprite to screen
        /// </summary>
        /// <param name="sprite">sprite to draw</param>
        /// <param name="pos">relative screen position</param>
        void DrawSprite(RenderableSpriteComponent sprite, Position pos)
        {
            SpriteEffects effects = sprite.FlippedHorizontally ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(TextureManager.GetTexture(sprite.Texture), Util.WorldToScreenPosition(pos), null, sprite.Tint, 0f, Vector2.Zero, Vector2.One, effects, 0f);
        }

        /// <summary>
        /// Draws mutli tile sprite to screen
        /// </summary>
        /// <param name="sprite">sprite to draw</param>
        /// <param name="pos">relative screen position</param>
        /// <param name="relAnchor">anchor of multi tile entity</param>
        void DrawSpriteMultiTile(RenderableSpriteComponent sprite, Position pos, Position relAnchor, bool flip, int width)
        {
            var localOffset = pos - relAnchor;

            if(flip)
            {
                localOffset.X = width - 1 - localOffset.X;
            }

            Point texLocation = new Point(localOffset.X * Util.TileSize, localOffset.Y * Util.TileSize);
            Point size = new Point(Util.TileSize, Util.TileSize);

            Rectangle sourceRect = new Rectangle(texLocation, size);

            SpriteEffects effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            spriteBatch.Draw(TextureManager.GetTexture(sprite.Texture), Util.WorldToScreenPosition(pos), sourceRect, sprite.Tint, 0f, Vector2.Zero, Vector2.One, effects, 0f);
        }
    }
}
