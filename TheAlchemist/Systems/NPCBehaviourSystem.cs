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
                var health = EntityManager.GetComponent<HealthComponent>(npc);

                if(health != null)
                {
                    if(health.Amount <= 0)
                    {
                        continue; // npc already died this turn
                    }
                }
               
                var pos = LocationSystem.GetPosition(npc);
                int distanceToPlayer = LocationSystem.GetDistance(Util.PlayerID, pos);

                // TODO: different ranges
                int range = 10;
                
                if(distanceToPlayer <= range)
                {                  
                    var nextPos = LocationSystem.StepTowards(Util.PlayerID, pos);
                    var dir = (nextPos - pos).ToDirection();
                    //Log.Message(String.Format("{0}: Pos: {1}, NexPos: {2}, Dir: {3}", DescriptionSystem.GetNameWithID(npc), pos, nextPos, dir.ToString()));
                    RaiseEnemyMovedEvent(npc, dir);
                }
                
            }

        }

        private void RaiseEnemyMovedEvent(int entity, Direction dir)
        {
            EnemyMovedEvent?.Invoke(entity, dir);
        }
    }
}
