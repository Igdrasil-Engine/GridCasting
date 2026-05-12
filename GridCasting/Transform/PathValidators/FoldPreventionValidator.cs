using GridCasting.Models.GridGraph;
using Path = GridCasting.Models.Path;
#if NET8_0_OR_GREATER
using FVector2 = IgdrasilEngine.Engine.Math.Vectors.FVector2;
#else
using FVector2 = UnityEngine.Vector2;
#endif

namespace GridCasting.Transform.PathValidators;

public class FoldPreventionValidator : IPathValidator
{
    public bool IsValid(GridGraph graph, FVector2 startPosition, Path path)
    {  
        var node = graph[path.StartNode];
        for (var i = 0; i < path.Directions.Length - 1; i++)
        {
            var edge = node.Edges[path.Directions[i]];
            var nextNode = node == edge.NodeA ? edge.NodeB : edge.NodeA;
            if (path.Directions[i + 1] == nextNode.Edges.IndexOf(edge)) return false;
            node = nextNode;
        }
        return true;
    }
}