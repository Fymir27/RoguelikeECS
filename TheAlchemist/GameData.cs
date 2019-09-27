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

        // string json = Entities[type][name]
        public Dictionary<EntityType, Dictionary<string, string>> Entities = new Dictionary<EntityType, Dictionary<string, string>>();

        public void Load(string basePath)
        {
            try
            {
                Log.Message("Loading terrain...");
                Entities[EntityType.Terrain] = LoadEntities(basePath + "/terrain.json");

                Log.Message("Loading structures...");
                Entities[EntityType.Structure] = LoadEntities(basePath + "/structures.json");

                Log.Message("Loading items...");
                Entities[EntityType.Item] = LoadEntities(basePath + "/items.json");

                Log.Message("Loading characters...");
                Entities[EntityType.Character] = LoadEntities(basePath + "/characters.json");                          
            }
            catch (JsonException e)
            {
                Log.Error("GameData failed to load: " + e.ToString());
            }
        }


        public List<string> GetCharacterNames()
        {
            return Entities[EntityType.Character].Keys.ToList();
        }

        public List<string> GetItemNames()
        {
            return Entities[EntityType.Item].Keys.ToList();
        }

        public List<string> GetTerrainNames()
        {
            return Entities[EntityType.Terrain].Keys.ToList();
        }

        public List<string> GetStructureNames()
        {
            return Entities[EntityType.Structure].Keys.ToList();
        }

        public int TryCreateEntity(Dictionary<string, string> dict, string name, EntityType type)
        {
            try
            {
                return EntityManager.CreateEntity(dict[name], type);
            }
            catch (KeyNotFoundException)
            {
                Log.Error("GameData.CreateEntity: This entity doesn't exist! ->" + name);
                return 0;
            }
        }

        public int CreateCharacter(string name)
        {
            return TryCreateEntity(Entities[EntityType.Character], name, EntityType.Character);
        }

        public int CreateItem(string name)
        {
            return TryCreateEntity(Entities[EntityType.Item], name, EntityType.Item);
        }

        public int CreateTerrain(string name)
        {
            return TryCreateEntity(Entities[EntityType.Terrain], name, EntityType.Terrain);
        }

        public int CreateStructure(string name)
        {
            return TryCreateEntity(Entities[EntityType.Structure], name, EntityType.Structure);
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

            return dict; //Util.DeserializeObject<Dictionary<string, string>>(rawText);
        }
    }
}
