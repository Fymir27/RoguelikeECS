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
        public bool[,] OccupationMatrix { get; set; }
        public List<Position> OccupiedPositions { get; set; } // from anchor
        public bool FlippedHorizontally { get; set; }

        public int Width => OccupationMatrix.GetLength(0);
        public int Height => OccupationMatrix.GetLength(1);
    }
}
