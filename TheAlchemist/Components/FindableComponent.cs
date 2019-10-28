using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class FindableComponent : Component<FindableComponent>
    {
        public int[,] DistanceMap { get; set; }
        public Position LastKnownPosition { get; set; }
    }
}
