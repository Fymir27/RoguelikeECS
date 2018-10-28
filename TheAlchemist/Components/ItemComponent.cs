using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class ItemComponent : Component<ItemComponent>
    {
        public int Count
        {
            get => count;
            set
            {
                if(value > MaxCount)
                {
                    Log.Error("Item failed stacking!");
                    throw new ArgumentException("Item failed stacking!", "ItemComponent.Count");
                }
                count = value;
            }
        }

        public int MaxCount { get; set; } = 1;

        public int Value { get; set; }    // of one
        public float Weight { get; set; } // of one

        public bool OnGround { get; set; } = true;

        int count = 1;
    }
}
