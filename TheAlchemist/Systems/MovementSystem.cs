﻿using System;
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

            var sprite = EntityManager.GetComponent<RenderableSpriteComponent>(entity);

            if (sprite != null)
            {
                if (dir == Direction.West)
                {
                    sprite.FlippedHorizontally = true;
                }
                else if (dir == Direction.East)
                {
                    sprite.FlippedHorizontally = false;
                }
            }

            var multiTileC = EntityManager.GetComponent<MultiTileComponent>(entity);

            if (multiTileC != null)
            {                   
                HandleMultiTileMovement(entity, multiTileC, newPos, sprite.FlippedHorizontally);
                multiTileC.FlippedHorizontally = sprite.FlippedHorizontally;
                return;
            }

            if (TryMove(entity, newPos))
            {
                // Move entity
                floor.RemoveCharacter(entityTransform.Position);
                floor.PlaceCharacter(newPos, entity);
                Util.TurnOver(entity);
            }                      
        }

        /// <summary>
        /// checks if moving onto newPos is possible
        /// and triggers any interaction otherwise
        /// </summary>
        /// <param name="entity">ID of entity to move</param>
        /// <param name="newPos">new Position to move to</param>
        /// <returns>wether movement is possible</returns>
        private bool TryMove(int entity, Position newPos)
        {
            var floor = Util.CurrentFloor;

            if(floor.IsOutOfBounds(newPos))
            {
                return false;
            }

            int otherCharacter = floor.GetCharacter(newPos);

            //check if someone's already there
            if (otherCharacter != 0 && otherCharacter != entity)
            {
                // check if collidable
                if (EntityManager.GetComponent<ColliderComponent>(otherCharacter) != null)
                {
                    //TODO: talk to npcs?
                    RaiseBasicAttackEvent(entity, otherCharacter);
                    return false;
                }
                else
                {
                    Log.Warning("Character without collider:");
                    Log.Data(DescriptionSystem.GetDebugInfoEntity(otherCharacter));
                    UISystem.Message("Something seems to be there...");
                    return false;
                }
            }

            int structure = floor.GetStructure(newPos);

            if (structure != 0 && EntityManager.GetComponent<ColliderComponent>(structure) != null)
            {
                bool solid = RaiseCollisionEvent(entity, structure);

                // check if interactable
                var interactable = EntityManager.GetComponent<InteractableComponent>(structure);

                // only interact with structures right away if they're solid ("bumping" into them)
                if (solid)
                {
                    if (interactable != null)
                    {
                        RaiseInteractionEvent(entity, structure);
                    }
                    return false;
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
                    return false;                                                                                                                                                
                }
            }

            //trigger special Message on step on
            if (entity == Util.PlayerID && terrain != 0)
            {
                string message = (DescriptionSystem.GetSpecialMessage(terrain, DescriptionComponent.MessageType.StepOn));

                if (message.Length > 0)
                {
                    UISystem.Message(message);
                }
            }

            return true;
        }

        private void HandleMultiTileMovement(int entity, MultiTileComponent multiTileC, Position newPos, bool flipped)
        {
            //bool[,] flippedOccupationMatrix = multiTileC.OccupationMatrix;//.FlipHorizontally();

            //multiTileC.Anchor = newPos;

            //int width = flippedOccupationMatrix.GetLength(0);
            //int height = flippedOccupationMatrix.GetLength(1);

            var floor = Util.CurrentFloor;

            bool movementPossible = true;

            foreach (var offsetPos in multiTileC.OccupiedPositions)
            {
                var newPartialPos = newPos + offsetPos;
                if(!TryMove(entity, newPartialPos))
                {
                    movementPossible = false;
                }
            }

            // removal and readding is always needed
            // because the entity might have flipped
            floor.RemoveCharacter(multiTileC.Anchor);

            multiTileC.FlippedHorizontally = flipped;

            if (movementPossible)
            {
                floor.PlaceCharacter(newPos, entity);
                //Util.CurrentFloor.LogCharactersAroundPlayer();
            }
            else
            {
                // Place on same position (although may be flipped now)
                floor.PlaceCharacter(multiTileC.Anchor, entity);
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
