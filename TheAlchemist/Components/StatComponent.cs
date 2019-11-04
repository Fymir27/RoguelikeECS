using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    public enum Stat
    {
        // Base
        Strength,
        Intelligence,
        Dexterity,
        // Resistance / Affinity
        Fire,
        Water,
        Nature,
        Wind
    }

    class StatComponent : Component<StatComponent>
    {
        public StatComponent()
        {
            int baseStatCount = Enum.GetValues(typeof(Stat)).Length;

            Values = new Dictionary<Stat, int>(baseStatCount);

            for (int i = 0; i < baseStatCount; i++)
            {
                Values[(Stat)i] = 0;
            }
        }

        public StatComponent(Dictionary<Stat, int> stats) : this()
        {
            foreach (var tuple in stats)
            {
                Values[tuple.Key] = tuple.Value;
            }
        }

        public Dictionary<Stat, int> Values { get; set; }
    }
}
