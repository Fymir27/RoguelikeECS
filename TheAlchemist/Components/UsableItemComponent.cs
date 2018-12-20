using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class UsableItemComponent : Component<UsableItemComponent>
    {    
        public enum EffectType
        {
            // Ressources
            Health,
            Mana,

            // Stats
            Str,
            Dex,
            Int,

            // Elemental
            Ice,
            Fire
        }

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
    }
}
