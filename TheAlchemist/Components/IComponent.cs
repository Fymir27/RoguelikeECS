using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    interface IComponent
    {
        int ComponentID { get; }
        int TypeID { get; }
    }
}
