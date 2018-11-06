using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class InventoryComponent : Component<InventoryComponent>
    {       
        public int Capacity { get; set; } 

        public List<int> Items
        {
            get => items;

            set
            {
                if(value.Count > Capacity)
                {
                    Log.Error("Inventory too small!");
                    throw new ArgumentException("Inventory too small!", "InventoryComponent.Items");
                }
                items = value;
            }
        }

        public bool Full
        {
            get => items.Count >= Capacity;
        }

        List<int> items = new List<int>();
    }
}
