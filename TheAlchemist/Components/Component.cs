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
        protected static Type type = typeof(T);

        int IComponent.TypeID { get => typeID; }
        int IComponent.ComponentID { get => componentID; }
        Type IComponent.Type { get => type; }

        public int EntityID
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
       
        public Component()
        {

        }

        /*
        public override string ToString()
        {
           return GetType() + ", TypeID: " + typeID + ", ComponentID: " + componentID;
        }
        */

        public T GetConcreteComponent()
        {
            return (T)Convert.ChangeType(this, typeof(T));
        }
    }
}
