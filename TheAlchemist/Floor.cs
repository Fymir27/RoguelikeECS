using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace TheAlchemist
{
    using Components;
    using Systems;

    public interface IVisionGrid
    {
        bool IsOpaque(Position pos);
        void SetSeen(Position pos);
    }

    class Floor : IVisionGrid
    {

        public bool IsOpaque(Position pos)
        {
            return IsTileBlocked(pos);
        }

        public void SetSeen(Position pos)
        {
            GetTile(pos).Discovered = true;
            seen.Add(pos);
        }

        [JsonProperty]
        int width, height;

        // data of the floor
        [JsonProperty]
        Tile[,] tiles;

        bool[,] assignedToRoom;

        List<Room> rooms;
        int curRoomNr = 1;
        public int GetNewRoomNr() { return curRoomNr++; }
        public int[,] roomNrs;

        // seen by player at this moment
        List<Position> seen = new List<Position>();

        // precalculated for visibility calc.
        //static float[][] angles = CalculateAngles(5); // assuming no entity see farther than 5 tiles

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

        public string FloorTexture = "";

        // ------------------------------------------------

        public Tile GetTile(int x, int y)
        {
            if (IsOutOfBounds(x, y))
            {
                return null;
            }
            return tiles[x, y];
        }

        public Tile GetTile(Position pos)
        {
            return GetTile(pos.X, pos.Y);
        }

        public void SetTile(int x, int y, Tile tile)
        {
            if (IsOutOfBounds(x, y))
            {
                return;
            }
            tiles[x, y] = tile;
        }

        public void SetTile(Position pos, Tile tile)
        {
            SetTile(pos.X, pos.Y, tile);
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

        public bool IsTileBlocked(int x, int y)
        {
            Tile tile = GetTile(x, y);

            if (tile.Terrain != 0)
            {
                var terrainCollider = EntityManager.GetComponent<ColliderComponent>(tile.Terrain);

                if (terrainCollider != null && terrainCollider.Solid)
                {
                    return true;
                }
            }

            if (tile.Structure != 0)
            {
                var structureCollider = EntityManager.GetComponent<ColliderComponent>(tile.Structure);

                if (structureCollider != null && structureCollider.Solid)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsTileBlocked(Position pos)
        {
            return IsTileBlocked(pos.X, pos.Y);
        }

        public bool IsBlockingMovement(int entity)
        {
            if(entity == 0)
            {
                return false;
            }

            var collider = EntityManager.GetComponent<ColliderComponent>(entity);

            if (collider != null && collider.Solid)
            {
                var interactable = EntityManager.GetComponent<InteractableComponent>(entity);

                if (interactable != null)
                {
                    return !interactable.ChangeSolidity;
                }

                return true;
            }

            return false;
        }

        public bool IsWalkable(Position pos)
        {
            Tile tile = GetTile(pos);

            return !(IsBlockingMovement(tile.Terrain) || IsBlockingMovement(tile.Structure));
        }

        // -------------------------------------------------

        private int SecondaryRay(Position pos, Position offset, int power)
        {
            if (power <= 0)
                return 0;

            pos += offset;

            SetSeen(pos);

            if (IsOpaque(pos))
            {
                return 1;
            }

            return 1 + SecondaryRay(pos, offset, power - 1);
        }

        public void CalculateTileVisibility()
        {
            seen.Clear();

            int playerRange = 4;

            Position[] mainRays =
            {
                new Position(0, -1), // N
                new Position(1, 0),  // E
                new Position(0, 1),  // S 
                new Position(-1, 0)  // W
            };

            Position[] secondaryRays =
            {
                new Position(1, 0),  // horizontal
                new Position(0, 1)   // vertical
            };

            var playerPos = EntityManager.GetComponent<TransformComponent>(Util.PlayerID).Position;
            SetSeen(playerPos);

            // cast main rays
            for (int i = 0; i < 4; i++)
            {
                Position pos = playerPos;
                int dampening1 = 1;
                int dampening2 = 1;

                for (int power = playerRange; power > 0; power--)
                {
                    pos += mainRays[i]; //extend ray
                    SetSeen(pos);

                    if (IsOpaque(pos))
                    {
                        // this is to better light corners of a room
                        var neighbour1 = pos + secondaryRays[i % 2];
                        var neighbour2 = pos - secondaryRays[i % 2];

                        if (IsOpaque(neighbour1))
                        {
                            if (power - dampening1 > 0)
                            {
                                SetSeen(neighbour1);
                            }
                        }

                        if (IsOpaque(neighbour2))
                        {
                            if (power - dampening2 > 0)
                            {
                                SetSeen(neighbour2);
                            }
                        }

                        break;
                    }

                    int maxExtention1 = SecondaryRay(pos, secondaryRays[i % 2], power - dampening1);
                    int maxExtention2 = SecondaryRay(pos, secondaryRays[i % 2] * (-1), power - dampening2);

                    bool blocked1 = maxExtention1 < power - dampening1;
                    bool blocked2 = maxExtention2 < power - dampening2;

                    if (blocked1)
                    {
                        dampening1++;
                    }

                    if (blocked2)
                    {
                        dampening2++;
                    }


                }
            }
        }



        /* public void CalculateTileVisibility()
         {
             seen.Clear();

             // "cirlce"
             Position[] edgeOfVision =
             {
                 new Position( 0, -3),
                 new Position( 1, -3),
                 new Position( 2, -2),
                 new Position( 3, -1),
                 new Position( 3, 0),
                 new Position( 3, 1),
                 new Position( 2, 2),
                 new Position( 1, 3),
                 new Position( 0, 3),
                 new Position(-1, 3),
                 new Position(-2, 2),
                 new Position(-3, 1),
                 new Position(-3, 0),
                 new Position(-3, -1),
                 new Position(-2, -2),
                 new Position(-1, -3)
             };

             //square
             Position[] edgeOfVision2 =
             {
                 new Position(-3, -3),
                 new Position(-2, -3),
                 new Position(-1, -3),
                 new Position(-0, -3),
                 new Position( 1, -3),
                 new Position( 2, -3),
                 new Position( 3, -2),
                 new Position( 3, -1),
                 new Position( 3, -0),
                 new Position( 3,  1),
                 new Position( 3,  2),
                 new Position( 3,  3),
                 new Position( 2,  3),
                 new Position( 1,  3),
                 new Position( 0,  3),
                 new Position(-1,  3),
                 new Position(-2,  3),
                 new Position(-3,  3),
                 new Position(-3,  2),
                 new Position(-3,  1),
                 new Position(-3,  0),
                 new Position(-3, -1),
                 new Position(-3, -2),
                 new Position(-3, -3),
             };

             var playerPos = EntityManager.GetComponent<TransformComponent>(Util.PlayerID).Position;

             foreach(var offset in edgeOfVision2)
             {              
                 var pos = playerPos + offset;
                 var line = GetLine(playerPos, pos);
                 foreach(var linePos in line)
                 {
                     seen.Add(linePos);
                     GetTile(linePos).Discovered = true;
                 }               
             }


         }*/

        /// determines which cells are visible to the player and updates them accordingly
        /// taken from: http://www.roguebasin.com/index.php?title=Restrictive_Precise_Angle_Shadowcasting
        /// octants:
        /// 6\7|0/1
        /// ---|---
        /// 5/4|3\2
        ///
        /*public void CalculateTileVisibility()
        {
            bool AngleBetween(float angle, float bigger, float smaller)
            {
                return angle >= smaller && angle <= bigger;
            }

            int playerRange = 4; // TODO: this shouldn't be here
            Position playerPos = EntityManager.GetComponent<TransformComponent>(Util.PlayerID).Position;
            GetTile(playerPos).Discovered = true;
            seen.Clear();
            seen.Add(playerPos);

            List<Position> solidTiles = new List<Position>();

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
                        int curTerrain = GetTerrain(new Position(pos.X, pos.Y));
                        if (curTerrain != 0)
                        {
                            var collider = EntityManager.GetComponent<ColliderComponent>(curTerrain);
                            if (collider != null && collider.Solid)
                            {
                                solid = true;
                                solidTiles.Add(pos);
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
                                blocked = centreBlocked && startingBlocked && endBlocked;
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

                        //Log.Message("Current block's angles: " + startingAngle + ", " + centreAngle + ", " + endAngle);
                        //Log.Message("Blocked: " + startingBlocked + centreBlocked + endBlocked);
                        //string angles = "";
                        //for (int i = 0; i < startingAngles.Count; i++)
                        //{
                        //    angles += startingAngles[i] + "|" + endAngles[i] + ", ";
                        //}
                        //Log.Message("Currently blocked angles: " + angles);


                        if (!blocked)
                        {
                            seen.Add(pos);
                            if (solid)
                            {
                                // add new blocked range and increase obstacle count
                                startingAngles.Add(startingAngle);
                                endAngles.Add(endAngle);
                                obstaclesFound++;
                                obstaclesThisRow++;
                            }
                        }

                        // move to next block
                        pos += octants[octant].PosChangePerBlock;
                    }
                }
            }

            //Console.WriteLine("Seen size before: " + seen.Count);
            //Console.WriteLine("Player pos: " + playerPos);
            
            //// cast ray in all 45 degree diagonals to make sure diagonal walls block LOS
            //for (int i = 0; i < 8; i += 2)
            //{
            //    Console.WriteLine("Octant: " + i);

            //    bool diagonalBlocked = false; // was this diagonal ray already blocked

            //    Position pos = playerPos; // pos of tile in diagonal
            //    for (int row = 1; row <= playerRange; row++)
            //    {
            //        pos += octants[i].PosChangePerRow;
            //        pos += octants[i].PosChangePerBlock;

            //        Console.Write(pos + " ");

            //        if(!seen.Contains(pos) || solidTiles.Contains(pos))
            //        {
            //            Console.WriteLine("Already hidden or solid!");
            //            break; // if current tile is already hidden, future will be as well anyways, so we stop
            //        }

            //        if(diagonalBlocked) // this diagonal ray has been blocked
            //        {
            //            Console.WriteLine("Blocked before; removing...");

            //            seen.RemoveAll(x => x == pos);

            //            if (seen.Contains(pos))
            //            {
            //                Console.WriteLine("WTF");
            //            }
            //            continue;
            //        }

            //        // n1 pos
            //        //  @ n2
            //        bool neighbour1Solid = solidTiles.Contains(pos - octants[i].PosChangePerBlock);
            //        bool neighbour2Solid = solidTiles.Contains(pos - octants[i].PosChangePerRow);
                    
            //        if(neighbour1Solid && neighbour2Solid)
            //        {
            //            Console.WriteLine("Block found!");
            //            seen.RemoveAll(x => x == pos);
            //            diagonalBlocked = true;
            //        }
            //        else
            //        {
            //            Console.WriteLine("Not Blocked!");
            //        }                  
            //    }
            //}

            //Console.WriteLine("Seen size after: " + seen.Count);

            try
            {
                seen.ForEach(pos => GetTile(pos).Discovered = true);
            }
            catch(NullReferenceException)
            {
                Log.Warning("Position out of range! (Calculate visibilty)");
            }
        }*/


        // one terrain entity per tile
        // one character entity per tile
        // a list of items per tile
        //[JsonProperty]
        //int[,] terrain;
        //[JsonProperty]
        //int[,] characters;
        //[JsonProperty]
        //List<int>[,] items;

        // discovered by the player
        // bool[,] discovered;      


        /// <summary>
        /// Creates floor from file 
        /// </summary>
        /// <param name="path">string of path to file</param>
        public Floor(string path)
        {
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

            /*
            terrain = new int[width, height];
            characters = new int[width, height];
            items = new List<int>[width, height]; // don't initialize each List yet to save space!
            discovered = new bool[width, height];
            */

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

            /*/ load items ////////////
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

            */

            Log.Message("Floor loaded: " + path + " (" + Width + "|" + Height + ")");
        }

        public Floor()
        {
            Width = 100;
            Height = 70;

            FloorTexture = "floor";

            tiles = new Tile[width, height];
            assignedToRoom = new bool[width, height];
            roomNrs = new int[width, height];

            foreach (var item in assignedToRoom)
            {
                if (item == true)
                    Log.Error("assignedToRoom init failed!");
            }

            int x, y;

            // instantiate tiles and set terrain
            for (y = 0; y < height; y++)
            {
                for (x = 0; x < width; x++)
                {
                    tiles[x, y] = new Tile();
                    PlaceTerrain(new Position(x, y), CreateWall());
                }
            }

            rooms = new List<Room>();

            for (int i = 0; i < 10; i++)
            {
                int minWidth = 10;
                int maxWidth = 20;

                int minHeight = 10;
                int maxHeight = 15;

                int roomWidth = 0;
                int roomHeight = 0;
                RoomShape shape = RoomShape.Rectangle;

                Position roomPos = Position.Zero;

                do
                {
                    roomWidth = Game.Random.Next(minWidth, maxWidth + 1);
                    roomHeight = Game.Random.Next(minHeight, maxHeight + 1);
                    roomPos = new Position(Game.Random.Next(1, Width - roomWidth - 1), Game.Random.Next(1, Height - roomHeight - 1));
                }
                while (!RoomPlaceable(roomPos, roomWidth, roomHeight));

                shape = (RoomShape)Game.Random.Next(Enum.GetValues(typeof(RoomShape)).Length);

                rooms.Add(PlaceRoom(roomPos, roomWidth, roomHeight, shape));
            }


            // find possible connections between rooms
            FindConnectionPoints();
            ConnectRoomsRDFS();
            CreateStructures();
            SpawnEnemies();
            SpawnItems();


            var riverFrom = new Position(Width / 2 + 3, 1);
            var riverTo = new Position(Width / 2 - 3, Height - 2);

            //CreateRiver(riverFrom, riverTo, 5);

            /*
            int door = CreateDoor();
            int wall = CreateWall();

            Log.Message("DoorID: " + door);
            Log.Message("WallID: " + wall);

            var terrain = new Dictionary<string, IEnumerable<IComponent>>();

            terrain.Add("door", EntityManager.GetComponents(door));

            terrain.Add("wall", EntityManager.GetComponents(wall));

            EntityManager.RemoveEntity(door);
            EntityManager.RemoveEntity(wall);
            EntityManager.CleanUpEntities();

            var water = new List<IComponent>()
            {
                new DescriptionComponent()
                {
                    Name = "Water",
                    Description = "It's wet",
                    SpecialMessages = new Dictionary<DescriptionComponent.MessageType, string>()
                    {
                        { DescriptionComponent.MessageType.StepOn, "Splash!" }
                    }
                },
                new RenderableSpriteComponent()
                {
                    Texture = "square",
                    Tint = Color.CadetBlue
                }
            };

            terrain.Add("water", water);

            File.WriteAllText(Util.ContentPath + "/terrain.json", Util.SerializeObject(terrain, true));
            */

            // spawn player in the middle of room 0
            var playerPos = rooms[0].Pos + new Position(rooms[0].Width / 2, rooms[0].Height / 2);

            // if there already is a creature there, just remove it entirely
            int oldChar = GetCharacter(playerPos);
            if (oldChar != 0)
            {
                EntityManager.RemoveEntity(oldChar);
                RemoveCharacter(playerPos);
            }

            PlaceCharacter(playerPos, CreatePlayer());

            //int size = 5;
            //for (y = playerPos.Y - size; y <= playerPos.Y + size; y++)
            //{
            //    for (x = playerPos.X - size; x <= playerPos.X + size; x++)
            //    {
            //        GetTile(x, y).Discovered = true;
            //    }
            //}

            //PlaceCharacter(new Position(Width / 2, Height / 2), CreatePlayer());
        }

        Room PlaceRoom(Position pos, int width, int height, RoomShape shape)
        {
            for (int y = pos.Y - 1; y <= pos.Y + height; y++)
            {
                for (int x = pos.X - 1; x <= pos.X + width; x++)
                {
                    assignedToRoom[x, y] = true;
                }
            }
            return new Room(pos, width, height, shape, this);
        }

        bool RoomPlaceable(Position pos, int width, int height)
        {
            for (int y = pos.Y; y < pos.Y + height; y++)
            {
                for (int x = pos.X; x < pos.X + width; x++)
                {
                    if (assignedToRoom[x, y])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// connects all the rooms using randomized Depth first search
        /// </summary>
        void ConnectRoomsRDFS()
        {
            Stack<Room> stack = new Stack<Room>();

            Room curRoom = rooms[0];
            List<int> neighbours = new List<int>();

            int connectedRooms = 1;

            while (connectedRooms < (rooms.Count))
            {
                curRoom.Connected = true;

                neighbours = curRoom.connectionPoints.Keys.Where(roomNr => !rooms[roomNr - 1].Connected).ToList();

                if (neighbours.Count == 0)
                {
                    curRoom = stack.Pop();
                    continue;
                }
                else if (neighbours.Count > 1)
                {
                    stack.Push(curRoom);
                }

                connectedRooms++;

                int nextRoomNr = Util.PickRandomElement(neighbours);

                ConnectRooms(curRoom.Nr, nextRoomNr);

                // randomly also connect another room if there are more neighbours
                if (neighbours.Count > 1 && Game.Random.Next(0, 3) == 0)
                {
                    int additionalRoomNr = nextRoomNr;
                    while (additionalRoomNr == nextRoomNr)
                    {
                        additionalRoomNr = Util.PickRandomElement(neighbours);
                    }
                    ConnectRooms(curRoom.Nr, additionalRoomNr);
                }

                // other room becomes cur room
                // push rest of neighbours onto stack
                curRoom = rooms[nextRoomNr - 1];

            }


            /* old strategy (not reliable)
            List<Tuple<int, int>> roomsConnected = new List<Tuple<int, int>>();

            foreach (var room in rooms)
            {
                int maxConnectionCount = 2;
                var dict = room.connectionPoints;

                if (dict.Count == 0)
                {
                    Log.Error("ROOM HAS NO CONNECTION POINTS, OH NO!");
                    continue;
                }

                int key = 0;
                Tuple<List<Position>, List<Position>> tuple = null;
                bool roomsAlreadyConnected = false;

                while(dict.Count > 0)
                {
                    key = dict.Keys.ElementAt(Game.Random.Next(0, Math.Min(dict.Count, maxConnectionCount + 1)));
                    tuple = dict[key];

                    roomsAlreadyConnected = roomsConnected.Any(connection =>
                         (connection.Item1 == room.Nr && connection.Item2 == key) ||
                         (connection.Item1 == key && connection.Item2 == room.Nr)
                    );

                    if(roomsAlreadyConnected)
                    {
                        dict.Remove(key);
                    }
                    else
                    {
                        var startPositions = tuple.Item1;
                        var endPositions = tuple.Item2;

                        var connectionIndex = Game.Random.Next(startPositions.Count);

                        var start = startPositions[connectionIndex];
                        var end = endPositions[connectionIndex];

                        var path = GetRandomLineNonDiagonal(start, end);

                        foreach (Position pos in path)
                        {
                            RemoveTerrain(pos);
                            roomNrs[pos.X, pos.Y] = room.Nr;
                        }

                        PlaceTerrain(start, CreateDoor());
                        PlaceTerrain(end, CreateDoor());

                        roomsConnected.Add(new Tuple<int, int>(room.Nr, key));
                    }
                }            
            }
            */
        }

        void CreateStructures()
        {
            GameData data = GameData.Instance;

            // place random berry bushes
            int bushCount = (int)Math.Round(rooms.Count * 1.5);

            for (int i = 0; i < bushCount; i++)
            {
                int roomIndex = Game.Random.Next(rooms.Count);
                var freePositions = rooms[roomIndex].freePositions;
                var spawnPos = Position.Zero;

                do
                {
                    spawnPos = freePositions[Game.Random.Next(freePositions.Count)];
                } while (IsTileBlocked(spawnPos));

                int bush = data.CreateStructure("bush");

                PlaceStructure(spawnPos, bush);

                // create berries to grow on bush
                int berries = data.CreateTemplateItem("berry");

                // add to bush
                var interactable = EntityManager.GetComponent<InteractableComponent>(bush);
                interactable.Items.Add(berries);

                // create random properties for berries
                var substance = EntityManager.GetComponent<SubstanceComponent>(berries);
                substance.Properties = ItemSystem.GenerateRandomProperties(2, -30, 30, true);

                // randomize amount
                EntityManager.GetComponent<ItemComponent>(berries).Count = Game.Random.Next(1, 4);
            }
        }

        void SpawnEnemies()
        {
            GameData data = GameData.Instance;
            List<string> names = data.GetCharacterNames();

            foreach (var room in rooms)
            {
                int maxSpawnCount = 3;
                int spawnCount = Game.Random.Next(0, maxSpawnCount + 1);

                for (int i = 0; i < spawnCount; i++)
                {
                    var pos = room.freePositions[Game.Random.Next(0, room.freePositions.Count)];
                    var name = Util.PickRandomElement(names);
                    PlaceCharacter(pos, GameData.Instance.CreateCharacter(name));
                    //Log.Data(DescriptionSystem.GetDebugInfoEntity(GetCharacter(pos)));
                    room.freePositions.Remove(pos);
                }
            }
        }

        void SpawnItems()
        {
            GameData data = GameData.Instance;
            List<string> names = data.GetItemNames();

            foreach (var room in rooms)
            {
                int maxSpawnCount = 1;
                int spawnCount = Game.Random.Next(0, maxSpawnCount + 1);

                for (int i = 0; i < spawnCount; i++)
                {
                    var pos = room.freePositions[Game.Random.Next(0, room.freePositions.Count)];
                    var name = Util.PickRandomElement(names);
                    PlaceItem(pos, GameData.Instance.CreateItem(name));
                    room.freePositions.Remove(pos);
                }
            }
        }

        void ConnectRooms(int roomNr1, int roomNr2)
        {
            Room room1 = rooms[roomNr1 - 1];
            var connection = room1.connectionPoints[roomNr2];

            // choose random path between the two chosen rooms
            int pathIndex = Game.Random.Next(0, connection.Item1.Count);

            Position from = connection.Item1[pathIndex];
            Position to = connection.Item2[pathIndex];
            var path = GetRandomLineNonDiagonal(from, to);

            // carve path
            foreach (Position pos in path)
            {
                var tile = GetTile(pos);
                if (tile.Terrain != 0)
                {
                    EntityManager.RemoveEntity(tile.Terrain);
                    tile.Terrain = 0;
                }
                //PlaceTerrain(pos, GameData.Instance.CreateTerrain("floor"));
                roomNrs[pos.X, pos.Y] = roomNr1;
            }

            //RemoveTerrain(from);
            PlaceStructure(from, CreateDoor());
            PlaceStructure(to, CreateDoor());
        }

        void ConnectRooms(Room room1, Room room2)
        {
            Position from = room1.GetPossibleDoor();
            Position to = room2.GetPossibleDoor();

            var line = GetRandomLineNonDiagonal(from, to);

            Position door1 = Position.Zero;
            Position door2 = Position.Zero;

            Position prev = Position.Zero;
            List<Position> doors = new List<Position>();
            bool insideWall = false;

            foreach (var pos in line)
            {
                if (IsSolid(pos))
                {
                    if (!insideWall)
                    {
                        doors.Add(pos);
                        insideWall = true;
                    }
                }
                else
                {
                    if (insideWall)
                    {
                        doors.Add(prev);
                        insideWall = false;
                    }
                }
                RemoveTerrain(pos);
                //PlaceTerrain(pos, GameData.Instance.CreateTerrain("floor"));
                prev = pos;
            }

            foreach (var doorPos in doors)
            {
                PlaceStructure(doorPos, CreateDoor());
            }
        }

        enum FindConnectionState
        {
            Init,
            FromFound,
            LookingForTo
        }

        // initializes the connection points between all the rooms (Positions where corridors between them start and end)
        void FindConnectionPoints()
        {
            int roomFrom = 0;
            Position from = Position.Zero;
            Position prev = Position.Zero;

            var state = FindConnectionState.Init;

            #region horizontal
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int nr = roomNrs[x, y];

                    switch (state)
                    {
                        case FindConnectionState.Init:
                            if (nr != 0)
                            {
                                roomFrom = nr;
                                state = FindConnectionState.FromFound;
                            }
                            break;

                        case FindConnectionState.FromFound:
                            if (nr == 0)
                            {
                                from = new Position(x, y);
                                state = FindConnectionState.LookingForTo;
                            }
                            break;

                        case FindConnectionState.LookingForTo:
                            if (nr != 0)
                            {
                                SaveConnection(roomFrom, nr, from, prev);
                                roomFrom = nr;
                                state = FindConnectionState.FromFound;
                                break;
                            }
                            else // check if the connection is an exact tangent to another room
                            {
                                int roomNrAbove = roomNrs[x, y - 1];
                                int roomNrBelow = roomNrs[x, y + 1];

                                if (roomNrAbove != 0)
                                {
                                    if (roomNrAbove == roomFrom)
                                    {
                                        from = new Position(x, y);
                                        break;
                                    }
                                    else
                                    {
                                        SaveConnection(roomFrom, roomNrAbove, from, new Position(x, y));
                                        roomFrom = 0;
                                        state = FindConnectionState.Init;
                                        break;
                                    }

                                }

                                if (roomNrBelow != 0)
                                {
                                    if (roomNrBelow == roomFrom)
                                    {
                                        from = new Position(x, y);
                                        break;
                                    }
                                    else
                                    {
                                        SaveConnection(roomFrom, roomNrBelow, from, new Position(x, y));
                                        roomFrom = 0;
                                        state = FindConnectionState.Init;
                                        break;
                                    }

                                }
                            }
                            break;
                    }
                    prev = new Position(x, y);
                }
                roomFrom = 0;
                state = FindConnectionState.Init;
            }
            #endregion

            roomFrom = 0;
            from = Position.Zero;
            prev = Position.Zero;

            state = FindConnectionState.Init;

            #region vertical
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int nr = roomNrs[x, y];

                    switch (state)
                    {
                        case FindConnectionState.Init:
                            if (nr != 0)
                            {
                                roomFrom = nr;
                                state = FindConnectionState.FromFound;
                            }
                            break;

                        case FindConnectionState.FromFound:
                            if (nr == 0)
                            {
                                from = new Position(x, y);
                                state = FindConnectionState.LookingForTo;
                            }
                            break;

                        case FindConnectionState.LookingForTo:
                            if (nr != 0)
                            {
                                SaveConnection(roomFrom, nr, from, prev);
                                roomFrom = nr;
                                state = FindConnectionState.FromFound;
                                break;
                            }
                            else // check if the connection is an exact tangent to another room
                            {
                                int roomNrRight = roomNrs[x + 1, y];
                                int roomNrLeft = roomNrs[x - 1, y];

                                if (roomNrRight != 0)
                                {
                                    if (roomNrRight == roomFrom)
                                    {
                                        from = new Position(x, y);
                                        break;
                                    }
                                    else
                                    {
                                        SaveConnection(roomFrom, roomNrRight, from, new Position(x, y));
                                        roomFrom = 0;
                                        state = FindConnectionState.Init;
                                        break;
                                    }

                                }

                                if (roomNrLeft != 0)
                                {
                                    if (roomNrLeft == roomFrom)
                                    {
                                        from = new Position(x, y);
                                        break;
                                    }
                                    else
                                    {
                                        SaveConnection(roomFrom, roomNrLeft, from, new Position(x, y));
                                        roomFrom = 0;
                                        state = FindConnectionState.Init;
                                        break;
                                    }

                                }
                            }
                            break;
                    }
                    prev = new Position(x, y);
                }
                roomFrom = 0;
                state = FindConnectionState.Init;
            }
            #endregion

            #region Debug output
            /*
            foreach (var room in rooms)
            {
                Log.Message("Connections FROM room " + room.Nr + ":");
                foreach (var keyValuePair in room.connectionPoints)
                {
                    Log.Message("Connections to room " + keyValuePair.Key + ":");
                    Log.Message("Start: " + Util.GetStringFromEnumerable(keyValuePair.Value.Item1));
                    Log.Message("End: " + Util.GetStringFromEnumerable(keyValuePair.Value.Item2));

                    foreach (var pos in keyValuePair.Value.Item1)
                    {
                        //roomNrs[pos.X, pos.Y] = keyValuePair.Key;
                    }

                    foreach (var pos in keyValuePair.Value.Item2)
                    {
                        //roomNrs[pos.X, pos.Y] = room.Nr;
                    }
                }
            }
            */
            #endregion
        }

        void SaveConnection(int room1, int room2, Position pos1, Position pos2)
        {
            rooms[room1 - 1].AddConnection(room2, pos1, pos2);
            rooms[room2 - 1].AddConnection(room1, pos2, pos1);
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
                return GetTile(pos).Discovered;
                //return discovered[(int)pos.X, (int)pos.Y];
            }
            catch (IndexOutOfRangeException)
            {
                Log.Error("Invalid Position! " + pos);
                throw;
            }
        }

        public int GetTerrain(Position pos)
        {
            if (IsOutOfBounds(pos))
            {
                return 0;
            }
            return GetTile(pos).Terrain;
            //return terrain[(int)pos.X, (int)pos.Y];
        }

        public bool IsSolid(Position pos)
        {
            int terrainID = GetTerrain(pos);

            if (terrainID == 0)
                return false;

            var terrainCollider = EntityManager.GetComponent<ColliderComponent>(terrainID);

            if (terrainCollider == null)
                return false;

            if (terrainCollider.Solid)
                return true;

            return false;
        }

        public int GetCharacter(Position pos)
        {
            if (IsOutOfBounds(pos))
            {
                return 0;
            }
            return GetTile(pos).Character;
            //return characters[(int)pos.X, (int)pos.Y];
        }

        public int GetFirstItem(Position pos)
        {
            if (IsOutOfBounds(pos))
            {
                return 0;
            }

            var itemsHere = GetTile(pos).Items; //items[(int)pos.X, (int)pos.Y];

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
            var itemList = GetTile(pos).Items; //items[(int)pos.X, (int)pos.Y];
            if (itemList == null)
            {
                return new int[0];
            }
            return itemList;
        }

        public int GetStructure(Position pos)
        {
            if (IsOutOfBounds(pos))
            {
                return 0;
            }
            return GetTile(pos).Structure;
        }

        //////////////////// Removal ////////////////////////////////////////

        // prepares entity for removal from floor by
        // by making the sprite invisible
        // returns true if successful
        private bool RemoveEntity(Position pos, int entity)
        {
            if (entity == 0)
            {
                Log.Warning("Trying to remove entity from " + pos + ": No entity of that type here!");
                return false;
            }

            if (IsOutOfBounds(pos))
            {
                Log.Warning("Trying to remove " + DescriptionSystem.GetNameWithID(entity) + " out of bounds! " + pos);
                return false;
            }

            var sprite = EntityManager.GetComponent<RenderableSpriteComponent>(entity);

            if (sprite != null) sprite.Visible = false;

            return true;
        }

        public bool RemoveEntityMultiTile(int entity, MultiTileComponent multiTileC)
        {           
            foreach (var offsetPos in multiTileC.OccupiedPositions[multiTileC.FlippedHorizontally])
            {
                if(IsOutOfBounds(multiTileC.Anchor + offsetPos))
                {
                    Log.Warning("Trying to remove " + DescriptionSystem.GetNameWithID(entity) + " out of bounds! " + multiTileC.Anchor);
                    return false;
                }
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

            int terrain = GetTile(pos).Terrain; //this.terrain[(int)pos.X, (int)pos.Y];

            if (!RemoveEntity(pos, terrain))
            {
                return;
            }

            GetTile(pos).Terrain = 0; // this.terrain[(int)pos.X, (int)pos.Y] = 0;
        }

        public void RemoveCharacter(Position pos)
        {     
            int character = GetTile(pos).Character; // characters[(int)pos.X, (int)pos.Y];

            if (character == 0)
            {
                Log.Warning("Trying to remove character from " + pos + ": No entity of that type here!");
                return;
            }

            var multiTileC = EntityManager.GetComponent<MultiTileComponent>(character);

            if (multiTileC != null)
            {
                if (!RemoveEntityMultiTile(character, multiTileC))
                {
                    return;
                }

                multiTileC.OccupiedPositions[multiTileC.FlippedHorizontally].ForEach(offsetPos => GetTile(multiTileC.Anchor + offsetPos).Character = 0);
            }
            else
            {
                if (!RemoveEntity(pos, character))
                {
                    return;
                }

                GetTile(pos).Character = 0;
            }
        }

        public void RemoveStructure(Position pos)
        {
            if (IsOutOfBounds(pos))
            {
                Log.Warning("Can't remove structure out of bounds! " + pos);
                return;
            }

            int structure = GetTile(pos).Structure;

            if (!RemoveEntity(pos, structure))
            {
                return;
            }

            GetTile(pos).Structure = 0;
        }

        public void RemoveItems(Position pos)
        {
            if (IsOutOfBounds(pos))
            {
                Log.Warning("Can't remove items out of bounds! " + pos);
                return;
            }

            var itemsHere = GetTile(pos).Items; // items[(int)pos.X, (int)pos.Y];

            if (itemsHere == null)
            {
                return; // nothing to remove
            }

            foreach (int item in itemsHere)
            {
                if (!RemoveEntity(pos, item))
                {
                    return;
                }
            }

            itemsHere = null;
        }

        public void RemoveItem(Position pos, int item)
        {
            if (!RemoveEntity(pos, item))
            {
                return;
            }

            var itemsHere = GetTile(pos).Items; // items[(int)pos.X, (int)pos.Y];

            if (itemsHere == null)
            {
                Log.Warning("Trying to remove item that's not here! " + pos);
                Log.Data(DescriptionSystem.GetDebugInfoEntity(item));
                return;
            }

            if (itemsHere.Count == 0 || !itemsHere.Contains(item))
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
            if (entity == 0) // can happen during init
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

        private bool PlaceEntityMultiTile(Position pos, int entity, MultiTileComponent multiTileC)
        {          
            foreach (var offsetPos in multiTileC.OccupiedPositions[multiTileC.FlippedHorizontally])
            {
                var realPos = pos + offsetPos;
                if(IsOutOfBounds(realPos))
                {
                    Log.Warning(String.Format("Trying to place Multi-tile entity {0} out of bounds! Anchor: {1} Offset: {2}", 
                        DescriptionSystem.GetNameWithID(entity), multiTileC.Anchor, offsetPos));
                    return false;
                }              
            }

            var transform = EntityManager.GetComponent<TransformComponent>(entity);

            if (transform == null)
            {
                transform = new TransformComponent();
                EntityManager.AddComponent(entity, transform);
            }

            transform.Position = pos;
            multiTileC.Anchor = pos;

            var sprite = EntityManager.GetComponent<RenderableSpriteComponent>(entity);

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

        public void PlaceTerrain(Position pos, int terrain)
        {
            if (!PlaceEntity(pos, terrain, RenderableSpriteComponent.RenderLayer.Terrain))
            {
                return;
            }
            //this.terrain[pos.X, pos.Y] = terrain;
            GetTile(pos).Terrain = terrain;
        }

        public void PlaceCharacter(Position pos, int character)
        {
            var multiTileC = EntityManager.GetComponent<MultiTileComponent>(character);

            List<Position> newPositions = new List<Position>();

            if (multiTileC != null)
            {
                if (!PlaceEntityMultiTile(pos, character, multiTileC))
                {
                    return;
                }

                foreach (var offsetPos in multiTileC.OccupiedPositions[multiTileC.FlippedHorizontally])
                {
                    newPositions.Add(pos + offsetPos);
                }
            }
            else
            {
                if (!PlaceEntity(pos, character, RenderableSpriteComponent.RenderLayer.Character))
                {
                    return;
                }

                newPositions.Add(pos);
            }

            if(newPositions.TrueForAll(newPos => GetTile(newPos).Character == 0))
            {
                newPositions.ForEach(newPos => GetTile(newPos).Character = character);
            }
            else
            { 
                Log.Warning(String.Format("Can't place {0} at {1} when there already is/are entitie(s)!",
                    DescriptionSystem.GetNameWithID(character),
                    pos));
            }
        }

        public void PlaceStructure(Position pos, int structure)
        {
            if (!PlaceEntity(pos, structure, RenderableSpriteComponent.RenderLayer.Structure))
            {
                return;
            }

            GetTile(pos).Structure = structure;
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
            if (!PlaceEntity(pos, item, RenderableSpriteComponent.RenderLayer.Item))
            {
                return;
            }

            var tile = GetTile(pos);

            if (tile.Items == null)
            {
                tile.Items = new List<int>() { item };
            }
            else
            {
                tile.Items.Add(item);
            }

            /*
            var itemsHere = items[(int)pos.X, (int)pos.Y];

            if (itemsHere == null)
            {
                items[(int)pos.X, (int)pos.Y] = new List<int>() { item };                
            }
            else
            {
                itemsHere.Add(item);
            }
            */
        }

        /* public Floor(int width, int height)
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
        }*/



        // returns shortest path
        public List<Position> GetPath(Position from, Position to)
        {
            return null;
        }

        // returns "straight" line (Bresenham)
        public List<Position> GetLine(Position from, Position to, bool stopAtSolid = true, bool onlyCardinal = false)
        {
            if (IsOutOfBounds(from) || IsOutOfBounds(to))
            {
                //Log.Error("Can't get Line between " + from + to + " - Out of bounds!");
                //return null;
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

                if (error < 0)
                {
                    error += dfast;

                    curPos += diagonalStep;

                    if (onlyCardinal)
                    {
                        result.Add(curPos - parallelStep);
                    }
                }
                else
                {
                    curPos += parallelStep;
                }

                result.Add(curPos);

                if (!stopAtSolid)
                    continue;

                if (IsSolid(curPos))
                    break;

            }
            return result;
        }

        // returns line without moving diagonally
        public List<Position> GetLineCardinal(Position from, Position to)
        {

            var line = new List<Position>() { from };

            Position verticalStep = from.Y > to.Y ? Position.Up : Position.Down;
            Position horizontalStep = from.X < to.X ? Position.Right : Position.Left;

            int deltaX = Math.Abs(to.X - from.X);
            int deltaY = Math.Abs(to.Y - from.Y);

            Position fastStep = deltaX > deltaY ? horizontalStep : verticalStep;
            Position slowStep = deltaX > deltaY ? verticalStep : horizontalStep;

            Position pos = from;



            while (pos != to)
            {
                bool up = Game.Random.Next(2) > 0;

                if (up && pos.Y != to.Y)
                {
                    pos += verticalStep;
                }
                else if (pos.X != to.X)
                {
                    pos += horizontalStep;
                }
                else
                {
                    pos += verticalStep;
                }

                //var pos = new Position(x, y);
                line.Add(pos);
            }

            return line;
        }

        public List<Position> GetRandomLineNonDiagonal(Position from, Position to)
        {
            List<Position> line = new List<Position>() { from };

            Position verticalStep = from.Y > to.Y ? Position.Up : Position.Down;
            Position horizontalStep = from.X < to.X ? Position.Right : Position.Left;

            Position pos = from;

            while (pos != to)
            {
                bool up = Game.Random.Next(2) > 0;

                if (up && pos.Y != to.Y)
                {
                    pos += verticalStep;
                }
                else if (pos.X != to.X)
                {
                    pos += horizontalStep;
                }
                else
                {
                    pos += verticalStep;
                }

                //var pos = new Position(x, y);
                line.Add(pos);
            }

            return line;
        }

        public static int CreatePlayer()
        {
            int player = EntityManager.CreateEntity(EntityType.Character);
            int playerWeapon = GameData.Instance.CreateTemplateItem("dagger");
            int playerArmor = GameData.Instance.CreateTemplateItem("lightArmor");        

            List<IComponent> playerComponents = new List<IComponent>()
            {
                new DescriptionComponent() { Name = "Player", Description = "That's you!" },
                new HealthComponent() { Amount = 30, Max = 30, RegenerationAmount = 0.1f },
                new PlayerComponent(),
                new RenderableSpriteComponent { Visible = true, Texture = "player" },
                new ColliderComponent() { Solid = false },
                new EquipmentComponent() { Weapon = playerWeapon, Armor = playerArmor },
                new InventoryComponent() { Capacity = 50 },
                new StatComponent(
                    new Dictionary<Stat, int>()
                    {
                        { Stat.Strength, 10 },
                        { Stat.Dexterity, 11 },
                        { Stat.Intelligence, 12 }
                    }
                ),
                new SubstanceKnowledgeComponent(),
                new FindableComponent()
            };

            EntityManager.AddComponents(player, playerComponents);

            Util.PlayerID = player;

            return player;
        }

        public static int CreateWall()
        {
            List<IComponent> wallComponents = new List<IComponent>()
            {
                new RenderableSpriteComponent() { Visible = true, Texture = "wall" },
                new ColliderComponent() { Solid = true },
                new DescriptionComponent() { Name = "Wall", Description = "A solid wall" }
            };

            return EntityManager.CreateEntity(wallComponents, EntityType.Terrain);
        }

        public static int CreateGold(int amount)
        {
            return EntityManager.CreateEntity(new List<IComponent>()
            {
                new DescriptionComponent() { Name = "Gold", Description = "Ohhh, shiny!" },
                new ItemComponent() { MaxCount = 999, Count = amount, Value = 1, Weight = 0.1f },
                new RenderableSpriteComponent() { Texture = "gold" }
            }, EntityType.Item);
        }

        public int CreateDoor()
        {
            return GameData.Instance.CreateStructure("door");
        }

        public void CreateRiver(Position from, Position to, int width)
        {
            Log.Message("Creating River...");

            int dx = Math.Abs(to.X - from.X);
            int dy = Math.Abs(to.Y - from.Y);

            bool horizontal = dx > dy;

            Position step = horizontal ? Position.Down : Position.Right;

            for (int i = 0; i < width; i++)
            {
                var line = GetLine(from, to, false, true);

                foreach (var pos in line)
                {
                    int terrain = GetTile(pos).Terrain;

                    if (terrain != 0)
                    {
                        RemoveTerrain(pos);
                        //PlaceTerrain(pos, GameData.Instance.CreateTerrain("floor"));
                    }

                    PlaceTerrain(pos, GameData.Instance.CreateTerrain("water"));
                }

                Log.Message("Line: " + from + to + "; Water tiles: " + line.Count);
                from += step;
                to += step;
            }
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

                for (int j = 1; j < (angleCount - 1); j++)
                {
                    result[i][j] = result[i][j - 1] + angleDifferenceHalf;
                }
            }
            return result;
        }

        public void GenerateImage(string path, GraphicsDevice gDevice)
        {
            var texFloor = new Texture2D(gDevice, Width, Height);

            Color[] colors = new Color[Width * Height];


            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var color = roomNrs[x, y] == 0 ? Color.Black : Util.Colors[(roomNrs[x, y] % (Util.Colors.Length))];
                    colors[y * Width + x % Width] = color;
                }
            }

            texFloor.SetData(colors);

            FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate);
            texFloor.SaveAsPng(fileStream, Width, Height);
        }

        public void LogCharactersAroundPlayer()
        {
            var playerPos = Util.GetPlayerPos();
            int xRadius = 5;
            int yRadius = 5;
            int xMin = Util.Clamp(playerPos.X - xRadius, 0, Width);
            int xMax = Util.Clamp(playerPos.X + xRadius, 0, Width);
            int yMin = Util.Clamp(playerPos.Y - yRadius, 0, Height);
            int yMax = Util.Clamp(playerPos.Y + yRadius, 0, Height);

            StringBuilder sb = new StringBuilder();

            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    sb.AppendFormat(" {0,5} ", GetTile(x,y).Character);
                }
                sb.AppendLine();
            }

            Log.Data("Characters in the vicinity of player: \n" + sb.ToString());
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
