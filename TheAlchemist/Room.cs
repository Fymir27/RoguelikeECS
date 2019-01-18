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

            //top/bottom
            for (int x = 0; x < Width; x++)
            {
                floor.PlaceTerrain(Pos + new Position(x, 0), Floor.CreateWall());
                floor.PlaceTerrain(Pos + new Position(x, Height -  1), Floor.CreateWall());
            }

            for (int y = 1; y < Height - 1; y++)
            {
                floor.PlaceTerrain(Pos + new Position(0, y), Floor.CreateWall());
                floor.PlaceTerrain(Pos + new Position(Width - 1, y), Floor.CreateWall());
            }
            
        }

        private void InitDiamond()
        {
            int iLeft = Width / 2;
            int iRight = Width / 2;

            if (Width % 2 == 0) //   ##
                iLeft--;        //  #  #

            // first and last row
            floor.PlaceTerrain(Pos + new Position(iLeft, 0), Floor.CreateWall());
            floor.PlaceTerrain(Pos + new Position(iLeft, Height - 1), Floor.CreateWall());

            if (iLeft != iRight)
            {
                floor.PlaceTerrain(Pos + new Position(iRight, 0), Floor.CreateWall());
                floor.PlaceTerrain(Pos + new Position(iRight, Height - 1), Floor.CreateWall());
            }

            for (int y = 1; y < Height - 1; y++)
            {
                if(y <= Height / 2)
                {
                    iLeft--;
                    iRight++;
                }
                else
                {
                    iLeft++;
                    iRight--;
                }

                floor.PlaceTerrain(Pos + new Position(iLeft, y), Floor.CreateWall());
                floor.PlaceTerrain(Pos + new Position(iRight, y), Floor.CreateWall());
            }
        }
    }
}
