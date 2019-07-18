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
    using Systems;

    // This class loads and saves JSON representations of template enemies/items/terrain...
    class GameData
    {
        public static GameData Instance = null;

        public Dictionary<string, string> Enemies { get; private set; }
        public Dictionary<string, string> Items { get; private set; }
        public Dictionary<string, string> Terrain { get; private set; }
        //public Dictionary<Craftable, >


        public void Load(string basePath)
        {
            try
            {

                Log.Message("Loading enemies...");
                Enemies = LoadEntities(basePath + "/enemies.json");

                Log.Message("Loading items...");
                Items = LoadEntities(basePath + "/items.json");

                Log.Message("Loading terrain...");
                Terrain = LoadEntities(basePath + "/terrain.json");
            }
            catch (JsonException e)
            {
                Log.Error("GameData failed to load: " + e.ToString());
            }
        }


        public List<string> GetEnemyNames()
        {
            return Enemies.Keys.ToList();
        }

        public List<string> GetItemNames()
        {
            return Items.Keys.ToList();
        }

        public List<string> GetTerrainNames()
        {
            return Terrain.Keys.ToList();
        }

        public int TryCreateEntity(Dictionary<string, string> dict, string name)
        {
            try
            {
                return EntityManager.CreateEntity(dict[name]);
            }
            catch (KeyNotFoundException e)
            {
                Log.Error("GameData.CreateEntity: This entity doesn't exist! ->" + name);
                return 0;
            }
        }

        public int CreateEnemy(string name)
        {
            return TryCreateEntity(Enemies, name);
        }

        public int CreateItem(string name)
        {
            return TryCreateEntity(Items, name);
        }

        public int CreateTerrain(string name)
        {
            return TryCreateEntity(Terrain, name);
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
