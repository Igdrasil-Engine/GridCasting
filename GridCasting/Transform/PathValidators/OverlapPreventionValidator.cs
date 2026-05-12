using GridCasting.Models.GridGraph;
using Path = GridCasting.Models.Path;
#if NET8_0_OR_GREATER
using FVector2 = IgdrasilEngine.Engine.Math.Vectors.FVector2;
#else
using FVector2 = UnityEngine.Vector2;
#endif

#if NET8_0_OR_GREATER
using FVector2Hashable = IgdrasilEngine.Engine.Math.Vectors.FVector2;
#else
using FVector2Hashable = GridCasting.Transform.FVector2Hashable;
#endif

namespace GridCasting.Transform.PathValidators;

public class OverlapPreventionValidator : IPathValidator
{
    private HashSet<FVector2Hashable> _occupied = [];
    
    private FVector2Hashable[] _lastPath;
    public bool IsValid(GridGraph graph, FVector2 startPosition, Path path)
    {
        var node = graph[path.StartNode];
        _lastPath = new FVector2Hashable[path.Directions.Length + 1];
        _lastPath[0] = new FVector2Hashable(startPosition);
        var sequentialCollisions = 0;
        for (var i = 0; i < path.Directions.Length; i++)
        {
            if (_occupied.Contains(_lastPath[i]))
            {
                sequentialCollisions++;
                if (sequentialCollisions > 1) return false;
            }
            else sequentialCollisions = 0;
            var direction = path.Directions[i];
            var edge = node.Edges[direction];
            var nextNode = edge.NodeA == node ? edge.NodeB : edge.NodeA;
            var length = edge.NodeA == node ? edge.Length : -edge.Length;
            _lastPath[i + 1] = new FVector2Hashable(
#if NET8_0_OR_GREATER
                _lastPath[i]
#else
                _lastPath[i].BaseVector 
#endif
                + new FVector2(
                length * MathF.Cos(edge.Angle),
                length * MathF.Sin(edge.Angle)
            ));
            node = nextNode;
        }
        return !_occupied.Contains(_lastPath[^1]);
    }

    public void Clear() => _occupied.Clear();
    public void PushPath()
    {
        foreach (var hashable in _lastPath) 
            _occupied.Add(hashable);
    }
}