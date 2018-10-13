using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class ColliderComponent : Component<ColliderComponent>
    {
        bool Solid { get; set; } = true;

    }
}
