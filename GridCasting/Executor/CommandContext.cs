using Path = GridCasting.Models.Path;

namespace GridCasting.Executor;

/// <summary>
/// Represents the context in which a command is executed.
/// This class encapsulates the command, the execution environment, and a stack for managing execution state.
/// </summary>
public class CommandContext
{
    /// <summary>
    /// Represents the primary command property for execution within a given context.
    /// This property encapsulates a path structure defined by a start node and a series of directions.
    /// </summary>
    public Path Command { get; }

    /// <summary>
    /// Represents the execution environment for the command context.
    /// This property provides a dictionary for storing key-value pairs that define the state, configuration, or resources
    /// necessary for command execution within the current context.
    /// </summary>
    public IDictionary<string, object> Environment { get; }

    /// <summary>
    /// Represents a stack used for managing execution state within the command context.
    /// This property provides a mechanism to maintain a dynamic collection of objects required during command execution.
    /// </summary>
    public Stack<object> Stack { get; }

    /// <summary>
    /// Represents the context in which a command is executed.
    /// </summary>
    /// <remarks>
    /// Encapsulates information required for the execution of a command, including
    /// the command itself, the execution environment, and the associated execution stack.
    /// </remarks>
    internal CommandContext(Path command, IDictionary<string, object> environment, Stack<object> stack)
    {
        Command = command;
        Environment = environment;
        Stack = stack;
    }
}