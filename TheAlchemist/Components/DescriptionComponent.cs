using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class DescriptionComponent : Component<DescriptionComponent>
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
