using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheAlchemist
{
    using Components;

    // This class loads and saves JSON representations of template enemies/items/terrain...
    class GameData
    {
        public static GameData Instance = null;

        Dictionary<string, string> enemies;
        Dictionary<string, string> items;
        Dictionary<string, string> terrain;


        public void Load(string basePath)
        {
            try
            {

                Log.Message("Loading enemies...");
                enemies = LoadEntities(basePath + "/enemies.json");

                Log.Message("Loading items...");
                items = LoadEntities(basePath + "/items.json");

                //Log.Message("Loading enemies...");
                //terrain = LoadEntities(basePath + "/terrain.json");
            }
            catch (JsonException e)
            {
                Log.Error("GameData failed to load: " + e.ToString());
            }
        }


        public List<string> GetEnemyNames()
        {
            return enemies.Keys.ToList();
        }

        public List<string> GetItemNames()
        {
            return items.Keys.ToList();
        }

        public List<string> GetTerrainNames()
        {
            return terrain.Keys.ToList();
        }


        public int CreateEnemy(string name)
        {
            return EntityManager.CreateEntity(enemies[name]);
        }

        public int CreateItem(string name)
        {
            return EntityManager.CreateEntity(items[name]);
        }

        public int CreateTerrain(string name)
        {
            return EntityManager.CreateEntity(terrain[name]);
        }


        private Dictionary<string, string> LoadEntities(string path)
        {
            string rawText = File.ReadAllText(path);
            JObject entitiesJson = JObject.Parse(rawText);

            var dict = new Dictionary<string, string>();

            foreach (var entity in entitiesJson)
            {
                dict.Add(entity.Key, entity.Value.ToString());
            }

            //var dict = new Dictionary<string, List<IComponent>>();

            return dict; //Util.DeserializeObject<Dictionary<string, string>>(rawText);
        }
    }
}
