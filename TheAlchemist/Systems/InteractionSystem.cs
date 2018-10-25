using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;

    // should return true if interaction was successful
    public delegate bool InteractionHandler(int actor, int other);

    class InteractionSystem
    {
        public bool HandleInteraction(int actor, int other)
        {
            Log.Message("Interaction between " + actor + "," + DescriptionSystem.GetName(actor) + " and " + other);

            var door = EntityManager.GetComponentOfEntity<DoorComponent>(other);

            if(door != null)
            {
                //Log.Message("Its a door!");

                door.Open = true;

                // open door by setting its colider to non solid
                EntityManager.GetComponentOfEntity<ColliderComponent>(other).Solid = false;

                // change texture to open
                EntityManager.GetComponentOfEntity<RenderableSpriteComponent>(other).Texture = door.TextureOpen;
            }

            Log.Data(DescriptionSystem.GetDebugInfoEntity(other));

            Util.TurnOver(actor);
            return true;
        }
    }
}
