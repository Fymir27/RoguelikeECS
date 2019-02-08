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

        // for possible doors
        List<Position> outline = new List<Position>();

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

            Log.Message("New Room created at " + pos + "-> Size: " + new Position(width, height) + ", " + shape.ToString());

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

            string outlineString = "Outline:\n";
            foreach (var item in outline)
            {
                outlineString += item.ToString() + ",";
            }
            Log.Data(outlineString);
        }

        private void InitRectangular()
        {
            // outline/shape

            for (int y = Pos.Y; y < Pos.Y + Height; y++)
            {
                for (int x = Pos.X; x < Pos.X + Width; x++)
                {
                    Position pos = new Position(x, y);
                    if(x == Pos.X || y == Pos.X || x == Pos.X + Width - 1 || y == Pos.Y + Height - 1)
                    {
                        outline.Add(pos);
                    }
                    floor.RemoveTerrain(pos);
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
                        rowStart = Util.Lerp(y, halfHeight, 0, Height - 1, halfWidth);
                    }
                }

                for (int x = rowStart; x < Width - rowStart; x++)
                {
                    if(x == rowStart || x == Width - rowStart - 1)
                    {
                        outline.Add(Pos + new Position(x, y));
                    }
                    floor.RemoveTerrain(Pos + new Position(x, y));
                }
            }        
        }


        // returns random position where door can be adjacent (outline)
        public Position GetPossibleDoor()
        {
            return outline[Game.Random.Next(outline.Count)];
        }
    }
}
