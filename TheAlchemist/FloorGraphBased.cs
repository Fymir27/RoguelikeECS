using System;
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

            Random rnd;
            int seed = 0; //1948689677;

            if (seed == 0)
                seed = (int)System.DateTime.Now.Ticks;

            Console.WriteLine("Seed: " + seed);
            rnd = new Random(seed);

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

                builder.Reset()
                    .MappedVertex<BasicRoom>()
                    .ReplacementVertexWithEdge<BasicRoom, Edge>();

                var addRoom = builder.GetResult();

                var rules = new Tuple<ReplacementRule, int>[]
                {
                Tuple.Create(addJunction, 3),
                Tuple.Create(stretch, 2),
                Tuple.Create(createLoop, 2),
                Tuple.Create(transformJunction, 1)
                };

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

                    int r = rnd.Next(acc);

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

            File.WriteAllText("advancedDungeon.gv", GraphPrinter.ToDot(dungeon));

            var room = GenerateRoom(Position.Zero, GameData.Instance.RoomTemplates["testRoom"]);

            InitPlayer(Position.One);
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
