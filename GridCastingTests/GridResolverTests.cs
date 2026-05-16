using GridCasting.Models.GridGraph;
using GridCasting.Transform;
#if NET8_0_OR_GREATER
using FVector2 = IgdrasilEngine.Engine.Math.Vectors.FVector2;
using IVector3 = IgdrasilEngine.Engine.Math.Vectors.IVector3;
using FBox2 = IgdrasilEngine.Engine.Math.Boxes.FBox2;
#else
using UnityEngine;
using FVector2 = UnityEngine.Vector2;
using IVector3 = UnityEngine.Vector3Int;
using FBox2 = UnityEngine.Rect;
#endif

namespace GridCastingTests;

public class GridResolverTests
{
    private GridResolver _resolver;
    private static readonly int[] Expected = [0, 2, 1];

    [SetUp]
    public void Setup()
    {
        // Create a simple hex grid
        var graph = new GridGraph();
        var node = new GridGraphNode();
        for (var i = 0; i < 6; i++)
            node.Edges.Add(new GridGraphEdge(node, node, MathF.PI * i / 3, 1));
        graph.Nodes.Add(node);
        
        _resolver = new GridResolver(
            graph,
            0.1f
        );
    }

    [Test]
    public void TestWorkingPath()
    {
        IVector3[] points = [
            new(7, 2, 5),
            new(7, 2, 6),
            new(7, 3, 6)
        ];
        var positions = points.Select(p =>
        {
#if NET8_0_OR_GREATER
            var result = FVector2.Zero;
#else
            var result = FVector2.zero;
#endif 
            for (var i = 0; i < 3; i++)
            {
                var angle = MathF.PI * i / 3;
                var value = i switch
                {
#if NET8_0_OR_GREATER
                    0 => p.X,
                    1 => p.Y,
                    2 => p.Z,
#else
                    0 => p.x,
                    1 => p.y,
                    2 => p.z,
#endif 
                    _ => throw new ArgumentOutOfRangeException(nameof(i))
                };
                result += new FVector2(
                    value * MathF.Cos(angle),
                    value * MathF.Sin(angle)
                );
            }
            return result;
        }).ToArray();
        var path = _resolver.GetPath(positions);
        Assert.Multiple(() =>
        {
            Assert.That(path.HasValue, Is.True, "Path is null");
            Assert.That(path.Value, Is.EqualTo(Expected), "Path is incorrect");
        });

    }

    [Test]
    public void TestRippedPath()
    {
        IVector3[] points = [
            new(7, 2, 5),
            new(7, 2, 6),
            new(7, 4, 6)
        ];
        var positions = points.Select(p =>
        {
#if NET8_0_OR_GREATER
            var result = FVector2.Zero;
#else
            var result = FVector2.zero;
#endif 
            for (var i = 0; i < 3; i++)
            {
                var angle = MathF.PI * i / 3;
                var value = i switch
                {
#if NET8_0_OR_GREATER
                    0 => p.X,
                    1 => p.Y,
                    2 => p.Z,
#else
                    0 => p.x,
                    1 => p.y,
                    2 => p.z,
#endif 
                    _ => throw new ArgumentOutOfRangeException(nameof(i))
                };
                result += new FVector2(
                    value * MathF.Cos(angle),
                    value * MathF.Sin(angle)
                );
            }
            return result;
        }).ToArray();
        var path = _resolver.GetPath(positions);
        Assert.That(path.HasValue, Is.False, "Path is not null");
    }

    [Test]
    public void TestEmptyPath()
    {
        FVector2[] positions = [];
        var path = _resolver.GetPath(positions);
        Assert.That(path.HasValue, Is.False, "Path is not null");
    }

    [Test]
    public void TestGrid()
    {
#if NET8_0_OR_GREATER
        var positions = _resolver.GetGridPositions(new FBox2(FVector2.One * -10, FVector2.One * 10));
#else
        var positions = _resolver.GetGridPositions(new FBox2(FVector2.one * -10, FVector2.one * 10));
#endif 
        var invSqrt3 = 1f / MathF.Sqrt(3);
        Assert.Multiple(() =>
        {
            foreach (var pos in positions)
            {
#if NET8_0_OR_GREATER
                var x = float.Abs(pos.X - invSqrt3 * pos.Y);
                var y = float.Abs(2 * invSqrt3 * pos.Y);
#else
                var x = Mathf.Abs(pos.x - invSqrt3 * pos.y);
                var y = Mathf.Abs(2 * invSqrt3 * pos.y);
#endif 
                x -= (int)x;
                y -= (int)y;
                if (x > 0.5) x -= 1;
                if (y > 0.5) y -= 1;

                Assert.That(x, Is.EqualTo(0).Within(0.001f), $"Position {pos} is not on the grid");
                Assert.That(y, Is.EqualTo(0).Within(0.001f), $"Position {pos} is not on the grid");
            }
        });
    }
}