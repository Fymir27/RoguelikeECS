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
            // TODO:
            var stuff = Util.Lerp(0, 0, 0, 0, 0);

            int rowStart = (Width - 1) / 2; // x coord of block in first row (relative to pos)
            float rowDifference = (float)rowStart / ((Height - 1) / 2);

            int blockCount = (Width % 2 == 0) ? 2 : 1;
            float accumulatedRowDifference = 0f;
            int addedBlocksCount = 0;

            for (int y = Pos.Y; y < Pos.Y + (Height + 1) / 2; y++)
            {               
                for (int x = Pos.X + rowStart; x < Pos.X + rowStart + blockCount; x++)
                {
                    Console.WriteLine(new Position(x, y));
                    floor.RemoveTerrain(new Position(x, y));
                }

                accumulatedRowDifference += rowDifference;

                addedBlocksCount = (int)Math.Floor(accumulatedRowDifference);

                rowStart -= addedBlocksCount;
                blockCount += 2 * addedBlocksCount;

                accumulatedRowDifference -= addedBlocksCount;
            }

            accumulatedRowDifference = 0f;
            rowStart = 0;
            blockCount -= 2 * addedBlocksCount;

            Console.WriteLine("Lower Half:");

            for (int y = Pos.Y + Height / 2; y < Pos.Y + Height; y++)
            {
                Console.WriteLine("Y: " + y);
                for (int x = Pos.X + rowStart; x < Pos.X + rowStart + blockCount; x++)
                {
                    Console.WriteLine(new Position(x, y));
                    floor.RemoveTerrain(new Position(x, y));
                }

                accumulatedRowDifference += rowDifference;

                addedBlocksCount = (int)Math.Floor(accumulatedRowDifference);

                rowStart += addedBlocksCount;
                blockCount = Math.Max(1, blockCount - 2 * addedBlocksCount);

                accumulatedRowDifference -= addedBlocksCount;
            }


            /*

            // horizontal coord (x) of first floor tile in first row (relative to Pos)
            //   X# 
            //  ####
            // ######
            // ######
            //  ####
            //   ##
            //
            int halfWidth = (Width - 1) / 2;

            // vertical coord (y) of row where floor tiles in row start to decrease (relative to Pos)
            //   ##
            //  ####
            // ######
            // Y#####
            //  ####
            //   ##
            //
            int halfHeight = (Height + 1) / 2;

            int blocksInRow = (Width % 2 == 0) ? 2 : 1;

            int rowStart = Pos.X + halfWidth;

            for (int y = Pos.Y; y < Pos.Y + halfHeight; y++)
            {
                Console.WriteLine("Row: " + (y - Pos.Y));
                for (int x = rowStart; x < rowStart + blocksInRow; x++)
                {
                    floor.RemoveTerrain(new Position(x, y));
                }
                rowStart--;
                blocksInRow += 2;
            }

            if (Height % 2 == 0)
            {
                rowStart += 1;
                blocksInRow -= 2;
            }
            else
            {
                rowStart += 2;
                blocksInRow -= 4;
            }

            for(int y = Pos.Y + halfHeight; y < Pos.Y + Height; y++)
            {
                for (int x = rowStart; x < rowStart + blocksInRow; x++)
                {
                    floor.RemoveTerrain(new Position(x, y));
                }
                rowStart++;
                blocksInRow -= 2;
            }
            */
        }
    }
}
