using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist
{
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

    static class Util
    {
        // gets called for every new type of component/entity/...
        public static class TypeID<T>
        {
            static int counter = 0;
            public static int Get()
            {
                Console.WriteLine("TypeID for" + typeof(T) + " is: " + (counter + 1));
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
    }
}
