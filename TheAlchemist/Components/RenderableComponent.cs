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
        //public Texture2D Texture { get; set; }
        public string Texture { get; set; } // name of texture
        public Vector2 Position
        {
            get
            {
                Vector2 worldPos = EntityManager
                    .GetComponentOfEntity<TransformComponent>(entityID)
                    .Position;
                int tileSize = Util.TileSize;
                return new Vector2(worldPos.X * tileSize, worldPos.Y * tileSize); // converted to screen pos
            }
        }
    }
}
