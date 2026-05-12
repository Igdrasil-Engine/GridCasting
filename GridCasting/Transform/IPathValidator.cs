using GridCasting.Models.GridGraph;
using Path = GridCasting.Models.Path;
#if NET8_0_OR_GREATER
using FVector2 = IgdrasilEngine.Engine.Math.Vectors.FVector2;
#else
using FVector2 = UnityEngine.Vector2;
#endif

namespace GridCasting.Transform;

public interface IPathValidator
{
    public bool IsValid(GridGraph graph, FVector2 startPosition, Path path);
}