using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist
{
    enum RoomShape
    {
        Rectangle,
        Diamond
    }

    class Room
    {
        public Position Pos;
        public int Width, Height;
        public RoomShape Shape;        

        public int Difficulty;
        public int Vegetation;
        // etc...

        private Floor floor;

        public Room(Position pos, int width, int height, RoomShape shape, Floor floor)
        {
            Pos = pos;
            Width = width;
            Height = height;
            Shape = shape;

            this.floor = floor;

            switch (shape)
            {
                case RoomShape.Rectangle:
                    InitRectangular();
                    break;

                case RoomShape.Diamond:

                    InitDiamond();
                    break;

                default:
                    Log.Error("Room shape not implemented! -> " + shape);
                    break;
            }
        }

        private void InitRectangular()
        {
            // outline/shape

            for (int y = Pos.Y; y < Pos.Y + Height; y++)
            {
                for (int x = Pos.X; x < Pos.Y + Width; x++)
                {
                    floor.RemoveTerrain(new Position(x, y));
                }
            }         
        }

        private void InitDiamond()
        {
            int halfWidth = (Width - 1) / 2;
            int halfHeight = (Height - 1) / 2;

            int rowStart;
            for (int y = 0; y < Height; y++)
            {
                if (y <= halfHeight)
                {
                    rowStart = Util.Lerp(y, 0, halfWidth, halfHeight, 0);
                }
                else
                {
                    if (Height % 2 == 0)
                    {
                        rowStart = Util.Lerp(y, halfHeight + 1, 0, Height - 1, halfWidth);
                    }
                    else
                    {
                        rowStart = Util.Lerp(y + 1, halfHeight + 1, 0, Height, halfWidth);
                    }
                }

                for (int x = rowStart; x < Width - rowStart; x++)
                {
                    floor.RemoveTerrain(Pos + new Position(x, y));
                }
            }        
        }
    }
}
