using GridCasting.Utils;
using Path = GridCasting.Models.Path;

namespace GridCasting.Executor;

/// <summary>
/// Represents a class that handles the execution of commands associated with specific path patterns.
/// Manages environment variables through a listenable dictionary and integrates capabilities for
/// reacting to changes within these variables.
/// </summary>
/// <remarks>
/// The <see cref="PathExecutor"/> class is designed to facilitate the execution of path-related commands
/// while allowing dynamic management of environment-specific variables. It supports the registration of
/// environment resolvers, maintains a collection of commands mapped to path patterns, and provides methods
/// for interacting with and executing commands.
/// </remarks>
public class PathExecutor
{
    /// <summary>
    /// Stores the internal mapping of commands to their corresponding path patterns.
    /// This data structure associates commands, which implement the <see cref="ICommand"/> interface,
    /// with specific path patterns and optionally supports pattern families for extended matching capabilities.
    /// </summary>
    /// <remarks>
    /// The field leverages a <see cref="Trie{TNode, TValue}"/> to efficiently manage
    /// hierarchical patterns and improve lookup performance during command execution.
    /// This is an internal implementation detail and is used to match and retrieve
    /// relevant commands based on the incoming path during execution.
    /// </remarks>
    private readonly Trie<int, ICommand> _commands = new();

    /// <summary>
    /// Maintains a collection of key-value pairs representing environment-specific variables and their associated values.
    /// This dictionary acts as the primary storage for environment-related data used during path execution and command processing.
    /// </summary>
    /// <remarks>
    /// The dictionary is updated and accessed through various methods in the <see cref="PathExecutor"/> class,
    /// often in conjunction with environment resolvers that implement the <see cref="IEnvironmentResolver"/> interface.
    /// Additionally, changes to this dictionary are synchronized with the listenable wrapper <see cref="ListenableDictionary{TKey, TValue}"/>
    /// to trigger appropriate events such as updates, removals, and clear operations.
    /// </remarks>
    private readonly Dictionary<string, object> _environmentVariables = new();

    /// <summary>
    /// Represents a listenable dictionary that wraps environment variables
    /// with the ability to react to updates, removals, and clearing of its contents.
    /// </summary>
    /// <remarks>
    /// This field is a <see cref="ListenableDictionary{TKey, TValue}"/> object
    /// that allows event-driven responses when changes occur in the underlying
    /// environment variables. It builds upon a base dictionary to manage key-value pairs,
    /// while invoking relevant event handlers for observed modifications.
    /// </remarks>
    private readonly ListenableDictionary<string, object> _environmentVariablesListenable;

    /// <summary>
    /// Maintains a mapping between environment variable keys and their corresponding environment resolvers.
    /// This dictionary enables association of specific environment variable keys with implementations
    /// of the <see cref="IEnvironmentResolver"/> interface to facilitate dynamic updates, load, and unload operations.
    /// </summary>
    /// <remarks>
    /// This mapping is used to handle the assignment and resolution of environment variables within the
    /// execution context. Each environment variable key is associated with an instance of an
    /// <see cref="IEnvironmentResolver"/>, which provides the logic for loading, updating, and
    /// unloading environment variables. Changes to the variable keys trigger relevant resolver events for
    /// synchronized state management.
    /// </remarks>
    private readonly Dictionary<string, IEnvironmentResolver> _environmentResolverMap = new();

    /// <summary>
    /// Represents the execution stack used internally by the <see cref="PathExecutor"/>.
    /// This stack manages objects relevant to the execution context of commands,
    /// facilitating state tracking and enabling command chaining during execution flows.
    /// </summary>
    /// <remarks>
    /// The stack operates as a first-in-last-out (FILO) data structure, where elements
    /// added during command execution are processed in reverse order of addition.
    /// It is primarily used to maintain the execution context and intermediate
    /// results during command processing within the PathExecutor.
    /// </remarks>
    private readonly Stack<object> _stack = new();

    /// <summary>
    /// Handles the execution of commands associated with specific path patterns and manages environment variables
    /// with the ability to listen and react to changes.
    /// </summary>
    /// <remarks>
    /// This class integrates a listenable dictionary for environment variables, enabling event-driven responses to updates,
    /// removals, or full-clears of its contents. It acts as a mediator for resolving context and executing commands
    /// based on the given path patterns.
    /// </remarks>
    public PathExecutor()
    {
        _environmentVariablesListenable = new ListenableDictionary<string, object>(_environmentVariables);
        _environmentVariablesListenable.OnUpdate += OnUpdate;
        _environmentVariablesListenable.OnRemove += OnRemove;
        _environmentVariablesListenable.OnClear += OnClear;
    }


    /// <summary>
    /// Gets the collection of context resolvers added to the PathExecutor.
    /// These context resolvers are responsible for handling specific execution contexts
    /// during the execution of commands.
    /// </summary>
    /// <remarks>
    /// The property returns a read-only list of <see cref="IEnvironmentResolver"/> instances
    /// that have been registered using the <see cref="AddEnvironmentResolver"/>.
    /// </remarks>
    public IEnumerable<IEnvironmentResolver> Resolvers => _environmentResolverMap.Values;

    /// <summary>
    /// Adds an environment resolver and integrates its associated variables and update events into the PathExecutor.
    /// </summary>
    /// <param name="resolver">
    /// The environment resolver to be added. This resolver provides a collection of environment variables
    /// on load and triggers update events when its state changes.
    /// </param>
    public void AddEnvironmentResolver(IEnvironmentResolver resolver)
    {
        foreach (var (key, value) in resolver.OnLoad())
        {
            _environmentVariables.Add(key, value);
            _environmentResolverMap.Add(key, resolver);
        }
        resolver.OnUpdate += OnResolverUpdate;
    }

    /// <summary>
    /// Removes an environment resolver and clears all associated environment variables
    /// from the resolver's mapped context.
    /// </summary>
    /// <param name="resolver">The environment resolver to be removed. It will be unregistered
    /// from its update events, and its context will be cleaned up.</param>
    public void RemoveContextResolver(IEnvironmentResolver resolver)
    {
        // Unmap all environment variables
        resolver.OnUpdate -= OnResolverUpdate;
        _environmentResolverMap.Where(x => x.Value == resolver).ToList()
            .ForEach(x =>
            {
                _environmentResolverMap.Remove(x.Key);
                _environmentVariables.Remove(x.Key);
            });
        resolver.OnUnload();
    }

    /// <summary>
    /// Adds a command to be executed with an associated path pattern and optional pattern family configuration.
    /// </summary>
    /// <param name="command">The command to add. Must implement the <see cref="ICommand"/> interface.</param>
    /// <param name="pattern">The path object used to define the execution pattern for the command.</param>
    /// <param name="patternFamily">A boolean flag indicating whether the path pattern should be treated as a family of patterns. Defaults to false.</param>
    public void AddCommand(ICommand command, Path pattern, bool patternFamily = false) => 
        _commands.Set(pattern.ToArray(), patternFamily, command);


    /// <summary>
    /// Clears all objects from the execution stack of the PathExecutor.
    /// This operation resets the stack to its initial state, removing any previously added elements.
    /// </summary>
    public void ResetStack() => _stack.Clear();

    /// <summary>
    /// Executes the command associated with the provided path.
    /// If a matching command is found, it is executed within the provided execution context.
    /// </summary>
    /// <param name="path">The path object defining the start node and directions for the execution.</param>
    /// <returns>
    /// Returns <c>true</c> if a matching command for the specified path is found and executed successfully;
    /// otherwise, returns <c>false</c>.
    /// </returns>
    public bool Execute(Path path)
    {
        if (!_commands.TryGetValue(path.ToArray(), out var command)) return false;
        command.Execute(new CommandContext(
            path,
            _environmentVariablesListenable,
            _stack
        ));
        return true;
    }

    /// <summary>
    /// Handles updates to environment variables by notifying the relevant environment resolver about the change.
    /// </summary>
    /// <param name="key">The key of the environment variable that has been updated.</param>
    /// <param name="value">The new value associated with the updated key.</param>
    private void OnUpdate(string key, object value)
    {
        if (!_environmentResolverMap.TryGetValue(key, out var resolver)) return;
        resolver.OnChange(key, value);
    }

    /// <summary>
    /// Handles the removal of a specific environment variable from the listenable dictionary and restores it to its default value
    /// if defined by the associated resolver.
    /// </summary>
    /// <param name="key">The key of the environment variable to be removed and potentially reset.</param>
    private void OnRemove(string key)
    {
        if (!_environmentResolverMap.TryGetValue(key, out var resolver)) return;
        var defaultValue = resolver.OnReset(key);
        if (defaultValue != null) _environmentVariables[key] = defaultValue;
    }

    /// <summary>
    /// Resets environment variables by invoking the reset logic for each associated environment resolver
    /// and re-populating the variables with their default values if provided.
    /// </summary>
    /// <remarks>
    /// This method is triggered when the `OnClear` event is raised from the `ListenableDictionary`.
    /// It iterates through the configured environment resolvers, calls their reset method for each key,
    /// and updates the environment variables with reset defaults if applicable.
    /// </remarks>
    private void OnClear()
    {
        foreach (var (key, resolver) in _environmentResolverMap)
        {
            var defaultValue = resolver.OnReset(key);
            if (defaultValue != null) _environmentVariables[key] = defaultValue;
        }
    }

    /// <summary>
    /// Updates the value of an environment variable in the internal storage when a corresponding change is triggered
    /// by an environment resolver.
    /// </summary>
    /// <param name="key">The key of the environment variable being updated.</param>
    /// <param name="value">The new value for the specified environment variable.</param>
    private void OnResolverUpdate(string key, object value) => _environmentVariables[key] = value;
}