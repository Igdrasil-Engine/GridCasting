namespace GridCasting.Executor;

/// <summary>
/// Represents an executable command that can be invoked with a specific execution context.
/// This interface defines a contract for implementing custom commands in the application.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Executes a command within the provided execution context.
    /// This method defines the operational logic for the command, utilizing the given context for execution.
    /// </summary>
    /// <param name="context">The execution context that contains the specific environment, stack, and command information required for executing this command.</param>
    void Execute(CommandContext context);
}