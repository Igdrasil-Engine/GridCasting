using GridCasting.Utils.BVH;
#if NET8_0_OR_GREATER
using FVector2 = IgdrasilEngine.Engine.Math.Vectors.FVector2;
#else
using FVector2 = UnityEngine.Vector2;
#endif

namespace GridCastingTests;

public class BvhTests
{
    [Test]
    public void PointBvhFindsNearestPointsWithinRadius()
    {
        var tree = new PointBVH2D<TestPoint>();
        var origin = new TestPoint("origin", new FVector2(0f, 0f));
        var near = new TestPoint("near", new FVector2(0.5f, 0f));
        var far = new TestPoint("far", new FVector2(3f, 0f));

        tree.Add(origin);
        tree.Add(near);
        tree.Add(far);

        var result = tree.FindNearest(new FVector2(0f, 0f), 0.75f);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EquivalentTo(new[] { origin, near }));
            Assert.That(tree.FindNearest(new FVector2(2.8f, 0f)), Is.SameAs(far));
            Assert.That(tree.Depth(), Is.GreaterThan(0));
        });
    }

    [Test]
    public void PointBvhSupportsOptimizedAddRemoveAndClear()
    {
        var tree = new PointBVH2D<TestPoint>();
        var first = new TestPoint("first", new FVector2(0f, 0f));
        var second = new TestPoint("second", new FVector2(2f, 0f));

        tree.OptimizedAdd(first);
        tree.OptimizedAdd(second);
        tree.Remove(first);

        Assert.Multiple(() =>
        {
            Assert.That(tree.FindNearest(new FVector2(0f, 0f), 0.25f), Is.Empty);
            Assert.That(tree.FindNearest(new FVector2(2f, 0f), 0.25f), Is.EquivalentTo(new[] { second }));
        });

        tree.Clear();

        Assert.That(tree.FindNearest(new FVector2(2f, 0f), 0.25f), Is.Empty);
    }

    [Test]
    public void PointTransformFindNearestUsesContainingTreeLeaf()
    {
        var tree = new PointBVH2D<TestPoint>();
        var first = new TestPoint("first", new FVector2(0f, 0f));
        var second = new TestPoint("second", new FVector2(0.25f, 0f));

        tree.Add(first);
        tree.Add(second);

        Assert.That(first.FindNearest(0.5f), Is.EquivalentTo(new[] { second }));
    }

    private sealed class TestPoint(string name, FVector2 position) : PointBVH2DTransform<TestPoint>
    {
        public string Name { get; } = name;
        public override FVector2 TreePosition { get; protected set; } = position;

        public override string ToString() => Name;
    }
}
