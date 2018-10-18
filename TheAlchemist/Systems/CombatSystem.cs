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
        public event PlayerTurnOverHandler PlayerTurnOverEvent;

        public void HandleBasicAttack(int attacker, int defender)
        {
            Log.Message(attacker + " attacks " + defender);
        
            var equipmentAttacker = EntityManager.GetComponentOfEntity<EquipmentComponent>(attacker);
            var equipmentDefender = EntityManager.GetComponentOfEntity<EquipmentComponent>(defender);

            // get weapon component of item used to attack
            var weaponComponent = EntityManager.GetComponentOfEntity<WeaponComponent>(equipmentAttacker.Weapon);

            if(weaponComponent == null)
            {
                //TODO: implement unarmed combat?
                return;
            }

            float damage = weaponComponent.Damage;

            // get armor component of item used to defend
            var armorComponent = EntityManager.GetComponentOfEntity<ArmorComponent>(equipmentDefender.Armor);

            if(armorComponent != null)
            {
                damage *= (100f - armorComponent.PercentMitigation) / 100f;
                damage -= armorComponent.FlatMitigation;
            }

            if (damage < 0) damage = 0;

            RaiseHealthLostEvent(defender, damage);

            if (attacker == Util.PlayerID)
                RaisePlayerTurnOverEvent();
        }

        void RaiseHealthLostEvent(int entity, float amount)
        {
            HealthLostEvent?.Invoke(entity, amount);
        }

        private void RaisePlayerTurnOverEvent()
        {
            PlayerTurnOverEvent?.Invoke();
        }
    }
}
