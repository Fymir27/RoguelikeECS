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

    struct Damage
    {
        public DamageType Type;
        public int Min;
        public int Max;

        public override string ToString()
        {
            return String.Format("[{0}, {1} - {2}]", Type, Min, Max);
        }
    }

    class CombatSystem
    {
        public event HealthLostHandler HealthLostEvent;

        private Damage defaultDamage = new Damage()
        {
            Type = DamageType.Piercing,
            Min = 3,
            Max = 7
        };

        public void HandleBasicAttack(int attacker, int defender)
        {
            var weaponDamages = GetWeaponDamage(attacker);

            if (weaponDamages.Count == 0)
            {
                weaponDamages.Add(defaultDamage);
            }

            //Log.Data("Damages pre mitigation:\n" + Util.GetStringFromEnumerable(weaponDamages));

            var damages = GetDamagesAfterMitigation(weaponDamages, defender);

            //Log.Data("Damages after mitigation:\n" + Util.GetStringFromEnumerable(damages));

            foreach (var damage in damages)
            {
                // TODO: handle different damage types (separate events?)
                RaiseHealthLostEvent(defender, Game.Random.Next(damage.Min, damage.Max));
            }

            HandleAttackMessage(attacker, defender);

            Util.TurnOver(attacker);
        }

        void RaiseHealthLostEvent(int entity, float amount)
        {
            HealthLostEvent?.Invoke(entity, amount);
        }

        void HandleAttackMessage(int attacker, int defender)
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

        public static List<Damage> GetWeaponDamage(int entity)
        {
            var weaponC = GetEquippedWeapon(entity);

            if (weaponC == null)
            {
                return new List<Damage>();
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
        public static List<Damage> GetDamagesAfterMitigation(List<Damage> damages, int target)
        {
            var result = new List<Damage>();

            var armor = GetEquippedArmor(target);

            var statC = EntityManager.GetComponent<StatComponent>(target);

            foreach (var damage in damages)
            {
                if(damage.Type.IsElemental())
                {
                    if(statC == null)
                    {                       
                        result.Add(damage); // Add unmitigated damage
                        continue;
                    }

                    Stat resistingStat = damage.Type.ResistedBy();
                    int percentChange = -statC.Values[resistingStat];

                    Damage modifiedDamage = new Damage()
                    {
                        Type = damage.Type,
                        Min = Util.ChangeValueByPercentage(damage.Min, percentChange),
                        Max = Util.ChangeValueByPercentage(damage.Max, percentChange)
                    };

                    result.Add(modifiedDamage);
                }
                else if(damage.Type.IsPhysical())
                {
                    if(armor == null)
                    {
                        result.Add(damage); // Add unmitigated damage
                        continue;
                    }

                    Damage modifiedDamage = new Damage()
                    {
                        Type = damage.Type,
                        // make sure physical damage isn't negative because of flat mitigation
                        Min = Math.Max(0, Util.ChangeValueByPercentage(damage.Min, -armor.PercentMitigation) - armor.FlatMitigation),
                        Max = Math.Max(0, Util.ChangeValueByPercentage(damage.Max, -armor.PercentMitigation) - armor.FlatMitigation)
                    };

                    result.Add(modifiedDamage);
                }
                else // should only be true damage left
                {
                    result.Add(damage);
                }
            }

            return result;
        }
       
    }
}
