using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;

    /// <summary>
    /// Changes certain stat by amount for duration
    /// </summary>
    /// <param name="stat">affected stat</param>
    /// <param name="amount">amount of change</param>
    /// <param name="duration">duration (0 means permanent)</param>
    public delegate void StatChangedHandler(int entity, Stat stat, int amount, int duration);

    /// <summary>
    /// holds info about a stat modification for a certain entity
    /// </summary>
    class StatModification
    {
        public Stat Stat;
        public int Amount;
        public int Duration;

        public StatModification(Stat stat, int amount, int duration)
        {            
            Stat = stat;
            Amount = amount;
            Duration = duration;
        }
    }

    class StatSystem
    {
        Dictionary<int, List<StatModification>> modifications = new Dictionary<int, List<StatModification>>();

        public void ChangeStat(int entity, Stat stat, int amount, int duration)
        {
            var stats = EntityManager.GetComponent<StatComponent>(entity);

            if(stats == null)
            {
                Log.Warning("This entity doesn't have stats!");
                return;
            }

            var baseStats = stats.Values;

            if(duration < 0)
            {
                Log.Warning("Stat modification duration can't be smaller than zero!");
                return;
            }

            if(duration == 0) // permanent!
            {
                baseStats[stat] += amount;
                UISystem.Message("Some attributes of " + DescriptionSystem.GetName(entity) + " have changed! (permanent)");
                return;
            }

            // add modifications to stats and add to dict for tracking

            baseStats[stat] += amount;

            var mod = new StatModification(stat, amount, duration + 1); // add one to duration to compensate for turn of use

            modifications.TryGetValue(entity, out List<StatModification> modsOfEntity);

            if(modsOfEntity == null)
            {
                //Console.WriteLine("No mod list for this entiy yet");
                modsOfEntity = new List<StatModification>() { mod };
                modifications.Add(entity, modsOfEntity);
            }
            else
            {
                modsOfEntity.Add(mod);
            }

            UISystem.Message("Some attributes of " + DescriptionSystem.GetName(entity) + " have changed! (" + duration + " turns)");
        }

        public void TurnOver(int entity)
        {
            modifications.TryGetValue(entity, out List<StatModification> modsOfEntity);

            if(modsOfEntity == null)
            {
                return;
            }

            // unless the entity got its stat component removed this should never be null
            var statsOfEntity = EntityManager.GetComponent<StatComponent>(entity);

            var stats = statsOfEntity.Values;

            bool effectRemoved = false;
            List<StatModification> toBeRemoved = new List<StatModification>();
            for (int i = 0; i < modsOfEntity.Count; i++)
            {
                var mod = modsOfEntity[i];

                mod.Duration -= 1;

                //Console.WriteLine(mod.Duration + " left");

                if (mod.Duration == 0)
                {
                    // reverse modification and mark for removal              
                    stats[mod.Stat] -= mod.Amount;
                    toBeRemoved.Add(mod);
                    effectRemoved = true;
                }
            }

            foreach (var mod in toBeRemoved)
            {
                modsOfEntity.Remove(mod);
            }

            if(effectRemoved)
            {
                UISystem.Message("Some attributes of " + DescriptionSystem.GetName(entity) + " return to normal.");
            }
        }
    }
}
