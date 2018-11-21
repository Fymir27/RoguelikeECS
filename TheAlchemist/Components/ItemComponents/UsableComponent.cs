using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Input;

namespace TheAlchemist.Components.ItemComponents
{
    abstract class UsableComponent : Component<UsableComponent>
    {
        public string Action { get; set; } = "Use";
        public Keys Key { get; set; }

        protected UsableComponent(string action, Keys key)
        {
            Action = action;
            Key = key;
        }
    }
}
