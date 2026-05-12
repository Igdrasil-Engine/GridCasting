using GridCasting.Models.GridGraph;
using Path = GridCasting.Models.Path;

namespace GridCasting.Transform.PathTransforms;

public class MirrorSymmetryTransform : IPathTransform
{
    public void Initialize(GridGraph graph) {}

    public Path Transform(GridGraph graph, Path path)
    {
        return new Path(
            path.StartNode, 
            path.Directions.Select(d => -d).ToArray()
        );
    }

    public bool IsRequired => false;
}