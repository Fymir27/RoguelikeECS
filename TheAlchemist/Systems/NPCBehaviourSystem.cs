using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;

    // plans and executes behaviour of enemies and other npcs
    class NPCBehaviourSystem
    {
        public event MovementEventHandler EnemyMovedEvent;

        public void EnemyTurn()
        {
            var npcs = EntityManager.GetEntitiesWithComponent<NPCComponent>();

            foreach(var npc in npcs)
            {
                // get random direction (for now only 4 directional)              
                Direction dir = (Direction)(Game.Random.Next(0, 4) * 2);
                RaiseEnemyMovedEvent(npc, dir);
            }
        }

        private void RaiseEnemyMovedEvent(int entity, Direction dir)
        {
            EnemyMovedEvent?.Invoke(entity, dir);
        }
    }
}
