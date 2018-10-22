using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;

    public delegate void InteractionHandler(int actor, int other);

    class InteractionSystem
    {
        public void HandleInteraction(int actor, int other)
        {
            var components = EntityManager.GetAllComponentsOfEntity(other);
            var door = (DoorComponent)components.ToList().FirstOrDefault(component => component.TypeID == DoorComponent.TypeID);

            if(door != null)
            {
                door.Open = !door.Open; // open/close
            }

        }
    }
}
