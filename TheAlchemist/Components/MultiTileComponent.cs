using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class MultiTileComponent : Component<MultiTileComponent>
    {
        public Position Anchor { get; set; }
        //public bool[,] OccupationMatrix { get; set; }
        public Dictionary<bool, List<Position>> OccupiedPositions { get; set; } // from anchor, Key = flipped
        public bool FlippedHorizontally { get; set; }

        public int Width; // => OccupationMatrix.GetLength(0);
        public int Height; // => OccupationMatrix.GetLength(1);

        [Newtonsoft.Json.JsonConstructor]
        MultiTileComponent(bool[,] occupationMatrix)
        {
            Width = occupationMatrix.GetLength(0);
            Height = occupationMatrix.GetLength(1);

            OccupiedPositions = new Dictionary<bool, List<Position>>()
            {
                { true, new List<Position>() },
                { false, new List<Position>() }
            };

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (occupationMatrix[x, y])
                    {
                        OccupiedPositions[false].Add(new Position(x, y));
                        OccupiedPositions[true].Add(new Position(Width - 1 - x, y));
                    }
                }
            }

            FlippedHorizontally = false;
        }
    }
}
