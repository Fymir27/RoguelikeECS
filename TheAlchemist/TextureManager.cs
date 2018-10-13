using Microsoft.Xna.Framework.Graphics;
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

        public static Texture2D GetTexture(string name)
        { 
            return textures.ContainsKey(name) ? textures[name] : null;
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
