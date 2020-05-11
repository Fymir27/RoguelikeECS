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
        public struct TileTemplate
        {
            public string Terrain;
            public string Structure;
            public string[] Items;
            public string Character;
        }

        public struct RoomTemplate
        {
            public List<char[,]> Layouts;
            public Dictionary<char, TileTemplate> SpecialTiles;
        }

        public static GameData Instance = null;

        // string json = Entities[type][name]
        public Dictionary<EntityType, Dictionary<string, string>> Entities = new Dictionary<EntityType, Dictionary<string, string>>();

        public Dictionary<string, string> TemplateItems = new Dictionary<string, string>();

        public Dictionary<string, RoomTemplate> RoomTemplates = new Dictionary<string, RoomTemplate>();

        public void Load(string contentPath)
        {
            try
            {
                Log.Message("Loading terrain...");
                Entities[EntityType.Terrain] = LoadEntities(contentPath + "/terrain.json");

                Log.Message("Loading items...");
                Entities[EntityType.Item] = LoadEntities(contentPath + "/items.json");
                TemplateItems = LoadEntities(contentPath + "/templateItems.json");

                Log.Message("Loading structures...");
                Entities[EntityType.Structure] = LoadEntities(contentPath + "/structures.json");             

                Log.Message("Loading characters...");
                Entities[EntityType.Character] = LoadEntities(contentPath + "/characters.json");

                Log.Message("Loading room templates...");
                LoadRoomTemplates(contentPath);
            }
            catch (JsonException e)
            {
                Log.Error("GameData failed to load: " + e.ToString());
                throw e;
            }
        }

        public void LoadRoomTemplates(string contentPath)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(contentPath + "/RoomTemplates");
                FileInfo[] files = dir.GetFiles("*.json");
                foreach (var file in files)
                {
                    string json = File.ReadAllText(file.FullName);
                    //Log.Data(file.Name + ": \n" + json);
                    var obj = JObject.Parse(json);
                    var layouts = obj.GetValue("layouts").Children();

                    RoomTemplate room;
                    room.Layouts = new List<char[,]>();
                    room.SpecialTiles = new Dictionary<char, TileTemplate>();

                    HashSet<char> placeholders = new HashSet<char>();

                    foreach (var serializedLayout in layouts)
                    {                        
                        var lines = serializedLayout.ToObject<string[]>();
                        if(lines.Length == 0)
                        {
                            Log.Error("Empty layout in " + file.Name);
                            break;
                        }
                        int width = lines[0].Length;
                        int height = lines.Length;
                        var layout = new char[width, height];
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                char c = lines[y][x];
                                layout[x, y] = c;
                                if (char.IsLetter(c))
                                {
                                    placeholders.Add(c);
                                }
                            }
                        }
                        room.Layouts.Add(layout);
                    }
                    foreach (char ph in placeholders)
                    {
                        if(!obj.ContainsKey(ph.ToString()))
                        {
                            Log.Error($"Placeholder '{ph}' not described!");
                            continue;
                        }

                        // TODO: parse TileTemplates
                        room.SpecialTiles.Add(ph, new TileTemplate());
                    }
                    if(placeholders.Count == room.SpecialTiles.Count)
                    {
                        RoomTemplates.Add(Path.GetFileNameWithoutExtension(file.Name), room);
                    }
                    else
                    {
                        Log.Error($"Room template '{file.Name}' failed to load!");
                    }
                }
            }
            catch (IOException e)
            {
                Log.Error($"An error occured while loading Room Templates from {contentPath}: {e.Message}");
            }
            catch (JsonException e)
            {
                Log.Error($"An error occured while parsing Room Templates: {e.Message}");
            }
            catch (Exception e)
            {
                Log.Error($"Something went wrong: {e.Message}");
            }
            Log.Message($"Room templates loaded ({RoomTemplates.Count}): {Util.GetStringFromEnumerable(RoomTemplates.Keys)}");
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

        public int CreateTemplateItem(string name)
        {
            return TryCreateEntity(TemplateItems, name, EntityType.Item);
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
            try
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
            catch(Exception e)
            {
                Log.Error("Failed to load entities from " + path);
                Log.Data(e.Message);
                throw e;
            }
        }
    }
}
