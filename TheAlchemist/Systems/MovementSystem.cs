using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;


namespace TheAlchemist.Systems
{
    using Components;

    public delegate void MovementEventHandler(int entity, Direction dir);

    class MovementSystem
    {
        public event CollisionEventHandler CollisionEvent;

        public MovementSystem()
        {
           
        }

        public void HandleMovementEvent(int entity, Direction dir)
        {
            var entityTransform = EntityManager.GetComponentOfEntity<TransformComponent>(entity);
            Vector2 newPos = entityTransform.Position + Util.GetUnitVectorInDirection(dir);
            var floor = Util.CurrentFloor;

            int otherCharacter = floor.GetCharacter(newPos);
            if(otherCharacter != 0)
            {
                if(RaiseCollisionEvent(entity, otherCharacter)) // check if other character cant be stepped on / is solid
                {
                    return;
                }
            }

            int terrain = floor.GetTerrain(newPos);
            if(terrain != 0)
            {
                if(RaiseCollisionEvent(entity, terrain)) // check if terrain is solid
                {
                    return;
                }
            }

            // TODO: implement item pickup

            floor.SetCharacter(entityTransform.Position, 0);
            floor.SetCharacter(newPos, entity);
            entityTransform.Position = newPos;
            
        }

        // returns wether entityB was solid
        private bool RaiseCollisionEvent(int entityA, int entityB)
        {
            if (CollisionEvent != null)
                return CollisionEvent(entityA, entityB);
            else
                return false;
        }
    }
}
