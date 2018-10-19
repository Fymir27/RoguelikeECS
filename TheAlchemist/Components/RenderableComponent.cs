using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace TheAlchemist.Components
{
    class RenderableComponent<T> : Component<T> where T : Component<T>
    {
        public bool Visible { get; set; } = true;

        // screen position
        // if this property is set manually it will have that fixed value
        // otherwise the position will always be calculated on the world position of the entity
        public Vector2 Position
        {
            get
            {
                if (hasFixedPosition)
                    return fixedPosition;

                Vector2 worldPos = EntityManager
                    .GetComponentOfEntity<TransformComponent>(entityID)
                    .Position;
                int tileSize = Util.TileSize;
                return new Vector2(worldPos.X * tileSize, worldPos.Y * tileSize); // converted to screen pos
            }
            set
            {
                hasFixedPosition = true;
                fixedPosition = value;
            }
        }

        private bool hasFixedPosition = false;
        private Vector2 fixedPosition;
    }

    class RenderableSpriteComponent : RenderableComponent<RenderableSpriteComponent>
    {
        public string Texture { get; set; } // name of texture
    }

    public delegate string StringGetter();
    
    class RenderableTextComponent : RenderableComponent<RenderableTextComponent>
    {
        public string Text
        {
            get
            {
                if (fixedText != null)
                    return fixedText;
                if (GetTextFrom != null)
                    return GetTextFrom();
                return "";
            }
            set
            {
                fixedText = value;
            }
        }


        public StringGetter GetTextFrom = null;

        public SpriteFont Font
        {
            get
            {
                if (font == null)
                    font = Util.DefaultFont;
                return font;
            }
            set
            {
                font = value;
            }
        }

        private SpriteFont font = null;
        private string fixedText = null;
    }
}
