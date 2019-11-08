using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;

    public delegate void BasicAttackHandler(int attacker, int defender);

    public enum DamageType
    {
        True,
        //Physical
        Bludgeoning,
        Piercing,
        Slashing,
        // Elemental
        Fire,
        Water,
        Nature,
        Wind
    }

    static class DamageTypeExtensions
    {
        static DamageType[] physical = { DamageType.Bludgeoning, DamageType.Piercing, DamageType.Slashing };
        static DamageType[] elemental = { DamageType.Fire, DamageType.Water, DamageType.Nature, DamageType.Wind };

        public static bool IsPhysical(this DamageType type)
        {
            return physical.Contains(type);
        }

        public static bool IsElemental(this DamageType type)
        {
            return elemental.Contains(type);
        }

        public static Stat ResistedBy(this DamageType type)
        {
            switch (type)
            {
                case DamageType.Fire: return Stat.Fire;
                case DamageType.Water: return Stat.Water;
                case DamageType.Nature: return Stat.Nature;
                case DamageType.Wind: return Stat.Wind;

                default:
                    Log.Warning("DamageType " + type + " is not blocked by any stat!");
                    return 0;
            }
        }
    }

    struct DamageRange
    {
        public DamageType Type;
        public int Min;
        public int Max;

        public override string ToString()
        {
            return String.Format("({0} - {1}, {2})", Min, Max, Type);
        }
    }

    class CombatSystem
    {
        public event HealthLostHandler HealthLostEvent;

        private DamageRange defaultDamage = new DamageRange()
        {
            Type = DamageType.True,
            Min = 1,
            Max = 1
        };

        public void HandleBasicAttack(int attacker, int defender)
        {
            var weaponDamages = GetWeaponDamage(attacker);

            if (weaponDamages.Count == 0)
            {
                weaponDamages.Add(defaultDamage);
            }

            //Log.Data("Damages pre mitigation:\n" + Util.GetStringFromEnumerable(weaponDamages));

            // group together all damages by type and roll damage between min and max
            Dictionary<DamageType, int> preMitigionDamage = new Dictionary<DamageType, int>();

            foreach (var damage in weaponDamages)
            {
                int damageValue = Game.Random.Next(damage.Min, damage.Max + 1);
                preMitigionDamage.AddOrIncrease(damage.Type, damageValue);
            }

            var finalDamages = GetDamagesAfterMitigation(preMitigionDamage, defender);

            //Log.Data("Damages after mitigation:\n" + Util.GetStringFromEnumerable(damages));
            Log.Message(DescriptionSystem.GetNameWithID(defender) + " gets hit for: " + Util.GetStringFromCollection(finalDamages));

            foreach (var damage in finalDamages)
            {
                // TODO: handle different damage types (separate events?)
                RaiseHealthLostEvent(defender, damage.Value);
            }

            HandleAttackMessage(attacker, defender, finalDamages);

            Util.TurnOver(attacker);
        }

        void RaiseHealthLostEvent(int entity, float amount)
        {
            HealthLostEvent?.Invoke(entity, amount);
        }

        void HandleAttackMessage(int attacker, int defender, Dictionary<DamageType, int> damages)
        {
            var posAttacker = EntityManager.GetComponent<TransformComponent>(attacker).Position;
            var posDefender = EntityManager.GetComponent<TransformComponent>(defender).Position;

            var seenPositions = Util.CurrentFloor.GetSeenPositions();

            bool attackerVisible = seenPositions.Contains(posAttacker);
            bool defenderVisible = seenPositions.Contains(posDefender);

            if (!(attackerVisible || defenderVisible))
            {
                return;
            }

            StringBuilder sb = new StringBuilder();

            if (attackerVisible)
            {
                sb.Append(DescriptionSystem.GetName(attacker));
            }
            else
            {
                sb.Append("Something");
            }

            sb.Append(" attacks ");

            if (defenderVisible)
            {
                sb.Append(DescriptionSystem.GetName(defender));
            }
            else
            {
                sb.Append("Something");
            }

            sb.Append("!");

            // e.g. "(69 fire, 42 water, ...)"
            sb.Append(" (");
            for (int i = 0; i < damages.Count; i++)
            {
                if(i > 0)
                {
                    sb.Append(", ");
                }              
                sb.AppendFormat("{0} {1}", 
                    damages.ElementAt(i).Value, 
                    damages.ElementAt(i).Key);
            }

            sb.Append(")");

            UISystem.Message(sb.ToString());
        }

        public static WeaponComponent GetEquippedWeapon(int entity)
        {
            var equipment = EntityManager.GetComponent<EquipmentComponent>(entity);

            if (equipment == null)
            {
                //Log.Warning("Can't get Weapon from " + DescriptionSystem.GetNameWithID(entity) + "; No EquipmentComponent attached!");
                return null;
            }

            if (equipment.Weapon == 0)
            {
                //Log.Warning("Can't get Weapon from " + DescriptionSystem.GetNameWithID(entity) + "; No weapon equipped!");
                return null;
            }

            return EntityManager.GetComponent<WeaponComponent>(equipment.Weapon);
        }

        public static ArmorComponent GetEquippedArmor(int entity)
        {
            var equipment = EntityManager.GetComponent<EquipmentComponent>(entity);

            if (equipment == null)
            {
                //Log.Warning("Can't get Armor from " + DescriptionSystem.GetNameWithID(entity) + "; No EquipmentComponent attached!");
                return null;
            }

            if (equipment.Armor == 0)
            {
                //Log.Warning("Can't get Armor from " + DescriptionSystem.GetNameWithID(entity) + "; No armor equipped!");
                return null;
            }

            return EntityManager.GetComponent<ArmorComponent>(equipment.Armor);
        }

        public static List<DamageRange> GetWeaponDamage(int entity)
        {
            var weaponC = GetEquippedWeapon(entity);

            if (weaponC == null)
            {
                return new List<DamageRange>();
            }

            return weaponC.Damages;
        }

        /// <summary>
        /// Takes in a list of damages and calculates
        /// the values after they have been mitigated by targets resistances/armor
        /// </summary>
        /// <param name="damages"> damages before any mitigation </param>
        /// <param name="target"> ID of target entity </param>
        /// <returns> list of damages after mitigation </returns>
        public static Dictionary<DamageType, int> GetDamagesAfterMitigation(Dictionary<DamageType, int> damages, int target)
        {
            var result = new Dictionary<DamageType, int>();

            var armor = GetEquippedArmor(target);

            var statC = EntityManager.GetComponent<StatComponent>(target);

            foreach (var tuple in damages)
            {
                var type = tuple.Key;
                var value = tuple.Value;

                if(type.IsElemental())
                {
                    if(statC == null)
                    {                       
                        result.Add(type, value); // Add unmitigated damage
                        continue;
                    }

                    Stat resistingStat = type.ResistedBy();
                    int percentChange = -statC.Values[resistingStat];

                    var modifiedValue = Util.ChangeValueByPercentage(value, percentChange);

                    result.Add(type, modifiedValue);                   
                }
                else if(type.IsPhysical())
                {
                    if(armor == null)
                    {
                        result.Add(type, value); // Add unmitigated damage
                        continue;
                    }

                    var modifiedValue = Util.ChangeValueByPercentage(value, -armor.PercentMitigation);
                    modifiedValue = Math.Max(0, modifiedValue - armor.FlatMitigation);

                    result.Add(type, modifiedValue);
                }
                else // should only be true damage left
                {
                    result.Add(type, value);
                }
            }

            return result;
        }
       
    }
}
