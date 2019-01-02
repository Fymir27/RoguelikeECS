using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    public enum Stat
    {
        Strength,
        Intelligence,
        Dexterity
    }

    class StatComponent : Component<StatComponent>
    {
        public StatComponent()
        {
            stats = new Dictionary<Stat, int>();
        }

        public StatComponent(Dictionary<Stat, int> initStats)
        {
            stats = initStats;
        }

        Dictionary<Stat, int> stats;
        public int this[Stat stat]
        {
            get
            {
                int val = 0;
                stats.TryGetValue(stat, out val);
                return val;
            }
            set
            {
                stats[stat] = value;
            }
        }
    }
}
