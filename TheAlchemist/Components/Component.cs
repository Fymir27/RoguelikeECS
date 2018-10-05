using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class Component<T> : IComponent
    {
        static int typeID = Util.TypeID<IComponent>.Get();
        int componentID = Util.UniqueID<IComponent>.Get();

        int IComponent.TypeID { get { return typeID; } }
        int IComponent.ComponentID { get { return componentID; } }

        public override string ToString()
        {
           return GetType() + ", TypeID: " + typeID + ", ComponentID: " + componentID;
        }
    }
}
