using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TheAlchemist
{
    using Components;
    using Newtonsoft.Json;

    class Floor
    {
        [JsonProperty]
        int width, height;
        // one terrain entity per tile
        // one character entity per tile
        // a list of items per tile
        [JsonProperty]
        int[,] terrain;
        [JsonProperty]
        int[,] characters;
        [JsonProperty]
        List<int>[,] items;

        public Floor(string path)
        {
            StreamReader file = new StreamReader(path);

            List<List<int>> tmpTerrain = new List<List<int>>();

            int y = 0;
            while(!file.EndOfStream)
            {
                int x = 0;
                var row = file.ReadLine();
                tmpTerrain.Add(new List<int>());
                foreach (var tile in row)
                {
                    switch(tile)
                    {
                        case '#':
                            tmpTerrain[y].Add(CreateWall(new Vector2(x, y)));
                            break;

                        case '+':
                            tmpTerrain[y].Add(CreateDoor(new Vector2(x, y)));
                            break;

                        default:
                            tmpTerrain[y].Add(0);
                            break;
                    }
                    x++;
                }
                if(x > 0)
                    y++;
            }

            this.width = tmpTerrain[0].Count;
            this.height = y;

            terrain = new int[width, height];
            characters = new int[width, height];
            items = new List<int>[width, height]; // don't initialize each List yet to save space!

            for (y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    terrain[x, y] = tmpTerrain[y][x];
                }
            }

            characters[1, 1] = Util.PlayerID = CreatePlayer(new Vector2(1, 1));

            int testEnemy = EntityManager.CreateEntity();
            int testArmor = EntityManager.CreateEntity();
            int testWeapon = EntityManager.CreateEntity();

            EntityManager.AddComponentToEntity(testArmor, new ArmorComponent() { FlatMitigation = 2 });
            EntityManager.AddComponentToEntity(testWeapon, new WeaponComponent() { Damage = 10 });

            List<IComponent> enemyComponents = new List<IComponent>()
            {
                new NPCComponent(),
                new HealthComponent() { Amount = 20, Max = 20, RegenerationAmount = 1 },
                new EquipmentComponent() { Weapon = testWeapon , Armor = testArmor },
                new TransformComponent() { Position = new Vector2(3, 3) },
                new ColliderComponent() { Solid = false },
                new RenderableSpriteComponent() { Visible = true, Texture = "enemy" }
            };

            EntityManager.AddComponentsToEntity(testEnemy, enemyComponents);



            characters[3, 3] = testEnemy;
        }

        public int GetTerrain(Vector2 pos)
        {
            if(IsOutOfBounds(pos))
            {
                return 0;
            }
            return terrain[(int)pos.X, (int)pos.Y];
        }

        public int GetCharacter(Vector2 pos)
        {
            if(IsOutOfBounds(pos))
            {
                return 0;
            }
            return characters[(int)pos.X, (int)pos.Y];
        }

        public int[] GetItems(Vector2 pos)
        {
            if (IsOutOfBounds(pos))
            {
                return new int[0];
            }
            var itemList = items[(int)pos.X, (int)pos.Y];
            if(itemList == null)
            {
                return new int[0];
            }
            return itemList.ToArray();
        }

        public void SetTerrain(Vector2 pos, int terrain)
        {
            if(IsOutOfBounds(pos))
            {
                return;
            }
            this.terrain[(int)pos.X, (int)pos.Y] = terrain;
        }

        public void SetCharacter(Vector2 pos, int character)
        {
            if(IsOutOfBounds(pos))
            {
                return;
            }
            characters[(int)pos.X, (int)pos.Y] = character;
        }

        public void AddItems(Vector2 pos, List<int> items)
        {
            if(IsOutOfBounds(pos))
            {
                return;
            }

            var existingItems = this.items[(int)pos.X, (int)pos.Y];
            if(existingItems == null)
            {
                existingItems = items;
            }
            else
            {
                existingItems.AddRange(items);
            }
        }

        public Floor(int width, int height)
        {
            this.width = width;
            this.height = height;

            terrain = new int[width, height];
            characters = new int[width, height];
            items = new List<int>[width, height]; // don't initialize each List yet to save space!

            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < height; x++)
                {
                    // create sourrounding wall
                    if(y == 0 || y == height - 1)
                    {
                        terrain[x, y] = CreateWall(new Vector2(x, y));
                    }
                    else if(x == 0 || x == width - 1)
                    {
                        terrain[x, y] = CreateWall(new Vector2(x, y));
                    }                      
                }
            }

            characters[1, 1] = Util.PlayerID = CreatePlayer(new Vector2(1, 1));

            int testEnemy = EntityManager.CreateEntity();
            int testArmor = EntityManager.CreateEntity();
            int testWeapon = EntityManager.CreateEntity();

            EntityManager.AddComponentToEntity(testArmor, new ArmorComponent() { FlatMitigation = 2 });
            EntityManager.AddComponentToEntity(testWeapon, new WeaponComponent() { Damage = 10 });

            List<IComponent> enemyComponents = new List<IComponent>()
            {
                new NPCComponent(),
                new HealthComponent() { Amount = 20, Max = 20, RegenerationAmount = 1 },
                new EquipmentComponent() { Weapon = testWeapon , Armor = testArmor },
                new TransformComponent() { Position = new Vector2(3, 3) },
                new ColliderComponent() { Solid = false },
                new RenderableSpriteComponent() { Visible = true, Texture = "enemy" }
            };

            EntityManager.AddComponentsToEntity(testEnemy, enemyComponents);



            characters[3, 3] = testEnemy;
        }

        public bool IsOutOfBounds(Vector2 pos)
        {
            int x = (int)pos.X;
            int y = (int)pos.Y;
            if (x < 0 || x >= width ||
                y < 0 || y >= height)
            {
                Log.Warning(pos + " is out of bounds!");
                return true;
            }
            return false;
        }

        public int CreatePlayer(Vector2 pos)
        {
            int player = EntityManager.CreateEntity();
            int playerWeapon = EntityManager.CreateEntity();
            int playerArmor = EntityManager.CreateEntity();

            EntityManager.AddComponentToEntity(playerWeapon, new WeaponComponent() { Damage = 5 });
            EntityManager.AddComponentToEntity(playerArmor, new ArmorComponent() { PercentMitigation = 20, FlatMitigation = 3});

            List<IComponent> playerComponents = new List<IComponent>()
            {
                new DescriptionComponent() { Name = "Player", Description = "That's you!" },
                new TransformComponent() { Position = pos },
                new HealthComponent() { Amount = 30, Max = 30, RegenerationAmount = 0.3f },
                new PlayerComponent(),
                new RenderableSpriteComponent { Visible = true, Texture = "player" },
                new ColliderComponent() { Solid = false },
                new EquipmentComponent() { Weapon = playerWeapon, Armor = playerArmor }
            };

            EntityManager.AddComponentsToEntity(player, playerComponents);

            return player;
        }

        public int CreateWall(Vector2 pos)
        {
            int wall = EntityManager.CreateEntity();

            List<IComponent> wallComponents = new List<IComponent>();
            wallComponents.Add(new TransformComponent() { Position = pos });
            wallComponents.Add(new RenderableSpriteComponent() { Visible = true, Texture = "wall" });
            wallComponents.Add(new ColliderComponent() { Solid = true });

            EntityManager.AddComponentsToEntity(wall, wallComponents);

            return wall;
        } 

        public int CreateDoor(Vector2 pos)
        {
            return EntityManager.CreateEntity(new List<IComponent>()
            {
                new TransformComponent() { Position = pos },
                new RenderableSpriteComponent() { Texture = "door" },
                new ColliderComponent() { Solid = false }
            });
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    /*
    class Floor
    {
        Tile[,] tiles;

        public Floor(int width, int height)
        {
            tiles = new Tile[width, height];
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    tiles[x, y] = new Tile();                 
                }
            }
        }

        Floor()
        {

        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int y = 0; y < tiles.GetLength(1); y++)
            {
                for (int x = 0; x < tiles.GetLength(0); x++)         
                {
                    sb.Append(tiles[x, y].ToString()).Append(' ');
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public static Floor ReadFromFile(string filename)
        {
            string contentPath = @"C:\Users\Oliver\Source\Repos\RoguelikeECS\TheAlchemist\Content";
            string[] rows = File.ReadAllLines(contentPath + @"\" + filename);
            int width = rows[0].Length;
            int height = rows.Length;
            Floor floor = new Floor();
            floor.tiles = new Tile[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    floor.tiles[x, y] = new Tile(rows[y][x]);
                }
            }
            return floor;
        }
    }
    */
}
