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

    public struct EntityTemplate
    {
        public string Name;
        public int Weight;

        // only used for item quantities
        public int Min;
        public int Max;

        public static EntityTemplate Empty = new EntityTemplate("");

        [JsonConstructor]
        public EntityTemplate(string name, int weight = 1, int min = 1, int max = 1)
        {
            Name = name;
            if(weight == 0)
            {
                weight = 1;
            }
            Weight = weight;
            Min = min;
            Max = max;
        }

        public static void WeightZeroWarning()
        {
            Log.Warning("Weight of EntityTemplate can't be 0! (set to 1)");
        }
    }

    public struct TileTemplate
    {
        public SortedList<int, EntityTemplate> Terrains;
        public SortedList<int, EntityTemplate> Structures;
        public SortedList<int, EntityTemplate> Items;
        public SortedList<int, EntityTemplate> Characters;

        [JsonConstructor]
        public TileTemplate(EntityTemplate[] terrains, EntityTemplate[] structures, EntityTemplate[] items, EntityTemplate[] characters)
        {
            SortedList<int, EntityTemplate> AccumulateWeights(EntityTemplate[] list)
            {
                int acc = 0;
                var result = new SortedList<int, EntityTemplate>();
                if(list == null)
                {
                    return result;
                }
                foreach (var template in list)
                {
                    if(template.Name == null)
                    {
                        acc++;
                        result.Add(acc, EntityTemplate.Empty);
                        continue;
                    }
                   

                    if(template.Weight == 0)
                    {
                        Log.Warning($"EntityTemplate with name '{template.Name}' has Weight 0 and will be ignored!");
                        continue;
                    }

                    acc += template.Weight;
                    result.Add(acc, template);
                }
                return result;
            }

            Terrains = AccumulateWeights(terrains);
            Structures = AccumulateWeights(structures);
            Items = AccumulateWeights(items);
            Characters = AccumulateWeights(characters);           
        }
    }

    public struct RoomTemplate
    {
        public List<char[,]> Layouts;
        public Dictionary<char, TileTemplate> CustomTiles;
        public static Dictionary<char, TileTemplate> DefaultTiles = new Dictionary<char, TileTemplate>()
            {
                { '#', new TileTemplate() { Terrains = new SortedList<int, EntityTemplate>() { { 1, new EntityTemplate("wall") } } } },
                { '+', new TileTemplate() { Structures = new SortedList<int, EntityTemplate>() { { 1, new EntityTemplate("door") } } } },
                { ' ', new TileTemplate() { } }
            };
    }

    // This class loads and saves JSON representations of template enemies/items/terrain...
    class GameData
    {
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
                Entities[EntityType.Character] = new Dictionary<string, string>();
                foreach (var filepath in Directory.GetFiles(contentPath + "/Entities/Characters", "*.json"))
                {                    
                    var deserialized = LoadEntities(filepath);
                    foreach (var keyValPair in deserialized)
                    {
                        Entities[EntityType.Character].Add(keyValPair.Key, keyValPair.Value);
                    }
                }                

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
                    room.CustomTiles = new Dictionary<char, TileTemplate>();

                    HashSet<char> placeholders = new HashSet<char>();

                    int layoutIndex = 0;
                    bool formatError = false;
                    foreach (var serializedLayout in layouts)
                    {
                        layoutIndex++;
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
                                try
                                {
                                    char c = lines[y][x];
                                    layout[x, y] = c;
                                    if (char.IsLetter(c))
                                    {
                                        placeholders.Add(c);
                                    }
                                    else if (!RoomTemplate.DefaultTiles.ContainsKey(c))
                                    {
                                        Log.Error("Unknown symbol in room layout: " + c);
                                    }
                                } 
                                catch(IndexOutOfRangeException)
                                {                                    
                                    formatError = true;
                                    layout[x, y] = ' ';
                                }
                            }
                        }
                        room.Layouts.Add(layout);

                        if(formatError)
                        {
                            Log.Error($"Room format with index {layoutIndex} in file {file.Name}!");
                        }
                    }
                    foreach (char ph in placeholders)
                    {
                        if(!obj.ContainsKey(ph.ToString()))
                        {
                            Log.Error($"Placeholder '{ph}' not described!");
                            continue;
                        }

                        string tileTemplateSerialized = obj.GetValue(ph.ToString()).ToString();
                        room.CustomTiles.Add(ph, JsonConvert.DeserializeObject<TileTemplate>(tileTemplateSerialized));
                    }
                    if(placeholders.Count == room.CustomTiles.Count)
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
            if (name.Length == 0) return 0;
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

        public static void WriteDummyToFile()
        {
            EntityTemplate empty = new EntityTemplate();
            EntityTemplate bat = new EntityTemplate("bat", 1);
            EntityTemplate spider = new EntityTemplate("spider", 3);
            EntityTemplate wall = new EntityTemplate("wall");
            EntityTemplate healthPotion = new EntityTemplate("healthPotion", 0, 1, 3);
            TileTemplate test = new TileTemplate(
                new EntityTemplate[] { wall, empty },
                new EntityTemplate[] { },
                new EntityTemplate[] { healthPotion, empty },
                new EntityTemplate[] { bat, spider }
            );

            string str = Util.SerializeObject(test, true);
            Log.Data(str);
        }
    }
}
