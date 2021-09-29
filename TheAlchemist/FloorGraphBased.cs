using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using GraphUtilities;
using Microsoft.Xna.Framework;
using TheAlchemist.GraphUtilities;

namespace TheAlchemist
{
    partial class Floor
    {
        abstract class RoomVertex : Vertex
        {
            public string roomTemplateName = "empty";

            public RoomVertex()
            {
                Data = "empty";
            }

            public RoomVertex(string roomTemplateName)
            {
                this.roomTemplateName = roomTemplateName;
                Data = roomTemplateName;
            }

            public override string ToString()
            {
                return "Room" + ID;
            }
        }

        class EmptyRoom : RoomVertex
        {
        }

        class Junction : RoomVertex
        {
            public Junction() : base("junction")
            {
            }

            public override string ToString()
            {
                return "Junc." + ID;
            }
        }

        class StartingRoom : RoomVertex
        {
            public override string ToString()
            {
                return "Start";
            }
        }

        class FountainRoom : RoomVertex
        {
            public FountainRoom() : base("fountain")
            {
            }

            public override string ToString()
            {
                return "Fount." + ID;
            }
        }

        class RandomMechsRoom : RoomVertex
        {
            public RandomMechsRoom() : base("randomMechs")
            {
            }

            public override string ToString()
            {
                return "Rand.Mechs." + ID;
            }
        }

        private Tuple<ReplacementRule, int>[] GetReplacementRules()
        {
            var builder = new ReplacementRuleBuilder();

            builder.Reset()
                .MappedVertex<EmptyRoom>("a")
                .PatternVertexWithEdge<EmptyRoom, Edge>("b")
                .MoveToTag("a").ReplacementVertexWithEdge<Junction, Edge>("j")
                .ReplacementVertexWithEdge<EmptyRoom, Edge>().MoveToTag("j")
                .ReplacementVertexWithEdge<EmptyRoom, Edge>().MapToTag("b");

            var addJunction = builder.GetResult();

            builder.Reset()
                .MappedVertex<EmptyRoom>("a")
                .PatternVertexWithEdge<EmptyRoom, Edge>("b").MoveToTag("a")
                .ReplacementVertexWithEdge<EmptyRoom, Edge>()
                .ReplacementVertexWithEdge<EmptyRoom, Edge>().MapToTag("b");

            var stretch = builder.GetResult();

            builder.Reset()
                .MappedVertex<EmptyRoom>("a")
                .PatternVertexWithEdge<Junction, Edge>("j")
                .PatternVertexWithEdge<EmptyRoom, Edge>("b").MoveToTag("j")
                .PatternVertexWithEdge<EmptyRoom, Edge>("c").MoveToTag("a")
                .ReplacementVertexWithEdge<EmptyRoom, Edge>().MapToTag("b")
                .ReplacementVertexWithEdge<EmptyRoom, Edge>().MapToTag("c")
                .ReplacementEdge<Edge>().MoveToTag("a");

            var transformJunction = builder.GetResult();

            builder.Reset()
                .MappedVertex<EmptyRoom>("a")
                .MappedVertexWithEdge<EmptyRoom, Edge>()
                .MappedVertexWithEdge<EmptyRoom, Edge>()
                .MappedVertexWithEdge<EmptyRoom, Edge>()
                .ReplacementEdge<Edge>().MoveToTag("a");

            var createLoop = builder.GetResult();

            builder.Reset()
                .MappedVertex<EmptyRoom>()
                .ReplacementEdge<Edge>()
                .ReplacementVertex<FountainRoom>();


            var createFountain = builder.GetResult();

            var randomMechs = new ReplacementRule();
            var pattern = new Graph();
            pattern.AddVertex(new EmptyRoom());
            randomMechs.Pattern = pattern;

            var replacement = new Graph();
            replacement.AddVertex(new RandomMechsRoom());
            randomMechs.Replacement = replacement;

            randomMechs.Mapping = new Dictionary<Vertex, Vertex>()
            {
                { pattern.Vertices.First(), replacement.Vertices.First() }
            };

            var rules = new[]
            {
                Tuple.Create(addJunction, 3),
                Tuple.Create(stretch, 2),
                Tuple.Create(createLoop, 1),
                Tuple.Create(transformJunction, 1),
                Tuple.Create(createFountain, 1),
                Tuple.Create(randomMechs, 4)
            };

            return rules;
        }

        private Graph GenerateGraph()
        {
            var builder = new ReplacementRuleBuilder();

            builder.MappedVertex<StartingRoom>("start")
                .ReplacementVertexWithEdge<EmptyRoom, Edge>().ReplacementVertexWithEdge<EmptyRoom, Edge>()
                .MoveToTag("start")
                .ReplacementVertexWithEdge<EmptyRoom, Edge>().ReplacementVertexWithEdge<EmptyRoom, Edge>()
                .MoveToTag("start")
                .ReplacementVertexWithEdge<EmptyRoom, Edge>().ReplacementVertexWithEdge<EmptyRoom, Edge>();

            var initialRule = builder.GetResult();

            var dungeon = new Graph();
            dungeon.Random = Game.Random;
            dungeon.AddVertex(new StartingRoom());
            dungeon.Replace(initialRule, true);


            for (int i = 0; i < 20; i++)
            {
                int endurance = 10;
                int ruleIndex;
                bool ruleSuccess;

                var rules = GetReplacementRules();
                int acc = 0;
                int[] absoluteDistribution = rules.Select(t => acc += t.Item2).ToArray();

                do
                {
                    if (endurance-- == 0)
                    {
                        builder.Reset()
                            .MappedVertex<EmptyRoom>()
                            .ReplacementVertexWithEdge<EmptyRoom, Edge>();

                        var addRoom = builder.GetResult();
                        dungeon.Replace(addRoom, true);
                        break;
                    }

                    int r = Game.Random.Next(acc);

                    for (ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
                    {
                        if (r < absoluteDistribution[ruleIndex])
                        {
                            break;
                        }
                    }

                    ruleSuccess = dungeon.Replace(rules[ruleIndex].Item1, true);
                } while (!ruleSuccess);
            }

            return dungeon;
        }

        public void GenerateGraphBased()
        {
            // instantiate tiles and set terrain
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tiles[x, y] = new Tile();
                    //PlaceTerrain(new Position(x, y), Crea());
                }
            }

            Graph dungeonGraph = GenerateGraph();
            File.WriteAllText("advancedDungeon.gv", dungeonGraph.ToDot(false));

            //IEnumerable<Cycle> cycles = dungeonGraph.GetCycles(true);
            Dictionary<Vertex, List<Cycle>> belongsToCycle = new Dictionary<Vertex, List<Cycle>>();

            foreach (var vertex in dungeonGraph.Vertices)
            {
                belongsToCycle[vertex] = new List<Cycle>();
            }

            /*
            foreach (var cycle in cycles)
            {
                foreach (var vertex in cycle.Vertices())
                {
                    belongsToCycle[vertex].Add(cycle);
                }
            }
            */
            
            HashSet<Vertex> verticesPlaced = new HashSet<Vertex>();
            Dictionary<Position, Vertex> grid = new Dictionary<Position, Vertex>();
            

            bool Placeable(Position p)
            {
                return !grid.ContainsKey(p);
            }

            List<Tuple<Position, int>> DjkstraMap(Position from, Position to, int threshold)
            {
                var result = new List<Tuple<Position, int>>();

                var posRequired = from.GetNeighboursHexPointyTop().Where(Placeable);
                var posRequiredCount = posRequired.Count();

                var distances = new Dictionary<Position, int>();
                var todo = new Queue<Position>();

                distances.Add(to, 0);
                todo.Enqueue(to);

                int requiredPositionsFound = 0;
                while (todo.Count > 0)
                {
                    var curPos = todo.Dequeue();
                    foreach (var neighbour in curPos.GetNeighboursHexPointyTop().Where(Placeable))
                    {
                        if (distances.ContainsKey(neighbour)) continue; // already visited
                        int newDist = distances[curPos] + 1;
                        if (newDist > threshold) continue;
                        if (posRequired.Contains(neighbour))
                        {
                            result.Add(Tuple.Create(neighbour, newDist));
                            if (++requiredPositionsFound == posRequiredCount) return result;
                        }

                        distances.Add(neighbour, newDist);
                        todo.Enqueue(neighbour);
                    }
                }

                return result;
            }

            List<Tuple<Position, int>> ValidNextPositionsForCycle(
                Cycle cycle,
                Position curPos,
                Vertex curVert,
                Vertex vertToAdd)
            {
                var cycleVerts = cycle.Vertices();

                int CyclicIndex(int i)
                {
                    int remainder = i % cycleVerts.Count;
                    return remainder < 0 ? cycleVerts.Count + remainder : remainder;
                }

                int indexOfVertToAdd = cycleVerts.IndexOf(vertToAdd);
                if (indexOfVertToAdd == -1)
                {
                    throw new ArgumentException("Cycle doesn't contain vertToAdd");
                }

                int nextIndex = CyclicIndex(indexOfVertToAdd + 1);
                int forwardStep = cycleVerts[nextIndex] == curVert ? -1 : 1;


                int distance = 0;
                int firstIndexToCheck = CyclicIndex(indexOfVertToAdd + forwardStep);
                for (int i = firstIndexToCheck; i != indexOfVertToAdd; i = CyclicIndex(i + forwardStep))
                {
                    distance++;
                    var vertToCheck = cycleVerts[i];
                    if (!verticesPlaced.Contains(vertToCheck)) continue;
                    var to = grid.First(t => t.Value == vertToCheck).Key;
                    var distances = DjkstraMap(curPos, to, distance + 1);
                    return distances.Where(t => t.Item2 <= distance).ToList();
                }

                return curPos.GetNeighboursHexPointyTop().Where(Placeable).Select(p => Tuple.Create(p, 1)).ToList();
            }

            IEnumerable<Position> NextPosForCycles(List<Cycle> inCycles, Position curPos, Vertex curVert,
                Vertex vertToAdd)
            {
                var result = new List<Position>();
                foreach (var cycle in inCycles)
                {
                    var nextForCycle = ValidNextPositionsForCycle(cycle, curPos, curVert, vertToAdd)
                        .Select(t => t.Item1);
                    result = result.Count > 0 ? result.Intersect(nextForCycle).ToList() : nextForCycle.ToList();
                }

                return result;
            }

            HashSet<Position> Place(Vertex vertex, Position position, bool debug = false)
            {
                Log.Message("Trying to placing vertex " + vertex);
                if (!Placeable(position) || verticesPlaced.Contains(vertex))
                    return null;

                grid.Add(position, vertex);
                verticesPlaced.Add(vertex);

                if (debug)
                {
                    Console.WriteLine(vertex + " " + position);

                    // write layout to text file for debugging
                    var localMin = Position.Zero;
                    var localMax = Position.Zero;
                    foreach (var pos in grid.Keys)
                    {
                        localMin.X = Math.Min(localMin.X, pos.X);
                        localMin.Y = Math.Min(localMin.Y, pos.Y);
                        localMax.X = Math.Max(localMax.X, pos.X);
                        localMax.Y = Math.Max(localMax.Y, pos.Y);
                    }

                    File.WriteAllText("roomLayout.txt", DrawLayout(grid, localMin, localMax));
                }

                var positionsPlacedFromCur = new HashSet<Position>() { position };
                foreach (var neighbour in vertex.Edges.Select(e =>
                    e.GetOtherVertex(vertex)).Where(other => !verticesPlaced.Contains(other)))
                {
                    IEnumerable<Position> validPositions;
                    if (belongsToCycle[neighbour].Count > 0)
                    {
                        validPositions = NextPosForCycles(belongsToCycle[neighbour], position, vertex, neighbour);
                    }
                    else
                    {
                        validPositions = position.GetNeighboursHexPointyTop().Where(Placeable);
                    }

                    bool success = false;
                    foreach (var neighbourPos in validPositions)
                    {
                        var newPositions = Place(neighbour, neighbourPos, debug); // recursive call
                        if (newPositions == null) continue;
                        success = true;
                        positionsPlacedFromCur.UnionWith(newPositions);
                        break;
                    }

                    if (!success)
                    {
                        // no neighbour position was found therefore we abort and
                        // clean up every pos placed from here (including the current position)
                        foreach (var pos in positionsPlacedFromCur)
                        {
                            verticesPlaced.Remove(grid[pos]);
                            grid.Remove(pos);
                        }

                        return null;
                    }
                }

                return positionsPlacedFromCur;
            }

            int vertexCount = dungeonGraph.Vertices.Count;
            var realPositions =
                PlaceForceBased(dungeonGraph);

            var minDist = float.MaxValue;
            var maxDist = float.MinValue;
            for (int i = 0; i < vertexCount; i++)
            {
                var vertexA = dungeonGraph.Vertices[i];

                foreach (var vertexB in vertexA.Neighbours())
                {
                    var delta = realPositions[vertexB] - realPositions[vertexA];
                    float dist = delta.Length();
                    if (dist > maxDist)
                    {
                        maxDist = dist;
                    }

                    if (dist < minDist)
                    {
                        minDist = dist;
                    }
                }
            }

            var roomTiles = new TileTemplate[vertexCount][,];
            float maxRoomCircumference = 0f;
            for (int i = 0; i < vertexCount; i++)
            {
                var vertex = dungeonGraph.Vertices[i];
                RoomTemplate roomTemplate = GameData.Instance.RoomTemplates["empty"];
                var templateName = vertex.Data;
                if (GameData.Instance.RoomTemplates.ContainsKey(templateName))
                {
                    roomTemplate = GameData.Instance.RoomTemplates[templateName];
                }
                else
                {
                    Log.Warning($"Unknown room template '{templateName}' for room '{vertex}'");
                }

                roomTiles[i] = GenerateRoom(roomTemplate);
                var roomWidth = roomTiles[i].GetLength(0);
                var roomHeight = roomTiles[i].GetLength(1);
                var roomCircumference = MathF.Ceiling(MathF.Sqrt(roomWidth * roomWidth + roomHeight * roomHeight));
                if (roomCircumference > maxRoomCircumference)
                {
                    maxRoomCircumference = roomCircumference;
                }
            }

            var minRealX = realPositions.Select(pair => pair.Value.X).Min();
            var minRealY = realPositions.Select(pair => pair.Value.Y).Min();

            float scale = (maxRoomCircumference + 1) / minDist;

            var roomAnchorPositions = realPositions.Select
            (
                pair => new Position
                (
                    (int)MathF.Round((pair.Value.X - minRealX) * scale) + 1, // +1 for left border
                    (int)MathF.Round((pair.Value.Y - minRealY) * scale) + 1 // +1 for top border
                )
            ).ToList();

            var xs = roomAnchorPositions.Select(pos => pos.X);
            var ys = roomAnchorPositions.Select(pos => pos.Y);

            width = xs.Max() + (int)MathF.Round(maxRoomCircumference) + 1; // +1 for right border
            height = ys.Max() + (int)MathF.Round(maxRoomCircumference) + 1; // +1 for bottom border

            tiles = new Tile[width, height];
            var asciiMap = new char[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    asciiMap[x, y] = '#';
                    tiles[x, y] = new Tile();
                    PlaceTerrain(new Position(x, y), CreateWall());
                }
            }

            CarveOutRoomSpace(roomAnchorPositions, roomTiles, asciiMap);

            ConnectRooms(dungeonGraph, roomAnchorPositions, roomTiles);
            
            InitRoomTiles(roomAnchorPositions, roomTiles);

            var sb = new StringBuilder();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    sb.Append(asciiMap[x, y]);
                }
                sb.AppendLine();
            }

            File.WriteAllText("asciiMap.txt", sb.ToString());

            //CreateRoomsFromGrid(roomLayout);

            //CreateStructures();
            //SpawnEnemies();
            //SpawnItems();
        }

        private void InitRoomTiles(List<Position> roomAnchorPositions, TileTemplate[][,] rooms)
        {
            for (int i = 0; i < roomAnchorPositions.Count; i++)
            {
                var room = rooms[i];
                var roomSize = new Position(room.GetLength(0), room.GetLength(1));
                for (int roomY = 0; roomY < roomSize.Y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.X; roomX++)
                    {
                        var globalPos = roomAnchorPositions[i] + new Position(roomX, roomY);
                        InitTileFromTemplate(globalPos, room[roomX, roomY]);
                        if (!IsSolid(globalPos) && Util.PlayerID == 0)
                        {
                            InitPlayer(globalPos);
                        }
                    }
                }
            }
        }

        private void CarveOutRoomSpace(List<Position> roomAnchorPositions, TileTemplate[][,] rooms, char[,] asciiMap)
        {
            for (int i = 0; i < roomAnchorPositions.Count; i++)
            {
                var room = rooms[i];
                var vertexName = i.ToString(); //dungeonGraph.Vertices[i].ID);
                var roomSize = new Position(room.GetLength(0), room.GetLength(1));
                for (int roomY = 0; roomY < roomSize.Y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.X; roomX++)
                    {
                        var globalPos = roomAnchorPositions[i] + new Position(roomX, roomY);
                        RemoveTerrain(globalPos);

                        if (roomY == 0 && roomX < vertexName.Length)
                        {
                            asciiMap[globalPos.X, globalPos.Y] = vertexName[roomX];
                        }
                        else
                        {
                            asciiMap[globalPos.X, globalPos.Y] = ' ';
                        }
                    }
                }
            }
        }

        private void ConnectRooms(Graph dungeonGraph, List<Position> roomAnchorPositions, TileTemplate[][,] rooms)
        {
            int vertexCount = dungeonGraph.Vertices.Count;
            var vertexIndices = new Dictionary<Vertex, int>();
            var roomsConnected = new List<int>[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                vertexIndices[dungeonGraph.Vertices[i]] = i;
                roomsConnected[i] = new List<int>();
            }

            var doors = new int[width, height];
            void PlaceDoor(Position pos)
            {
                RemoveTerrain(pos);
                int door = CreateDoor();
                doors[pos.X, pos.Y] = door;
                PlaceStructure(pos, door);
            }

            for (int roomIndex = 0; roomIndex < roomAnchorPositions.Count; roomIndex++)
            {
                var room = rooms[roomIndex];
                var roomBox = new Rectangle
                (
                    roomAnchorPositions[roomIndex].X,
                    roomAnchorPositions[roomIndex].Y,
                    room.GetLength(0),
                    room.GetLength(1)
                );
                var connectedTo = roomsConnected[roomIndex];

                bool IsUnconnected(Vertex vertex) => !connectedTo.Contains(vertexIndices[vertex]);

                foreach (var neighbour in dungeonGraph.Vertices[roomIndex].Neighbours().Where(IsUnconnected))
                {
                    int neighbourIndex = vertexIndices[neighbour];
                    var otherRoom = rooms[neighbourIndex];
                    var otherRoomBox = new Rectangle
                    (
                        roomAnchorPositions[neighbourIndex].X,
                        roomAnchorPositions[neighbourIndex].Y,
                        otherRoom.GetLength(0),
                        otherRoom.GetLength(1)
                    );

                    var connection = GetLineCardinal(new Position(roomBox.Center), new Position(otherRoomBox.Center));
                    Position? exitToThisRoom = null;
                    Position? entryToNewRoom = null;
                    foreach (var connectionPos in connection)
                    {   
                        if (doors[connectionPos.X, connectionPos.Y] != 0)
                        {
                            exitToThisRoom = connectionPos;
                            continue;
                        }
                        
                        if (!IsSolid(connectionPos)) 
                            continue;

                        var neighbours = connectionPos.GetNeighbours();

                        if (neighbours.Any(pos => roomBox.Contains(pos.X, pos.Y)))
                        {
                            exitToThisRoom = connectionPos;
                        }
                        else if (neighbours.Any(pos => otherRoomBox.Contains(pos.X, pos.Y)))
                        {
                            entryToNewRoom = connectionPos;
                            break;
                        }
                        else
                        {
                            RemoveTerrain(connectionPos); // hallway
                        }
                    }

                    try
                    {
                        PlaceDoor(exitToThisRoom ?? throw new Exception("Failed to find exit to room"));
                        PlaceDoor(entryToNewRoom ?? throw new Exception("Failed to find entry to new room"));
                    }
                    catch
                    {
                        Log.Warning($"Exit/Entry to room not found! {dungeonGraph.Vertices[roomIndex].ID}->{dungeonGraph.Vertices[neighbourIndex].ID}");
                    }

                    connectedTo.Add(neighbourIndex);
                    roomsConnected[neighbourIndex].Add(roomIndex);
                }
            }
        }

        private Dictionary<Vertex, Vector2> PlaceForceBased(Graph dungeonGraph)
        {
            int vertexCount = dungeonGraph.Vertices.Count;

            var random = new Random(Game.Seed);
            int[] xPositions = Enumerable.Range(0, vertexCount).OrderBy(_ => random.Next()).ToArray();
            int[] yPositions = Enumerable.Range(0, vertexCount).OrderBy(_ => random.Next()).ToArray();

            // randomly place vertices
            var positions = new Dictionary<Vertex, Vector2>();
            for (var i = 0; i < vertexCount; i++)
            {
                var pos = new Vector2(xPositions[i], yPositions[i]);
                positions.Add(dungeonGraph.Vertices[i], pos);
            }

            var graphDistances = dungeonGraph.CalculateDistances();
            var graphDistancesLookup = new Dictionary<Vertex, int>[vertexCount];
            for (int fromVertexIndex = 0; fromVertexIndex < vertexCount; fromVertexIndex++)
            {
                graphDistancesLookup[fromVertexIndex] = new Dictionary<Vertex, int>();
                for (int toVertexIndex = 0; toVertexIndex < vertexCount; toVertexIndex++)
                {
                    graphDistancesLookup[fromVertexIndex][dungeonGraph.Vertices[toVertexIndex]] =
                        graphDistances[fromVertexIndex, toVertexIndex];
                }
            }

            const int iterationCount = 1000;
            for (var i = 0; i < iterationCount; i++)
            {
                var forces = new Vector2[vertexCount];
                for (var vi = 0; vi < vertexCount; vi++)
                {
                    var vertex = dungeonGraph.Vertices[vi];
                    forces[vi] = CalculateForce(vertex, dungeonGraph, positions, graphDistancesLookup[vi]);
                }

                for (var vi = 0; vi < vertexCount; vi++)
                {
                    var vertex = dungeonGraph.Vertices[vi];
                    positions[vertex] += forces[vi] * -0.1f;
                }
            }

            Log.Data("Xs", Util.SerializeObject(positions.Select(t => t.Value.X).ToArray()));
            Log.Data("Ys", Util.SerializeObject(positions.Select(t => t.Value.Y).ToArray()));
            Log.Data("Labels", Util.SerializeObject(positions.Select(pair => pair.Key.ID).ToArray()));
            return positions;
        }

        private Vector2 CalculateForce(Vertex vertex, Graph dungeonGraph, Dictionary<Vertex, Vector2> simulatedPositions, Dictionary<Vertex, int> graphDistances)
        {
            const float k = .1f;
            var fTotal = Vector2.Zero;
            foreach (var (otherVertex, simulatedPosition) in simulatedPositions)
            {
                if (otherVertex == vertex)
                    continue;

                var r = graphDistances[otherVertex]; //dungeonGraph.Distance(vertex, otherVertex);
                var simulatedDelta = simulatedPosition - simulatedPositions[vertex];
                var simulatedDistance = simulatedDelta.Length();
                fTotal += -k * (simulatedDistance - r) * (simulatedDelta / simulatedDistance);
            }

            return fTotal;
        }

        public void CreateRoomsFromGrid(Vertex[,] roomLayout)
        {
            // instantiate tiles and set terrain
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    PlaceTerrain(new Position(x, y), CreateWall());
                }
            }

            bool playerInitialized = false;

            int roomGridWidth = roomLayout.GetLength(0);
            int roomGridHeight = roomLayout.GetLength(1);

            TileTemplate[,][,] rooms = new TileTemplate[roomGridWidth, roomGridHeight][,];

            var rowY = new int[roomGridHeight];
            rowY[0] = 1;

            int verMargin = 5;

            int maxRoomWidth = 0;

            // Generate rooms
            for (int roomGridY = 0; roomGridY < roomGridHeight; roomGridY++)
            {
                int rowHeight = 0;
                for (int roomGridX = 0; roomGridX < roomGridWidth; roomGridX++)
                {
                    var vert = roomLayout[roomGridX, roomGridY] as RoomVertex;

                    if (vert == null)
                        continue;

                    string templateName = vert.roomTemplateName;

                    RoomTemplate roomTemplate = GameData.Instance.RoomTemplates["empty"];
                    if (GameData.Instance.RoomTemplates.ContainsKey(templateName))
                    {
                        roomTemplate = GameData.Instance.RoomTemplates[templateName];
                    }
                    else
                    {
                        Log.Error($"Unknown room template '{templateName}' for room '{vert.ToString()}'");
                    }

                    TileTemplate[,] room = GenerateRoom(roomTemplate);

                    rooms[roomGridX, roomGridY] = room;

                    int roomWidth = room.GetLength(0);
                    int roomHeight = room.GetLength(1);

                    maxRoomWidth = Math.Max(maxRoomWidth, roomWidth);
                    rowHeight = Math.Max(rowHeight, roomHeight);
                }

                // set Y of next row
                if (roomGridY < (roomGridHeight - 1))
                    rowY[roomGridY + 1] = rowY[roomGridY] + rowHeight + verMargin;
            }

            int horMargin = 5;

            var anchors = new Position[roomGridWidth, roomGridHeight];

            // Carve out room shapes
            for (int roomGridX = 0; roomGridX < roomGridWidth; roomGridX++)
            {
                for (int roomGridY = 0; roomGridY < roomGridHeight; roomGridY++)
                {
                    var room = rooms[roomGridX, roomGridY];
                    if (room == null) continue;

                    var anchor = new Position(1 + maxRoomWidth / 2 * roomGridY + (maxRoomWidth + horMargin) * roomGridX,
                        rowY[roomGridY]);

                    anchors[roomGridX, roomGridY] = anchor;

                    int roomWidth = room.GetLength(0);
                    int roomHeight = room.GetLength(1);

                    Console.WriteLine("Room at: " + anchor);

                    // carve room shape
                    for (int localY = 0; localY < roomHeight; localY++)
                    {
                        for (int localX = 0; localX < roomWidth; localX++)
                        {
                            Position tilePos = anchor + new Position(localX, localY);
                            RemoveTerrain(tilePos);
                        }
                    }
                }
            }

            // Connect rooms
            for (int roomGridX = 0; roomGridX < roomGridWidth; roomGridX++)
            {
                for (int roomGridY = 0; roomGridY < roomGridHeight; roomGridY++)
                {
                    var room = rooms[roomGridX, roomGridY];
                    if (room == null)
                        continue;
                    var curRoomCenter = anchors[roomGridX, roomGridY] +
                                        new Position(room.GetLength(0) / 2, room.GetLength(1) / 2);
                    var roomGridPos = new Position(roomGridX, roomGridY);
                    var curVertex = roomLayout[roomGridX, roomGridY];

                    foreach (var neighbourRoomPos in roomGridPos.GetNeighboursHexPointyTop().Take(3))
                    {
                        if (neighbourRoomPos.X < 0 || neighbourRoomPos.X >= roomGridWidth ||
                            neighbourRoomPos.Y < 0 || neighbourRoomPos.Y >= roomGridHeight)
                            continue;

                        var neighbour = rooms[neighbourRoomPos.X, neighbourRoomPos.Y];

                        if (neighbour == null)
                            continue;

                        var neighbourVertex = roomLayout[neighbourRoomPos.X, neighbourRoomPos.Y];

                        if (!curVertex.Edges.Any(e => e.AttachedTo(neighbourVertex)))
                            continue;

                        var neighbourCenter = anchors[neighbourRoomPos.X, neighbourRoomPos.Y] +
                                              new Position(neighbour.GetLength(0) / 2, neighbour.GetLength(1) / 2);
                        var hallway = GetLineCardinal(curRoomCenter, neighbourCenter);
                        foreach (var pos in hallway)
                        {
                            var tile = GetTile(pos);
                            if (tile.Terrain > 0)
                                RemoveTerrain(pos);
                        }
                    }
                }
            }

            File.WriteAllText("asciiLayout.txt", ASCII());

            // Populate rooms
            for (int roomGridX = 0; roomGridX < roomGridWidth; roomGridX++)
            {
                for (int roomGridY = 0; roomGridY < roomGridHeight; roomGridY++)
                {
                    var room = rooms[roomGridX, roomGridY];
                    if (room == null) continue;

                    var anchor = anchors[roomGridX, roomGridY];

                    int roomWidth = room.GetLength(0);
                    int roomHeight = room.GetLength(1);

                    // place room in world
                    for (int localY = 0; localY < roomHeight; localY++)
                    {
                        for (int localX = 0; localX < roomWidth; localX++)
                        {
                            Position tilePos = anchor + new Position(localX, localY);
                            var template = room[localX, localY];
                            InitTileFromTemplate(tilePos, template);
                        }
                    }

                    if (!playerInitialized)
                    {
                        InitPlayer(anchor + Position.One);
                        playerInitialized = true;
                    }
                }
            }
        }


        public string DrawLayout(Dictionary<Position, Vertex> roomLayout, Position min, Position max)
        {
            StringBuilder sb = new StringBuilder();

            int tileWidth = 4;

            for (int y = min.Y; y <= max.Y; y++)
            {
                StringBuilder connectionRow = new StringBuilder();
                int rowNr = y + Math.Abs(min.Y);
                sb.Append(new String(' ', 2 * rowNr));
                connectionRow.Append(new String(' ', 2 * rowNr));
                for (int x = min.X; x <= max.X; x++)
                {
                    var pos = new Position(x, y);
                    if (roomLayout.ContainsKey(pos))
                    {
                        var vertex = roomLayout[pos];
                        Vertex otherVertex = null;
                        roomLayout.TryGetValue(pos + Position.Left, out otherVertex);
                        if (vertex.Edges.Any(e => e.GetOtherVertex(vertex) == otherVertex))
                        {
                            sb.Append("-");
                        }
                        else
                        {
                            sb.Append(" ");
                        }

                        sb.Append(vertex.ID.ToString().PadLeft(tileWidth, '0'));
                        /*
                        roomLayout.TryGetValue(pos + Position.Right, out otherVertex);
                        if (vertex.Edges.Any(e => e.GetOtherVertex(vertex) == otherVertex))
                        {
                            sb.Append("-");
                        }
                        else
                        {
                            sb.Append("   ");
                        }
                        */
                        roomLayout.TryGetValue(pos + Position.Down + Position.Left, out otherVertex);
                        if (vertex.Edges.Any(e => e.GetOtherVertex(vertex) == otherVertex))
                        {
                            connectionRow.Append(" /");
                        }
                        else
                        {
                            connectionRow.Append("  ");
                        }

                        connectionRow.Append(" ");
                        roomLayout.TryGetValue(pos + Position.Down, out otherVertex);
                        if (vertex.Edges.Any(e => e.GetOtherVertex(vertex) == otherVertex))
                        {
                            connectionRow.Append("\\");
                        }
                        else
                        {
                            connectionRow.Append(" ");
                        }
                    }
                    else
                    {
                        sb.Append("    ");
                        connectionRow.Append("    ");
                    }
                }

                sb.AppendLine();
                sb.AppendLine(connectionRow.ToString());
            }

            return sb.ToString();
        }

        public TileTemplate[,] GenerateRoom(RoomTemplate template, bool random = true, int layoutIndex = 0)
        {
            char[,] layout;

            if (!GameData.Instance.Tilesets.ContainsKey(template.tileset))
            {
                Log.Error("Unkown tileset: " + template.tileset);
                template.tileset = "default";
            }

            if (random)
            {
                layout = Util.PickRandomElement(template.Layouts);
            }
            else if (layoutIndex >= 0 && layoutIndex < template.Layouts.Count)
            {
                layout = template.Layouts[layoutIndex];
            }
            else
            {
                Log.Error("Invalid layout index: " + layoutIndex);
                return null;
            }

            int width = layout.GetLength(0);
            int height = layout.GetLength(1);

            var tiles = new TileTemplate[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char symbol = layout[x, y];

                    Position tilePos = new Position(x, y);

                    if (IsOutOfBounds(tilePos))
                    {
                        Log.Warning("Trying to place tile out of bounds! " + tilePos);
                    }

                    if (template.CustomTiles.ContainsKey(symbol))
                    {
                        tiles[x, y] = template.CustomTiles[symbol];
                    }
                    else
                    {
                        GameData.Instance.Tilesets.TryGetValue(template.tileset, out var tileset);
                        if (tileset == null)
                        {
                            tiles[x, y] = new TileTemplate();
                        }
                        else if (tileset.ContainsKey(symbol))
                        {
                            tiles[x, y] = tileset[symbol];
                        }
                        else
                        {
                            Log.Warning("Unkown symbol from Layout: " + symbol);
                            tiles[x, y] = new TileTemplate();
                        }
                    }
                }
            }

            return tiles;
        }
    }
}