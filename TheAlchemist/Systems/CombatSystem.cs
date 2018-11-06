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

        private float unarmedDamage = 1f;

        public void HandleBasicAttack(int attacker, int defender)
        {           
            Log.Message(attacker + " attacks " + defender);

            float damage = unarmedDamage;

            var equipmentAttacker = EntityManager.GetComponentOfEntity<EquipmentComponent>(attacker);
            var equipmentDefender = EntityManager.GetComponentOfEntity<EquipmentComponent>(defender);

            if(equipmentAttacker != null)
            {          
                // get weapon component of item used to attack
                var weaponComponent = EntityManager.GetComponentOfEntity<WeaponComponent>(equipmentAttacker.Weapon);
                if (weaponComponent != null)
                {
                    damage = weaponComponent.Damage;
                }              
            }

            if(equipmentDefender != null)
            {
                // get armor component of item used to defend
                var armorComponent = EntityManager.GetComponentOfEntity<ArmorComponent>(equipmentDefender.Armor);

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
