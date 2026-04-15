using Path = GridCasting.Models.Path;

namespace GridCasting.Executor;

public interface ICommand
{
    void Execute(CommandContext context);
}