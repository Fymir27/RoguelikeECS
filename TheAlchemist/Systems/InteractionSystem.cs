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

            var door = EntityManager.GetComponent<DoorComponent>(other);

            if(door != null)
            {
                if(door.Open)
                {
                    return false;
                }
                //Log.Message("Its a door!");

                door.Open = true;

                // open door by setting its colider to non solid
                EntityManager.GetComponent<ColliderComponent>(other).Solid = false;

                // change texture to open
                EntityManager.GetComponent<RenderableSpriteComponent>(other).Texture = door.TextureOpen;
            }

            Log.Message("Interaction between " + DescriptionSystem.GetNameWithID(actor) + " and " + DescriptionSystem.GetNameWithID(other));

            Util.TurnOver(actor);
            return true;
        }
    }
}
