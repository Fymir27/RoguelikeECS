using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TheAlchemist.Components.ItemComponents
{
    using Systems;

    class ConsumableComponent : UsableComponent//<ConsumableComponent>
    {
        public ItemSystem.ItemEffectDescription[] Effects { get; set; }  

        public ConsumableComponent() : base("Consume", Microsoft.Xna.Framework.Input.Keys.C)
        {
            Handler = (itemSystem, character) => itemSystem.ConsumeItem(character, this);
        }
    }
}

/*
        public class ValueArray<T>
        {
            T[] values;
            bool[] valuePresent;

            public ValueArray(int size)
            {
                values = new T[size];
                valuePresent = new bool[size];
            }

            public T this[int i]
            {
                get
                {
                    if (valuePresent[i])
                        return values[i];
                    Log.Error("Value " + i + " is not present!");// + " of " + DescriptionSystem.GetNameWithID());
                    return default(T);
                }
                set
                {
                    values[i] = value;
                    valuePresent[i] = true;
                }
            }          
        }
        */
