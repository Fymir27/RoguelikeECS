using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using GraphUtilities;
using Microsoft.Xna.Framework.Graphics;
using System.Security.Cryptography.X509Certificates;

namespace TheAlchemist
{
    partial class Floor
    {
        class RoomVertex : Vertex
        {
            public override string ToString()
            {
                return "Room" + ID;
            }
        }

        class FragmentVertex : Vertex
        {
            public override string ToString()
            {
                return "Fragment" + ID;
            }
        }

        class Junction : FragmentVertex
        {
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

        class BasicRoom : RoomVertex { }

        class FinalRoom : RoomVertex { }

        private Tuple<ReplacementRule, int>[] GetReplacementRules()
        {
            var builder = new ReplacementRuleBuilder();

            builder.Reset()
                   .MappedVertex<BasicRoom>("a")
                   .PatternVertexWithEdge<BasicRoom, Edge>("b")
                   .MoveToTag("a").ReplacementVertexWithEdge<Junction, Edge>("j")
                   .ReplacementVertexWithEdge<BasicRoom, Edge>().MoveToTag("j")
                   .ReplacementVertexWithEdge<BasicRoom, Edge>().MapToTag("b");

            var addJunction = builder.GetResult();

            builder.Reset()
                .MappedVertex<BasicRoom>("a")
                .PatternVertexWithEdge<BasicRoom, Edge>("b").MoveToTag("a")
                .ReplacementVertexWithEdge<BasicRoom, Edge>()
                .ReplacementVertexWithEdge<BasicRoom, Edge>().MapToTag("b");

            var stretch = builder.GetResult();

            builder.Reset()
                .MappedVertex<BasicRoom>("a")
                .PatternVertexWithEdge<Junction, Edge>("j")
                .PatternVertexWithEdge<BasicRoom, Edge>("b").MoveToTag("j")
                .PatternVertexWithEdge<BasicRoom, Edge>("c").MoveToTag("a")
                .ReplacementVertexWithEdge<BasicRoom, Edge>().MapToTag("b")
                .ReplacementVertexWithEdge<BasicRoom, Edge>().MapToTag("c")
                .ReplacementEdge<Edge>().MoveToTag("a");

            var transformJunction = builder.GetResult();

            builder.Reset()
                .MappedVertex<BasicRoom>("a")
                .MappedVertexWithEdge<BasicRoom, Edge>()
                .MappedVertexWithEdge<BasicRoom, Edge>()
                .MappedVertexWithEdge<BasicRoom, Edge>()
                .ReplacementEdge<Edge>().MoveToTag("a");

            var createLoop = builder.GetResult();

            var rules = new Tuple<ReplacementRule, int>[]
            {
                Tuple.Create(addJunction, 3),
                Tuple.Create(stretch, 2),
                Tuple.Create(createLoop, 1),
                Tuple.Create(transformJunction, 1)
            };

            return rules;     
    }

        private Graph GenerateGraph()
        {  
            var builder = new ReplacementRuleBuilder();

            builder.MappedVertex<StartingRoom>("start")
                .ReplacementVertexWithEdge<BasicRoom, Edge>().ReplacementVertexWithEdge<BasicRoom, Edge>().MoveToTag("start")
                .ReplacementVertexWithEdge<BasicRoom, Edge>().ReplacementVertexWithEdge<BasicRoom, Edge>().MoveToTag("start")
                .ReplacementVertexWithEdge<BasicRoom, Edge>().ReplacementVertexWithEdge<BasicRoom, Edge>();

            var initialRule = builder.GetResult();

            var dungeon = new Graph();
            dungeon.Random = Game.Random;
            dungeon.AddVertex(new StartingRoom());
            dungeon.Replace(initialRule, true);

            for (int i = 0; i < 10; i++)
            {
                var rules = GetReplacementRules();

                builder.Reset()
                    .MappedVertex<BasicRoom>()
                    .ReplacementVertexWithEdge<BasicRoom, Edge>();

                var addRoom = builder.GetResult();

                int acc = 0;
                int[] absoluteDistribution = rules.Select(t => acc += t.Item2).ToArray();

                int endurance = 10;
                int ruleIndex;
                bool ruleSuccess;

                do
                {
                    if (endurance-- == 0)
                    {
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
            File.WriteAllText("advancedDungeon.gv", GraphPrinter.ToDot(dungeonGraph, true, true));

            var room = GenerateRoom(Position.Zero, GameData.Instance.RoomTemplates["fountain"]);

            InitPlayer(new Position(4, 6));


            List<Cycle> cycles = dungeonGraph.GetCycles(true);
            Dictionary<Vertex, List<Cycle>> belongsToCycle = new Dictionary<Vertex, List<Cycle>>();

            foreach (var vertex in dungeonGraph.Vertices)
            {
                belongsToCycle[vertex] = new List<Cycle>();
            }

            foreach (var cycle in cycles)
            {
                foreach (var vertex in cycle.Vertices())
                {
                    belongsToCycle[vertex].Add(cycle);
                }
            }

            HashSet<Vertex> verticesPlaced = new HashSet<Vertex>();
            Dictionary<Position, Vertex> grid = new Dictionary<Position, Vertex>();
          
            // tuples consist of vertex to place and position of ancestor(=neighbour)
            Stack<Tuple<Vertex, Position>> todo = new Stack<Tuple<Vertex, Position>>();

            Vertex v = dungeonGraph.Vertices.First();
            Position pos = Position.Zero;

            Position min = Position.Zero;
            Position max = Position.Zero;

            void Place(Vertex vertex, Position position)
            {
                min.X = Math.Min(min.X, position.X);
                min.Y = Math.Min(min.Y, position.Y);
                max.X = Math.Max(max.X, position.X);
                max.Y = Math.Max(max.Y, position.Y);

                grid.Add(position, vertex);
                verticesPlaced.Add(vertex);

                foreach (var neighbour in vertex.Edges.Select(e => 
                    e.GetOtherVertex(vertex)).Where(n => 
                        !verticesPlaced.Contains(n) && 
                        !(todo.Select(t => t.Item1).Contains(n))))
                {
                    todo.Push(Tuple.Create(neighbour, position));
                }

                var tmpLayout = DrawLayout(grid, min, max);
                File.WriteAllText("roomLayout.txt", tmpLayout);
            }

            Place(v, pos);

            try
            {

                int totalVertexCount = dungeonGraph.Vertices.Count;
                while (verticesPlaced.Count < totalVertexCount)
                {
                    // get next neighbour that hasn't been placed yet
                    while (verticesPlaced.Contains(v))
                    {
                        var next = todo.Pop();
                        v = next.Item1;
                        pos = next.Item2;
                    }

                    if (belongsToCycle[v].Count == 0)
                    {
                        var newPos = pos;
                        foreach (var dir in Position.HexDirections)
                        {
                            if (!grid.ContainsKey(pos + dir))
                            {
                                newPos = pos + dir;
                            }
                        }
                        if (newPos == pos)
                        {
                            // TODO:
                            throw new Exception("No neighbours reachable!");
                        }

                        Place(v, newPos);
                        pos = newPos;
                        continue;
                    }
                    
                    List<Cycle> inCycles = belongsToCycle[v];
                    if (inCycles.Count > 1)
                    {
                        if (inCycles.Count > 2)
                        {
                            throw new Exception("Vertex in more than 2 cylces... Can't handle that (yet)!");
                        }

                        var overlaps = inCycles[0].Overlap(inCycles[1]);

                        if (overlaps.Count > 1)
                        {
                            throw new Exception("Can't handle cycles with more than one overlap!");
                        }

                        var oldPos = pos;
                        int sharedVertexCount = overlaps.First().Count + 1; // vertices = edges + 1
                        var sharedLine = new List<Position>();
                        for (int directionIndex = 0; directionIndex < 6; directionIndex++)
                        {
                            var dir = Position.HexDirections[directionIndex];
                            pos = oldPos;
                            sharedLine.Clear();

                            for (int i = 0; i < sharedVertexCount; i++)
                            {
                                pos += dir;
                                if (grid.ContainsKey(pos))
                                {
                                    break;
                                }
                                sharedLine.Add(pos);
                            }

                            if (sharedLine.Count != sharedVertexCount)
                                continue; // try other direction

                            Place(v, sharedLine[0]);
                            for (int i = 0; i < sharedVertexCount - 1; i++)
                            {                               
                                v = overlaps.First()[i].GetOtherVertex(v);
                                Place(v, sharedLine[i + 1]);                                
                            }

                            var dir0 = Position.HexDirections[(directionIndex + 1) % 6];
                            var dir1 = Position.HexDirections[(directionIndex + 5) % 6];                                    

                            var cycle0 = sharedLine[sharedVertexCount - 1].HexCircle(sharedLine.ToArray(), inCycles[0].EdgeCount, 1);
                            var cycle1 = sharedLine[sharedVertexCount - 1].HexCircle(sharedLine.ToArray(), inCycles[1].EdgeCount, -1);

                            var overlappingCyclesPositions = new Position[][] { cycle0, cycle1 };

                            bool cyclesPlacable = overlappingCyclesPositions.All(c => c.All(p => sharedLine.Contains(p) || !grid.ContainsKey(p)));

                            if (!cyclesPlacable)
                                continue; // try new direction

                            var overlappingCycles = new List<Vertex>[] { inCycles[0].Vertices(), inCycles[1].Vertices() };
                                       
                            var overlap = overlaps.First();

                            for(int cycleIndex = 0; cycleIndex < overlappingCycles.Count(); cycleIndex++) 
                            {
                                var cycleVerts = overlappingCycles[cycleIndex];
                                var cyclePositions = overlappingCyclesPositions[cycleIndex];

                                // check if vertices in cycle are (counter)clockwise
                                bool increaseIndex = overlap.First().GetOtherVertex(cycleVerts[0]) == cycleVerts[1];
                                int increment = increaseIndex ? 1 : cycleVerts.Count - 1;

                                int vertIndex = 0;
                                int posIndex = 0;
                                while(posIndex < cyclePositions.Count())
                                {
                                    if (!sharedLine.Contains(cyclePositions[posIndex]))
                                    {
                                        pos = cyclePositions[posIndex];
                                        v = cycleVerts[vertIndex];
                                        Place(v, pos);
                                    }
                                    posIndex++;
                                    vertIndex = (vertIndex + increment) % cycleVerts.Count;
                                }                                                                          
                            }
                            break;
                        }
                    }
                    else
                    {
                        Cycle cycle = inCycles.First();
                        int circumference = cycle.EdgeCount;
                        var cycleVertices = cycle.Vertices();

                        var newPos = pos;
                        foreach (var dir in Position.HexDirections)
                        {
                            var circlePositions = newPos.HexCircle(dir, circumference);
                            bool circlePlacable = circlePositions.All(p => !grid.ContainsKey(p));

                            if (circlePlacable)
                            {
                                newPos = pos + dir;
                                int cycleOffset = cycleVertices.IndexOf(v);
                                int circleOffset = Array.IndexOf(circlePositions, newPos);
                                for (int i = 0; i < circlePositions.Length; i++)
                                {
                                    int cycleIndex = (i + cycleOffset) % circlePositions.Length;
                                    v = cycleVertices[cycleIndex];
                                    int circleIndex = (i + circleOffset) % circlePositions.Length;
                                    newPos = circlePositions[circleIndex];
                                    Place(v, newPos);
                                    belongsToCycle[v].Clear();

                                    foreach (var neighbour in v.Edges.Select(e => e.GetOtherVertex(v)).Where(n => !verticesPlaced.Contains(n)))
                                    {
                                        if (!cycleVertices.Contains(neighbour))
                                        {
                                            todo.Push(Tuple.Create(neighbour, newPos));
                                        }
                                    }
                                }
                                break;
                            }
                        }
                        if (newPos == pos)
                        {
                            // TODO:
                            throw new Exception("Circle not placable!");
                        }
                        pos = newPos;
                    }                    
                }
            } 
            catch(Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.Message);
            }
          
            var layoutAsText = DrawLayout(grid, min, max);
            File.WriteAllText("roomLayout.txt", layoutAsText);
        }

        public string DrawLayout(Dictionary<Position, Vertex> roomLayout, Position min, Position max)
        {
            StringBuilder sb = new StringBuilder();

            int tileWidth = 3;

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

        public Tile[,] GenerateRoom(Position pos, RoomTemplate template, bool random = true, int layoutIndex = 0)
        {
            char[,] layout;

            if(random)
            {
                layout = Util.PickRandomElement(template.Layouts);
            }
            else if(layoutIndex >= 0 && layoutIndex < template.Layouts.Count)
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

            var room = new Tile[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char symbol = layout[x, y];

                    TileTemplate tileTemplate;
                    Position tilePos = pos + new Position(x, y);

                    if(IsOutOfBounds(tilePos))
                    {
                        Log.Warning("Trying to place tile out of bounds! " + tilePos);
                    }

                    if(RoomTemplate.DefaultTiles.TryGetValue(symbol, out tileTemplate))
                    {
                        InitTileFromTemplate(tilePos, tileTemplate);
                    }
                    else if(template.CustomTiles.TryGetValue(symbol, out tileTemplate))
                    {
                        InitTileFromTemplate(tilePos, tileTemplate);
                    }
                    else
                    {
                        Log.Warning("Unkown symbol from Layout: " + symbol);
                    }
                }
            }

            return room;
        }
    }
}
