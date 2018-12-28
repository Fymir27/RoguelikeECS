using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class StatComponent : Component<StatComponent>
    {
        public int Strength { get; set; } = 0;
        public int Intelligence { get; set; } = 0;
        public int Dexterity { get; set; } = 0;
    }
}
