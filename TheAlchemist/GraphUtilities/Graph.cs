using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphUtilities;
using Newtonsoft.Json.Linq;

namespace TheAlchemist.GraphUtilities
{
    /// <summary>
    /// represents an element-wise mapping from query graph (pattern) to matched sub-graph
    /// </summary>
    public class MatchResult
    {
        /// <summary>
        /// Maps from pattern Vertex to matched Vertex in the searched graph
        /// </summary>
        public Dictionary<Vertex, Vertex> Vertices;

        /// <summary>
        /// Maps from pattern Edge to matched Edge in the searched graph
        /// </summary>
        public Dictionary<Edge, Edge> Edges;

        public MatchResult()
        {
            Vertices = new Dictionary<Vertex, Vertex>();
            Edges = new Dictionary<Edge, Edge>();
        }
    }


    public class Graph
    {
        public List<Vertex> Vertices { get; private set; }
        public List<Edge> Edges { get; private set; }

        public Random Random { get; set; } = new Random();

        public Graph()
        {
            Vertices = new List<Vertex>();
            Edges = new List<Edge>();
        }

        public static Graph FromJSON(string json)
        {
            var graph = new Graph();            
            
            var obj = JObject.Parse(json);
            var vertices = obj["vertices"]?.ToObject<Dictionary<string, Vertex>>();
            if(vertices is null)
            {
                throw new ArgumentException("Missing 'vertices' property on graph");
            }

            var edges = obj["edges"]?.ToObject<Dictionary<string, EdgeFromJson>>();
            if (edges is null)
            {
                throw new ArgumentException("Missing 'edges' property on graph");
            }
                        
            foreach (string vertexID in vertices.Keys)
            {
                var v = vertices[vertexID];
                v.ID = vertexID;
                graph.Vertices.Add(v);
            }

            foreach (string edgeID in edges.Keys)
            {
                var edgeDescription = edges[edgeID];
                var v1 = vertices[edgeDescription.V1];
                var v2 = vertices[edgeDescription.V2];
                var edge = new Edge(v1, v2, edgeDescription.Data) {ID = edgeID};
                v1.Edges.Add(edge);
                v2.Edges.Add(edge);
                graph.Edges.Add(edge);
            }

            return graph;
        }

        public string ToDot(bool printVertexData = true, bool printEdgeData = false)
        {
            var sb = new StringBuilder();

            sb.AppendLine("strict graph {");

            var usedEdges = new List<Edge>();
            foreach (var vertex in Vertices)
            {
                if (printVertexData)
                    sb.AppendFormat("{0} [label = \"{1}\"];\n", vertex.ID, vertex.Data);

                foreach (var edge in vertex.Edges.Where(edge => !usedEdges.Contains(edge)))
                {
                    usedEdges.Add(edge);

                    sb.AppendFormat("{0} -- {1}", edge.V1.ID, edge.V2.ID);

                    if (printEdgeData)
                        sb.AppendFormat(" [label = \"{0}\"]", edge);

                    sb.AppendLine(";");
                }
            }

            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// Adds a vertex to the graph
        /// </summary>
        /// <returns>Reference to the created vertex</returns>
        public void AddVertex<TVertex>(TVertex vertex) where TVertex : Vertex
        {
            if (vertex == null)
            {
                throw new ArgumentException("Can't add a vertex that is null!");
            }
            if (Vertices.Contains(vertex))
            {
                throw new ArgumentException("Can't add a vertex twice! " + vertex);
            }
            Vertices.Add(vertex);
            foreach (var edge in vertex.Edges)
            {
                if (!Edges.Contains(edge))
                {
                    Edges.Add(edge);
                }
            }
        }

        /// <summary>
        /// Adds an edge to the graph
        /// </summary>
        /// <returns>Reference to the created edge</returns>
        public void AddEdge(Edge edge)
        {
            AssertEdge(edge);
            if (Edges.Contains(edge))
            {
                throw new ArgumentException("Edge already present in Graph!");
            }
            Edges.Add(edge);
            edge.V1.Edges.Add(edge);
            edge.V2.Edges.Add(edge);
        }

        /// <summary>
        /// removes a vertex from the graph (and it's edges)
        /// </summary>
        /// <param name="vertex">Vertex to remove</param>
        public void RemoveVertex(Vertex vertex)
        {
            AssertVertex(vertex);

            Vertices.Remove(vertex);

            // remove all edges that would now be dangling
            foreach (var edge in vertex.Edges)
            {
                edge.GetOtherVertex(vertex).Edges.Remove(edge);
                Edges.Remove(edge);
            }

            vertex.Edges.Clear();
        }

        /// <summary>
        /// removes an edge from the graph
        /// </summary>
        /// <param name="edge">Edge to remove</param>
        public void RemoveEdge(Edge edge)
        {
            AssertEdge(edge);
            if (!Edges.Contains(edge))
            {
                throw new ArgumentException("Edge not in Graph!");
            }
            Edges.Remove(edge);
            // remove the edge from both end vertices
            edge.V1.Edges.Remove(edge);
            edge.V2.Edges.Remove(edge);
        }

        /// <summary>
        /// Finds ALL vertices of the same type as query vertex in the graph
        /// </summary>
        /// <param name="queryVertex">vertex of type you want to find</param>
        /// <returns>all vertices of type you want to find</returns>
        public IEnumerable<Vertex> FindVerticesLike(Vertex queryVertex)
        {
            return Vertices.Where(queryVertex.SameType);
        }

        /// <summary>
        /// Finds a pattern (subgraph) in the host graph (this graph)
        /// Returns null on failing to match
        /// </summary>
        /// <param name="pattern">pattern graph to look for</param>
        /// <param name="randomMatch"></param>
        /// <param name="verbose">set true for writing trace to console</param>
        /// <returns>a mapping from pattern to host graph for every vertex + edge</returns>
        public MatchResult FindPattern(Graph pattern, bool randomMatch = false, bool verbose = false)
        {
            if (pattern == null || pattern.Vertices.Count == 0)
            {
                throw new ArgumentException("Invalid pattern!");
            }

            var patternVertex = pattern.Vertices[0]; // TODO: make it random?
            return MatchRecursively(patternVertex, randomMatch, verbose);
        }

        /// <summary>
        /// Validates a vertex. Throws an exception if it is null or not in the graph
        /// </summary>
        /// <param name="vertex">vertex to validate</param>
        public void AssertVertex(Vertex vertex)
        {
            if (vertex == null)
            {
                throw new ArgumentException("Vertex is null!");
            }

            if (!InGraph(vertex))
            {
                throw new ArgumentException("Vertex not contained in this graph!");
            }
        }

        /// <summary>
        /// Validates an edge. Throws an exception if either vertex is invalid
        /// </summary>
        /// <param name="edge">edge to validate</param>
        public void AssertEdge(Edge edge)
        {
            AssertVertex(edge.V1);
            AssertVertex(edge.V2);
        }

        /// <summary>
        /// checks if vertex is in graph
        /// </summary>
        /// <param name="vertex">vertex to check</param>
        /// <returns>wether vertex is in graph</returns>
        private bool InGraph(Vertex vertex)
        {
            return Vertices.Contains(vertex);
        }

        /// <summary>
        /// recursive strategy for FindPattern()
        /// </summary>
        /// <param name="startVertex">pattern vertex to start with</param>
        /// <param name="randomMatch"></param>
        /// <param name="verbose">set true for writing trace to console</param>
        /// <returns>a mapping from pattern to host graph for every vertex + edge</returns>
        private MatchResult MatchRecursively(Vertex startVertex, bool randomMatch, bool verbose)
        {
            var possibleFirstMatches = FindVerticesLike(startVertex);

            if (randomMatch)
            {
                possibleFirstMatches = Shuffle(possibleFirstMatches);
            }

            var visitedPatternEdges = new List<Edge>();
            var matchedEdges = new List<Edge>();
            var result = new MatchResult();


            foreach (var firstMatch in possibleFirstMatches)
            {
                if (IterateVertex(startVertex, firstMatch))
                {
                    return result;
                }
                visitedPatternEdges.Clear();
                matchedEdges.Clear();
                result = new MatchResult();
            }

            return null;

            bool IterateVertex(Vertex patternVertex, Vertex matchVertex)
            {
                if (verbose)
                {
                    Console.WriteLine($"Pattern: {EnumerableToString(result.Vertices.Keys.Select(v => v.ID))} -> {patternVertex.ID}");
                    Console.WriteLine($"Matched: {EnumerableToString(result.Vertices.Values.Select(v => v.ID))} -> {matchVertex.ID}");
                    Console.WriteLine("---");
                }

                if (!result.Vertices.ContainsKey(patternVertex))
                {
                    if (result.Vertices.Values.Contains(matchVertex))
                        return false;

                    result.Vertices.Add(patternVertex, matchVertex);
                }
                else if (result.Vertices[patternVertex] != matchVertex)
                {
                    return false;
                }

                var unvisitedPatternEdges = patternVertex.Edges
                    .Where(e => !visitedPatternEdges.Contains(e));

                if (randomMatch)
                {
                    unvisitedPatternEdges = Shuffle(unvisitedPatternEdges);
                }

                foreach (var patternEdge in unvisitedPatternEdges)
                {
                    if (visitedPatternEdges.Contains(patternEdge))
                        continue;

                    visitedPatternEdges.Add(patternEdge);

                    var nextPatternVertex = patternEdge.GetOtherVertex(patternVertex);

                    var unvisitedEdges = matchVertex.Edges
                        .Where(e =>
                            !matchedEdges.Contains(e) &&
                            patternEdge.SameType(e) &&
                            nextPatternVertex.SameType(e.GetOtherVertex(matchVertex))
                        )
                        .ToList();

                    if (!unvisitedEdges.Any())
                    {
                        visitedPatternEdges.Remove(patternEdge);
                        result.Vertices.Remove(patternVertex);
                        return false;
                    }

                    if (randomMatch)
                    {
                        unvisitedEdges = Shuffle(unvisitedEdges).ToList();
                    }

                    bool success = false;
                    foreach (var e in unvisitedEdges)
                    {
                        //could in the meantime already be matched
                        if (matchedEdges.Contains(e))
                            continue;

                        var nextMatchedVertex = e.GetOtherVertex(matchVertex);
                        matchedEdges.Add(e);
                        success = IterateVertex(nextPatternVertex, nextMatchedVertex);
                        if (success)
                        {
                            result.Edges.Add(patternEdge, e);
                            break;
                        }

                        matchedEdges.Remove(e);
                    }
                    if (!success)
                    {
                        visitedPatternEdges.Remove(patternEdge);
                        result.Vertices.Remove(patternVertex);
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// finds a pattern in the graph and replaces it with another graph
        /// </summary>
        /// <param name="pattern">pattern to find</param>
        /// <param name="replacement">replacement graph</param>
        /// <param name="directMapping">identity mapping from pattern to replacement Vertices</param>
        /// <param name="randomMatch">set true to find pattern randomly in graph</param>
        /// <returns>true on success; false otherwise</returns>
        public bool Replace(Graph pattern, Graph replacement, Dictionary<Vertex, Vertex> directMapping, bool randomMatch = false)
        {
            var matchResult = FindPattern(pattern, randomMatch);

            if (matchResult == null)
                return false;

            // go through all matched vertices to find which ones are to be directly replaced by another
            foreach (var vertexPair in matchResult.Vertices)
            {
                Vertex patternVertex = vertexPair.Key;
                Vertex matchVertex = vertexPair.Value;

                bool mappedDirectly = directMapping.TryGetValue(patternVertex, out Vertex replacementVertex);

                if (!mappedDirectly)
                {
                    RemoveVertex(matchVertex);
                    continue;
                }

                // tie edges of host graphs to replacement vertex
                var unmatchedEdges = matchVertex.Edges.Where(e => !matchResult.Edges.Values.Contains(e)).ToArray();
                replacementVertex.Edges.AddRange(unmatchedEdges);
                foreach (var e in unmatchedEdges)
                {
                    e.ReplaceVertex(matchVertex, replacementVertex);
                }

                // don't use this.RemoveVertex(), because it would remove lose edges
                // however, they aren't lose, because we just reconnected them to the replacement vertex
                Vertices.Remove(matchVertex);
                AddVertex(replacementVertex);
            }

            // Add the rest of the replacement edges (that aren't directly replacing anything)
            foreach (var vertex in replacement.Vertices)
            {
                if (!Vertices.Contains(vertex))
                {
                    AddVertex(vertex);
                }
            }

            return true;
        }

        /// <summary>
        /// finds a pattern in the graph and replaces it with another graph according to the rule
        /// </summary>
        /// <param name="rule">Rule by which to replace</param>
        /// <param name="randomMatch">set true to find pattern randomly in graph</param>
        /// <returns>true on success; false otherwise</returns>
        public bool Replace(ReplacementRule rule, bool randomMatch = false)
        {
            return Replace(rule.Pattern, rule.Replacement, rule.Mapping, randomMatch);
        }

        public static string EnumerableToString(IEnumerable enumerable)
        {
            return $"[{string.Join(',', enumerable)}]";
        }

        // based on Fisher-Yates shuffle
        public IEnumerable<T> Shuffle<T>(IEnumerable<T> enumerable)
        {
            var list = new List<T>(enumerable);
            for (int n = list.Count() - 1; n >= 0; n--)
            {
                int r = Random.Next(n + 1);
                (list[r], list[n]) = (list[n], list[r]);
            }
            return list;
        }

        public IEnumerable<Cycle> GetCycles(bool simplifyToSmallest = false)
        {
            if (Vertices.Count < 3)
            {
                return new List<Cycle>();
            }

            var result = new List<Cycle>();
            var used = new List<Vertex>();
            var parent = new Dictionary<Vertex, Vertex>();
            var todo = new Stack<Vertex>();

            todo.Push(Vertices[0]);
            parent.Add(Vertices[0], Vertices[0]);

            while (todo.Count > 0)
            {
                var cur = todo.Pop();
                used.Add(cur);

                foreach (var neighbour in cur.Edges.Select(edge => edge.GetOtherVertex(cur))
                    .Where(neighbour => parent[cur] != neighbour))
                {
                    if (used.Contains(neighbour))
                    {
                        var cycle = new Cycle(this, parent[cur]);

                        cycle.AddEdge(parent[cur].GetEdgeTo(cur));
                        cycle.AddEdge(cur.GetEdgeTo(neighbour));

                        var v = neighbour;
                        while (v != parent[cur])
                        {
                            var e = v.GetEdgeTo(parent[v]);
                            cycle.AddEdge(e);
                            v = parent[v];
                        }

                        result.Add(cycle);
                    }
                    else if (!parent.ContainsKey(neighbour))
                    {
                        parent.Add(neighbour, cur);
                        todo.Push(neighbour);
                    }
                }
            }

            if (simplifyToSmallest && result.Count > 0)
            {
                return SmallestBase(result);
            }

            return result;
        }

        private List<Cycle> SmallestBase(List<Cycle> initialBase)
        {
            var smallestBase = new List<Cycle>(initialBase);

            var toRemove = new HashSet<Cycle>(); // indexes

            int baseCycleCount = initialBase.Count;
            for (int outer = 0; outer < baseCycleCount; outer++)
            {
                for (int inner = outer + 1; inner < baseCycleCount; inner++)
                {
                    var a = initialBase[inner];
                    var b = initialBase[outer];
                    var newCycle = a.MergeWith(b);
                    
                    if (newCycle.EdgeCount == 0 || smallestBase.Contains(newCycle)) 
                        continue;
                    
                    Console.WriteLine($"New cycle found: {newCycle}");
                    if (newCycle.EdgeCount > a.EdgeCount || newCycle.EdgeCount > b.EdgeCount)
                    {
                        Console.WriteLine("Ignoring... (more edges than a and b)");
                        continue;
                    }

                    if (a.EdgeCount > b.EdgeCount)
                    {
                        Console.WriteLine($"Removing inner cycle: {a}");
                        toRemove.Add(a);
                    }
                    else
                    {
                        Console.WriteLine($"Removing outer cycle: {b}");
                        toRemove.Add(b);
                    }

                    smallestBase.Add(newCycle);
                }
            }

            smallestBase.RemoveAll(toRemove.Contains);

            return smallestBase;
        }

        public int Distance(Vertex from, Vertex to)
        {
            var distances = Vertices.ToDictionary(vertex => vertex, vertex => int.MaxValue);
            distances[from] = 0;
            var todo = new Queue<Vertex>();
            todo.Enqueue(from);
            while (todo.Count > 0)
            {
                var cur = todo.Dequeue();
                int curDist = distances[cur];
                foreach (var neighbour in cur.Neighbours())
                {
                    if (neighbour == to)
                    {
                        return curDist + 1;
                    }

                    if (distances[neighbour] <= (curDist + 1))
                    {
                        continue;
                    }

                    distances[neighbour] = curDist + 1;
                    todo.Enqueue(neighbour);
                }
            }
            
            throw new ArgumentException("'to' Vertex not found in graph!");
        }

        public int[,] CalculateDistances()
        {
            var distances = new int[Vertices.Count, Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
            {
                for (int j = i; j < Vertices.Count; j++)
                {
                    distances[i, j] = distances[j, i] = i == j ? int.MaxValue : Distance(Vertices[i], Vertices[j]);
                }
            }
            return distances;
        }
    }
}
