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
        public MovementSystem(InputSystem input)
        {
            input.MovementEvent += HandleMovementEvent;
        }

        private void HandleMovementEvent(int entity, Direction dir)
        {
            var playerTransform = (TransformComponent)EntityManager.GetComponentOfEntity(entity, TransformComponent.TypeID);
            playerTransform.Position += Util.GetUnitVectorInDirection(dir);
        }
    }
}
