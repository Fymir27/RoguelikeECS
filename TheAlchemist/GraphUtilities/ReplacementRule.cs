using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheAlchemist.GraphUtilities;

namespace GraphUtilities
{
    /// <summary>
    /// A rule by which to replace a subgraph defined by "Pattern"
    /// with a "Replacement" graph with the mapping between them being
    /// defined by "Mapping
    /// </summary>
    public class ReplacementRule
    {
        public Graph Pattern = new Graph();
        public Graph Replacement = new Graph();
        public Dictionary<Vertex, Vertex> Mapping = new Dictionary<Vertex, Vertex>();
    }

    /// <summary>
    /// Class that provides a fluent interface for creating a ReplacementRule
    /// </summary>
    public class ReplacementRuleBuilder
    {
        /// <summary>
        /// possible states of the builders 
        /// internal state machine
        /// </summary>
        private enum State
        {
            Start,
            PatternVertex,
            ReplacementVertex,
            MappedVertex,
            PatternEdge,
            ReplacementEdge,
            MappedEdge,
            End
        }

        /// <summary>
        /// internal instance of ReplacementRule that is beeing built
        /// </summary>
        private ReplacementRule result;

        /// <summary>
        /// last added pattern vertex
        /// </summary>
        private Vertex currentPatternVertex;

        /// <summary>
        /// last added replacement vertex
        /// </summary>
        private Vertex currentReplacementVertex;

        /// <summary>
        /// current uncompleted pattern edge
        /// </summary>
        private Edge currentPatternEdge;

        /// <summary>
        /// current uncompleted replacement edge
        /// </summary>
        private Edge currentReplacementEdge;

        /// <summary>
        /// maps tags to corresponding pattern vertices
        /// </summary>
        private Dictionary<string, Vertex> taggedPatternVertices;

        /// <summary>
        /// maps tags to corresponding replacement vertices
        /// </summary>
        private Dictionary<string, Vertex> taggedReplacementVertices;

        /// <summary>
        /// current internal state
        /// </summary>
        private State currentState;

        /// <summary>
        /// last valid state (before State.End was reached)
        /// </summary>
        private State lastValidState = State.Start;

        /// <summary>
        /// if true state does not get changed by ChangeState()
        /// </summary>
        private bool freezeState;

        /// <summary>
        /// maps a state to possible next states
        /// </summary>
        private static Dictionary<State, State[]> possibleNextStates = new Dictionary<State, State[]>
            {
                {
                    State.Start, new[]
                    {
                        State.PatternVertex,
                        State.ReplacementVertex,
                        State.MappedVertex
                    }
                },
                {
                    State.PatternVertex, new[]
                    {
                        State.PatternEdge,
                        State.End
                    }
                },
                {
                    State.ReplacementVertex, new[]
                    {
                        State.ReplacementEdge,
                        State.End
                    }
                },
                {
                    State.MappedVertex, new[]
                    {
                        State.PatternEdge,
                        State.ReplacementEdge,
                        State.MappedEdge,
                        State.End
                    }
                },
                {
                    State.PatternEdge, new[]
                    {
                        State.PatternVertex
                    }
                },
                {
                    State.ReplacementEdge, new[]
                    {
                        State.ReplacementVertex
                    }
                },
                {
                    State.MappedEdge, new[]
                    {
                        State.MappedVertex
                    }
                },
                {
                    State.End, new State[0]
                }
            };

        /// <summary>
        /// basic constructor - initializes attributes and state
        /// </summary>
        public ReplacementRuleBuilder()
        {
            currentState = State.Start;
            result = new ReplacementRule();
            taggedPatternVertices = new Dictionary<string, Vertex>();
            taggedReplacementVertices = new Dictionary<string, Vertex>();
        }

        /// <summary>
        /// Adds a pattern vertex to the rule 
        /// and finishes the current uncomplete pattern edge 
        /// </summary>
        /// <param name="vertex">pattern vertex to add</param>
        /// <param name="tag">string to identify vertex later</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder PatternVertex(Vertex vertex, string tag = null)
        {
            ChangeState(State.PatternVertex);

            if (vertex == null)
                throw new ArgumentNullException("vertex");

            result.Pattern.AddVertex(vertex);
            FinalizePatternEdge(vertex);
            currentPatternVertex = vertex;

            if (tag != null)
            {
                taggedPatternVertices[tag] = vertex;
            }

            return this;
        }

        /// <summary>
        /// Adds a pattern vertex to the rule 
        /// and finishes the current uncomplete pattern edge 
        /// </summary>
        /// <typeparam name="TVertex">type of pattern vertex to create</typeparam>
        /// <param name="tag">string to identify vertex later</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder PatternVertex<TVertex>(string tag = null) where TVertex : Vertex, new()
        {
            return PatternVertex(new TVertex(), tag);
        }

        /// <summary>
        /// Adds a replacement vertex to the rule 
        /// and finishes the current uncomplete replacement edge 
        /// </summary>
        /// <param name="vertex">replacement vertex to add</param>
        /// <param name="tag">string to identify vertex later</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder ReplacementVertex(Vertex vertex, string tag = null)
        {
            ChangeState(State.ReplacementVertex);

            result.Replacement.AddVertex(vertex);
            FinalizeReplacementEdge(vertex);
            currentReplacementVertex = vertex;

            if (tag != null)
                taggedReplacementVertices[tag] = vertex;

            return this;
        }

        /// <summary>
        /// Adds a replacement vertex to the rule 
        /// and finishes the current uncomplete replacement edge 
        /// </summary>
        /// <typeparam name="TVertex">type of replacement vertex to create</typeparam>
        /// <param name="tag">string to identify vertex later</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder ReplacementVertex<TVertex>(string tag = null) where TVertex : Vertex, new()
        {
            return ReplacementVertex(new TVertex(), tag);
        }

        /// <summary>
        /// Adds a pattern and replacement vertex to the rule, maps one to the other
        /// and finishes the current uncomplete pattern and replacement edge 
        /// </summary>
        /// <param name="patternVertex">pattern vertex to add</param>
        /// <param name="replacementVertex">replacement vertex to add</param>
        /// <param name="tag">string to identify the vertices later</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder MappedVertex<TVertex>(TVertex patternVertex, TVertex replacementVertex, string tag = null) where TVertex : Vertex
        {
            ChangeState(State.MappedVertex);
            freezeState = true;
            PatternVertex(patternVertex, tag);
            ReplacementVertex(replacementVertex, tag);
            freezeState = false;
            result.Mapping.Add(currentPatternVertex, currentReplacementVertex);
            return this;
        }

        /// <summary>
        /// Adds a pattern and replacement vertex to the rule, maps one to the other
        /// and finishes the current uncomplete pattern and replacement edge 
        /// </summary>
        /// <typeparam name="TVertex">type of pattern/replacement vertex to create</typeparam>
        /// <param name="tag">string to identify the vertices later</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder MappedVertex<TVertex>(string tag = null) where TVertex : Vertex, new()
        {
            return MappedVertex(new TVertex(), new TVertex(), tag);
        }

        #region basic edge adding methods
        /// <summary>
        /// Adds a new (incomplete) edge to the last added pattern vertex  
        /// </summary>
        /// <param name="edge">edge to add</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder PatternEdge(Edge edge)
        {
            ChangeState(State.PatternEdge);

            currentPatternEdge = edge;
            return this;
        }

        /// <summary>
        /// Adds a new (incomplete) edge to the last added pattern vertex  
        /// </summary>
        /// <typeparam name="TEdge">type of edge to add</typeparam>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder PatternEdge<TEdge>() where TEdge : Edge, new()
        {
            return PatternEdge(new TEdge());
        }

        /// <summary>
        /// Adds a new (incomplete) edge to the last added replacement vertex  
        /// </summary>
        /// <param name="edge">edge to add</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder ReplacementEdge(Edge edge)
        {
            ChangeState(State.ReplacementEdge);

            currentReplacementEdge = edge;
            return this;
        }

        /// <summary>
        /// Adds a new (incomplete) edge to the last added replacement vertex  
        /// </summary>
        /// <typeparam name="TEdge">type of edge to add</typeparam>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder ReplacementEdge<TEdge>() where TEdge : Edge, new()
        {
            return ReplacementEdge(new TEdge());
        }

        /// <summary>
        /// Adds a new (incomplete) edge to the last added pattern AND replacement (=mapped) vertex  
        /// </summary>
        /// <typeparam name="TEdge">type of edges to add (should be implicit)</typeparam>
        /// <param name="patternEdge">edge to add to param vertex</param>
        /// <param name="replacementEdge">edge to add to replacement vertex</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder MappedEdge<TEdge>(TEdge patternEdge, TEdge replacementEdge) where TEdge : Edge
        {
            ChangeState(State.MappedEdge);
            freezeState = true;
            PatternEdge(patternEdge);
            ReplacementEdge(replacementEdge);
            freezeState = false;
            return this;
        }

        /// <summary>
        /// Adds a new (incomplete) edge to the last added pattern AND replacement (=mapped) vertex  
        /// </summary>
        /// <typeparam name="TEdge">type of edges to add</typeparam>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder MappedEdge<TEdge>() where TEdge : Edge, new()
        {
            return MappedEdge(new TEdge(), new TEdge());
        }
        #endregion

        /// <summary>
        /// moves the builders state to the vertex with a specific tag (if mapped: the vertices)
        /// </summary>
        /// <param name="tag">tag of the vertex/vertices to move to</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder MoveToTag(string tag)
        {
            bool patternTagged = taggedPatternVertices.TryGetValue(tag, out Vertex patternVertex);
            bool replacementTagged = taggedReplacementVertices.TryGetValue(tag, out Vertex replacementVertex);

            if (!patternTagged && !replacementTagged)
            {
                throw new ArgumentException(tag + " doesnt tag a Vertex!");
            }

            // if we have any dangling edges, they MUST be finshed:
            if (currentPatternEdge != null && !patternTagged)
            {
                throw new ArgumentException(tag + " doesnt tag a patternVertex! -> dangling Edge!");
            }
            if (currentReplacementEdge != null && !replacementTagged)
            {
                throw new ArgumentException(tag + " doesnt tag a replacementVertex! -> dangling Edge!");
            }

            switch (currentState)
            {
                case State.PatternEdge:
                    FinalizePatternEdge(patternVertex);
                    break;
                case State.ReplacementEdge:
                    FinalizeReplacementEdge(replacementVertex);
                    break;
                case State.MappedEdge:
                    FinalizePatternEdge(patternVertex);
                    FinalizeReplacementEdge(replacementVertex);
                    break;
                case State.End:
                    throw new InvalidOperationException("Please call Continue(tag) instead!");
            }

            if (patternTagged)
            {
                if (replacementTagged)
                {
                    currentState = State.MappedVertex;
                    currentPatternVertex = patternVertex;
                    currentReplacementVertex = replacementVertex;
                }
                else
                {
                    currentState = State.PatternVertex;
                    currentPatternVertex = patternVertex;
                }
            }
            else /* implied if (replacementTagged) */
            {
                currentState = State.ReplacementVertex;
                currentReplacementVertex = replacementVertex;
            }

            return this;
        }

        /// <summary>
        /// maps the last added vertex to the vertex tagged by "tag" and also tags it.
        /// works only if last operation was adding a single (pattern/replacement) vertex 
        /// and the tag refers to the other type (pattern/replacement) of vertex
        /// </summary>
        /// <param name="tag">tag of vertex to map to</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder MapToTag(string tag)
        {
            taggedPatternVertices.TryGetValue(tag, out Vertex patternVertex);
            taggedReplacementVertices.TryGetValue(tag, out Vertex replacementVertex);

            switch(currentState)
            {
                case State.PatternVertex:
                    if(replacementVertex == null)
                    {
                        throw new ArgumentException("No vertex to map to with that tag!", "tag");
                    }
                    if(!currentPatternVertex.SameType(replacementVertex))
                    {
                        throw new ArgumentException("Can't map two vertices of different type!");
                    }
                    result.Mapping.Add(currentPatternVertex, replacementVertex);
                    taggedPatternVertices.Add(tag, currentPatternVertex);
                    break;

                case State.ReplacementVertex:
                    if (patternVertex == null)
                    {
                        throw new ArgumentException("No vertex to map to with that tag!", "tag");
                    }
                    if (!currentReplacementVertex.SameType(patternVertex))
                    {
                        throw new ArgumentException("Can't map two vertices of different type!");
                    }
                    result.Mapping.Add(patternVertex, currentReplacementVertex);
                    taggedReplacementVertices.Add(tag, currentReplacementVertex);
                    break;

                default:
                    throw new InvalidOperationException("You can only do that right after adding a pattern or replacement vertex!");
            }

            currentState = State.MappedVertex;
            return this;
        }

        /// <summary>
        /// finalizes the building of the rule and returns it
        /// </summary>
        /// <returns>ReplacementRule that was built</returns>
        public ReplacementRule GetResult()
        {
            ChangeState(State.End);
            return result;
        }

        /// <summary>
        /// allows the builder to continue after it has already finished by calling GetResult() (USE WITH CAUTION)
        /// </summary>
        /// <param name="tag">tag to continue from</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder Continue(string tag = null)
        {
            if (currentState != State.End)
            {
                throw new InvalidOperationException("Continue() should only be called on a Builder in [End] State!");
            }

            if (tag == null)
            {
                currentState = lastValidState;
            }
            else
            {
                currentState = State.Start;
                MoveToTag(tag);
            }

            return this;
        }

        /// <summary>
        /// allows reuse of builder instance by starting a new empty ReplacementRule
        /// </summary>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder Reset()
        {
            // already good to go!
            if(currentState == State.Start)
            {
                return this;
            }

            if (currentState != State.End)
            {
                throw new InvalidOperationException("Reset() should only be called on a Builder in [End] State! (did you forget to GetResult()?)");
            }

            result = new ReplacementRule();
            currentPatternVertex = null;
            currentReplacementVertex = null;
            currentReplacementEdge = null;
            currentPatternEdge = null;
            taggedPatternVertices.Clear();
            taggedReplacementVertices.Clear();
            freezeState = false;
            lastValidState = State.Start;
            currentState = State.Start;
            return this;
        }

        #region shortcut methods
        /// <summary>
        /// starts a new pattern edge and finishes it with a pattern vertex
        /// </summary>
        /// <param name="vertex">vertex to add</param>
        /// <param name="edge">edge to add</param>
        /// <param name="tag">string to identify vertex later</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder PatternVertexWithEdge(Vertex vertex, Edge edge, string tag = null)
        {
            PatternEdge(edge);
            PatternVertex(vertex, tag);
            return this;
        }

        /// <summary>
        /// starts a new pattern edge and finishes it with a pattern vertex
        /// </summary>
        /// <typeparam name="TVertex">type of vertex to add</typeparam>
        /// <typeparam name="TEdge">type of edge to add</typeparam>
        /// <param name="tag">string to identify vertex later</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder PatternVertexWithEdge<TVertex, TEdge>(string tag = null)
            where TVertex : Vertex, new()
            where TEdge : Edge, new()
        {
            PatternEdge<TEdge>();
            PatternVertex<TVertex>(tag);
            return this;
        }

        /// <summary>
        /// starts a new replacement edge and finishes it with a replacement vertex
        /// </summary>
        /// <param name="vertex">vertex to add</param>
        /// <param name="edge">edge to add</param>
        /// <param name="tag">string to identify vertex later</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder ReplacementVertexWithEdge(Vertex vertex, Edge edge, string tag = null)
        {
            ReplacementEdge(edge);
            ReplacementVertex(vertex, tag);
            return this;
        }

        /// <summary>
        /// starts a new replacement edge and finishes it with a replacement vertex
        /// </summary>
        /// <typeparam name="TVertex">type of vertex to add</typeparam>
        /// <typeparam name="TEdge">type of edge to add</typeparam>
        /// <param name="tag">string to identify vertex later</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder ReplacementVertexWithEdge<TVertex, TEdge>(string tag = null)
            where TVertex : Vertex, new()
            where TEdge : Edge, new()
        {
            ReplacementEdge<TEdge>();
            ReplacementVertex<TVertex>(tag);
            return this;
        }

        /// <summary>
        /// Starts a pattern and a replacment edge and finishes them with a pattern and replacement vertex
        /// </summary>
        /// <typeparam name="TVertex">type of vertex (implicit)</typeparam>
        /// <typeparam name="TEdge">type of edge (implicit)</typeparam>
        /// <param name="patternVertex">pattern vertex to add</param>
        /// <param name="replacementVertex">replacement vertex to add</param>
        /// <param name="patternEdge">edge to add to pattern vertex</param>
        /// <param name="replacementEdge">edge to add to replacment vertex</param>
        /// <param name="tag">string to identify vertex later</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder MappedVertexWithEdge<TVertex, TEdge>(TVertex patternVertex, TVertex replacementVertex,
            TEdge patternEdge, TEdge replacementEdge, string tag = null)
            where TVertex : Vertex
            where TEdge : Edge
        {
            MappedEdge(patternEdge, replacementEdge);
            MappedVertex(patternVertex, replacementVertex, tag);
            return this;
        }

        /// <summary>
        /// Starts a pattern and a replacment edge and finishes them with a pattern and replacement vertex
        /// </summary>
        /// <typeparam name="TVertex">type of vertices to add</typeparam>
        /// <typeparam name="TEdge">type of edges to add</typeparam>
        /// <param name="tag">string to identify vertex later</param>
        /// <returns>builder instance</returns>
        public ReplacementRuleBuilder MappedVertexWithEdge<TVertex, TEdge>(string tag = null)
            where TVertex : Vertex, new()
            where TEdge : Edge, new()
        {
            MappedEdge<TEdge>();
            MappedVertex<TVertex>(tag);
            return this;
        }
        #endregion

        #region private methods
        /// <summary>
        /// finalizes current uncompleted pattern edge
        /// </summary>
        /// <param name="vertex">vertex to complete edge with</param>
        private void FinalizePatternEdge(Vertex vertex)
        {
            if (currentPatternEdge == null) return;
            currentPatternEdge.Init(currentPatternVertex, vertex);
            result.Pattern.AddEdge(currentPatternEdge);
            currentPatternEdge = null;
        }

        /// <summary>
        /// finalizes current uncompleted replacment edge
        /// </summary>
        /// <param name="vertex">vertex to complete edge with</param>
        private void FinalizeReplacementEdge(Vertex vertex)
        {
            if (currentReplacementEdge == null) return;
            currentReplacementEdge.Init(currentReplacementVertex, vertex);
            result.Replacement.AddEdge(currentReplacementEdge);
            currentReplacementEdge = null;
        }

        /// <summary>
        /// Changes internal state to a new one
        /// </summary>
        /// <param name="newState">new stat to change to</param>
        private void ChangeState(State newState)
        {
            if (freezeState)
            {
                return;
            }

            if (possibleNextStates[currentState].Contains(newState))
            {
                currentState = newState;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot go from {currentState.ToString()} to {newState.ToString()}");
            }

            if (currentState != State.End)
            {
                lastValidState = currentState;
            }
        }
    }
    #endregion
}
