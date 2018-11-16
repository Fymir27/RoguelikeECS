using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components.ItemComponents
{
    abstract class UsableComponent : Component<UsableComponent>
    {
        public string Action { get; set; } = "Use";

        protected UsableComponent(string action)
        {
            Action = action;
        }
    }
}
