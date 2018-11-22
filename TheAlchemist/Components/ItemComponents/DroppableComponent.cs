﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components.ItemComponents
{
    class DroppableComponent : UsableComponent
    {
        public DroppableComponent() : base("Drop", Microsoft.Xna.Framework.Input.Keys.D)
        {
            Handler = (itemSystem, character) => itemSystem.DropItem(character, this);
        }
    }
}
