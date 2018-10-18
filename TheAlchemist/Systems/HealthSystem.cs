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

            if (healthComponent.Amount > healthComponent.Max)
                healthComponent.Amount = healthComponent.Max;
        }

        public void HandleLostHealth(int entity, float amountLost)
        {
            var healthComponent = EntityManager.GetComponentOfEntity<HealthComponent>(entity);

            healthComponent.Amount -= amountLost;

            Log.Message("Entity " + entity + " HP: " + healthComponent.Amount + "|" + healthComponent.Max + " (-" + amountLost + ")");

            // TODO: what if character dies?
        }
    }
}
