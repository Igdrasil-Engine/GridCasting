using GridCasting.Models.GridGraph;
using Path = GridCasting.Models.Path;

namespace GridCasting.Transform.PathTransforms;

public class InversionSymmetryTransform : IPathTransform
{
    public void Initialize(GridGraph graph) { }

    public Path Transform(GridGraph graph, Path path)
    {
        var node = graph[path.StartNode];
        var directions = new int[path.Directions.Length];
        for (var i = 0; i < directions.Length; i++)
        {
            var direction = path.Directions[i];
            var edge = node.Edges[direction];
            var nextNode = node == edge.NodeA ? edge.NodeB : edge.NodeA;
            directions[^(i + 1)] = nextNode.Edges.IndexOf(edge);
            node = nextNode;
        }
        return new Path(graph.Nodes.IndexOf(node), directions);
    }

    public bool IsRequired => false;
}