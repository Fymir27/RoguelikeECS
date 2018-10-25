using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class DoorComponent : Component<DoorComponent>
    {
        public bool Open { get; set; } = false;
        public string TextureOpen { get; set; }
        public string TextureClosed { get; set; }
    }
}
