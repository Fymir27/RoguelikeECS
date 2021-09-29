using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TheAlchemist.GraphUtilities;

namespace GraphUtilities
{
    public static class BitArrayExtensions
    {
        public static List<int> TrueBitsIndices(this BitArray bits)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                {
                    result.Add(i);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// DISCLAIMER: if you use this class, don't change the graph, otherwise you might get problems
    /// </summary>
    public struct Cycle
    {
        public readonly Vertex StartingVertex;
        public int EdgeCount => edges.Count;

        private Graph graph;
        private List<Edge> edges;
        private BitArray fingerprint;
        private Vertex lastAddedVertex;

        public Cycle(Graph graph, Vertex startingVertex)
        {
            this.graph = graph;
            StartingVertex = startingVertex;
            lastAddedVertex = startingVertex;
            edges = new List<Edge>();
            fingerprint = new BitArray(graph.Edges.Count);
        }

        public void AddEdge(Edge e)
        {
            if (!graph.Edges.Contains(e))
            {
                throw new ArgumentException("Can't add edge from a different graph!");
            }
            if (edges.Contains(e))
            {
                throw new ArgumentException("Edge already in cycle!");
            }
            if (!e.AttachedTo(lastAddedVertex))
            {
                throw new ArgumentException("Edge doesn't continue cycle!");
            }
            edges.Add(e);
            fingerprint.Set(graph.Edges.IndexOf(e), true);
            lastAddedVertex = e.GetOtherVertex(lastAddedVertex);
        }

        public List<Vertex> Vertices()
        {
            var result = new List<Vertex>();
            var v = StartingVertex;
            edges.ForEach(e =>
            {
                result.Add(v);
                v = e.GetOtherVertex(v);
            });
            return result;
        }

        public Cycle MergeWith(Cycle other)
        {
            var overlap = new BitArray(fingerprint);
            overlap.And(other.fingerprint);

            if (overlap.TrueBitsIndices().Count == 0)
            {
                return new Cycle(graph, null);
            }

            var newFingerprint = new BitArray(fingerprint);
            newFingerprint.Xor(other.fingerprint);
            List<int> edgeIndices = newFingerprint.TrueBitsIndices();

            if (edgeIndices.Count == 0)
            {
                return new Cycle(graph, null);
            }

            var edgesToAdd = new List<Edge>();
            foreach (int index in edgeIndices)
            {
                edgesToAdd.Add(graph.Edges[index]);
            }

            var vertex = edgesToAdd[0].V1;
            var newCycle = new Cycle(graph, vertex);
            while (edgesToAdd.Count > 0)
            {
                var next = vertex.Edges.First(edgesToAdd.Contains);
                newCycle.AddEdge(next);
                edgesToAdd.Remove(next);
                vertex = next.GetOtherVertex(vertex);
            }

            return newCycle;
        }

        public List<List<Edge>> Overlap(Cycle other)
        {
            var overlapFingerprint = new BitArray(fingerprint);
            overlapFingerprint.And(other.fingerprint);
            var overlappingEdgeIndices = overlapFingerprint.TrueBitsIndices();

            if (overlappingEdgeIndices.Count == 0)
            {
                return new List<List<Edge>>();
            }

            var overlappingEdges = new List<Edge>();
            foreach (var index in overlappingEdgeIndices)
            {
                overlappingEdges.Add(graph.Edges[index]);
            }

            var result = new List<List<Edge>>();
            var curSegment = new List<Edge>();
            Vertex v = overlappingEdges[0].V1;

            while (overlappingEdges.Count > 0)
            {
                Edge next = v.Edges.FirstOrDefault(overlappingEdges.Contains);

                if (next == null)
                {
                    result.Add(curSegment);
                    curSegment = new List<Edge>();
                    v = overlappingEdges[0].V1;
                    continue;
                }

                curSegment.Add(next);
                overlappingEdges.Remove(next);
                v = next.GetOtherVertex(v);
            }

            result.Add(curSegment);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Cycle other))
                return false;

            if (graph != other.graph)
                return false;

            var differentBits = fingerprint.Xor(other.fingerprint);
            return !differentBits.Cast<bool>().Any(b => b);
        }
    }
}