using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;

    public delegate void BasicAttackHandler(int attacker, int defender);

    class CombatSystem
    {
        public event HealthLostHandler HealthLostEvent;

        private float unarmedDamage = 5f;

        public void HandleBasicAttack(int attacker, int defender)
        {
            var posAttacker = EntityManager.GetComponent<TransformComponent>(attacker).Position;
            var posDefender = EntityManager.GetComponent<TransformComponent>(defender).Position;

            var seenPositions = Util.CurrentFloor.GetSeenPositions();

            if (seenPositions.Contains(posAttacker) || seenPositions.Contains(posDefender))
            {
                UISystem.Message(DescriptionSystem.GetNameWithID(attacker) + " attacks " + DescriptionSystem.GetNameWithID(defender));
            }

            float damage = unarmedDamage;

            var equipmentAttacker = EntityManager.GetComponent<EquipmentComponent>(attacker);
            var equipmentDefender = EntityManager.GetComponent<EquipmentComponent>(defender);

            if (equipmentAttacker != null)
            {
                // get weapon component of item used to attack
                var weaponComponent = EntityManager.GetComponent<WeaponComponent>(equipmentAttacker.Weapon);
                if (weaponComponent != null)
                {
                    damage = weaponComponent.Damage;
                }
            }

            if (equipmentDefender != null)
            {
                // get armor component of item used to defend
                var armorComponent = EntityManager.GetComponent<ArmorComponent>(equipmentDefender.Armor);

                if (armorComponent != null)
                {
                    damage *= (100f - armorComponent.PercentMitigation) / 100f;
                    damage -= armorComponent.FlatMitigation;
                }
            }

            if (damage < 0) damage = 0;

            RaiseHealthLostEvent(defender, damage);

            Util.TurnOver(attacker);
        }

        void RaiseHealthLostEvent(int entity, float amount)
        {
            HealthLostEvent?.Invoke(entity, amount);
        }
    }
}
