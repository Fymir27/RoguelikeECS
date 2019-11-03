using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    public enum BaseStat
    {
        Strength,
        Intelligence,
        Dexterity
    }

    public enum ElementalResistance
    {
        Fire,
        Water,
        Nature,
        Wind
    }

    class StatComponent : Component<StatComponent>
    {
        public StatComponent()
        {
            int baseStatCount = Enum.GetValues(typeof(BaseStat)).Length;

            BaseStats = new Dictionary<BaseStat, int>(baseStatCount);

            for (int i = 0; i < baseStatCount; i++)
            {
                BaseStats[(BaseStat)i] = 0;
            }

            int resistanceStatCount = Enum.GetValues(typeof(ElementalResistance)).Length;

            ResistanceStats = new Dictionary<ElementalResistance, int>(resistanceStatCount);

            for (int i = 0; i < resistanceStatCount; i++)
            {
                ResistanceStats[(ElementalResistance)i] = 0;
            }
        }

        public StatComponent(Dictionary<BaseStat, int> baseStats, Dictionary<ElementalResistance, int> resistanceStats)
        {
            this.BaseStats = baseStats;
            this.ResistanceStats = resistanceStats;
        }

        public Dictionary<BaseStat, int> BaseStats { get; set; }
        public Dictionary<ElementalResistance, int> ResistanceStats { get; set; }
    }
}
