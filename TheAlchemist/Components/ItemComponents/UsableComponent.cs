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
        public string Action { get; private set; } = "Use";
        public Keys Key { get; private set; }
        public Action<Systems.ItemSystem, int> Handler;

        protected UsableComponent(string action, Keys key)//, Action<int> handler)
        {
            Action = action;
            Key = key;
        }
    }
}
