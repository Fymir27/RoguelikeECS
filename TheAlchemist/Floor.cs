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
    using Systems;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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

        // discovered by the player
        bool[,] discovered;
        // seen by player at this moment
        List<Vector2> seen = new List<Vector2>();
        // precalculated for visibility calc.
        float[][] angles = CalculateAngles(5);

        public Floor(string path)
        {
            //TODO: generate floor procedurally

            StreamReader file = new StreamReader(path);

            List<List<int>> tmpTerrain = new List<List<int>>();

            Vector2 playerPos = new Vector2(1, 1);

            int y = 0;
            while (!file.EndOfStream)
            {
                int x = 0;
                var row = file.ReadLine();
                tmpTerrain.Add(new List<int>());
                foreach (var tile in row)
                {
                    switch (tile)
                    {
                        case '#':
                            tmpTerrain[y].Add(CreateWall());
                            break;

                        case '+':
                            tmpTerrain[y].Add(CreateDoor());
                            break;

                        case '@':
                            playerPos = new Vector2(x, y);
                            tmpTerrain[y].Add(0);
                            break;

                        default:
                            tmpTerrain[y].Add(0);
                            break;
                    }
                    x++;
                }
                if (x > 0)
                    y++;
            }

            this.width = tmpTerrain[0].Count;
            this.height = y;

            terrain = new int[width, height];
            characters = new int[width, height];
            items = new List<int>[width, height]; // don't initialize each List yet to save space!

            discovered = new bool[width, height];

            for (y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    PlaceTerrain(new Vector2(x, y), tmpTerrain[y][x]);
                }
            }

            PlaceCharacter(playerPos, CreatePlayer());
           
            for(int i = 0; i < 5; i++)
            {
                PlaceItem(playerPos + new Vector2(-1, 0), CreateGold(100));
            }

            /*
            int potion = EntityManager.CreateEntity(new List<IComponent>()
            {
                new TransformComponent() { Position = playerPos + new Vector2(0, -1) },
                new ItemComponent() {},
                new DescriptionComponent() { Name = "Potion", Description = "Quaff it!" },
                new Components.ItemComponents.DroppableComponent(),
                new Components.ItemComponents.ConsumableComponent()
                {
                    Effects = new ItemSystem.ItemEffectDescription[] 
                    {
                        new ItemSystem.ItemEffectDescription()
                        {
                            Type = ItemSystem.ItemEffectType.RestoreHealth,
                            Values = new float[] { 42 }
                        }
                    }
                },
                new Components.ItemComponents.ThrowableComponent(),
                new RenderableSpriteComponent() { Texture = "potion", Tint = Color.Red }
            });
            */

            JObject itemsFile = JObject.Parse(File.ReadAllText(Util.ContentPath + "/items.json"));
            int healthPotion = EntityManager.CreateEntity(itemsFile["healthPotion"].ToString());
            PlaceItem(playerPos + new Vector2(0, -1), healthPotion);

            int poison = EntityManager.CreateEntity(itemsFile["poisonPotion"].ToString());
            PlaceItem(playerPos + new Vector2(-1, -1), poison);


            /* //creation of test enemy

            int testEnemy = EntityManager.CreateEntity();
            int testArmor = EntityManager.CreateEntity();
            int testWeapon = EntityManager.CreateEntity();

            EntityManager.AddComponentToEntity(testArmor, new ArmorComponent() { FlatMitigation = 2 });
            EntityManager.AddComponentToEntity(testWeapon, new WeaponComponent() { Damage = 5 });

            List<IComponent> enemyComponents = new List<IComponent>()
            {
                new NPCComponent(),
                new HealthComponent() { Amount = 20, Max = 20, RegenerationAmount = 1 },
                new EquipmentComponent() { Weapon = testWeapon , Armor = testArmor },
                new TransformComponent() { Position = new Vector2(10, 10) },
                new ColliderComponent() { Solid = false },
                new RenderableSpriteComponent() { Visible = true, Texture = "enemy" }
            };

            EntityManager.AddComponentsToEntity(testEnemy, enemyComponents);

            */

            JObject enemies = JObject.Parse(File.ReadAllText(Util.ContentPath + "/enemies.json"));

            int rat = EntityManager.CreateEntity(enemies["rat"].ToString());
            int bat = EntityManager.CreateEntity(enemies["bat"].ToString());
            int spider = EntityManager.CreateEntity(enemies["spider"].ToString());

            PlaceCharacter(new Vector2(10, 10), rat);
            PlaceCharacter(new Vector2(16, 5), bat);
            PlaceCharacter(new Vector2(16, 1), spider);

            Log.Data(DescriptionSystem.GetDebugInfoEntity(rat));
            Log.Data(DescriptionSystem.GetDebugInfoEntity(bat));
            Log.Data(DescriptionSystem.GetDebugInfoEntity(spider));
        }


        struct Octant
        {
            public Vector2 PosChangePerRow;
            public Vector2 PosChangePerBlock;

            public Octant(Vector2 posChangePerRow, Vector2 posChangePerBlock)
            {
                PosChangePerRow = posChangePerRow;
                PosChangePerBlock = posChangePerBlock;
            }
        }

        Octant[] octants = new Octant[]
        {
            new Octant(new Vector2(0, -1), new Vector2(1, 0)),  // 0
            new Octant(new Vector2(1, 0), new Vector2(0, -1)),  // 1
            new Octant(new Vector2(1, 0), new Vector2(0, 1)),   // 2
            new Octant(new Vector2(0, 1), new Vector2(1, 0)),   // 3 
            new Octant(new Vector2(0, 1), new Vector2(-1, 0)),  // 4
            new Octant(new Vector2(-1, 0), new Vector2(0, -1)), // 5
            new Octant(new Vector2(-1, 0), new Vector2(0, 1)),  // 6
            new Octant(new Vector2(0, -1), new Vector2(-1, 0)), // 7
        };

        public int Width { get => width; set => width = value; }
        public int Height { get => height; set => height = value; }

        // determines which cells are visible to the player and updates them accordingly
        // taken from: http://www.roguebasin.com/index.php?title=Restrictive_Precise_Angle_Shadowcasting
        // octants:
        // 6\7|0/1
        // ---|---
        // 5/4|3\2
        //
        public void CalculateTileVisibility()
        { 
            bool AngleBetween(float angle, float bigger, float smaller)
            {
                return angle >= smaller && angle <= bigger;
            }

            int playerRange = 4;
            Vector2 playerPos = EntityManager.GetComponentOfEntity<TransformComponent>(Util.PlayerID).Position;
            discovered[(int)playerPos.X, (int)playerPos.Y] = true;          
            seen.Clear();
            seen.Add(playerPos);

            for (int octant = 0; octant < 8; octant++)
            {
                // Octant (here: NorthNorthWest):
                // calculation always from orthagonal line to diagonal 
                // (block nr. 0 always lies on orthagonal line)
                // rows numbered like this:
                //
                // 3 :   . . . .
                // 2 :     . . .
                // 1 :       . .
                // 0 :         @
                //

                int obstaclesFound = 0;
                List<float> startingAngles = new List<float>();
                List<float> endAngles = new List<float>();

                //Log.Message("Octant " + octant);

                // remember position of row on orhtagonal line
                //
                //       x
                //       X
                //       X
                // x x x @ x x x   <= xs are orthagonal positions
                //       x
                //       x
                //       x
                //
                var posOrthagonal = playerPos;

                for (int row = 1; row <= playerRange; row++)
                {                   
                    int blocksInRow = row + 1;
                    int obstaclesThisRow = 0;

                    posOrthagonal += octants[octant].PosChangePerRow;

                    //var pos = new Vector2(posOrthagonal.X, posOrthagonal.Y);
                    var pos = posOrthagonal;

                    for(int block = 0; block < blocksInRow; block ++)
                    {
                        int indexStartingAngle = block * 2;
                        float startingAngle = angles[row][indexStartingAngle];
                        float centreAngle   = angles[row][indexStartingAngle + 1];
                        float endAngle      = angles[row][indexStartingAngle + 2];

                        // check if cell is blocked
                        bool blocked = false;
                        bool centreBlocked = false;
                        bool startingBlocked = false;
                        bool endBlocked = false;

                        // check if cell is solid
                        bool solid = false;
                        int curTerrain = GetTerrain(pos);
                        if (curTerrain != 0)
                        {
                            var collider = EntityManager.GetComponentOfEntity<Components.ColliderComponent>(GetTerrain(pos));
                            if (collider != null && collider.Solid)
                            {
                                solid = true;
                            }
                        }

                        for (int i = 0; i < obstaclesFound - obstaclesThisRow; i++)
                        {
                            if (AngleBetween(centreAngle, endAngles[i], startingAngles[i]))
                                centreBlocked = true;
                            if (AngleBetween(startingAngle, endAngles[i], startingAngles[i]))
                                startingBlocked = true;
                            if (AngleBetween(endAngle, endAngles[i], startingAngles[i]))
                                endBlocked = true;

                            
                            if (solid)
                            {
                                blocked = startingBlocked && endBlocked && centreBlocked;
                            }
                            else
                            {
                                
                                switch (Util.FOV)
                                {
                                    // bugged! dont use!
                                    case FOV.Permissive:
                                        blocked = true; // centreBlocked && startingBlocked && endBlocked;
                                        break;

                                    case FOV.Medium:
                                        blocked = centreBlocked && (startingBlocked || endBlocked);
                                        break;

                                    case FOV.Restricted:
                                        blocked = centreBlocked || startingBlocked || endBlocked;
                                        break;
                                }
                            }
                            

                            if (blocked)
                            {
                                break;
                            }

                        }

                        /*
                        Log.Message("Current block's angles: " + startingAngle + ", " + centreAngle + ", " + endAngle);
                        Log.Message("Blocked: " + startingBlocked + centreBlocked + endBlocked);
                        string angles = "";
                        for (int i = 0; i < startingAngles.Count; i++)
                        {
                            angles += startingAngles[i] + "|" + endAngles[i] + ", ";
                        }
                        Log.Message("Currently blocked angles: " + angles);
                        */

                        if (!blocked)
                        {
                            if(solid)
                            {
                                // add new blocked range and increase obstacle count
                                startingAngles.Add(startingAngle);
                                endAngles.Add(endAngle);
                                obstaclesFound++;
                                obstaclesThisRow++;
                            }
                            seen.Add(pos);
                        }

                        // move to next block
                        pos += octants[octant].PosChangePerBlock;
                    }
                }
            }

            int x = 0;
            int y = 0;
            try
            {           
                seen.ForEach(pos => discovered[x = (int)pos.X, y = (int)pos.Y] = true);
            }
            catch(IndexOutOfRangeException)
            {
                Log.Warning(x + "|" + y + " is out of range! (Calculate visibilty)");
            }
        }
 
        
        ////////////////////// Getters //////////////////////////////////////

        public IEnumerable<Vector2> GetSeenPositions()
        {
            return seen;
        }

        public bool IsDiscovered(Vector2 pos)
        {
            try
            {
                return discovered[(int)pos.X, (int)pos.Y];
            }
            catch(IndexOutOfRangeException)
            {
                Log.Error("Invalid Position! " + pos);
                throw;
            }
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

        public int GetFirstItem(Vector2 pos)
        {
            if(IsOutOfBounds(pos))
            {
                return 0;
            }

            var itemsHere = items[(int)pos.X, (int)pos.Y];

            if (itemsHere == null)
                return 0;

            if (itemsHere.Count == 0) // shouldn't happen; List should be null instead
            {
                return 0;
            }

            return itemsHere.First();
        }

        public IEnumerable<int> GetItems(Vector2 pos)
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
            return itemList;
        }


        //////////////////// Removal ////////////////////////////////////////

        // prepares entity for removal from floor by
        // by making the sprite invisible
        // returns true if successful
        private bool RemoveEntity(Vector2 pos, int entity)
        {
            if (IsOutOfBounds(pos))
            {
                // TODO: throw exception?
                Log.Warning("Trying to remove " + DescriptionSystem.GetNameWithID(entity) + " out of bounds! " + pos);
                return false;
            }

            EntityManager.GetComponentOfEntity<RenderableSpriteComponent>(entity).Visible = false;

            return true;
        }

        public void RemoveTerrain(Vector2 pos)
        {
            if (IsOutOfBounds(pos))
            {
                Log.Warning("Can't remove terrain out of bounds! " + pos);
                return;
            }

            int terrain = this.terrain[(int)pos.X, (int)pos.Y];

            if (!RemoveEntity(pos, terrain))
            {
                return;
            }

            characters[(int)pos.X, (int)pos.Y] = 0;
        }

        public void RemoveCharacter(Vector2 pos)
        {
            if(IsOutOfBounds(pos))
            {
                Log.Warning("Can't remove character out of bounds! " + pos);
                return;
            }

            int character = characters[(int)pos.X, (int)pos.Y];

            if(!RemoveEntity(pos, character))
            {
                return;
            }

            characters[(int)pos.X, (int)pos.Y] = 0;
        }

        public void RemoveItems(Vector2 pos)
        {        
           if(IsOutOfBounds(pos))
            {
                Log.Warning("Can't remove items out of bounds! " + pos);
                return;
            }

            var itemsHere = items[(int)pos.X, (int)pos.Y];

            if(itemsHere == null)
            {
                return; // nothing to remove
            }

            foreach(int item in itemsHere)
            {
                if(!RemoveEntity(pos, item))
                {
                    return;
                }
            }         

            itemsHere = null;
        }

        public void RemoveItem(Vector2 pos, int item)
        {
            if(!RemoveEntity(pos, item))
            {
                return;
            }

            var itemsHere = items[(int)pos.X, (int)pos.Y];

            if(itemsHere == null)
            {
                Log.Warning("Trying to remove item that's not here! " + pos);
                Log.Data(DescriptionSystem.GetDebugInfoEntity(item));
                return;
            }

            if(itemsHere.Count == 0 || !itemsHere.Contains(item))
            {
                Log.Warning("Trying to remove item that's not here! " + pos);
                Log.Data(DescriptionSystem.GetDebugInfoEntity(item));
                return;
            }


            itemsHere.Remove(item);
            if (itemsHere.Count == 0) itemsHere = null; // remove List to save space
        }


        ///////////////////// Placement /////////////////////////////////////////

        // prepares the entity for placement as item/character/terrain by
        // configuring/adding the the transform to the position and
        // setting the sprite to visible
        // returns true if successful
        private bool PlaceEntity(Vector2 pos, int entity)
        {
            if(entity == 0) // can happen during init
            {
                return false; 
            }

            if (IsOutOfBounds(pos))
            {
                Log.Warning("Trying to place " + DescriptionSystem.GetNameWithID(entity) + " out of bounds! " + pos);
                return false;
            }

            var transform = EntityManager.GetComponentOfEntity<TransformComponent>(entity);

            if (transform == null)
            {
                transform = new TransformComponent();
                EntityManager.AddComponentToEntity(entity, transform);
            }

            transform.Position = pos;

            var sprite = EntityManager.GetComponentOfEntity<RenderableSpriteComponent>(entity);

            if (sprite == null)
            {
                // might be desirable in some cases
                Log.Warning("Sprite missing for " + DescriptionSystem.GetNameWithID(entity) + "!");
            }
            else
            {
                sprite.Visible = true;
            }
            return true;
        }

        public void PlaceTerrain(Vector2 pos, int terrain)
        {
            if (!PlaceEntity(pos, terrain))
            {
                return;
            }
            this.terrain[(int)pos.X, (int)pos.Y] = terrain;
        }

        public void PlaceCharacter(Vector2 pos, int character)
        {
            if (!PlaceEntity(pos, character))
            {
                return;
            }
            characters[(int)pos.X, (int)pos.Y] = character;
        }

        public void PlaceItems(Vector2 pos, IEnumerable<int> items)
        {
            foreach(int item in items)
            {
                if (!PlaceEntity(pos, item))
                {
                    return;
                }
            }            

            var itemsHere = this.items[(int)pos.X, (int)pos.Y];

            if (itemsHere == null)
            {
                itemsHere = new List<int>();
                itemsHere.AddRange(items);
            }
            else
            {
                itemsHere.AddRange(items);
            }
         
        }

        public void PlaceItem(Vector2 pos, int item)
        {
            if(!PlaceEntity(pos, item))
            {
                return;
            }

            var itemsHere = items[(int)pos.X, (int)pos.Y];

            if (itemsHere == null)
            {
                items[(int)pos.X, (int)pos.Y] = new List<int>() { item };
            }
            else
            {
                itemsHere.Add(item);
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
                        PlaceTerrain(new Vector2(x, y), CreateWall());
                    }
                    else if(x == 0 || x == width - 1)
                    {
                        PlaceTerrain(new Vector2(x, y), CreateWall());
                    }                      
                }
            }

            PlaceCharacter(new Vector2(1, 1), CreatePlayer());

            int testEnemy = EntityManager.CreateEntity();
            int testArmor = EntityManager.CreateEntity();
            int testWeapon = EntityManager.CreateEntity();

            EntityManager.AddComponentToEntity(testArmor, new ArmorComponent() { FlatMitigation = 2 });
            EntityManager.AddComponentToEntity(testWeapon, new WeaponComponent() { Damage = 5 });

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
                //Log.Warning(pos + " is out of bounds!");
                return true;
            }
            return false;
        }

        public int CreatePlayer()
        {
            int player = EntityManager.CreateEntity();
            int playerWeapon = EntityManager.CreateEntity();
            int playerArmor = EntityManager.CreateEntity();

            EntityManager.AddComponentToEntity(playerWeapon, new WeaponComponent() { Damage = 5 });
            EntityManager.AddComponentToEntity(playerArmor, new ArmorComponent() { PercentMitigation = 0, FlatMitigation = 0});

            List<IComponent> playerComponents = new List<IComponent>()
            {
                new DescriptionComponent() { Name = "Player", Description = "That's you!" },
                new HealthComponent() { Amount = 30, Max = 30, RegenerationAmount = 0.3f },
                new PlayerComponent(),
                new RenderableSpriteComponent { Visible = true, Texture = "player" },
                new ColliderComponent() { Solid = false },
                new EquipmentComponent() { Weapon = playerWeapon, Armor = playerArmor },
                new InventoryComponent() { Capacity = 50 }
            };

            EntityManager.AddComponentsToEntity(player, playerComponents);

            Util.PlayerID = player;

            return player;
        }

        public int CreateWall()
        {
            int wall = EntityManager.CreateEntity();

            List<IComponent> wallComponents = new List<IComponent>();
            wallComponents.Add(new RenderableSpriteComponent() { Visible = true, Texture = "wall" });
            wallComponents.Add(new ColliderComponent() { Solid = true });

            EntityManager.AddComponentsToEntity(wall, wallComponents);

            return wall;
        } 

        public int CreateGold(int amount)
        {
            return EntityManager.CreateEntity(new List<IComponent>()
            {
                new DescriptionComponent() { Name = "Gold", Description = "Ohhh, shiny!" },
                new ItemComponent() { MaxCount = 999, Count = amount, Value = 1, Weight = 0.1f },
                new RenderableSpriteComponent() { Texture = "gold" }
            });
        }

        public int CreateDoor()
        {
            var doorComponent = new DoorComponent() { Open = false, TextureClosed = "doorClosed", TextureOpen = "doorOpen" };
            return EntityManager.CreateEntity(new List<IComponent>()
            {
                doorComponent,
                new DescriptionComponent() { Name = "Door", Description = "" },
                new InteractableComponent() { },
                new RenderableSpriteComponent() { Texture = doorComponent.Open ? doorComponent.TextureOpen : doorComponent.TextureClosed },
                new ColliderComponent() { Solid = true },
                //new RenderableTextComponent() { GetTextFrom = () => doorComponent.Open.ToString() }
            });
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        private static float[][] CalculateAngles(int maxRows)
        {
            float[][] result = new float[maxRows][];
            for (int i = 1; i < maxRows; i++)
            {
                int blocks = i + 1;
                int angleCount = blocks * 2 + 1; // space for all start/centre/end angles
                float angleDifferenceHalf = 1f / (blocks * 2);

                result[i] = new float[angleCount]; 
               
                result[i][0] = 0f;
                result[i][angleCount - 1] = 1f;

                for(int j = 1; j < (angleCount - 1); j++)
                {
                    result[i][j] = result[i][j - 1] + angleDifferenceHalf;
                }
            }
            return result;
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
