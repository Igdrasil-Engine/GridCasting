using GridCasting.Executor;
using GridCasting.Models.GridGraph;
using Path = GridCasting.Models.Path;

namespace GridCastingTests;

public class ExecutorTests
{
    private GridGraph _graph;
    private static readonly Path _path = new(0, 1, 1, 2, 3);
    [SetUp]
    public void Setup()
    {
        // Create a simple hex grid
        _graph = new GridGraph();
        var node = new GridGraphNode();
        for (var i = 0; i < 6; i++)
            node.Edges.Add(new GridGraphEdge(node, node, MathF.PI * i / 3, 1));
        _graph.Nodes.Add(node);
    }
    
    [Test]
    public void TestNoCommands()
    {
        var executor = new PathExecutor(_graph, []);
        Assert.That(executor.Execute(_path), Is.False, "Executor should not execute without commands");
    }

    [Test]
    public void TestWrongCommand()
    {
        var executor = new PathExecutor(_graph, []);
        executor.AddCommand(new TestPassedCommand(), _path);

        Assert.Multiple(() =>
        {
            Assert.That(executor.Execute(new Path(0, 1, 2, 1)), Is.False, "Executor should not execute with wrong command");
        });
    }

    [Test]
    public void TestCommandExecution()
    {
        var executor = new PathExecutor(_graph, []);
        executor.AddCommand(new TestPassedCommand(), _path);

        Assert.Multiple(() =>
        {
            Assert.That(executor.Execute(_path), Is.True, "Executor should execute with commands");
        });
    }

    [Test]
    public void TestCommandFamily()
    {
        var executor = new PathExecutor(_graph, []);
        executor.AddCommand(new TestPassedCommand(), new Path(0, 1, 1, 2), true);

        Assert.Multiple(() =>
        {
            Assert.That(executor.Execute(_path), Is.True, "Executor should execute with commands");
        });
    }

    public class TestPassedCommand : ICommand
    {
        public void Execute(CommandContext context)
        {
            Assert.That(context.Command, Is.EqualTo(_path), "Command executed successfully");
        }
    }
}