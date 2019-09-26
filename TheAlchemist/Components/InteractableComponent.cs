using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class InteractableComponent : Component<InteractableComponent>
    {
        public bool ChangeSolidity { get; set; }

        public bool ChangeTexture { get; set; }
        public string AlternateTexture { get; set; }

        public bool GrantsItems { get; set; }
        public List<int> Items { get; set; }
    }
}
