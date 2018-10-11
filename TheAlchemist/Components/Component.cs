using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class Component<T> : IComponent
    {      
        public static int TypeID { get { return typeID; } }

        protected static int typeID = Util.TypeID<IComponent>.Get();
        protected int componentID = Util.UniqueID<IComponent>.Get();
        protected int entityID = 0;

        int IComponent.TypeID { get { return typeID; } }
        int IComponent.ComponentID { get { return componentID; } }
        int IComponent.EntityID
        {
            get
            {
                return entityID;
            }
            set
            {
                entityID = value;
            }
        }
        

        public override string ToString()
        {
           return GetType() + ", TypeID: " + typeID + ", ComponentID: " + componentID;
        }
    }
}
