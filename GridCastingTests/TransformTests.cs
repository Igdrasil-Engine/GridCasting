using GridCasting.Models.GridGraph;
using GridCasting.Transform.PathTransforms;
using Path = GridCasting.Models.Path;

namespace GridCastingTests;


public class TransformTests
{

    private static GridGraph InitializeHexGridGraph()
    {
        // Create a simple hex grid
        var graph = new GridGraph();
        var node = new GridGraphNode();
        for (var i = 0; i < 6; i++)
            node.Edges.Add(new GridGraphEdge(node, node, MathF.PI * i / 3, 1));
        graph.Nodes.Add(node);
        return graph;
    }

    [Test]
    public void TestInversionSymmetry()
    {
        var graph = InitializeHexGridGraph();
        var symmetry = new InversionSymmetryTransform();
        symmetry.Initialize(graph);
        var transformed = symmetry.Transform(graph, new Path(
            0, 1, 1, 2, 3
        ));
        Assert.That(transformed, Is.EqualTo(new Path(
            0, 3, 2, 1, 1
            
        )));
    }

    [Test]
    public void TestMirrorSymmetry()
    {
        var graph = InitializeHexGridGraph();
        var symmetry = new MirrorSymmetryTransform();
        symmetry.Initialize(graph);
        var transformed = symmetry.Transform(graph, new Path(
            0, 1, 1, 2, 3
        ));
        Assert.That(transformed, Is.EqualTo(new Path(
            0, -1, -1, -2, -3
            
        )));
    }

    [Test]
    public void TestTurnSymmetry()
    {
        var graph = InitializeHexGridGraph();
        var symmetry = new TurnSymmetryTransform();
        symmetry.Initialize(graph);
        var transformed = symmetry.Transform(graph, new Path(
            0, 1, 1, 2, 3
        ));
        Assert.That(transformed, Is.EqualTo(new Path(
            0, 0, 0, 1, 1
            
        )));
    }

    [Test]
    public void TestRotationSymmetry()
    {
        var graph = InitializeHexGridGraph();
        var symmetry = new RotationSymmetryTransform(6);
        symmetry.Initialize(graph);
        var transformed = symmetry.Transform(graph, new Path(
            0, 1, 1, 2, 3
        ));
        Assert.That(transformed, Is.EqualTo(new Path(
            0, 0, 0, 2, 2
            
        )));
    }
}