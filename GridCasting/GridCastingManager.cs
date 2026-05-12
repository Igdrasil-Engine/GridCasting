using GridCasting.Executor;
using GridCasting.Models.GridGraph;
using GridCasting.Transform;
#if NET8_0_OR_GREATER
using FVector2 = IgdrasilEngine.Engine.Math.Vectors.FVector2;
#else
using FVector2 = UnityEngine.Vector2;
#endif
using Path = GridCasting.Models.Path;

namespace GridCasting;

/// <summary>
/// Represents the main class for performing grid-based operations
/// with casting mechanisms. This class integrates a path executor
/// with a grid graph and allows operations based on positional data
/// provided by the user.
/// </summary>
public class GridCastingManager(
    GridGraph graph, 
    float sensitivity, 
    IPathTransform[] transforms,
    IPathValidator[] validators
) {
    /// <summary>
    /// A private, read-only instance of the <see cref="GridResolver"/> class that is responsible
    /// for resolving grid-based operations such as determining paths, generating grids,
    /// and acquiring positional information. It operates on the provided <see cref="GridGraph"/>
    /// and uses a specified sensitivity value for fine-tuning resolution behavior.
    /// </summary>
    public GridResolver Resolver { get; } = new(graph, sensitivity, validators);

    /// <summary>
    /// Gets an instance of the <see cref="PathExecutor"/> class, responsible for executing
    /// pathfinding and grid-based operations. The executor interacts with the underlying
    /// <see cref="GridGraph"/> and applies transformations using the specified
    /// <see cref="IPathTransform"/> instances to modify or adapt the path resolution process.
    /// </summary>
    public PathExecutor Executor { get; } = new(graph, transforms);

    /// <summary>
    /// Executes an operation by resolving a path from the given positions
    /// and delegating the execution to the associated path executor.
    /// </summary>
    /// <param name="positions">
    /// An array of <see cref="FVector2"/> representing the positions
    /// used to compute the path in the grid-based operations.
    /// </param>
    public void Execute(FVector2[] positions)
    {
        var path = Resolver.GetPath(positions);
        if (path.HasValue) Executor.Execute(path.Value);
    }
}