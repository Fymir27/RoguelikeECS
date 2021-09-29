using System;
using GraphUtilities;

namespace TheAlchemist.GraphUtilities
{
    struct EdgeFromJson
    {
        public string V1;
        public string V2;
        public string Data;
    }

    public class Edge
    {
        public string ID = "";
        public string Data;
        
        private static int nextAvailableID;

        public Vertex V1 { get; private set; }
        public Vertex V2 { get; private set; }

        public Edge(string data)
        {
            ID = $"E{nextAvailableID++}";
            Data = data;
        }

        public Edge() : this("")
        {
            
        }

        public Edge(Vertex v1, Vertex v2, string data)
        {
            if(v1 == null || v2 == null)
            {
                throw new ArgumentException("Vertices cannot be null");
            }

            V1 = v1;
            V2 = v2;
            Data = data;
        }

        public void Init(Vertex first, Vertex second)
        {
            if (first == null || second == null)
            {
                throw new ArgumentException("Edges with null Vertices are not allowed!");
            }

            if (first == second)
            {
                throw new ArgumentException("Edges with same start and end vertex are not allowed!");
            }

            V1 = first;
            V2 = second;
        }

        public bool AttachedTo(Vertex v)
        {
            return V1 == v || V2 == v;
        }

        public Vertex GetOtherVertex(Vertex vertex)
        {
            if (vertex == V1)
                return V2;
            if (vertex == V2)
                return V1;
            throw new ArgumentException("Vertex is not adjacent to this edge!");
        }

        public void ReplaceVertex(Vertex oldVertex, Vertex newVertex)
        {
            if (oldVertex == V1)
            {
                V1.Edges.Remove(this);
                V1 = newVertex;
            }
            else if (oldVertex == V2)
            {
                V2.Edges.Remove(this);
                V2 = newVertex;
            }
            else
                throw new ArgumentException("Vertex is not adjacent to this edge!");
        }       

        public virtual bool SameType(Edge other)
        {
            return GetType() == other.GetType();
        }

        public override string ToString()
        {
            return $"E{ID} {Data}";
        }
    }
}
