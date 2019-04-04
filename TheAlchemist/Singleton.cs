using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist
{
    class Singleton<T> where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected Singleton()
        {
            Instance = (T)this;
        }
    }
}
