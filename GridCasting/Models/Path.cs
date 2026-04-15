using System.Collections;

namespace GridCasting.Models;

/// <summary>
/// Represents a path structure consisting of a start node and a series of directions.
/// The path is iterable, allowing iteration over the start node followed by the direction sequence.
/// </summary>
public struct Path(int startNode, int[] directions) : IEnumerable<int>
{
    /// <summary>
    /// Gets or sets the starting node of the path. This represents the initial point
    /// in the sequence before any directions are taken.
    /// </summary>
    public int StartNode { get; } = startNode;

    /// <summary>
    /// Gets or sets the sequence of directions within the path. Each direction
    /// represents a step or movement from the initial starting node.
    /// </summary>
    public int[] Directions { get; } = directions;

    /// <summary>
    /// Returns an enumerator that iterates through the path, starting with the StartNode followed by the sequence of Directions.
    /// </summary>
    /// <returns>An enumerator that iterates through the elements of the path.</returns>
    public IEnumerator<int> GetEnumerator()
    {
        yield return StartNode;
        foreach (var direction in Directions)
            yield return direction;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the path, starting with the StartNode followed by the sequence of Directions.
    /// </summary>
    /// <returns>An enumerator that iterates through the elements of the path.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}