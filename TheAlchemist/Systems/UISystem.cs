﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    public delegate void InventoryToggledHandler();

    class UISystem
    {
        public bool InventoryOpen { get; set; } = false;

        public void HandleInventoryToggled()
        {
            UI.InventoryOpen = !UI.InventoryOpen;
        }
    }
}
