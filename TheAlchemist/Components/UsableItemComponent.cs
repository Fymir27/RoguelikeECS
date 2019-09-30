using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TheAlchemist.Components
{
    using static TheAlchemist.Systems.ItemSystem;

    class UsableItemComponent : Component<UsableItemComponent>
    {
        public struct ItemEffect
        {
            public EffectType Type;
            public bool Harmful;
            public int Potency;
            public ItemEffect(EffectType type, bool harmful, int potency)
            {
                Type = type;
                Harmful = harmful;
                Potency = potency;
            }

            public override string ToString()
            {
                return "(" + Type.ToString() + ", " + Harmful + ", " + Potency + ")";
            }
        }

        public List<ItemEffect> Effects { get; set; }
        public List<Systems.ItemUsage> Usages { get; set; }

        public bool BreakOnThrow { get; set; } = true;
    }

    class ConsumableItemComponent : UsableItemComponent
    {

    }

    class ThrowableItemComponent : UsableItemComponent
    {

    }
}
