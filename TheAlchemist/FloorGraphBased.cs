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

            Vertex v = dungeonGraph.Vertices.First();
            Position pos = Position.Zero;

            Position min = Position.Zero;
            Position max = Position.Zero;

            bool Placeable(Position p) { return !grid.ContainsKey(p); }

            // Picks neighbours with the most distance to a Position, 
            // that doesn't exceed a threshold
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

            List<List<Tuple<Position, int>>> validNextPositionsForCycles(List<Cycle> inCycles, Position curPos, Vertex curVert, Vertex vertToAdd)
            {
                var result = new List<List<Tuple<Position, int>>>();
                foreach (var cycle in inCycles)
                {
                    var cycleResult = new List<Tuple<Position, int>>();
                    var cycleVerts = cycle.Vertices();
                    // closest vertices that have already been placed (in both directions)
                    int prevIndex = -1;
                    int nextIndex = -1;
                    int vertToAddIndex = int.MaxValue;
                    for (int i = 0; i < cycleVerts.Count; i++)
                    {
                        var vert = cycleVerts[i];
                        if (vert == vertToAdd)
                        {
                            vertToAddIndex = i;
                            continue;
                        }

                        if (verticesPlaced.Contains(vert))
                        {
                            if (i > vertToAddIndex)
                            {
                                if (nextIndex < vertToAddIndex)
                                {
                                    nextIndex = i;
                                    if (prevIndex >= 0) break;
                                }
                                prevIndex = i;
                            }
                            else
                            {
                                prevIndex = i;
                                if (nextIndex < 0) nextIndex = i;
                            }
                        }
                    }

                    int pathLenToPrev = prevIndex < vertToAddIndex ? vertToAddIndex - prevIndex : vertToAddIndex + cycleVerts.Count() - prevIndex;
                    int pathLenToNext = nextIndex > vertToAddIndex ? nextIndex - vertToAddIndex : nextIndex + cycleVerts.Count() - vertToAddIndex;

                    if (prevIndex == -1 && nextIndex == -1)
                    {
                        result.Add(curPos.GetNeighboursHexPointyTop().Where(Placeable).Select(p => Tuple.Create(p, int.MaxValue)).ToList());
                        continue;
                    }


                    if (cycleVerts[prevIndex] == curVert)
                    {
                        result.Add(DjkstraMap(curPos, grid.Keys.First(k => grid[k] == cycleVerts[nextIndex]), pathLenToNext));
                        continue;
                    }

                    if (cycleVerts[nextIndex] == curVert)
                    {
                        result.Add(DjkstraMap(curPos, grid.Keys.First(k => grid[k] == cycleVerts[prevIndex]), pathLenToPrev));
                        continue;
                    }



                    var possibleStepsTowardsPrev = DjkstraMap(curPos, grid.Keys.First(k => grid[k] == cycleVerts[prevIndex]), pathLenToPrev);
                    if (possibleStepsTowardsPrev.Count() == 0)
                    {
                        result.Add(new List<Tuple<Position, int>>());
                        continue;
                    }
                    var possibleStepsTowardsNext = DjkstraMap(curPos, grid.Keys.First(k => grid[k] == cycleVerts[nextIndex]), pathLenToNext);
                    if (possibleStepsTowardsNext.Count() == 0)
                    {
                        result.Add(new List<Tuple<Position, int>>());
                        continue;
                    }
                    var possibleCombinedSteps = possibleStepsTowardsPrev.Union(possibleStepsTowardsNext);
                    result.Add(possibleCombinedSteps.ToList());
                }
                return result;
            }

            IEnumerable<Position> NextPosForCycles(List<Cycle> inCycles, Position curPos, Vertex curVert, Vertex vertToAdd)
            {
                var validPositions = validNextPositionsForCycles(inCycles, curPos, curVert, vertToAdd); // doesnt place 260 so it can reach 266
                var seed = validPositions.First();
                var positionsOrdered = validPositions.Select(l => l.OrderBy(t => t.Item2).Select(t => t.Item1));
                return positionsOrdered.Aggregate(positionsOrdered.First(), (aggr, cur) => cur.Intersect(aggr), aggr => aggr);                                        
            }

            bool Place(Vertex vertex, Position position)
            {
                if (!Placeable(position) || verticesPlaced.Contains(vertex))
                    return false;

                grid.Add(position, vertex);
                verticesPlaced.Add(vertex);

                #region drawLayout
                min.X = Math.Min(min.X, position.X);
                min.Y = Math.Min(min.Y, position.Y);
                max.X = Math.Max(max.X, position.X);
                max.Y = Math.Max(max.Y, position.Y);
                var tmpLayout = DrawLayout(grid, min, max);
                File.WriteAllText("roomLayout.txt", tmpLayout);
                #endregion

                var neighboursSuccessfullyPlaced = new List<Tuple<Position, Vertex>>();
                foreach (var neighbour in vertex.Edges.Select(e =>
                    e.GetOtherVertex(vertex)).Where(n => !verticesPlaced.Contains(n)))
                {
                    IEnumerable<Position> validPositions;
                    if (belongsToCycle[neighbour].Count > 0)
                    {
                        validPositions = NextPosForCycles(belongsToCycle[neighbour], position, vertex, neighbour).ToList();
                    }
                    else
                    {
                        validPositions = position.GetNeighboursHexPointyTop().Where(Placeable);
                    }

                    bool success = false;
                    foreach (var neighbourPos in validPositions)
                    {
                        Console.WriteLine(neighbour.ToString() + ";" + neighbourPos);
                        if (success = Place(neighbour, neighbourPos))
                        {
                            neighboursSuccessfullyPlaced.Add(Tuple.Create(neighbourPos, neighbour));
                            break;
                        }
                    }
                    if (!success)
                    {
                        foreach (var tup in neighboursSuccessfullyPlaced)
                        {
                            verticesPlaced.Remove(tup.Item2);
                            grid.Remove(tup.Item1);
                        }
                        verticesPlaced.Remove(vertex);
                        grid.Remove(position);
                        return false;
                    }
                }

                return true;
            }

            // start of recursive placing   
            try
            {
                bool success = Place(v, pos);
                if (!success) throw new Exception("Failed to generate dungeon");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.Message);
                Console.WriteLine("Graph generation failed!");
                return;
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

            var room = new Tile[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char symbol = layout[x, y];

                    TileTemplate tileTemplate;
                    Position tilePos = pos + new Position(x, y);

                    if (IsOutOfBounds(tilePos))
                    {
                        Log.Warning("Trying to place tile out of bounds! " + tilePos);
                    }

                    if (RoomTemplate.DefaultTiles.TryGetValue(symbol, out tileTemplate))
                    {
                        InitTileFromTemplate(tilePos, tileTemplate);
                    }
                    else if (template.CustomTiles.TryGetValue(symbol, out tileTemplate))
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
