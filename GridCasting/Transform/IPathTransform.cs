using GridCasting.Models.Grid;
using GridCasting.Models.GridGraph;
using Path = GridCasting.Models.Path;

namespace GridCasting.Transform;

/// <summary>
/// Defines methods to transform and reverse-transform paths based on a grid graph.
/// </summary>
public interface IPathTransform
{
    /// <summary>
    /// Gets a value indicating whether this transformation is required.
    /// </summary>
    /// <value>
    /// A boolean indicating the necessity of the transformation.
    /// If true, the transformation must be applied; if false, the transformation is optional.
    /// </value>
    public bool IsRequired { get; }

    public void Initialize(GridGraph graph);
    
    /// <summary>
    /// Transforms the given path based on the specified grid graph.
    /// </summary>
    /// <param name="graph">The grid graph on which the transformation is based.</param>
    /// <param name="path">The path to be transformed.</param>
    /// <returns>A transformed path as per the grid graph rules.</returns>
    public Path Transform(GridGraph graph, Path path);
}