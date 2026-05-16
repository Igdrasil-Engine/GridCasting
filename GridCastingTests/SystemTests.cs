using GridCasting;
using GridCasting.Executor;
using GridCasting.Models.GridGraph;
using GridCasting.Transform;
using GridCasting.Transform.PathTransforms;
using Path = GridCasting.Models.Path;
#if NET8_0_OR_GREATER
using FVector2 = IgdrasilEngine.Engine.Math.Vectors.FVector2;
#else
using FVector2 = UnityEngine.Vector2;
#endif

namespace GridCastingTests;

public class SystemTests
{
    private static readonly Path ExpectedPath = new(0, 0, 2, 1);

    [Test]
    public void ExecuteThroughManagerInvokesCommandAndUpdatesEnvironment()
    {
        var manager = new GridCastingManager(CreateHexLoopGraph(), 0.1f, [], []);
        var resolver = new TestEnvironmentResolver();
        var command = new RecordingCommand(context =>
        {
            context.Environment["spellReady"] = true;
            context.Stack.Push("prepared");
        });
        Path? successfulPath = null;
        Path? failedPath = null;

        manager.Executor.AddEnvironmentResolver(resolver);
        manager.Executor.AddCommand(command, ExpectedPath);
        manager.Executor.OnCommandSuccess += path => successfulPath = path;
        manager.Executor.OnCommandFailed += path => failedPath = path;

        manager.Execute(CreateExpectedPositions());

        Assert.Multiple(() =>
        {
            Assert.That(command.ExecutionCount, Is.EqualTo(1));
            Assert.That(command.LastCommand, Is.EqualTo(ExpectedPath));
            Assert.That(successfulPath, Is.EqualTo(ExpectedPath));
            Assert.That(failedPath, Is.Null);
            Assert.That(resolver.State["spellReady"], Is.True);
            Assert.That(command.LastStackCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ExecuteThroughManagerDoesNotInvokeExecutorWhenResolverRejectsPath()
    {
        var manager = new GridCastingManager(CreateHexLoopGraph(), 0.1f, [], []);
        var command = new RecordingCommand();
        var failedEvents = 0;

        manager.Executor.AddCommand(command, ExpectedPath);
        manager.Executor.OnCommandFailed += _ => failedEvents++;

        manager.Execute([
            new FVector2(0f, 0f),
            new FVector2(1f, 0f),
            new FVector2(3f, 0f)
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(command.ExecutionCount, Is.Zero);
            Assert.That(failedEvents, Is.Zero);
        });
    }

    [Test]
    public void ExecuteThroughManagerRespectsPathValidators()
    {
        var validator = new RejectingValidator();
        var manager = new GridCastingManager(CreateHexLoopGraph(), 0.1f, [], [validator]);
        var command = new RecordingCommand();

        manager.Executor.AddCommand(command, ExpectedPath);
        manager.Execute(CreateExpectedPositions());

        Assert.Multiple(() =>
        {
            Assert.That(validator.CallCount, Is.EqualTo(1));
            Assert.That(command.ExecutionCount, Is.Zero);
        });
    }

    [Test]
    public void ExecutorRaisesFailureEventForValidUnregisteredPath()
    {
        var executor = new PathExecutor(CreateHexLoopGraph());
        Path? failedPath = null;

        executor.OnCommandFailed += path => failedPath = path;

        var executed = executor.Execute(ExpectedPath);

        Assert.Multiple(() =>
        {
            Assert.That(executed, Is.False);
            Assert.That(failedPath, Is.EqualTo(ExpectedPath));
        });
    }

    [Test]
    public void OptionalTransformAllowsAlternativePatternWithoutRegisteringOriginalPath()
    {
        var executor = new PathExecutor(CreateHexLoopGraph(), new MirrorSymmetryTransform());
        var command = new RecordingCommand();
        var mirroredPath = new Path(0, 0, -2, -1);

        executor.AddCommand(command, mirroredPath);
        var executed = executor.Execute(ExpectedPath);

        Assert.Multiple(() =>
        {
            Assert.That(executed, Is.True);
            Assert.That(command.ExecutionCount, Is.EqualTo(1));
            Assert.That(command.LastCommand, Is.EqualTo(ExpectedPath));
        });
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

    private static FVector2[] CreateExpectedPositions() =>
    [
        new FVector2(0f, 0f),
        new FVector2(1f, 0f),
        new FVector2(0.5f, 0.8660254f),
        new FVector2(1f, 1.7320508f)
    ];

    private sealed class RecordingCommand(Action<CommandContext>? action = null) : ICommand
    {
        public int ExecutionCount { get; private set; }
        public Path? LastCommand { get; private set; }
        public int LastStackCount { get; private set; }

        public void Execute(CommandContext context)
        {
            ExecutionCount++;
            LastCommand = context.Command;
            action?.Invoke(context);
            LastStackCount = context.Stack.Count;
        }
    }

    private sealed class TestEnvironmentResolver : IEnvironmentResolver
    {
        public Dictionary<string, object> State { get; } = new()
        {
            ["spellReady"] = false
        };

        public IEnumerable<KeyValuePair<string, object>> OnLoad() => State;

        public void OnUnload()
        {
        }

        public object? OnReset(string key) => key == "spellReady" ? false : null;

        public void OnChange(string key, object value) => State[key] = value;

        public event Action<string, object> OnUpdate = delegate { };
    }

    private sealed class RejectingValidator : IPathValidator
    {
        public int CallCount { get; private set; }

        public bool IsValid(GridGraph graph, FVector2 startPosition, Path path)
        {
            CallCount++;
            return false;
        }
    }
}
