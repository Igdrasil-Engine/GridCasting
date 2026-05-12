using GridCasting.Models.Grid;
using GridCasting.Models.GridGraph;
using GridCasting.Utils;
using GridCasting.Utils.BVH;
#if NET8_0_OR_GREATER
using FVector2 = IgdrasilEngine.Engine.Math.Vectors.FVector2;
using FVector2Hashable = IgdrasilEngine.Engine.Math.Vectors.FVector2;
using FBox2 = IgdrasilEngine.Engine.Math.Boxes.FBox2;
#else
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;
using UnityEngine;
using FVector2 = UnityEngine.Vector2;
using FBox2 = UnityEngine.Rect;
#endif
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
    /// 
    /// </summary>
    private readonly IPathValidator[] _validators;

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
    public GridResolver(GridGraph graph, float sensitivity, params IPathValidator[] validators)
    {
        if (graph.Nodes.Count == 0)
            throw new ArgumentException("Graph must contain at least one node.", nameof(graph));
        _graph = graph;
        _sensitivity = sensitivity;
        _validators = validators;
#if NET8_0_OR_GREATER
        var aabb = new FBox2(FVector2.Zero, FVector2.Zero);
#else
        var aabb = new FBox2(FVector2.zero, FVector2.zero);
#endif
        HashSet<GridGraphNode> completedNodes = [];
        TraverseGraph(_graph,
            point =>
            {
#if NET8_0_OR_GREATER
                aabb = FBox2.Union(aabb, point.TreePosition);
#else
                aabb = new FBox2(
                    FVector2.Min(aabb.min, point.TreePosition),
                    FVector2.Max(aabb.max, point.TreePosition)
                );
#endif
                completedNodes.Add(point.Node);
                _bvh.Add(point);
                return point;
            },
            (_, to) => completedNodes.Contains(to.Node) ? TraversalResult.SkipNode : TraversalResult.EnqueueNode
        );
        _graphAABB = aabb;
    }


    /// <summary>
    /// Creates a graph representation from the given grid structure.
    /// </summary>
    /// <param name="grid">The grid structure containing nodes and their connections that should be converted into a graph.</param>
    /// <returns>
    /// A <c>GridGraph</c> instance representing the converted grid, or <c>null</c> if the grid is invalid
    /// or cannot be successfully converted.
    /// </returns>
    public static GridGraph? CreateGridGraph(Grid grid)
    {
        if (grid.Nodes.Count == 0) return null;
        var graph = new GridGraph();
        var graphNodes = new Dictionary<GridNode, GridGraphNode>();
        var queue = new Queue<(GridNode? prev, GridNode curr)>();
        queue.Enqueue((null, grid.Nodes[0]));

        while (queue.TryDequeue(out var result))
        {
            var (prev, curr) = result;
            if (graphNodes.TryGetValue(curr, out var match)) continue;
            
            foreach (var connection in curr.Connections.OfType<GridNode>()) 
                queue.Enqueue((curr, connection));
            
            var matches = 0;
            var freeConnections = curr.Connections.Count(c => c != null);
            foreach (var graphNode in graph)
            {
                if (graphNode.Edges.Count != curr.Connections.Count) continue;

                var usedPositions = graphNode.Edges.Count(edge =>
                {
                    var len = edge.NodeA == graphNode ? edge.Length : -edge.Length;
                    var position = curr.Position + new FVector2(
                        len * MathF.Cos(edge.Angle),
                        len * MathF.Sin(edge.Angle)
                    );
                    // Add check for edge multiuse
                    return curr.Connections.Any(c => c != null && FVector2.Distance(c.Position, position) < 1e-4f);
                });
                if (freeConnections != usedPositions) continue;
                if (matches != 0) return null; // Multiple matches
                match = graphNode;
                matches++;
            }

            if (match == null && freeConnections == curr.Connections.Count) // Create a new node only if there are no hanging edges
            {
                match = new GridGraphNode();
                foreach (var node in curr.Connections.OfType<GridNode>())
                {
                    var direction = node.Position - curr.Position;
#if NET8_0_OR_GREATER
                    var angle = MathF.Atan2(direction.Y, direction.X);
                    var len = direction.Length;
#else
                    var angle = Mathf.Atan2(direction.y, direction.x);
                    var len = direction.magnitude;
#endif
                    match.Edges.Add(new GridGraphEdge(match, null, angle, len));
                }
                graph.Nodes.Add(match);
            }
            if (match == null) continue;
            
            graphNodes.Add(curr, match);
            if (prev == null || !graphNodes.TryGetValue(prev, out var prevNode)) continue;
            var linked = false;
            foreach (var edge in from edge in prevNode.Edges
                     let len = edge.NodeA == prevNode ? edge.Length : -edge.Length
                     let position = prev.Position + new FVector2(
                         len * MathF.Cos(edge.Angle),
                         len * MathF.Sin(edge.Angle)
                     )
                     where !(FVector2.Distance(curr.Position, position) >= 1e-4f)
                     select edge)
            {
                if (edge.NodeA == prevNode)
                {
                    if (edge.NodeB != null && edge.NodeB != match) return null;
                    edge.NodeB = match;
                }
                else if (edge.NodeB == prevNode)
                {
                    if (edge.NodeA != null && edge.NodeA != match) return null;
                    edge.NodeA = match;
                }
                else continue;
                linked = true;
                break;
            }
            if (!linked) return null;
        }

        return graphNodes.Values.Distinct()
            .Any(value => value.Edges.Any(edge => edge.NodeA == null || edge.NodeB == null))
            ? null
            : graph;
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
#if NET8_0_OR_GREATER
                if (!range.ContainsInclusive(to.TreePosition))
#else
                if (!range.Contains(to.TreePosition))
#endif
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
        HashSet<FVector2Hashable> points = [];
        TraverseGraph(
            _graph,
            point => {
                points.Add(new FVector2Hashable(point.TreePosition));
                return point;
            },
#if NET8_0_OR_GREATER
            (_, to) => !range.ContainsInclusive(to.TreePosition) || points.Contains(to.TreePosition)
#else
            (_, to) => !range.Contains(to.TreePosition) 
                       || points.Contains(new FVector2Hashable(to.TreePosition))
#endif
                ? TraversalResult.SkipNode
                : TraversalResult.EnqueueNode
        );
#if NET8_0_OR_GREATER
        return points.ToArray();
#else
        return points.Select(p => p.BaseVector).ToArray();
#endif
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
        if (positions.Length == 0) return null;
        var point = GetNearestPoint(positions[0]);
        if (point == null) return null;
        var directions = new int[positions.Length - 1];
        var node = point.Node;
        var position = point.TreePosition;
        var start = node;
        var startPos = position;
        for (var i = 1; i < positions.Length; i++)
        {
            var updated = false;
            for (var j = 0; j < node.Edges.Count; j++)
            {
                var edge = node.Edges[j];
                var length = edge.NodeA == node ? edge.Length : -edge.Length;
                var newPos = position + new FVector2(
                    length * MathF.Cos(edge.Angle),
                    length * MathF.Sin(edge.Angle)
                );
                if (FVector2.Distance(positions[i], newPos) > _sensitivity) continue;
                updated = true;
                directions[i - 1] = j;
                position = newPos;
                node = edge.NodeA == node ? edge.NodeB : edge.NodeA;
                break;
            }
            if (!updated) return null; // Path doesn't exist
        }

        var path = new Path(_graph.IndexOf(start), directions);
        return _validators.Any(validator => !validator.IsValid(_graph, startPos, path)) ? null : path;
    }

    public FVector2? GetNearestNode(FVector2 position) => GetNearestPoint(position)?.TreePosition;

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
#if NET8_0_OR_GREATER
        var threshold = _graphAABB.Size.Length * 2;
#else
        var threshold = _graphAABB.size.magnitude * 2;
#endif
        var min = _bvh.FindNearest(position) ?? new GridGraphPoint(new FVector2(0, 0), _graph.Nodes[0]);
        var minDistance = FVector2.Distance(position, min.TreePosition);
        queue.Enqueue(min, minDistance);
        while (queue.TryDequeue(out var point, out var priority))
        {
            if (!completedPoints.Add(point)) continue;
            if (_bvh.FindNearest(point.TreePosition, _sensitivity).Count == 0) _bvh.Add(point);
            if (priority < minDistance)
            {
                minDistance = priority;
                min = point;
                if (priority < _sensitivity) break;
            }
            else if (priority > minDistance + threshold) break;

            foreach (var edge in point.Node.Edges)
            {
                var nextNode = edge.NodeA == point.Node ? edge.NodeB : edge.NodeA;
                var length = edge.NodeA == point.Node ? edge.Length : -edge.Length;
                var dir = new FVector2(
                    MathF.Cos(edge.Angle),
                    MathF.Sin(edge.Angle)
                );
                var nextPosition = point.TreePosition + length * dir;
                var nextPoint = new GridGraphPoint(nextPosition, nextNode);
// #if NET8_0_OR_GREATER
//                 var dirToTarget = (position - point.TreePosition).Normalized;
// #else
//                 var dirToTarget = (position - point.TreePosition).normalized;
// #endif
//                 if (FVector2.Dot(dirToTarget, dir) > -0.3f) continue;
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
                var length = edge.NodeA == point.Node ? edge.Length : -edge.Length;
                var nextPosition = point.TreePosition + new FVector2(
                    length * MathF.Cos(edge.Angle),
                    length * MathF.Sin(edge.Angle)
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
#if NET8_0_OR_GREATER
                MathF.Round(p.X / eps) * eps,
                MathF.Round(p.Y / eps) * eps
#else
                MathF.Round(p.x / eps) * eps,
                MathF.Round(p.y / eps) * eps
#endif
            );
        }

        protected bool Equals(GridGraphPoint other)
        {
            return Quantize(TreePosition).Equals(Quantize(other.TreePosition)) && Node.Equals(other.Node);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((GridGraphPoint)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Quantize(TreePosition), Node);
        }
    }
}

#if !NET8_0_OR_GREATER

/// <summary>
/// Provides a hashable wrapper for the <c>Vector2</c> type, allowing for accurate
/// usage in hash-based collections such as <c>HashSet</c> and <c>Dictionary</c>.
/// </summary>
/// <remarks>
/// The <c>FVector2Hashable</c> class ensures that floating-point precision issues do not
/// interfere when comparing or hashing <c>Vector2</c> instances. Instances of this class are
/// compared using a precision threshold and are hashed using scaled and floored values of the
/// wrapped <c>Vector2</c>.
/// </remarks>
internal class FVector2Hashable(FVector2 vector)
{
    protected bool Equals(FVector2Hashable other) => 
        MathF.Abs(vector.x - other.BaseVector.x) < 1e-4f && MathF.Abs(vector.y - other.BaseVector.y) < 1e-4f;

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((FVector2Hashable)obj);
    }
    public override int GetHashCode() {
        return HashCode.Combine(MathF.Floor(vector.x * 1e4f), MathF.Floor(vector.y * 1e4f));
    }

    public FVector2 BaseVector => vector;
}

/// <summary>
///  Represents a min priority queue.
/// </summary>
/// <typeparam name="TElement">Specifies the type of elements in the queue.</typeparam>
/// <typeparam name="TPriority">Specifies the type of priority associated with enqueued elements.</typeparam>
/// <remarks>
///  Implements an array-backed quaternary min-heap. Each element is enqueued with an associated priority
///  that determines the dequeue order: elements with the lowest priority get dequeued first.
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
public class PriorityQueue<TElement, TPriority>
{
    /// <summary>
    /// Represents an implicit heap-ordered complete d-ary tree, stored as an array.
    /// </summary>
    private (TElement Element, TPriority Priority)[] _nodes;

    /// <summary>
    /// Custom comparer used to order the heap.
    /// </summary>
    private readonly IComparer<TPriority>? _comparer;

    /// <summary>
    /// The number of nodes in the heap.
    /// </summary>
    private int _size;

    /// <summary>
    /// Version updated on mutation to help validate enumerators operate on a consistent state.
    /// </summary>
    private int _version;

    /// <summary>
    /// Specifies the arity of the d-ary heap, which here is quaternary.
    /// It is assumed that this value is a power of 2.
    /// </summary>
    private const int Arity = 4;

    /// <summary>
    /// The binary logarithm of <see cref="Arity" />.
    /// </summary>
    private const int Log2Arity = 2;

#if DEBUG
    static PriorityQueue()
    {
        Debug.Assert(Log2Arity > 0 && Math.Pow(2, Log2Arity) == Arity);
    }
#endif

    /// <summary>
    ///  Initializes a new instance of the <see cref="PriorityQueue{TElement, TPriority}"/> class.
    /// </summary>
    public PriorityQueue()
    {
        _nodes = Array.Empty<(TElement, TPriority)>();
        _comparer = InitializeComparer(null);
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="PriorityQueue{TElement, TPriority}"/> class
    ///  with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">Initial capacity to allocate in the underlying heap array.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  The specified <paramref name="initialCapacity"/> was negative.
    /// </exception>
    public PriorityQueue(int initialCapacity)
        : this(initialCapacity, comparer: null)
    {
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="PriorityQueue{TElement, TPriority}"/> class
    ///  with the specified custom priority comparer.
    /// </summary>
    /// <param name="comparer">
    ///  Custom comparer dictating the ordering of elements.
    ///  Uses <see cref="Comparer{T}.Default" /> if the argument is <see langword="null"/>.
    /// </param>
    public PriorityQueue(IComparer<TPriority>? comparer)
    {
        _nodes = Array.Empty<(TElement, TPriority)>();
        _comparer = InitializeComparer(comparer);
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="PriorityQueue{TElement, TPriority}"/> class
    ///  with the specified initial capacity and custom priority comparer.
    /// </summary>
    /// <param name="initialCapacity">Initial capacity to allocate in the underlying heap array.</param>
    /// <param name="comparer">
    ///  Custom comparer dictating the ordering of elements.
    ///  Uses <see cref="Comparer{T}.Default" /> if the argument is <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  The specified <paramref name="initialCapacity"/> was negative.
    /// </exception>
    public PriorityQueue(int initialCapacity, IComparer<TPriority>? comparer)
    {
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        _nodes = new (TElement, TPriority)[initialCapacity];
        _comparer = InitializeComparer(comparer);
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="PriorityQueue{TElement, TPriority}"/> class
    ///  that is populated with the specified elements and priorities.
    /// </summary>
    /// <param name="items">The pairs of elements and priorities with which to populate the queue.</param>
    /// <exception cref="ArgumentNullException">
    ///  The specified <paramref name="items"/> argument was <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///  Constructs the heap using a heapify operation,
    ///  which is generally faster than enqueuing individual elements sequentially.
    /// </remarks>
    public PriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items)
        : this(items, comparer: null)
    {
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="PriorityQueue{TElement, TPriority}"/> class
    ///  that is populated with the specified elements and priorities,
    ///  and with the specified custom priority comparer.
    /// </summary>
    /// <param name="items">The pairs of elements and priorities with which to populate the queue.</param>
    /// <param name="comparer">
    ///  Custom comparer dictating the ordering of elements.
    ///  Uses <see cref="Comparer{T}.Default" /> if the argument is <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///  The specified <paramref name="items"/> argument was <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///  Constructs the heap using a heapify operation,
    ///  which is generally faster than enqueuing individual elements sequentially.
    /// </remarks>
    public PriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items, IComparer<TPriority>? comparer)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        _nodes = items.ToArray();
        _size = _nodes.Length;
        _comparer = InitializeComparer(comparer);

        if (_size > 1)
        {
            Heapify();
        }
    }

    /// <summary>
    ///  Gets the number of elements contained in the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    public int Count => _size;

    /// <summary>
    ///  Gets the total numbers of elements the queue's backing storage can hold without resizing.
    /// </summary>
    public int Capacity => _nodes.Length;

    /// <summary>
    ///  Gets the priority comparer used by the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    public IComparer<TPriority> Comparer => _comparer ?? Comparer<TPriority>.Default;


    /// <summary>
    ///  Adds the specified element with associated priority to the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    /// <param name="element">The element to add to the <see cref="PriorityQueue{TElement, TPriority}"/>.</param>
    /// <param name="priority">The priority with which to associate the new element.</param>
    public void Enqueue(TElement element, TPriority priority)
    {
        // Virtually add the node at the end of the underlying array.
        // Note that the node being enqueued does not need to be physically placed
        // there at this point, as such an assignment would be redundant.

        var currentSize = _size;
        _version++;

        if (_nodes.Length == currentSize)
        {
            Grow(currentSize + 1);
        }

        _size = currentSize + 1;

        if (_comparer == null)
        {
            MoveUpDefaultComparer((element, priority), currentSize);
        }
        else
        {
            MoveUpCustomComparer((element, priority), currentSize);
        }
    }

    /// <summary>
    ///  Returns the minimal element from the <see cref="PriorityQueue{TElement, TPriority}"/> without removing it.
    /// </summary>
    /// <exception cref="InvalidOperationException">The <see cref="PriorityQueue{TElement, TPriority}"/> is empty.</exception>
    /// <returns>The minimal element of the <see cref="PriorityQueue{TElement, TPriority}"/>.</returns>
    public TElement Peek() => _size == 0 ? throw new InvalidOperationException("The queue is empty.") : _nodes[0].Element;

    /// <summary>
    ///  Removes and returns the minimal element from the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The queue is empty.</exception>
    /// <returns>The minimal element of the <see cref="PriorityQueue{TElement, TPriority}"/>.</returns>
    public TElement Dequeue()
    {
        if (_size == 0)
            throw new InvalidOperationException("The queue is empty.");
        

        var element = _nodes[0].Element;
        RemoveRootNode();
        return element;
    }

    /// <summary>
    ///  Removes the minimal element and then immediately adds the specified element with associated priority to the <see cref="PriorityQueue{TElement, TPriority}"/>,
    /// </summary>
    /// <param name="element">The element to add to the <see cref="PriorityQueue{TElement, TPriority}"/>.</param>
    /// <param name="priority">The priority with which to associate the new element.</param>
    /// <exception cref="InvalidOperationException">The queue is empty.</exception>
    /// <returns>The minimal element removed before performing the enqueue operation.</returns>
    /// <remarks>
    ///  Implements an extract-then-insert heap operation that is generally more efficient
    ///  than sequencing Dequeue and Enqueue operations: in the worst case scenario only one
    ///  shift-down operation is required.
    /// </remarks>
    public TElement DequeueEnqueue(TElement element, TPriority priority)
    {
        if (_size == 0)
            throw new InvalidOperationException("The queue is empty.");

        var root = _nodes[0];

        if (_comparer == null)
        {
            if (Comparer<TPriority>.Default.Compare(priority, root.Priority) > 0)
            {
                MoveDownDefaultComparer((element, priority), 0);
            }
            else
            {
                _nodes[0] = (element, priority);
            }
        }
        else
        {
            if (_comparer.Compare(priority, root.Priority) > 0)
            {
                MoveDownCustomComparer((element, priority), 0);
            }
            else
            {
                _nodes[0] = (element, priority);
            }
        }

        _version++;
        return root.Element;
    }

    /// <summary>
    ///  Removes the minimal element from the <see cref="PriorityQueue{TElement, TPriority}"/>,
    ///  and copies it to the <paramref name="element"/> parameter,
    ///  and its associated priority to the <paramref name="priority"/> parameter.
    /// </summary>
    /// <param name="element">The removed element.</param>
    /// <param name="priority">The priority associated with the removed element.</param>
    /// <returns>
    ///  <see langword="true"/> if the element is successfully removed;
    ///  <see langword="false"/> if the <see cref="PriorityQueue{TElement, TPriority}"/> is empty.
    /// </returns>
    public bool TryDequeue([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
    {
        if (_size != 0)
        {
            (element, priority) = _nodes[0];
            RemoveRootNode();
            return true;
        }

        element = default;
        priority = default;
        return false;
    }

    /// <summary>
    ///  Returns a value that indicates whether there is a minimal element in the <see cref="PriorityQueue{TElement, TPriority}"/>,
    ///  and if one is present, copies it to the <paramref name="element"/> parameter,
    ///  and its associated priority to the <paramref name="priority"/> parameter.
    ///  The element is not removed from the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    /// <param name="element">The minimal element in the queue.</param>
    /// <param name="priority">The priority associated with the minimal element.</param>
    /// <returns>
    ///  <see langword="true"/> if there is a minimal element;
    ///  <see langword="false"/> if the <see cref="PriorityQueue{TElement, TPriority}"/> is empty.
    /// </returns>
    public bool TryPeek([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
    {
        if (_size != 0)
        {
            (element, priority) = _nodes[0];
            return true;
        }

        element = default;
        priority = default;
        return false;
    }

    /// <summary>
    ///  Adds the specified element with associated priority to the <see cref="PriorityQueue{TElement, TPriority}"/>,
    ///  and immediately removes the minimal element, returning the result.
    /// </summary>
    /// <param name="element">The element to add to the <see cref="PriorityQueue{TElement, TPriority}"/>.</param>
    /// <param name="priority">The priority with which to associate the new element.</param>
    /// <returns>The minimal element removed after the enqueue operation.</returns>
    /// <remarks>
    ///  Implements an insert-then-extract heap operation that is generally more efficient
    ///  than sequencing Enqueue and Dequeue operations: in the worst case scenario only one
    ///  shift-down operation is required.
    /// </remarks>
    public TElement EnqueueDequeue(TElement element, TPriority priority)
    {
        if (_size != 0)
        {
            var root = _nodes[0];

            if (_comparer == null)
            {
                if (Comparer<TPriority>.Default.Compare(priority, root.Priority) > 0)
                {
                    MoveDownDefaultComparer((element, priority), 0);
                    _version++;
                    return root.Element;
                }
            }
            else
            {
                if (_comparer.Compare(priority, root.Priority) > 0)
                {
                    MoveDownCustomComparer((element, priority), 0);
                    _version++;
                    return root.Element;
                }
            }
        }

        return element;
    }

    /// <summary>
    ///  Enqueues a sequence of element/priority pairs to the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    /// <param name="items">The pairs of elements and priorities to add to the queue.</param>
    /// <exception cref="ArgumentNullException">
    ///  The specified <paramref name="items"/> argument was <see langword="null"/>.
    /// </exception>
    public void EnqueueRange(IEnumerable<(TElement Element, TPriority Priority)> items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        var count = 0;
        var collection = items as ICollection<(TElement Element, TPriority Priority)>;
        if (collection is not null && (count = collection.Count) > _nodes.Length - _size)
        {
            Grow(checked(_size + count));
        }

        if (_size == 0)
        {
            // build using Heapify() if the queue is empty.

            if (collection is not null)
            {
                collection.CopyTo(_nodes, 0);
                _size = count;
            }
            else
            {
                var i = 0;
                (TElement, TPriority)[] nodes = _nodes;
                foreach ((var element, var priority) in items)
                {
                    if (nodes.Length == i)
                    {
                        Grow(i + 1);
                        nodes = _nodes;
                    }

                    nodes[i++] = (element, priority);
                }

                _size = i;
            }

            _version++;

            if (_size > 1)
            {
                Heapify();
            }
        }
        else
        {
            foreach ((var element, var priority) in items)
            {
                Enqueue(element, priority);
            }
        }
    }

    /// <summary>
    ///  Enqueues a sequence of elements pairs to the <see cref="PriorityQueue{TElement, TPriority}"/>,
    ///  all associated with the specified priority.
    /// </summary>
    /// <param name="elements">The elements to add to the queue.</param>
    /// <param name="priority">The priority to associate with the new elements.</param>
    /// <exception cref="ArgumentNullException">
    ///  The specified <paramref name="elements"/> argument was <see langword="null"/>.
    /// </exception>
    public void EnqueueRange(IEnumerable<TElement> elements, TPriority priority)
    {
        if (elements is null)
            throw new ArgumentNullException(nameof(elements));

        int count;
        if (elements is ICollection<TElement> collection &&
            (count = collection.Count) > _nodes.Length - _size)
        {
            Grow(checked(_size + count));
        }

        if (_size == 0)
        {
            // If the queue is empty just append the elements since they all have the same priority.

            var i = 0;
            (TElement, TPriority)[] nodes = _nodes;
            foreach (var element in elements)
            {
                if (nodes.Length == i)
                {
                    Grow(i + 1);
                    nodes = _nodes;
                }

                nodes[i++] = (element, priority);
            }

            _size = i;
            _version++;
        }
        else
        {
            foreach (var element in elements)
            {
                Enqueue(element, priority);
            }
        }
    }

    /// <summary>
    /// Removes the first occurrence that equals the specified parameter.
    /// </summary>
    /// <param name="element">The element to try to remove.</param>
    /// <param name="removedElement">The actual element that got removed from the queue.</param>
    /// <param name="priority">The priority value associated with the removed element.</param>
    /// <param name="equalityComparer">The equality comparer governing element equality.</param>
    /// <returns><see langword="true"/> if matching entry was found and removed, <see langword="false"/> otherwise.</returns>
    /// <remarks>
    /// The method performs a linear-time scan of every element in the heap, removing the first value found to match the <paramref name="element"/> parameter.
    /// In case of duplicate entries, what entry does get removed is non-deterministic and does not take priority into account.
    ///
    /// If no <paramref name="equalityComparer"/> is specified, <see cref="EqualityComparer{TElement}.Default"/> will be used instead.
    /// </remarks>
    public bool Remove(
        TElement element,
        [MaybeNullWhen(false)] out TElement removedElement,
        [MaybeNullWhen(false)] out TPriority priority,
        IEqualityComparer<TElement>? equalityComparer = null)
    {
        var index = FindIndex(element, equalityComparer);
        if (index < 0)
        {
            removedElement = default;
            priority = default;
            return false;
        }

        var nodes = _nodes;
        (removedElement, priority) = nodes[index];
        var newSize = --_size;

        if (index < newSize)
        {
            // We're removing an element from the middle of the heap.
            // Pop the last element in the collection and sift from the removed index.
            var lastNode = nodes[newSize];

            if (_comparer == null)
            {
                if (Comparer<TPriority>.Default.Compare(lastNode.Priority, priority) < 0)
                {
                    MoveUpDefaultComparer(lastNode, index);
                }
                else
                {
                    MoveDownDefaultComparer(lastNode, index);
                }
            }
            else
            {
                if (_comparer.Compare(lastNode.Priority, priority) < 0)
                {
                    MoveUpCustomComparer(lastNode, index);
                }
                else
                {
                    MoveDownCustomComparer(lastNode, index);
                }
            }
        }

        nodes[newSize] = default;
        _version++;
        return true;
    }

    /// <summary>
    ///  Removes all items from the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<(TElement, TPriority)>())
        {
            // Clear the elements so that the gc can reclaim the references
            Array.Clear(_nodes, 0, _size);
        }

        _size = 0;
        _version++;
    }

    /// <summary>
    ///  Ensures that the <see cref="PriorityQueue{TElement, TPriority}"/> can hold up to
    ///  <paramref name="capacity"/> items without further expansion of its backing storage.
    /// </summary>
    /// <param name="capacity">The minimum capacity to be used.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  The specified <paramref name="capacity"/> is negative.
    /// </exception>
    /// <returns>The current capacity of the <see cref="PriorityQueue{TElement, TPriority}"/>.</returns>
    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        if (_nodes.Length >= capacity) return _nodes.Length;
        Grow(capacity);
        _version++;

        return _nodes.Length;
    }

    /// <summary>
    ///  Sets the capacity to the actual number of items in the <see cref="PriorityQueue{TElement, TPriority}"/>,
    ///  if that is less than 90 percent of current capacity.
    /// </summary>
    /// <remarks>
    ///  This method can be used to minimize a collection's memory overhead
    ///  if no new elements will be added to the collection.
    /// </remarks>
    public void TrimExcess()
    {
        var threshold = (int)(_nodes.Length * 0.9);
        if (_size >= threshold) return;
        Array.Resize(ref _nodes, _size);
        _version++;
    }

    /// <summary>
    /// Grows the priority queue to match the specified min capacity.
    /// </summary>
    private void Grow(int minCapacity)
    {
        Debug.Assert(_nodes.Length < minCapacity);

        const int GrowFactor = 2;
        const int MinimumGrow = 4;

        var newcapacity = GrowFactor * _nodes.Length;

        // Allow the queue to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when _nodes.Length overflowed thanks to the (uint) cast
        if ((uint)newcapacity > 0X7FFFFFC7) newcapacity = 0X7FFFFFC7;

        // Ensure minimum growth is respected.
        newcapacity = Math.Max(newcapacity, _nodes.Length + MinimumGrow);

        // If the computed capacity is still less than specified, set to the original argument.
        // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
        if (newcapacity < minCapacity) newcapacity = minCapacity;

        Array.Resize(ref _nodes, newcapacity);
    }

    /// <summary>
    /// Removes the node from the root of the heap
    /// </summary>
    private void RemoveRootNode()
    {
        var lastNodeIndex = --_size;
        _version++;

        if (lastNodeIndex > 0)
        {
            var lastNode = _nodes[lastNodeIndex];
            if (_comparer == null)
            {
                MoveDownDefaultComparer(lastNode, 0);
            }
            else
            {
                MoveDownCustomComparer(lastNode, 0);
            }
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<(TElement, TPriority)>())
        {
            _nodes[lastNodeIndex] = default;
        }
    }

    /// <summary>
    /// Gets the index of an element's parent.
    /// </summary>
    private static int GetParentIndex(int index) => (index - 1) >> Log2Arity;

    /// <summary>
    /// Gets the index of the first child of an element.
    /// </summary>
    private static int GetFirstChildIndex(int index) => (index << Log2Arity) + 1;

    /// <summary>
    /// Converts an unordered list into a heap.
    /// </summary>
    private void Heapify()
    {
        // Leaves of the tree are in fact 1-element heaps, for which there
        // is no need to correct them. The heap property needs to be restored
        // only for higher nodes, starting from the first node that has children.
        // It is the parent of the very last element in the array.

        var nodes = _nodes;
        var lastParentWithChildren = GetParentIndex(_size - 1);

        if (_comparer == null)
        {
            for (var index = lastParentWithChildren; index >= 0; --index)
            {
                MoveDownDefaultComparer(nodes[index], index);
            }
        }
        else
        {
            for (var index = lastParentWithChildren; index >= 0; --index)
            {
                MoveDownCustomComparer(nodes[index], index);
            }
        }
    }

    /// <summary>
    /// Moves a node up in the tree to restore heap order.
    /// </summary>
    private void MoveUpDefaultComparer((TElement Element, TPriority Priority) node, int nodeIndex)
    {
        // Instead of swapping items all the way to the root, we will perform
        // a similar optimization as in the insertion sort.

        Debug.Assert(_comparer is null);
        Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

        var nodes = _nodes;

        while (nodeIndex > 0)
        {
            var parentIndex = GetParentIndex(nodeIndex);
            var parent = nodes[parentIndex];

            if (Comparer<TPriority>.Default.Compare(node.Priority, parent.Priority) < 0)
            {
                nodes[nodeIndex] = parent;
                nodeIndex = parentIndex;
            }
            else
            {
                break;
            }
        }

        nodes[nodeIndex] = node;
    }

    /// <summary>
    /// Moves a node up in the tree to restore heap order.
    /// </summary>
    private void MoveUpCustomComparer((TElement Element, TPriority Priority) node, int nodeIndex)
    {
        // Instead of swapping items all the way to the root, we will perform
        // a similar optimization as in the insertion sort.

        Debug.Assert(_comparer is not null);
        Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

        var comparer = _comparer;
        var nodes = _nodes;

        while (nodeIndex > 0)
        {
            var parentIndex = GetParentIndex(nodeIndex);
            var parent = nodes[parentIndex];

            if (comparer.Compare(node.Priority, parent.Priority) < 0)
            {
                nodes[nodeIndex] = parent;
                nodeIndex = parentIndex;
            }
            else
            {
                break;
            }
        }

        nodes[nodeIndex] = node;
    }

    /// <summary>
    /// Moves a node down in the tree to restore heap order.
    /// </summary>
    private void MoveDownDefaultComparer((TElement Element, TPriority Priority) node, int nodeIndex)
    {
        // The node to move down will not actually be swapped every time.
        // Rather, values on the affected path will be moved up, thus leaving a free spot
        // for this value to drop in. Similar optimization as in the insertion sort.

        Debug.Assert(_comparer is null);
        Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

        var nodes = _nodes;
        var size = _size;

        int i;
        while ((i = GetFirstChildIndex(nodeIndex)) < size)
        {
            // Find the child node with the minimal priority
            var minChild = nodes[i];
            var minChildIndex = i;

            var childIndexUpperBound = Math.Min(i + Arity, size);
            while (++i < childIndexUpperBound)
            {
                var nextChild = nodes[i];
                if (Comparer<TPriority>.Default.Compare(nextChild.Priority, minChild.Priority) < 0)
                {
                    minChild = nextChild;
                    minChildIndex = i;
                }
            }

            // Heap property is satisfied; insert node in this location.
            if (Comparer<TPriority>.Default.Compare(node.Priority, minChild.Priority) <= 0)
            {
                break;
            }

            // Move the minimal child up by one node and
            // continue recursively from its location.
            nodes[nodeIndex] = minChild;
            nodeIndex = minChildIndex;
        }

        nodes[nodeIndex] = node;
    }

    /// <summary>
    /// Moves a node down in the tree to restore heap order.
    /// </summary>
    private void MoveDownCustomComparer((TElement Element, TPriority Priority) node, int nodeIndex)
    {
        // The node to move down will not actually be swapped every time.
        // Rather, values on the affected path will be moved up, thus leaving a free spot
        // for this value to drop in. Similar optimization as in the insertion sort.

        Debug.Assert(_comparer is not null);
        Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

        var comparer = _comparer;
        var nodes = _nodes;
        var size = _size;

        int i;
        while ((i = GetFirstChildIndex(nodeIndex)) < size)
        {
            // Find the child node with the minimal priority
            var minChild = nodes[i];
            var minChildIndex = i;

            var childIndexUpperBound = Math.Min(i + Arity, size);
            while (++i < childIndexUpperBound)
            {
                var nextChild = nodes[i];
                if (comparer.Compare(nextChild.Priority, minChild.Priority) < 0)
                {
                    minChild = nextChild;
                    minChildIndex = i;
                }
            }

            // Heap property is satisfied; insert node in this location.
            if (comparer.Compare(node.Priority, minChild.Priority) <= 0)
            {
                break;
            }

            // Move the minimal child up by one node and continue recursively from its location.
            nodes[nodeIndex] = minChild;
            nodeIndex = minChildIndex;
        }

        nodes[nodeIndex] = node;
    }

    /// <summary>
    /// Scans the heap for the first index containing an element equal to the specified parameter.
    /// </summary>
    private int FindIndex(TElement element, IEqualityComparer<TElement>? equalityComparer)
    {
        equalityComparer ??= EqualityComparer<TElement>.Default;
        ReadOnlySpan<(TElement Element, TPriority Priority)> nodes = _nodes.AsSpan(0, _size);

        // Currently the JIT doesn't optimize direct EqualityComparer<T>.Default.Equals
        // calls for reference types, so we want to cache the comparer instance instead.
        // TODO https://github.com/dotnet/runtime/issues/10050: Update if this changes in the future.
        if (typeof(TElement).IsValueType && equalityComparer == EqualityComparer<TElement>.Default)
        {
            for (var i = 0; i < nodes.Length; i++)
            {
                if (EqualityComparer<TElement>.Default.Equals(element, nodes[i].Element))
                {
                    return i;
                }
            }
        }
        else
        {
            for (var i = 0; i < nodes.Length; i++)
            {
                if (equalityComparer.Equals(element, nodes[i].Element))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Initializes the custom comparer to be used internally by the heap.
    /// </summary>
    private static IComparer<TPriority>? InitializeComparer(IComparer<TPriority>? comparer)
    {
        if (typeof(TPriority).IsValueType)
        {
            if (comparer == Comparer<TPriority>.Default)
            {
                // if the user manually specifies the default comparer,
                // revert to using the optimized path.
                return null;
            }

            return comparer;
        }
        else
        {
            // Currently the JIT doesn't optimize direct Comparer<T>.Default.Compare
            // calls for reference types, so we want to cache the comparer instance instead.
            // TODO https://github.com/dotnet/runtime/issues/10050: Update if this changes in the future.
            return comparer ?? Comparer<TPriority>.Default;
        }
    }
}
#endif
