using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace TheAlchemist
{
    class NineSlicedSprite
    {
        public string TextureName { get; }
        public Rectangle Rect { get; private set; }
        public Texture2D Texture { get; private set; }

        private GraphicsDevice graphicsDevice;

        public NineSlicedSprite(string textureName, Rectangle rect, GraphicsDevice graphics)
        {
            TextureName = textureName;
            graphicsDevice = graphics;
            Update(rect);
        }

        public void Draw(SpriteBatch spriteBatch, Color tint)
        {
            spriteBatch.Draw(Texture, Rect, tint);
        }

        public void Update(Rectangle rect)
        {
            if(rect == Rect)
            {
                return;
            }

            if (rect.Size == Rect.Size)
            {
                Rect = rect;
                return;
            }

            Rect = rect;

            if(Texture == null || Texture.Bounds.Size != Rect.Size)
            {
                Texture = new Texture2D(graphicsDevice, Rect.Width, Rect.Height);
            }

            Texture2D originalTexture = TextureManager.GetTexture(TextureName);
            Point oTexSize = originalTexture.Bounds.Size;

            int tSize = Util.TileSize;

            if(oTexSize != new Point(tSize * 3, tSize * 3))
            {
                Log.Error("Original texture for NineSlicedSprite needs to be TileSize x TileSize!");
                return;
            }

            var originalDataRaw = new Color[oTexSize.X * oTexSize.Y];
            originalTexture.GetData(originalDataRaw);
            var originalData = new Array1D<Color>(originalDataRaw, oTexSize.X, oTexSize.Y);

            var newData = new Array1D<Color>(Rect.Width, Rect.Height);              

            // from original texture
            var cornerNW = new Rectangle(0, 0, tSize, tSize);
            var cornerNE = new Rectangle(tSize * 2, 0, tSize, tSize);
            var cornerSW = new Rectangle(0, tSize * 2, tSize, tSize);
            var cornerSE = new Rectangle(tSize * 2, tSize * 2, tSize, tSize);

            var borderN = new Rectangle(tSize, 0, tSize, tSize);
            var borderW = new Rectangle(0, tSize, tSize, tSize);
            var borderE = new Rectangle(tSize * 2, tSize, tSize, tSize);
            var borderS = new Rectangle(tSize, tSize * 2, tSize, tSize);

            var center = new Rectangle(tSize, tSize, tSize, tSize);

            Copy(originalData, newData, cornerNW, new Point(0, 0));
            Copy(originalData, newData, cornerNE, new Point(Rect.Width - tSize, 0));
            Copy(originalData, newData, cornerSW, new Point(0, Rect.Height - tSize));
            Copy(originalData, newData, cornerSE, new Point(Rect.Width - tSize, Rect.Height - tSize));

            CopyRepeating(originalData, newData, borderN, new Rectangle(tSize, 0, Rect.Width - tSize * 2, tSize));
            CopyRepeating(originalData, newData, borderW, new Rectangle(0, tSize, tSize, Rect.Height - tSize * 2));
            CopyRepeating(originalData, newData, borderE, new Rectangle(Rect.Width - tSize, tSize, tSize, Rect.Height - tSize * 2));
            CopyRepeating(originalData, newData, borderS, new Rectangle(tSize, Rect.Height - tSize, Rect.Width - tSize * 2, tSize));

            CopyRepeating(originalData, newData, center, new Rectangle(tSize, tSize, Rect.Width - tSize * 2, Rect.Height - tSize * 2));

            //for (int y = 0; y < Rect.Height; y++)
            //{
            //    for (int x = 0; x < Rect.Width; x++)
            //    {
            //        int pixelIndex = y * Rect.Width + x;

            //        if(cornerNW.Contains(x, y))
            //        {
            //            int originalPixelIndex = tSize * 3 * y + x;
            //            newData[pixelIndex] = originalData[originalPixelIndex];   
            //        }
            //        else if(cornerNE.Contains(x, y))
            //        {
            //            int originalPixelIndex = tSize * 3 * y + tSize * 2 + x - cornerNE.Location.X;
            //            newData[pixelIndex] = originalData[originalPixelIndex];
            //        }
            //        else if (cornerSW.Contains(x, y))
            //        {
            //            int originalPixelIndex = tSize * 3 * (tSize * 2 + (y - cornerSW.Y)) + x;
            //            newData[pixelIndex] = originalData[originalPixelIndex];
            //        }
            //        else if (cornerSE.Contains(x, y))
            //        {
            //            int originalPixelIndex = tSize * 3 * (tSize * 2 + (y - cornerSW.Y)) + tSize * 2 + x - cornerNE.Location.X;
            //            newData[pixelIndex] = originalData[originalPixelIndex];
            //        }
            //        else
            //        {
            //            //newData[pixelIndex] = Color.White;
            //        }
            //    }
            //}

            Texture.SetData(newData.GetData());
        }
      

        private void Copy(Array1D<Color> from, Array1D<Color> to, Rectangle source, Point destination)
        {
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    to[destination.X + x, destination.Y + y] = from[source.X + x, source.Y + y];
                }
            }
        }

        private void CopyRepeating(Array1D<Color> from, Array1D<Color> to, Rectangle source, Rectangle destination)
        {
            for (int y = 0; y < destination.Height; y++)
            {
                for (int x = 0; x < destination.Width; x++)
                {
                    to[destination.X + x, destination.Y + y] = from[source.X + (x % source.Width), source.Y + (y % source.Height)];
                }
            }
        }
    }
}
