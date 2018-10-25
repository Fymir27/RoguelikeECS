using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;

    public delegate void HealthLostHandler(int entity, float amountLost);
    public delegate void HealthGainedHanler(int entity, float amountGained);
    // TODO: Health regeneration event

    // responsible for registering damage taken by entities
    // and health regeneration
    class HealthSystem
    {
        public void HandleGainedHealth(int entity, float amountGained)
        {
            Log.Message("Entity " + entity + " gained " + amountGained + " health");

            var healthComponent = EntityManager.GetComponentOfEntity<HealthComponent>(entity);

            healthComponent.Amount += amountGained;
        }

        public void HandleLostHealth(int entity, float amountLost)
        {
            var healthComponent = EntityManager.GetComponentOfEntity<HealthComponent>(entity);

            healthComponent.Amount -= amountLost;

            // ded
            if (healthComponent.Amount <= 0)
                EntityManager.RemoveEntity(entity);

            //Log.Message("Entity " + entity + " HP: " + healthComponent.Amount + "|" + healthComponent.Max + " (-" + amountLost + ")");
        }

        public void RegenerateEntity(int entity)
        {
            var healthComponent = EntityManager.GetComponentOfEntity<HealthComponent>(entity);

            healthComponent.RegenerationProgress += healthComponent.RegenerationAmount;
        }
    }
}
