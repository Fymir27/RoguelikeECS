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
        }

        public List<ItemEffect> Effects;
        public List<Systems.ItemUsage> Usages;

        public bool BreakOnThrow = true;
    }
}
