using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    // entityA crashed into entityB
    // returns false if entityB is solid
    public delegate bool CollisionEventHandler(int entityA, int entityB);

    class CollisionSystem
    {
        public CollisionSystem()
        {

        }

        public bool HandleCollision(int entityA, int entityB)
        {
            Console.WriteLine("Collision between objects: " + entityA + " - " + entityB);
            return true;
        }
    }
}
