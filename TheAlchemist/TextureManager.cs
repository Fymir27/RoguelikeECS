using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

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

        public static void AddTexture(Texture2D texture, string name)
        {
            if(texture == null)
            {
                Log.Error("Texture wasn't loaded correctly! => " + name);
                return;
            }
            textures.Add(name, texture);
        }

        public static void LoadTextures(string[] textures)
        {
            foreach (var texture in textures)
            {
                AddTexture(contentManager.Load<Texture2D>(texture), texture);
            }
        }

        public static void LoadAllTextures(string directory)
        {
            Log.Message("Loading Textures...");

            IEnumerable<string> pngFiles = Directory.GetFiles(@".\Content\" + directory + "\\");

            StringBuilder sb = new StringBuilder();

            foreach (var file in pngFiles)
            {
                string name = Path.GetFileName(file);
                name = name.Substring(0, name.Length - 4); // remove file extension

                // add directory to name
                sb.Append(directory)
                  .Append("\\")
                  .Append(name);

                //Log.Data(sb.ToString());

                AddTexture(contentManager.Load<Texture2D>(sb.ToString()), name);

                sb.Clear();
            }

            //Log.Data(Util.GetStringFromEnumerable(textures.Keys));                             
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
            Log.Data("Textures loaded: " + Util.GetStringFromCollection(textures));
            //foreach(var item in textures)
            //{
            //    Console.WriteLine(item.Key);
            //}
        }
    }
}
