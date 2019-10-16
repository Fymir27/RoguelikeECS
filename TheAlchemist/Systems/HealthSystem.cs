using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;

    public delegate void HealthLostHandler(int entity, float amountLost);
    public delegate void HealthGainedHandler(int entity, float amountGained);

    // responsible for registering damage taken by entities
    // and health regeneration
    class HealthSystem
    {
        public void HandleGainedHealth(int entity, float amountGained)
        {
            Log.Message("Entity " + entity + " gained " + amountGained + " health");

            var healthComponent = EntityManager.GetComponent<HealthComponent>(entity);

            healthComponent.Amount += amountGained;
        }

        public void HandleLostHealth(int entity, float amountLost)
        {
            var healthComponent = EntityManager.GetComponent<HealthComponent>(entity);

            healthComponent.Amount -= amountLost;

            // ded
            if (healthComponent.Amount <= 0)
            {
                UISystem.Message(DescriptionSystem.GetNameWithID(entity) + " dies!");
                EntityManager.RemoveEntity(entity); // mark entity for deletion
            }

            //Log.Message("Entity " + entity + " HP: " + healthComponent.Amount + "|" + healthComponent.Max + " (-" + amountLost + ")");
        }

        public void RegenerateEntity(int entity)
        {
            var healthComponent = EntityManager.GetComponent<HealthComponent>(entity);

            healthComponent.RegenerationProgress += healthComponent.RegenerationAmount;
        }
    }
}
