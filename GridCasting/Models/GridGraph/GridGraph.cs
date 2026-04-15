
using System.Collections;

namespace GridCasting.Models.GridGraph;

/// <summary>
/// Represents a graph structure designed for grid-based systems. The GridGraph class serves as a foundational component for
/// creating, managing, and transforming grids, paths, and related operations in systems that require spatial organization.
/// </summary>
public class GridGraph : IEnumerable<GridGraphNode>
{
    /// <summary>
    /// Represents the collection of grid nodes within the grid graph.
    /// Each node symbolizes a distinct element or point in the graph.
    /// This list enables navigation and structural definition of the graph.
    /// </summary>
    public List<GridGraphNode> Nodes { get; } = [];

    /// <summary>
    /// Provides an indexer to access a specific grid graph node from the collection
    /// by its index. The indexer allows direct retrieval of a node based on its
    /// position in the node list.
    /// </summary>
    /// <param name="index">The zero-based index of the node to retrieve.</param>
    /// <returns>The grid graph node at the specified index in the collection.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the specified index is outside the bounds of the node collection.
    /// </exception>
    public GridGraphNode this[int index] => Nodes[index];

    /// <summary>
    /// Returns an enumerator that iterates through the collection of nodes
    /// in the grid graph.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the nodes of the grid graph.
    /// </returns>
    public IEnumerator<GridGraphNode> GetEnumerator() => Nodes.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the collection of nodes
    /// in the grid graph.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the nodes of the grid graph.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}