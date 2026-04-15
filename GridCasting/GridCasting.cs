using GridCasting.Executor;
using GridCasting.Models.GridGraph;
using GridCasting.Transform;
using IgdrasilEngine.Engine.Math.Vectors;

namespace GridCasting;

public class GridCasting(
    PathExecutor executor,
    GridGraph graph,
    float sensitivity,
    params IEnumerable<IPathTransform> transforms)
{
    private readonly GridResolver _resolver = new(graph, sensitivity);
    private readonly PathExecutor _executor = executor;
    private readonly IEnumerable<IPathTransform> _transforms = transforms;

    public void Execute(FVector2[] positions)
    {
        var path = _resolver.GetPath(positions);
        
    }
}