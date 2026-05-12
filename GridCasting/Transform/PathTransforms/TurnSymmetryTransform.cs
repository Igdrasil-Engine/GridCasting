using GridCasting.Models.GridGraph;
using Path = GridCasting.Models.Path;

namespace GridCasting.Transform.PathTransforms;

public class TurnSymmetryTransform : IPathTransform
{
    public void Initialize(GridGraph graph)
    {
        // Sort edges by angle for rotation consistency
        foreach (var node in graph.Nodes) 
            node.Edges.Sort((a, b) => a.Angle.CompareTo(b.Angle));
    }

    public Path Transform(GridGraph graph, Path path)
    {
        var node = graph[path.StartNode];
        var directions = new int[path.Directions.Length];
        var rotation = 0;
        for (var i = 0; i < directions.Length; i++)
        {
            var direction = path.Directions[i];
            if (i == 0) rotation = direction;
            directions[i] = direction - rotation;
            var edge = node.Edges[direction];
            var nextNode = node == edge.NodeA ? edge.NodeB : edge.NodeA;
            rotation = nextNode.Edges.IndexOf(edge);
            node = nextNode;
        }

        return new Path(path.StartNode, directions);
    }

    public bool IsRequired => true;
}