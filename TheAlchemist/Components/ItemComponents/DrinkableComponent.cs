using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TheAlchemist.Components.ItemComponents
{
    class ConsumableComponent : Component<ConsumableComponent>
    {
        public float HealthChange { get; set; } = 0f;  // damage/heal     
    }
}
