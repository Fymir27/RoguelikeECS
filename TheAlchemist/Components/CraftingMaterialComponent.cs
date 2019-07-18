using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class CraftingMaterialComponent : Component<CraftingMaterialComponent>
    {
        public enum MaterialType
        {
            Potion,
            Plant,
            Mineral
            //...
        }

        public MaterialType Type { get; set; }

        public List<UsableItemComponent.ItemEffect> Effects { get; set; }

        public int Instability { get; set; } //TODO:
    }
}
