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
        public List<Position> OccupiedPositions  // from anchor
        {
            get
            {
                if(FlippedHorizontally)
                {
                    return occupiedPositionsFlipped;
                }
                else
                {
                    return occupiedPositions;
                }
            }
        }
        public bool FlippedHorizontally { get; set; }

        public int Width; // => OccupationMatrix.GetLength(0);
        public int Height; // => OccupationMatrix.GetLength(1);

        private List<Position> occupiedPositions;
        private List<Position> occupiedPositionsFlipped;

        [Newtonsoft.Json.JsonConstructor]
        MultiTileComponent(bool[,] occupationMatrix)
        {
            Width = occupationMatrix.GetLength(0);
            Height = occupationMatrix.GetLength(1);

            occupiedPositions = new List<Position>();
            occupiedPositionsFlipped = new List<Position>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (occupationMatrix[x, y])
                    {
                        occupiedPositions.Add(new Position(x, y));
                        occupiedPositionsFlipped.Add(new Position(Width - 1 - x, y));
                    }
                }
            }

            FlippedHorizontally = false;
        }
    }
}
