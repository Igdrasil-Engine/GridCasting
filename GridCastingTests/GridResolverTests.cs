using GridCasting.Models.GridGraph;
using GridCasting.Transform;
using IgdrasilEngine.Engine.Math.Boxes;
using IgdrasilEngine.Engine.Math.Vectors;

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
            var result = FVector2.Zero;
            for (var i = 0; i < 3; i++)
            {
                var angle = MathF.PI * i / 3;
                var value = i switch
                {
                    0 => p.X,
                    1 => p.Y,
                    2 => p.Z,
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
            var result = FVector2.Zero;
            for (var i = 0; i < 3; i++)
            {
                var angle = MathF.PI * i / 3;
                var value = i switch
                {
                    0 => p.X,
                    1 => p.Y,
                    2 => p.Z,
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
        var positions = _resolver.GetGridPositions(new FBox2(FVector2.One * -5, FVector2.One * 5));
        var invSqrt3 = 1f / MathF.Sqrt(3);
        Assert.Multiple(() =>
        {
            foreach (var pos in positions)
            {
                var x = float.Abs(pos.X - invSqrt3 * pos.Y);
                var y = float.Abs(2 * invSqrt3 * pos.Y);
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