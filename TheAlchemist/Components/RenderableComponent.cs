using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace TheAlchemist.Components
{
    class RenderableComponent : Component<RenderableComponent>
    {
        public bool Visible { get; set; } = true;
        public Texture2D Texture { get; set; }
        public Vector2 Position
        {
            get
            {
                return EntityManager
                    .GetComponentOfEntity<TransformComponent>(entityID)
                    .Position;
            }
        }
    }
}
