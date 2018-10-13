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
        Vector2 position; // world position (tile)
        public Vector2 Position
        {
            get { return position; }
            set
            {
                position = value;
                Console.WriteLine("Position set to: " + position);
            }
        }
    }
}
