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
        public int Nr;
        public Position Pos;
        public int Width, Height;
        public RoomShape Shape;

        public Dictionary<int, Tuple<List<Position>, List<Position>>> connectionPoints = new Dictionary<int, Tuple<List<Position>, List<Position>>>();

        // for possible doors
        List<Position> outline = new List<Position>();
        public List<Position> freePositions = new List<Position>();

        public int Difficulty;
        public int Vegetation;
        // etc...

        private Floor floor;

        public Room(Position pos, int width, int height, RoomShape shape, Floor floor)
        {
            Nr = floor.GetNewRoomNr();
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

            /*
            string outlineString = "Outline:\n";
            foreach (var item in outline)
            {
                outlineString += item.ToString() + ",";
            }
            Log.Data(outlineString);
            */
        }

        private void InitRectangular()
        {
            // outline/shape

            for (int y = Pos.Y; y < Pos.Y + Height; y++)
            {
                for (int x = Pos.X; x < Pos.X + Width; x++)
                {
                    Position pos = new Position(x, y);
                    if (x == Pos.X || y == Pos.X || x == Pos.X + Width - 1 || y == Pos.Y + Height - 1)
                    {
                        outline.Add(pos);
                    }
                    AddToRoom(pos);
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
                    var pos = Pos + new Position(x, y);
                    if (x == rowStart || x == Width - rowStart - 1)
                    {
                        outline.Add(pos);
                    }
                    AddToRoom(pos);
                }
            }
        }

        private void AddToRoom(Position pos)
        {
            floor.RemoveTerrain(pos);
            floor.roomNrs[pos.X, pos.Y] = Nr;
            freePositions.Add(pos);
        }

        // returns random position where door can be adjacent (outline)
        public Position GetPossibleDoor()
        {
            return outline[Game.Random.Next(outline.Count)];
        }

        public void AddConnection(int otherRoomNr, Position from, Position to)
        {
            if (connectionPoints.ContainsKey(otherRoomNr))
            {
                connectionPoints[otherRoomNr].Item1.Add(from);
                connectionPoints[otherRoomNr].Item2.Add(to);
            }
            else
            {
                connectionPoints[otherRoomNr] = new Tuple<List<Position>, List<Position>>(
                    new List<Position>() { from },
                    new List<Position>() { to }
                );
            }
        }
    }
}
