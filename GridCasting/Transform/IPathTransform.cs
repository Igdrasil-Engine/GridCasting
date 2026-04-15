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
    /// Transforms the given path based on the specified grid graph.
    /// </summary>
    /// <param name="graph">The grid graph on which the transformation is based.</param>
    /// <param name="path">The path to be transformed.</param>
    /// <returns>A transformed path as per the grid graph rules.</returns>
    public Path Transform(GridGraph graph, Path path);

    /// <summary>
    /// Reverses the transformation applied to the provided path based on the specified grid graph.
    /// </summary>
    /// <param name="graph">The grid graph used to reverse the transformation.</param>
    /// <param name="path">The path to be reversed.</param>
    /// <returns>A path that has the reverse transformation applied based on the grid graph rules.</returns>
    public Path Reverse(GridGraph graph, Path path);
}