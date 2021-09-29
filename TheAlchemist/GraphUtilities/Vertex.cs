using System.Collections.Generic;
using System.Linq;

namespace TheAlchemist.GraphUtilities
{
    public class Vertex
    {
        public string ID;
        public string Data;
        public List<Edge> Edges;

        private static int nextAvailableID;

        public Vertex(string data = "")
        {
            ID = $"V{nextAvailableID++}";
            Data = data;
            Edges = new List<Edge>();
        }

        public virtual bool SameType(Vertex other)
        {
            return this.Data == other.Data;
        }

        public override string ToString()
        {
            return $"V{ID} {Data}";
        }

        public Edge GetEdgeTo(Vertex other)
        {
            return Edges.FirstOrDefault(e => e.GetOtherVertex(this) == other);
        }

        public IEnumerable<Vertex> Neighbours()
        {
            return Edges.Select(e => e.GetOtherVertex(this));
        }
    }
}
