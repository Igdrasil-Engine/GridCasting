using GridCasting.Models.Grid;
using GridCasting.Models.GridGraph;
using GridCasting.Utils.BVH;
using IgdrasilEngine.Engine.Math.Boxes;
using IgdrasilEngine.Engine.Math.Vectors;
using IgdrasilEngine.Engine.Utils;
using Path = GridCasting.Models.Path;

namespace GridCasting.Transform;

/// <summary>
/// Provides functionality for resolving, validating, and manipulating grid-based structures,
/// as well as transforming those structures into paths or other representations.
/// </summary>
/// <remarks>
/// The <c>GridResolver</c> class enables operations on grid graphs by integrating a set of transforms and ensuring
/// that grid structures are consistent and functional.
/// </remarks>
public class GridResolver
{
    /// <summary>
    /// Represents the underlying grid graph structure used by the <c>GridResolver</c> for processing grid-related sequences and operations.
    /// </summary>
    /// <remarks>
    /// The <c>_graph</c> field is an instance of <see cref="GridGraph"/>, which serves as the core data structure to store grid nodes and their relationships.
    /// It is used for graph traversal, pathfinding, range-based queries, and various graph-related manipulations within the <c>GridResolver</c> class.
    /// </remarks>
    private readonly GridGraph _graph;

    /// <summary>
    /// Defines the sensitivity threshold for operations within the <c>GridResolver</c>, impacting the resolution or precision
    /// of grid-based calculations, transformations, and spatial queries.
    /// </summary>
    /// <remarks>
    /// The <c>_sensitivity</c> field is primarily used to control tolerance levels in various methods, such as determining
    /// proximity, distance checks, and bounding region adjustments. It directly affects the accuracy and granularity of
    /// operations such as pathfinding and spatial queries within grid structures handled by the <c>GridResolver</c>.
    /// </remarks>
    private readonly float _sensitivity;

    /// <summary>
    /// Represents a 2D Bounding Volume Hierarchy (BVH) structure for efficiently storing and querying grid graph points
    /// used within the <c>GridResolver</c> class for spatial operations such as nearest-neighbor searches.
    /// </summary>
    /// <remarks>
    /// The <c>_bvh</c> field is an instance of <see cref="PointBVH2D{T}"/>, specifically of type <see cref="GridGraphPoint"/>.
    /// It facilitates spatial organization and query optimizations, enabling efficient handling of operations within the grid,
    /// such as pathfinding, point lookups, and range-based searches.
    /// This structure is dynamically built when the <c>GridResolver</c> is initialized based on the nodes of the associated grid graph.
    /// </remarks>
    private readonly PointBVH2D<GridGraphPoint> _bvh = new();

    private readonly FBox2 _graphAABB;

    /// <summary>
    /// Provides functionality for resolving, validating, and manipulating grid-based structures,
    /// as well as transforming those structures into paths or other representations.
    /// </summary>
    /// <remarks>
    /// The <c>GridResolver</c> class enables operations on grid graphs by integrating a set of transforms and ensuring
    /// that grid structures are consistent and functional.
    /// </remarks>
    public GridResolver(GridGraph graph, float sensitivity)
    {
        _graph = graph;
        _sensitivity = sensitivity;
        var aabb = new FBox2(FVector2.Zero, FVector2.Zero);
        HashSet<GridGraphNode> completedNodes = [];
        TraverseGraph(_graph,
            point =>
            {
                aabb = FBox2.Union(aabb, point.TreePosition);
                completedNodes.Add(point.Node);
                _bvh.Add(point);
                return point;
            },
            (_, to) => completedNodes.Contains(to.Node) ? TraversalResult.SkipNode : TraversalResult.EnqueueNode
        );
        _graphAABB = aabb;
    }


    /// <summary>
    /// Creates a grid graph from a given grid structure, converting the grid's nodes into a graph format,
    /// which enables advanced graph-based operations and transformations.
    /// </summary>
    /// <param name="graph">The source grid structure containing nodes to be converted into a graph representation.</param>
    /// <returns>
    /// A new instance of <c>GridGraph</c> representing the grid in a graph format if the conversion is successful;
    /// otherwise, returns null if the grid cannot be converted.
    /// </returns>
    public static GridGraph? CreateGridGraph(Grid graph)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// Verifies the structural integrity of a given grid graph by ensuring that each node maintains
    /// consistent positional relationships and no node is assigned multiple positions.
    /// </summary>
    /// <param name="graph">The grid graph to be verified.</param>
    /// <returns>
    /// A boolean value indicating whether the grid graph is valid. Returns true if the graph is structurally consistent,
    /// and false if inconsistencies, such as multiple positions for the same node, are detected.
    /// </returns>
    public static bool VerifyGridGraph(GridGraph graph)
    {
        Dictionary<GridGraphNode, FVector2> nodes = new();
        return TraverseGraph(
            graph,
            point =>
            {
                nodes.Add(point.Node, point.TreePosition);
                return point;
            },
            (_, to) => nodes.TryGetValue(to.Node, out var existingPosition)
                ? FVector2.Distance(to.TreePosition, existingPosition) > 1e-5
                    ? TraversalResult.AbortTraversal // Multiple positions for same node
                    : TraversalResult.SkipNode // Already traversed
                : TraversalResult.EnqueueNode // New node
        );
    }

    /// <summary>
    /// Generates a grid structure within a specified range by traversing a grid graph.
    /// It adds nodes and their corresponding connections based on the positions defined
    /// in the graph and the constraints of the given range.
    /// </summary>
    /// <param name="range">The rectangular range within which the grid structure is generated.</param>
    /// <returns>
    /// A new <see cref="Grid"/> instance containing nodes and connections that exist within
    /// the specified range in the grid graph.
    /// </returns>
    public Grid GenerateGrid(FBox2 range)
    {
        Dictionary<FVector2, GridNode> points = [];
        Grid grid = new();
        TraverseGraph(
            _graph,
            point => {
                if (points.TryGetValue(point.TreePosition, out var node)) return node;
                node = new GridNode(point.TreePosition);
                grid.Nodes.Add(node);
                return node;
            },
            (from, to) =>
            {
                if (!range.ContainsInclusive(to.TreePosition))
                {
                    from.Connections.Add(null);
                    return TraversalResult.SkipNode;
                }
                if (points.TryGetValue(to.TreePosition, out var node))
                {
                    from.Connections.Add(node);
                    return TraversalResult.SkipNode;
                }
                node = new GridNode(to.TreePosition);
                points.Add(node.Position, node);
                grid.Nodes.Add(node);
                from.Connections.Add(node);
                return TraversalResult.EnqueueNode;
            }
        );
        return grid;
    }

    /// <summary>
    /// Retrieves all grid positions from the specified range within the grid graph.
    /// Ensures that only unique positions are included and limits results to the defined range.
    /// </summary>
    /// <param name="range">The bounding box that defines the area within which grid positions are collected.</param>
    /// <returns>
    /// An array of FVector2 objects representing the unique grid positions contained within the specified range.
    /// </returns>
    public FVector2[] GetGridPositions(FBox2 range)
    {
        HashSet<FVector2> points = [];
        TraverseGraph(
            _graph,
            point => {
                points.Add(point.TreePosition);
                return point;
            },
            (_, to) => !range.ContainsInclusive(to.TreePosition) || points.Contains(to.TreePosition)
                ? TraversalResult.SkipNode
                : TraversalResult.EnqueueNode
        );
        return points.ToArray();
    }

    /// <summary>
    /// Generates a navigable path through a grid graph based on a sequence of positions.
    /// Validates the possibility of connecting the provided positions within the grid graph.
    /// </summary>
    /// <param name="positions">An array of positional vectors specifying the targeted path in the grid graph.</param>
    /// <returns>
    /// An instance of the <c>Path</c> struct if a valid path exists connecting the given positions,
    /// otherwise returns null if the path cannot be constructed due to inconsistencies or breaks.
    /// </returns>
    public Path? GetPath(FVector2[] positions)
    { 
        var point = GetNearestPoint(positions[0]);
        if (point == null) return null;
        var directions = new int[positions.Length - 1];
        var node = point.Node;
        var position = point.TreePosition;
        for (var i = 1; i < positions.Length; i++)
        {
            var updated = false;
            for (var j = 0; j < node.Edges.Count; j++)
            {
                var newPos = position + new FVector2(
                    node.Edges[j].Length * MathF.Cos(node.Edges[j].Angle),
                    node.Edges[j].Length * MathF.Sin(node.Edges[j].Angle)
                );
                if (FVector2.Distance(positions[i], newPos) > _sensitivity) continue;
                updated = true;
                directions[i - 1] = j;
                position = newPos;
                node = node.Edges[j].NodeA == node ? node.Edges[j].NodeB : node.Edges[j].NodeA;
                break;
            }
            if (!updated) return null; // Path doesn't exist
        }
        return new Path(_graph.IndexOf(node), directions);
    }

    private GridGraphPoint? GetNearestPoint(FVector2 position)
    {
        var points = _bvh.FindNearest(position, _sensitivity);
        switch (points.Count)
        {
            case 1:
                return points[0];
            case > 1:
                return null;
        }
        var completedPoints = new HashSet<GridGraphPoint>();
        PriorityQueue<GridGraphPoint, float> queue = new();
        var threshold = _graphAABB.Size.Length;
        var min = new GridGraphPoint(new FVector2(0, 0), _graph.Nodes[0]);
        var minDistance = FVector2.Distance(position, min.TreePosition);
        queue.Enqueue(min, minDistance);
        while (queue.TryDequeue(out var point, out var priority))
        {
            if (priority < _sensitivity) break;
            if (!completedPoints.Add(point)) continue;
            if (_bvh.FindNearest(point.TreePosition, 1e-5f).Count == 0) _bvh.Add(point);
            if (priority < minDistance)
            {
                minDistance = priority;
                min = point;
            }
            else if (priority > minDistance + threshold) break;

            foreach (var edge in point.Node.Edges)
            {
                var nextNode = edge.NodeA == point.Node ? edge.NodeB : edge.NodeA;
                var dir = new FVector2(
                    MathF.Cos(edge.Angle),
                    MathF.Sin(edge.Angle)
                );
                var nextPosition = point.TreePosition + edge.Length * dir;
                var nextPoint = new GridGraphPoint(nextPosition, nextNode);
                var dirToTarget = (position - point.TreePosition).Normalized;
                if (FVector2.Dot(dirToTarget, dir) < -0.3f) continue;
                queue.Enqueue(nextPoint, FVector2.Distance(nextPoint.TreePosition, position));
            }
        }
        
        return minDistance < _sensitivity ? min : null;
    }

    /// <summary>
    /// Traverses the specified grid graph using a breadth-first approach. The traversal process is controlled
    /// through a combination of a point update function and a traversal callback to determine the next course of action
    /// for each node being evaluated.
    /// </summary>
    /// <typeparam name="T">The type parameter determined by the result of the update function, which is forwarded to the traversal callback.</typeparam>
    /// <param name="graph">The grid graph to traverse.</param>
    /// <param name="update">A function that processes each grid point during traversal and returns a value of type <typeparamref name="T"/>.</param>
    /// <param name="callback">
    /// A function that decides the traversal behavior for each edge of the graph, using the result of the update function
    /// and the next grid point being evaluated. The callback returns a <see cref="TraversalResult"/> to either enqueue the node, skip it, or abort the traversal.
    /// </param>
    /// <param name="start">The starting point of the traversal. If not provided, the first node in the graph is used.</param>
    /// <returns>
    /// A boolean value indicating the success of the traversal. Returns true if the entire graph was processed without
    /// encountering a condition to abort traversal, and false otherwise.
    /// </returns>
    private static bool TraverseGraph<T>(GridGraph graph, Func<GridGraphPoint, T> update,
        Func<T, GridGraphPoint, TraversalResult> callback, GridGraphPoint? start = null)
    {
        Queue<GridGraphPoint> queue = new();
        queue.Enqueue(start ?? new GridGraphPoint(new FVector2(0, 0), graph.Nodes[0]));
        while (queue.TryDequeue(out var point))
        {
            var pointRepresentation = update(point);
            foreach (var edge in point.Node.Edges)
            {
                var nextNode = edge.NodeA == point.Node ? edge.NodeB : edge.NodeA;
                var nextPosition = point.TreePosition + new FVector2(
                    edge.Length * MathF.Cos(edge.Angle),
                    edge.Length * MathF.Sin(edge.Angle)
                );
                var nextPoint = new GridGraphPoint(nextPosition, nextNode);
                switch (callback(pointRepresentation, nextPoint))
                {
                    case TraversalResult.AbortTraversal:
                        return false;
                    case TraversalResult.SkipNode:
                        break;
                    case TraversalResult.EnqueueNode:
                    default:
                        queue.Enqueue(nextPoint);
                        break;
                }
            }
        }
        return true;
    }


    /// <summary>
    /// Represents the result of a traversal operation in a grid graph.
    /// </summary>
    private enum TraversalResult
    {
        EnqueueNode,
        SkipNode,
        AbortTraversal
    }
    

    /// <summary>
    /// Represents a point in a grid graph, used within a 2D BVH (Bounding Volume Hierarchy) tree structure.
    /// </summary>
    /// <remarks>
    /// The <c>GridGraphPoint</c> class is used to store and manage nodes of a grid graph, allowing for spatial operations
    /// such as nearest neighbor searches in 2D space. It inherits from <c>PointBVH2DTransform</c>, enabling integration
    /// with BVH for fast spatial queries.
    /// </remarks>
    private class GridGraphPoint(FVector2 position, GridGraphNode node) : PointBVH2DTransform<GridGraphPoint>
    {
        public override FVector2 TreePosition { get; protected set; } = position;
        public GridGraphNode Node { get; } = node;
        
        private static FVector2 Quantize(FVector2 p)
        {
            const float eps = 0.001f;
            return new FVector2(
                MathF.Round(p.X / eps) * eps,
                MathF.Round(p.Y / eps) * eps
            );
        }
        public override int GetHashCode() => HashCode.Combine(Node, Quantize(TreePosition));
    }
}