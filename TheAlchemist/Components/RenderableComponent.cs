using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    using Systems; // for access to description system

    class RenderableComponent<T> : Component<T> where T : Component<T>
    {
        public class RenderLayer
        {
            public static readonly int Terrain = 0;
            public static readonly int Item = 1;
            public static readonly int Character = 2;
        }

        public bool Visible { get; set; } = true;
        public int Layer { get; set; } = 0;


        // screen position
        // if this property is set manually it will have that fixed value
        // otherwise the position will always be calculated on the world position of the entity
        [Newtonsoft.Json.JsonIgnore]
        public Vector2 Position
        {
            get
            {
                if (hasFixedPosition)
                    return fixedPosition;

                try
                {
                    Position worldPos = EntityManager
                        .GetComponent<TransformComponent>(entityID)
                        .Position;
                    return Util.WorldToScreenPosition(worldPos);
                }
                catch (NullReferenceException)
                {
                    Log.Error("RenderableComponent has neither a fixed Position nor can it find a TransformComponent!" + DescriptionSystem.GetName(EntityID) + " (Entity " + EntityID + ", Component " + componentID + ")");
                    throw;
                }
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
        public Color Tint { get; set; } = Color.White;
    }

    class RenderableSpriteArrayComponent : RenderableComponent<RenderableSpriteArrayComponent>
    {
        public string[] Textures { get; set; }
        public Color[] Tints { get; set; }
    }

    public delegate string StringGetter();

    class RenderableTextComponent : RenderableComponent<RenderableTextComponent>
    {
        // if this is set manually the component will have a fixed text
        // dynamic text can be configured by setting GetTextFrom instead
        public string Text
        {
            get
            {
                if (fixedText != null)
                    return fixedText;
                if (GetTextFrom != null)
                    return GetTextFrom();
                Log.Warning("No renderable text available! " + DescriptionSystem.GetName(EntityID) + " (Entity " + EntityID + ", Component " + componentID + ")");
                return fixedText = "[MISSING TEXT]";
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
