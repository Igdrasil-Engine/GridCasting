using IgdrasilEngine.Engine.Math.Vectors;

namespace GridCasting.Models.Grid;

public class GridNode(FVector2 position)
{
    /// <summary>
    /// Represents the position of the GridNode in a two-dimensional space.
    /// This position is defined using an instance of the FVector2 struct,
    /// which encapsulates the X and Y coordinates as floating-point values.
    /// </summary>
    public FVector2 Position { get; } = position;

    /// <summary>
    /// Represents a collection of neighboring GridNode instances that are connected to the current GridNode.
    /// The connections define the relationships or pathways between this node and others in the grid structure.
    /// </summary>
    public List<GridNode?> Connections { get; } = [];
}