namespace GridCasting.Models.GridGraph;

/// <summary>
/// Represents an edge in a grid system, connecting two nodes and including metadata about its geometry.
/// </summary>
public class GridGraphEdge(GridGraphNode nodeA, GridGraphNode nodeB, float angle, float length)
{
    /// <summary>
    /// Represents the starting node of the grid edge.
    /// </summary>
    public GridGraphNode NodeA { get; } = nodeA;

    /// <summary>
    /// Represents the ending node of the grid edge.
    /// </summary>
    public GridGraphNode NodeB { get; } = nodeB;

    /// <summary>
    /// Represents the angle of the grid edge in degrees.
    /// </summary>
    public float Angle { get; } = angle;

    /// <summary>
    /// Represents the length of the grid edge.
    /// </summary>
    public float Length { get; } = length;
}