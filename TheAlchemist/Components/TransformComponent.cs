using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class TransformComponent : Component<TransformComponent>
    {
        Position position; // world position (tile)
        public Position Position
        {
            get { return position; }
            set
            {
                position = value;
                //Console.WriteLine("Position set to: " + position);
            }
        }
    }
}
