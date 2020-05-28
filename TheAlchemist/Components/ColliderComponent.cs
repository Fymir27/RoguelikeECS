using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class ColliderComponent : Component<ColliderComponent>
    {
        public bool Solid { get; set; }
        public bool Opaque { get; set; }

        [Newtonsoft.Json.JsonConstructor]
        public ColliderComponent(bool solid = true)
        {
            EntityID = 0;
            Solid = solid;
            Opaque = solid;
        }
    }
}
