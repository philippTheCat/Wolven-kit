using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace WolvenKit.FlowTreeEditors {
    // Interface for a generic graph layout.
    public interface IGraphLayout
    {
        IEnumerable<Vertex> vertices { get; }
        IEnumerable<Edge> edges { get; }

        bool leftToRight { get; }

        void CalculateLayout(Graph graph);
    }

    public class Edge
    {
        // Indices in the vertex array of the layout algorithm.
        public Edge(Vertex src, Vertex dest)
        {
            source = src;
            destination = dest;
        }

        public Vertex source { get; private set; }

        public Vertex destination { get; private set; }
    }

    // One vertex is associated to each node in the graph.
    public class Vertex
    {
        // Center of the node in the graph layout.
        public Vector2 position { get; set; }

        // The Node represented by the vertex.
        public GraphNode node { get; private set; }

        public Vertex(GraphNode node)
        {
            this.node = node;
        }
    }

    // Implementation of Reingold and Tilford algorithm for graph layout
    // "Tidier Drawings of Trees", IEEE Transactions on Software Engineering Vol SE-7 No.2, March 1981
    // The implementation has been customized to support graphs with multiple roots and unattached nodes.
    public class ReingoldTilford : IGraphLayout
    {
        // By convention, all graph layout algorithms should have a minimum distance of 1 unit between nodes
        private static readonly float s_DistanceBetweenNodes = 1.0f;

        // Used to specify the vertical distance two non-attached trees in the graph.
        private static readonly float s_VerticalDistanceBetweenTrees = 3.0f;

        // Used to lengthen the wire when lots of children are connected. If 1, all levels will be evenly separated
        private static readonly float s_WireLengthFactorForLargeSpanningTrees = 3.0f;

        private static readonly float s_MaxChildrenThreshold = 6.0f;

        // Helper structure to easily find the vertex associated to a given Node.
        public readonly Dictionary<GraphNode, Vertex> m_NodeVertexLookup = new Dictionary<GraphNode, Vertex>();

        public ReingoldTilford(bool leftToRight = true)
        {
            this.leftToRight = leftToRight;
        }

        public IEnumerable<Vertex> vertices
        {
            get { return m_NodeVertexLookup.Values; }
        }

        public IEnumerable<Edge> edges
        {
            get
            {
                var edgesList = new List<Edge>();
                foreach (var node in m_NodeVertexLookup)
                {
                    Vertex v = node.Value;
                    foreach (GraphNode child in v.node.children)
                    {
                        edgesList.Add(new Edge(m_NodeVertexLookup[child], v));
                    }
                }
                return edgesList;
            }
        }

        public bool leftToRight { get; private set; }

        // Main entry point of the algorithm
        public void CalculateLayout(Graph graph)
        {
            m_NodeVertexLookup.Clear();
            foreach (GraphNode node in graph)
            {
                if (!m_NodeVertexLookup.ContainsKey(node)) {
                    m_NodeVertexLookup.Add(node, new Vertex(node));
                }
            }

            if (m_NodeVertexLookup.Count == 0) return;

            IList<float> horizontalPositions = ComputeHorizontalPositionForEachLevel();

            List<GraphNode> roots = m_NodeVertexLookup.Keys.Where(n => n.parent == null).ToList();

            for (int i = 0; i < roots.Count; ++i)
            {
                RecursiveLayout(roots[i], 0, horizontalPositions);

                if (i > 0)
                {
                    Vector2 previousRootRange = ComputeRangeRecursive(roots[i - 1]);
                    RecursiveMoveSubtree(roots[i], previousRootRange.Y + s_VerticalDistanceBetweenTrees + s_DistanceBetweenNodes);
                }
            }
        }

        // Precompute the horizontal position for each level.
        // Levels with few wires (as measured by the maximum number of children for one node) are placed closer
        // apart; very cluttered levels are placed further apart.
        private float[] ComputeHorizontalPositionForEachLevel()
        {
            // Gather information about depths.
            var maxDepth = int.MinValue;
            var nodeDepths = new Dictionary<int, List<GraphNode>>();
            foreach (GraphNode node in m_NodeVertexLookup.Keys)
            {
                int d = node.depth;
                List<GraphNode> nodes;
                if (!nodeDepths.TryGetValue(d, out nodes))
                {
                    nodeDepths[d] = nodes = new List<GraphNode>();
                }
                nodes.Add(node);
                maxDepth = Math.Max(d, maxDepth);
            }

            // Bake the left to right horizontal positions.
            var horizontalPositionForDepth = new float[maxDepth];
            horizontalPositionForDepth[0] = 0;
            for (int d = 1; d < maxDepth; ++d)
            {
                IEnumerable<GraphNode> nodesOnThisLevel = nodeDepths[d + 1];

                int maxChildren = nodesOnThisLevel.Max(x => x.children.Count);

                float wireLengthHeuristic = lerp(1, s_WireLengthFactorForLargeSpanningTrees,
                        Math.Min(1, maxChildren / s_MaxChildrenThreshold));

                horizontalPositionForDepth[d] = horizontalPositionForDepth[d - 1] +
                    s_DistanceBetweenNodes * wireLengthHeuristic;
            }

            return leftToRight ? horizontalPositionForDepth : horizontalPositionForDepth.Reverse().ToArray();
        }
        
        float lerp(float v0, float v1, float t) {
            return (1 - t) * v0 + t * v1;
        }
        // Traverse the graph and place all nodes according to the algorithm
        private void RecursiveLayout(GraphNode node, int depth, IList<float> horizontalPositions)
        {
            IList<GraphNode> children = node.children;
            foreach (GraphNode child in children)
            {
                RecursiveLayout(child, depth + 1, horizontalPositions);
            }

            var yPos = 0.0f;
            if (children.Count > 0)
            {
                SeparateSubtrees(children);
                yPos = GetAveragePosition(children).Y;
            }

            var pos = new Vector2(horizontalPositions[depth], yPos);
            m_NodeVertexLookup[node].position = pos;
        }

        private Vector2 ComputeRangeRecursive(GraphNode node)
        {
            Vector2 range = Vector2.One * m_NodeVertexLookup[node].position.Y;
            foreach (GraphNode child in node.children)
            {
                Vector2 childRange =  ComputeRangeRecursive(child);
                range.X = Math.Min(range.X, childRange.X);
                range.Y = Math.Max(range.Y, childRange.Y);
            }
            return range;
        }

        // Determine parent's vertical position based on its children
        private Vector2 GetAveragePosition(ICollection<GraphNode> children)
        {
            Vector2 centroid = new Vector2();

            centroid = children.Aggregate(centroid, (current, n) => current + m_NodeVertexLookup[n].position);

            if (children.Count > 0)
                centroid /= children.Count;

            return centroid;
        }

        // Separate the given subtrees so they do not overlap
        private void SeparateSubtrees(IList<GraphNode> subroots)
        {
            if (subroots.Count < 2)
                return;

            GraphNode upperNode = subroots[0];

            Dictionary<int, Vector2> upperTreeBoundaries = GetBoundaryPositions(upperNode);
            for (int s = 0; s < subroots.Count - 1; s++)
            {
                GraphNode lowerNode = subroots[s + 1];
                Dictionary<int, Vector2> lowerTreeBoundaries = GetBoundaryPositions(lowerNode);

                int minDepth = upperTreeBoundaries.Keys.Min();
                if (minDepth != lowerTreeBoundaries.Keys.Min())
                    
                    Debug.WriteLine("Cannot separate subtrees which do not start at the same root depth");

                int lowerMaxDepth = lowerTreeBoundaries.Keys.Max();
                int upperMaxDepth = upperTreeBoundaries.Keys.Max();
                int maxDepth = System.Math.Min(upperMaxDepth, lowerMaxDepth);

                for (int depth = minDepth; depth <= maxDepth; depth++)
                {
                    float delta = s_DistanceBetweenNodes - (lowerTreeBoundaries[depth].X - upperTreeBoundaries[depth].Y);
                    delta = System.Math.Max(delta, 0);
                    RecursiveMoveSubtree(lowerNode, delta);
                    for (int i = minDepth; i <= lowerMaxDepth; i++)
                        lowerTreeBoundaries[i] += new Vector2(delta, delta);
                }
                upperTreeBoundaries = CombineBoundaryPositions(upperTreeBoundaries, lowerTreeBoundaries);
            }
        }

        // Using a Vector2 at each depth to hold the extrema vertical positions
        private Dictionary<int, Vector2> GetBoundaryPositions(GraphNode subTreeRoot)
        {
            var extremePositions = new Dictionary<int, Vector2>();

            IEnumerable<GraphNode> descendants = GetSubtreeNodes(subTreeRoot);

            foreach (var node in descendants)
            {
                int depth = m_NodeVertexLookup[node].node.depth;
                float pos =  m_NodeVertexLookup[node].position.Y;
                if (extremePositions.ContainsKey(depth))
                    extremePositions[depth] = new Vector2(Math.Min(extremePositions[depth].X, pos),
                            Math.Max(extremePositions[depth].Y, pos));
                else
                    extremePositions[depth] = new Vector2(pos, pos);
            }

            return extremePositions;
        }

        // Includes all descendants and the subtree root itself
        private IEnumerable<GraphNode> GetSubtreeNodes(GraphNode root)
        {
            var allDescendants = new List<GraphNode> { root };
            foreach (GraphNode child in root.children)
            {
                allDescendants.AddRange(GetSubtreeNodes(child));
            }
            return allDescendants;
        }

        // After adjusting a subtree, compute its new boundary positions
        private Dictionary<int, Vector2> CombineBoundaryPositions(Dictionary<int, Vector2> upperTree, Dictionary<int, Vector2> lowerTree)
        {
            var combined = new Dictionary<int, Vector2>();
            int minDepth = upperTree.Keys.Min();
            int maxDepth = System.Math.Max(upperTree.Keys.Max(), lowerTree.Keys.Max());

            for (int d = minDepth; d <= maxDepth; d++)
            {
                float upperBoundary = upperTree.ContainsKey(d) ? upperTree[d].X : lowerTree[d].X;
                float lowerBoundary = lowerTree.ContainsKey(d) ? lowerTree[d].Y : upperTree[d].Y;
                combined[d] = new Vector2(upperBoundary, lowerBoundary);
            }
            return combined;
        }

        // Apply a vertical delta to all nodes in a subtree
        private void RecursiveMoveSubtree(GraphNode subtreeRoot, float yDelta)
        {
            Vector2 pos = m_NodeVertexLookup[subtreeRoot].position;
            m_NodeVertexLookup[subtreeRoot].position = new Vector2(pos.X, pos.Y + yDelta);

            foreach (GraphNode child in subtreeRoot.children)
            {
                RecursiveMoveSubtree(child, yDelta);
            }
        }
    }
    
}