﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using GraphUtilities;

namespace TheAlchemist
{
    partial class Floor
    {
        class RoomVertex : Vertex
        {
            public override string ToString()
            {
                return "Room";
            }
        }

        class FragmentVertex : Vertex
        {
            public override string ToString()
            {
                return "Fragment";
            }
        }

        class Junction : FragmentVertex
        {
            public override string ToString()
            {
                return "Junc.";
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
                Tuple.Create(createLoop, 2),
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
            dungeon.AddVertex(new StartingRoom());
            dungeon.Replace(initialRule, true);

            for (int i = 0; i < 15; i++)
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

            var room = GenerateRoom(Position.Zero, GameData.Instance.RoomTemplates["fountain"]);

            InitPlayer(new Position(4, 6));

            Queue<Position> todo = new Queue<Position>();
            Dictionary<Position, Vertex> roomLayout = new Dictionary<Position, Vertex>();

            roomLayout.Add(Position.Zero, dungeonGraph.Vertices[0]);
            todo.Enqueue(Position.Zero);

            while (todo.Count > 0)
            {
                Position curPos = todo.Dequeue();
                var curVertex = roomLayout[curPos];

                var freeAdjacentPositions = curPos.GetNeighboursHexPointyTop().Where(p => !roomLayout.ContainsKey(p));

                // TODO: place room
                Console.WriteLine("Visiting " + curVertex + " at " + curPos);

                var notVisited = curVertex.Edges.Select(e => e.GetOtherVertex(curVertex)).Where(v => !roomLayout.Values.Contains(v));
                int neighbourCount = notVisited.Count();

                // collapse neighbouring rooms into junctions that lead into those rooms
                // until there's only max neighbours left
                int maxNeighbours = freeAdjacentPositions.Count();
                if(maxNeighbours == 0)
                {
                    // TODO: fallback/retry
                    throw new Exception("No more room to place room!");
                }
                int iterationCount = neighbourCount - maxNeighbours;
                for (int i = 0; i < iterationCount; i++)
                {
                    Console.WriteLine("Iteration " + i + " of neighbour count reduction on " + curVertex);

                    // remember rooms/edges
                    Edge edge1 = curVertex.Edges[i];
                    Vertex room1 = edge1.GetOtherVertex(curVertex);
                    Edge edge2 = curVertex.Edges[neighbourCount - i - 1];
                    Vertex room2 = edge2.GetOtherVertex(curVertex);

                    // add junction instead of room1 and remove edge to room2 entirely
                    var junc = new Junction();
                    dungeonGraph.AddVertex(junc);
                    edge1.ReplaceVertex(room1, junc);
                    dungeonGraph.RemoveEdge(edge2);

                    // connect rooms to the junction
                    Edge newEdge1 = new Edge(junc, room1);
                    Edge newEdge2 = new Edge(junc, room2);
                    dungeonGraph.AddEdge(newEdge1);
                    dungeonGraph.AddEdge(newEdge2);
                }

                // update after possible reduction of neighbours
                notVisited = curVertex.Edges.Select(e => e.GetOtherVertex(curVertex)).Where(v => !roomLayout.Values.Contains(v));

                Console.WriteLine("Neighbours: " + notVisited.Count());
                var randomizedAdjacentPositions = new Stack<Position>(Util.Shuffle(freeAdjacentPositions));
                foreach (var vertex in notVisited)
                {
                    var pos = randomizedAdjacentPositions.Pop();
                    roomLayout.Add(pos, vertex);
                    todo.Enqueue(pos);
                    Console.WriteLine("Placing " + vertex + " at " + pos);
                }
            }

            File.WriteAllText("advancedDungeon.gv", GraphPrinter.ToDot(dungeonGraph));
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
