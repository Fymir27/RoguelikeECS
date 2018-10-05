using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist
{
    enum Direction
    {
        North,   
        NorthEast,  
        Eeast,
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
    }
}
