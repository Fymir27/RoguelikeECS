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

        // data of the floor
        [JsonProperty]
        Tile[,] tiles;

        // seen by player at this moment
        List<Position> seen = new List<Position>();

        // precalculated for visibility calc.
        static float[][] angles = CalculateAngles(5); // assuming no entity see farther than 5 tiles

        // used for visibility calc
        struct Octant
        {
            public Position PosChangePerRow;
            public Position PosChangePerBlock;

            public Octant(Position posChangePerRow, Position posChangePerBlock)
            {
                PosChangePerRow = posChangePerRow;
                PosChangePerBlock = posChangePerBlock;
            }
        }
        static Octant[] octants = new Octant[]
        {
            new Octant(new Position(0, -1), new Position(1, 0)),  // 0
            new Octant(new Position(1, 0), new Position(0, -1)),  // 1
            new Octant(new Position(1, 0), new Position(0, 1)),   // 2
            new Octant(new Position(0, 1), new Position(1, 0)),   // 3 
            new Octant(new Position(0, 1), new Position(-1, 0)),  // 4
            new Octant(new Position(-1, 0), new Position(0, -1)), // 5
            new Octant(new Position(-1, 0), new Position(0, 1)),  // 6
            new Octant(new Position(0, -1), new Position(-1, 0)), // 7
        };

        public int Width { get => width; set => width = value; }
        public int Height { get => height; set => height = value; }


        // ------------------------------------------------

        public Tile GetTile(int x, int y)
        {
            if(IsOutOfBounds(x, y))
            {
                return null;
            }
            return tiles[x, y];
        }

        public Tile GetTile(Position pos)
        {
            return GetTile(pos.X, pos.Y);
        }

        public bool IsOutOfBounds(int x, int y)
        {
            if (x < 0 || x >= width ||
                y < 0 || y >= height)
            {
                return true;
            }
            return false;
        }

        public bool IsOutOfBounds(Position pos)
        {
            return IsOutOfBounds(pos.X, pos.Y);
        }

        // -------------------------------------------------

        /// determines which cells are visible to the player and updates them accordingly
        /// taken from: http://www.roguebasin.com/index.php?title=Restrictive_Precise_Angle_Shadowcasting
        /// octants:
        /// 6\7|0/1
        /// ---|---
        /// 5/4|3\2
        ///
        public void CalculateTileVisibility()
        {
            bool AngleBetween(float angle, float bigger, float smaller)
            {
                return angle >= smaller && angle <= bigger;
            }

            int playerRange = 4; // TODO: this shouldn't be here
            Position playerPos = EntityManager.GetComponent<TransformComponent>(Util.PlayerID).Position;
            discovered[playerPos.X, playerPos.Y] = true;
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

                    for (int block = 0; block < blocksInRow; block++)
                    {
                        int indexStartingAngle = block * 2;
                        float startingAngle = angles[row][indexStartingAngle];
                        float centreAngle = angles[row][indexStartingAngle + 1];
                        float endAngle = angles[row][indexStartingAngle + 2];

                        // check if cell is blocked
                        bool blocked = false;
                        bool centreBlocked = false;
                        bool startingBlocked = false;
                        bool endBlocked = false;

                        // check if cell is solid
                        bool solid = false;
                        int curTerrain = GetTerrain(new Vector2(pos.X, pos.Y));
                        if (curTerrain != 0)
                        {
                            var collider = EntityManager.GetComponent<ColliderComponent>(curTerrain);
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
                            if (solid)
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
            catch (IndexOutOfRangeException)
            {
                Log.Warning(x + "|" + y + " is out of range! (Calculate visibilty)");
            }
        }


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

        public Floor(string path)
        {
            //TODO: generate floor procedurally

            StreamReader file = new StreamReader(path);

            

            List<List<int>> tmpTerrain = new List<List<int>>();

            Position playerPos = new Position(1, 1);

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
                            playerPos = new Position(x, y);
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

            Width = tmpTerrain[0].Count;
            Height = y;

            terrain = new int[width, height];
            characters = new int[width, height];
            items = new List<int>[width, height]; // don't initialize each List yet to save space!
            discovered = new bool[width, height];

            tiles = new Tile[width, height];

            // instantiate tiles and set terrain
            // reeused y from above
            for (y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tiles[x, y] = new Tile();
                    PlaceTerrain(new Position(x, y), tmpTerrain[y][x]);
                }
            }

            PlaceCharacter(playerPos, CreatePlayer());

            // normal gold
            for (int i = 0; i < 5; i++)
            {
                PlaceItem(playerPos + new Position(-1, 0), CreateGold(666));
            }

            // create target indicator
            Util.TargetIndicatorID = EntityManager.CreateEntity(new List<IComponent>()
            {
                new TransformComponent() { Position = new Position(1, 1) },
            });        

            // load items ////////////
            JObject itemsFile = JObject.Parse(File.ReadAllText(Util.ContentPath + "/items.json"));

            int healthPotion = EntityManager.CreateEntity(itemsFile["healthPotion"].ToString());
            PlaceItem(playerPos + new Position(-1, 0), healthPotion);

            int poison = EntityManager.CreateEntity(itemsFile["poisonPotion"].ToString());          
            PlaceItem(playerPos + new Position(-1, 0), poison);

            int strengthPotion = EntityManager.CreateEntity(itemsFile["strengthPotion"].ToString());
            PlaceItem(playerPos + new Position(-2, 0), strengthPotion);

            int dexterityPotion = EntityManager.CreateEntity(itemsFile["dexterityPotion"].ToString());
            PlaceItem(playerPos + new Position(-3, 0), dexterityPotion);

            int intelligencePotion = EntityManager.CreateEntity(itemsFile["intelligencePotion"].ToString());
            PlaceItem(playerPos + new Position(-4, 0), intelligencePotion);

            // load enemies //////////
            JObject enemies = JObject.Parse(File.ReadAllText(Util.ContentPath + "/enemies.json"));

            int rat = EntityManager.CreateEntity(enemies["rat"].ToString());
            int bat = EntityManager.CreateEntity(enemies["bat"].ToString());
            int spider = EntityManager.CreateEntity(enemies["spider"].ToString());

            PlaceCharacter(new Position(10, 10), rat);
            PlaceCharacter(new Position(16, 5), bat);
            PlaceCharacter(new Position(16, 1), spider);

            //Log.Data(DescriptionSystem.GetDebugInfoEntity(rat));
            //Log.Data(DescriptionSystem.GetDebugInfoEntity(bat));
            //Log.Data(DescriptionSystem.GetDebugInfoEntity(spider));
        }


 
        
        ////////////////////// Getters //////////////////////////////////////

        public IEnumerable<Position> GetSeenPositions()
        {
            return seen;
        }

        public bool IsDiscovered(Position pos)
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

        public int GetTerrain(Position pos)
        {
            if(IsOutOfBounds(pos))
            {
                return 0;
            }
            return terrain[(int)pos.X, (int)pos.Y];
        }

        public int GetCharacter(Position pos)
        {
            if(IsOutOfBounds(pos))
            {
                return 0;
            }
            return characters[(int)pos.X, (int)pos.Y];
        }

        public int GetFirstItem(Position pos)
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

            return itemsHere.Last();
        }

        public IEnumerable<int> GetItems(Position pos)
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
        private bool RemoveEntity(Position pos, int entity)
        {
            if (IsOutOfBounds(pos))
            {
                Log.Warning("Trying to remove " + DescriptionSystem.GetNameWithID(entity) + " out of bounds! " + pos);
                return false;
            }

            var sprite = EntityManager.GetComponent<RenderableSpriteComponent>(entity);

            if (sprite != null) sprite.Visible = false; 

            return true;
        }

        public void RemoveTerrain(Position pos)
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

        public void RemoveCharacter(Position pos)
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

        public void MoveCharacter(Position oldPos, Position newPos)
        {
            if(IsOutOfBounds(oldPos) || IsOutOfBounds(newPos))
            {
                Log.Error("Movement from " + oldPos + " to " + newPos + " not possible; out of bounds!");
                return;
            }

            if (characters[(int)newPos.X, (int)newPos.Y] != 0)
            {
                Log.Error("Can't move there; occupied! " + newPos);
                return;
            }

            characters[(int)newPos.X, (int)newPos.Y] = characters[(int)oldPos.X, (int)oldPos.Y];
            characters[(int)oldPos.X, (int)oldPos.Y] = 0;
        }

        public void RemoveItems(Position pos)
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

        public void RemoveItem(Position pos, int item)
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
        private bool PlaceEntity(Position pos, int entity, int renderLayer = 0)
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

            var transform = EntityManager.GetComponent<TransformComponent>(entity);

            if (transform == null)
            {
                transform = new TransformComponent();
                EntityManager.AddComponent(entity, transform);
            }

            transform.Position = pos;

            var sprite = EntityManager.GetComponent<RenderableSpriteComponent>(entity);

            if (sprite == null)
            {
                // might be desirable in some cases
                Log.Warning("Sprite missing for " + DescriptionSystem.GetNameWithID(entity) + "!");
            }
            else
            {
                sprite.Visible = true;
                sprite.Layer = renderLayer;
            }
            return true;
        }

        public void PlaceTerrain(Position pos, int terrain)
        {
            if (!PlaceEntity(pos, terrain, RenderableSpriteComponent.RenderLayer.Terrain))
            {
                return;
            }
            this.terrain[pos.X, pos.Y] = terrain;
            GetTile(pos).Terrain = terrain;
        }

        public void PlaceCharacter(Position pos, int character)
        {
            if (!PlaceEntity(pos, character, RenderableSpriteComponent.RenderLayer.Character))
            {
                return;
            }
            characters[pos.X, pos.Y] = character;
            GetTile(pos).Character = character;
        }

        /* public void PlaceItems(Position pos, IEnumerable<int> items)
        {
            foreach(int item in items)
            {
                if (!PlaceEntity(pos, item, RenderableSpriteComponent.RenderLayer.Item))
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
        */

        public void PlaceItem(Position pos, int item)
        {
            if(!PlaceEntity(pos, item, RenderableSpriteComponent.RenderLayer.Item))
            {
                return;
            }

            var tile = GetTile(pos);

            if(tile.Items == null)
            {
                tile.Items = new List<int>() { item };
            }
            else
            {
                tile.Items.Add(item);
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

            EntityManager.AddComponent(testArmor, new ArmorComponent() { FlatMitigation = 2 });
            EntityManager.AddComponent(testWeapon, new WeaponComponent() { Damage = 5 });

            List<IComponent> enemyComponents = new List<IComponent>()
            {
                new NPCComponent(),
                new HealthComponent() { Amount = 20, Max = 20, RegenerationAmount = 1 },
                new EquipmentComponent() { Weapon = testWeapon , Armor = testArmor },
                new TransformComponent() { Position = new Vector2(3, 3) },
                new ColliderComponent() { Solid = false },
                new RenderableSpriteComponent() { Visible = true, Texture = "enemy" }
            };

            EntityManager.AddComponents(testEnemy, enemyComponents);



            characters[3, 3] = testEnemy;
        }

        

        // returns shortest path
        public List<Position> GetPath(Position from, Position to)
        {
            return null;
        }

        // returns "straight" line (Bresenham)
        public List<Position> GetLine(Position from, Position to, bool stopAtSolid = true)
        {
            if(IsOutOfBounds(from) || IsOutOfBounds(to))
            {
                Log.Error("Can't get Line between " + from + to + " - Out of bounds!");
                return null;
            }

            // integer vector makes more sense here
            List<Position> result = new List<Position>();

            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            int sx = (dx > 0) ? 1 : -1;
            int sy = (dy > 0) ? 1 : -1;           

            Position diagonalStep = new Position(sx, sy);

            int dfast, dslow;
            Position parallelStep;

            if (Math.Abs(dx) > Math.Abs(dy))
            {
                dfast = Math.Abs(dx);
                dslow = Math.Abs(dy);
                parallelStep = new Position(sx, 0);
            }
            else
            {
                dfast = Math.Abs(dy);
                dslow = Math.Abs(dx);
                parallelStep = new Position(0, sy);
            }

            Position curPos = new Position(from);
            int error = dfast / 2;
            result.Add(from);

            while (curPos != to)
            {
                error -= dslow;

                if(error < 0)
                {
                    error += dfast;
                    curPos += diagonalStep;
                }
                else
                {
                    curPos += parallelStep;
                }

                result.Add(curPos);

                if (!stopAtSolid)
                    continue;

                int terrainID = terrain[curPos.X, curPos.Y];

                if (terrainID == 0)
                    continue;

                var terrainCollider = EntityManager.GetComponent<ColliderComponent>(terrainID);

                if (terrainCollider == null)
                    continue;

                if (terrainCollider.Solid)
                    break;

            }
            return result;
        }

        public int CreatePlayer()
        {
            int player = EntityManager.CreateEntity();
            int playerWeapon = EntityManager.CreateEntity();
            int playerArmor = EntityManager.CreateEntity();

            EntityManager.AddComponent(playerWeapon, new WeaponComponent() { Damage = 5 });
            EntityManager.AddComponent(playerArmor, new ArmorComponent() { PercentMitigation = 0, FlatMitigation = 0});

            List<IComponent> playerComponents = new List<IComponent>()
            {
                new DescriptionComponent() { Name = "Player", Description = "That's you!" },
                new HealthComponent() { Amount = 30, Max = 30, RegenerationAmount = 0.3f },
                new PlayerComponent(),
                new RenderableSpriteComponent { Visible = true, Texture = "player" },
                new ColliderComponent() { Solid = false },
                new EquipmentComponent() { Weapon = playerWeapon, Armor = playerArmor },
                new InventoryComponent() { Capacity = 50 },
                new StatComponent(new Dictionary<Stat, int>()
                {
                    { Stat.Strength, 10 },
                    { Stat.Dexterity, 11 },
                    { Stat.Intelligence, 13 }
                })
            };

            EntityManager.AddComponents(player, playerComponents);

            Util.PlayerID = player;

            return player;
        }

        public int CreateWall()
        {
            int wall = EntityManager.CreateEntity();

            List<IComponent> wallComponents = new List<IComponent>();
            wallComponents.Add(new RenderableSpriteComponent() { Visible = true, Texture = "wall" });
            wallComponents.Add(new ColliderComponent() { Solid = true });
            wallComponents.Add(new DescriptionComponent() { Name = "Wall", Description = "A solid wall" });

            EntityManager.AddComponents(wall, wallComponents);

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
                new DescriptionComponent() { Name = "Door", Description = "What may be behind this one?" },
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
