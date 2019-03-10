using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class DescriptionComponent : Component<DescriptionComponent>
    {
        public enum MessageType
        {
            // terrain
            StepOn,
            Interact,

            // items
            Use,
            Throw
        }

        public string Name { get; set; }
        public string Description { get; set; }

        public Dictionary<MessageType, string> SpecialMessages { get; set; }
    }
}
