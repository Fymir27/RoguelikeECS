using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    interface IComponent
    {
        int ComponentID { get; }   // unique for every existing component
        int TypeID { get; }        // unique for every type of component
        int EntityID { get; set; } // ID of entity this component is attached to
    }
}
