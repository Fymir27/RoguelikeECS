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
        public event BasicAttackHandler BasicAttackEvent;
        public event InteractionHandler InteractionEvent;

        public MovementSystem()
        {

        }

        public void HandleMovementEvent(int entity, Direction dir)
        {
            // move inventory cursor instead!
            if (entity == Util.PlayerID && UI.InventoryOpen)
            {
                return;
            }

            var entityTransform = EntityManager.GetComponent<TransformComponent>(entity);
            Position newPos = entityTransform.Position + Util.GetUnitVectorInDirection(dir);
            var floor = Util.CurrentFloor;

            // target indicator does not need collision etc...
            // just move position if its not oob
            if (entity == Util.TargetIndicatorID)
            {
                if (floor.IsOutOfBounds(newPos))
                    return;

                if (!floor.IsDiscovered(newPos))
                    return;

                entityTransform.Position = newPos;
                return;
            }

            int otherCharacter = floor.GetCharacter(newPos);

            //check if someone's already there
            if (otherCharacter != 0)
            {
                // check if collidable
                if (EntityManager.GetComponent<ColliderComponent>(otherCharacter) != null)
                {
                    //TODO: talk to npcs?
                    RaiseBasicAttackEvent(entity, otherCharacter);
                    return; // don't move
                }
                else
                {
                    Log.Warning("Character without collider:");
                    Log.Data(DescriptionSystem.GetDebugInfoEntity(otherCharacter));
                    UISystem.Message("Something seems to be there...");
                    return;
                }
            }

            int terrain = floor.GetTerrain(newPos);

            // check if collidable with
            if (terrain != 0 && EntityManager.GetComponent<ColliderComponent>(terrain) != null)
            {
                // check if terrain is solid before possible interaction
                // this is because solidity might be changed by interaction (e.g. door gets opened)
                bool solid = RaiseCollisionEvent(entity, terrain);

                // check if interactable
                var interactable = EntityManager.GetComponent<InteractableComponent>(terrain);
                if (interactable != null)
                {
                    RaiseInteractionEvent(entity, terrain);
                }

                // check if terrain is solid
                if (solid)
                {
                    return; // don't move
                }
            }

            // TODO: implement automatic item pickup?

            // Move entity
            floor.RemoveCharacter(entityTransform.Position);
            floor.PlaceCharacter(newPos, entity);

            //trigger special Message on step on
            if (entity == Util.PlayerID && terrain != 0)
            {
                string message = (DescriptionSystem.GetSpecialMessage(terrain, DescriptionComponent.MessageType.StepOn));

                if (message.Length > 0)
                {
                    UISystem.Message(message);
                }
            }

            Util.TurnOver(entity);
        }

        // returns wether entityB was solid
        private bool RaiseCollisionEvent(int entityA, int entityB)
        {
            // SHOULD throw an exception if event is not handled
            return CollisionEvent(entityA, entityB);
        }

        private bool RaiseInteractionEvent(int actor, int other)
        {
            // SHOULD throw an exception if event is not handled
            return InteractionEvent(actor, other);
        }

        private void RaiseBasicAttackEvent(int attacker, int defender)
        {
            // SHOULD throw an exception if event is not handled
            BasicAttackEvent(attacker, defender);
        }
    }
}
