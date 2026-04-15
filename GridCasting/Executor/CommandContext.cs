using Path = GridCasting.Models.Path;

namespace GridCasting.Executor;

public class CommandContext
{
    public Path Command { get; }
    public IDictionary<string, object> Environment { get; }
    public Stack<object> Stack { get; }
    
    internal CommandContext(Path command, IDictionary<string, object> environment, Stack<object> stack)
    {
        Command = command;
        Environment = environment;
        Stack = stack;
    }
}