namespace GridCasting.Models.GridGraph;

/// <summary>
/// Represents a node in a grid system. Each node may be connected to other nodes
/// in the grid via grid edges, which define relationships and structural topology.
/// </summary>
public class GridGraphNode
{
    /// <summary>
    /// A collection of edges connected to the grid node, representing its relationships
    /// with adjacent nodes. Each edge contains information about the connected nodes
    /// and additional metadata such as length and angle.
    /// </summary>
    public List<GridGraphEdge> Edges { get; } = [];
}