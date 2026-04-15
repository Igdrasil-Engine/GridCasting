using System.Collections;

namespace GridCasting.Models.Grid;

/// <summary>
/// Represents a grid structure that serves as a core abstraction for spatially organizing and managing elements
/// in a grid-based layout or system. Provides foundational functionality to integrate with and support operations
/// like grid graph generation, position resolution, and pathfinding using other related classes.
/// </summary>
public class Grid : IEnumerable<GridNode>
{
    /// <summary>
    /// Represents a collection of <see cref="GridNode"/> instances within a grid structure.
    /// The <c>Nodes</c> variable holds the individual grid elements that collectively
    /// form the grid, allowing for operations such as traversal, manipulation, or querying
    /// of the underlying grid layout.
    /// </summary>
    public List<GridNode> Nodes { get; } = [];

    /// <summary>
    /// Provides indexed access to the grid nodes within the grid structure
    /// by returning the element at the specified position in the node collection.
    /// </summary>
    /// <param name="index">The zero-based index of the grid node in the collection.</param>
    /// <returns>Returns the <see cref="GridNode"/> instance located at the specified index.</returns>
    public GridNode this[int index] => Nodes[index];

    /// <summary>
    /// Returns an enumerator that iterates through the collection of grid nodes
    /// within the current grid structure.
    /// </summary>
    /// <returns>An enumerator for iterating through the <see cref="GridNode"/> elements in the grid.</returns>
    public IEnumerator<GridNode> GetEnumerator() => Nodes.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the collection of grid nodes
    /// within the current grid structure.
    /// </summary>
    /// <returns>An enumerator for iterating through the <see cref="GridNode"/> elements in the grid.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}