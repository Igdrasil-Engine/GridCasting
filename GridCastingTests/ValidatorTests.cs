using GridCasting.Models.GridGraph;
using GridCasting.Transform.PathValidators;
using Path = GridCasting.Models.Path;
#if NET8_0_OR_GREATER
using FVector2 = IgdrasilEngine.Engine.Math.Vectors.FVector2;
#else
using FVector2 = UnityEngine.Vector2;
#endif

namespace GridCastingTests;

public class ValidatorTests
{
    [Test]
    public void FoldPreventionValidatorRejectsImmediateBacktracking()
    {
        var graph = CreateLineGraph();
        var validator = new FoldPreventionValidator();

        var isValid = validator.IsValid(graph, new FVector2(0f, 0f), new Path(0, 0, 0));

        Assert.That(isValid, Is.False);
    }

    [Test]
    public void FoldPreventionValidatorAllowsForwardPath()
    {
        var graph = CreateLineGraph();
        var validator = new FoldPreventionValidator();

        var isValid = validator.IsValid(graph, new FVector2(0f, 0f), new Path(0, 0, 1));

        Assert.That(isValid, Is.True);
    }

    [Test]
    public void OverlapPreventionValidatorRejectsPathEndingOnOccupiedPoint()
    {
        var graph = CreateHexLoopGraph();
        var validator = new OverlapPreventionValidator();
        var firstPath = new Path(0, 0, 2);
        var overlappingPath = new Path(0, 0, 2);

        Assert.That(validator.IsValid(graph, new FVector2(0f, 0f), firstPath), Is.True);
        validator.PushPath();

        Assert.That(validator.IsValid(graph, new FVector2(0f, 0f), overlappingPath), Is.False);
    }

    [Test]
    public void OverlapPreventionValidatorAllowsPathAfterClear()
    {
        var graph = CreateHexLoopGraph();
        var validator = new OverlapPreventionValidator();
        var path = new Path(0, 0, 2);

        Assert.That(validator.IsValid(graph, new FVector2(0f, 0f), path), Is.True);
        validator.PushPath();
        validator.Clear();

        Assert.That(validator.IsValid(graph, new FVector2(0f, 0f), path), Is.True);
    }

    private static GridGraph CreateLineGraph()
    {
        var graph = new GridGraph();
        var first = new GridGraphNode();
        var second = new GridGraphNode();
        var third = new GridGraphNode();
        var firstSecond = new GridGraphEdge(first, second, 0f, 1f);
        var secondThird = new GridGraphEdge(second, third, 0f, 1f);

        first.Edges.Add(firstSecond);
        second.Edges.Add(firstSecond);
        second.Edges.Add(secondThird);
        third.Edges.Add(secondThird);
        graph.Nodes.AddRange([first, second, third]);
        return graph;
    }

    private static GridGraph CreateHexLoopGraph()
    {
        var graph = new GridGraph();
        var node = new GridGraphNode();
        for (var i = 0; i < 6; i++)
            node.Edges.Add(new GridGraphEdge(node, node, MathF.PI * i / 3, 1));
        graph.Nodes.Add(node);
        return graph;
    }
}
