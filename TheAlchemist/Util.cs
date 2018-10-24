using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist
{
    using Systems;
    using Components;
    
    public enum Direction
    {
        North,   
        NorthEast,  
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    public delegate void TurnOverHandler(int entity);

    static class Util
    {
        public static event TurnOverHandler TurnOverEvent;

        public static int TileSize { get; } = 10;
        public static int PlayerID { get; set; } = 0;
        public static Floor CurrentFloor { get; set; } = null;
        public static SpriteFont DefaultFont { get; set; } = null;

        public static bool PlayerTurnOver { get; set; } = false;

        // gets called for every new type of component/entity/...
        public static class TypeID<T>
        {
            static int counter = 0;
            public static int Get()
            {             
                return ++counter;
            }
        }

        // gets called for every new instance of component/entity/... respectively
        public static class UniqueID<T>
        {
            static int counter = 0;
            public static int Get()
            {
                return ++counter;
            }
        }

        // transforms world position to screen position based on tile size
        public static Vector2 WorldToScreenPosition(Vector2 worldPos)
        {
            return new Vector2(worldPos.X * TileSize, worldPos.Y * TileSize);
        }

        // returns Direction 180 degrees from param direction
        public static Direction getOppositeDirection(Direction direction)
        {
            int nrDirections = 8;
            return (Direction)(((int)direction + nrDirections / 2) % nrDirections);
        }

        // returns a "unit" vector in Direction dir
        public static Vector2 GetUnitVectorInDirection(Direction dir)
        {
            switch(dir)
            {
                case Direction.North:
                    return new Vector2(0, -1);

                case Direction.East:
                    return new Vector2(1, 0);

                case Direction.South:
                    return new Vector2(0, 1);

                case Direction.West:
                    return new Vector2(-1, 0);

                default:
                    Console.WriteLine("No vector known for " + dir);
                    return new Vector2(0, 0);
            }
        }

        public static void TurnOver(int entity)
        {
            Log.Message("Turn over for " + DescriptionSystem.GetNameWithID(entity));

            CurrentFloor.CalculateCellVisibility();

            TurnOverEvent?.Invoke(entity);

            if (entity == PlayerID)
                PlayerTurnOver = true;
        }
    }
}
