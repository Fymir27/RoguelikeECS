using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist
{
    static class TextureManager
    {
        static Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static ContentManager contentManager;

        public static void Init(ContentManager contentManager)
        {
            TextureManager.contentManager = contentManager;
        }

        public static void AddTexture(Texture2D texture)
        {
            string name = texture.Name;
            if(texture == null)
            {
                Console.WriteLine("Texture wasn't loaded correctly! => " + name);
                return;
            }
            textures.Add(name, texture);
        }

        public static void LoadTextures(string[] textures)
        {
            foreach (var texture in textures)
            {
                AddTexture(contentManager.Load<Texture2D>(texture));
            }
        }

        public static Texture2D GetTexture(string name)
        {
            try
            {
                return textures[name];
            }
            catch(KeyNotFoundException)
            {
                Log.Error("No such texture loaded: " + name);
                throw;
            }
        }

        public static void PrintLoadedTextures()
        {
            foreach(var item in textures)
            {
                Console.WriteLine(item.Key);
            }
        }
    }
}
