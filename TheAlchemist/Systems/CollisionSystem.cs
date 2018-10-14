using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;
    // entityA collided with entityB
    // returns true if entityB is solid
    public delegate bool CollisionEventHandler(int entityA, int entityB);

    class CollisionSystem
    {
        public CollisionSystem()
        {

        }

        public bool HandleCollision(int entityA, int entityB)
        {
            Console.WriteLine("Collision between objects: " + entityA + " - " + entityB);
            var colliderB = EntityManager.GetComponentOfEntity<ColliderComponent>(entityB);
            if (colliderB == null)
                return false;
            if (colliderB.Solid)
                return true;
            return false;
        }
    }
}
