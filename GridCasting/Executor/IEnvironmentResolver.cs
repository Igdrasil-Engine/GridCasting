namespace GridCasting.Executor;

/// <summary>
/// Provides an abstraction for managing and resolving environment-specific variables,
/// enabling the dynamic loading, updating, resetting, and unloading of environment configurations.
/// </summary>
public interface IEnvironmentResolver
{
    /// <summary>
    /// Loads and returns a collection of key-value pairs representing environment variables.
    /// </summary>
    /// <returns>
    /// A collection of key-value pairs where the key is a string representing the variable name
    /// and the value is the associated object.
    /// </returns>
    public IEnumerable<KeyValuePair<string, object>> OnLoad();

    /// <summary>
    /// Unloads and performs necessary cleanup operations for the current environment,
    /// ensuring resources are released and any outstanding tasks are handled appropriately.
    /// </summary>
    public void OnUnload();

    /// <summary>
    /// Resets the environment variable identified by the specified key to its default value.
    /// </summary>
    /// <param name="key">The string key of the environment variable to reset.</param>
    /// <returns>
    /// The default value of the environment variable as an object if a reset value is defined;
    /// otherwise, null.
    /// </returns>
    public object? OnReset(string key);

    /// <summary>
    /// Updates the value of the specified environment variable to a new value.
    /// </summary>
    /// <param name="key">The string key representing the name of the environment variable to update.</param>
    /// <param name="value">The new value to assign to the specified environment variable.</param>
    public void OnChange(string key, object value);

    /// <summary>
    /// An event that is triggered whenever an environment variable is updated.
    /// </summary>
    /// <remarks>
    /// The event provides the key of the updated variable and its new value. This is useful for monitoring
    /// changes to environment variables in real-time and ensuring the associated logic reacts accordingly.
    /// </remarks>
    /// <event>
    /// Subscribing to this event allows external components to respond dynamically to updates
    /// in the environment configuration.
    /// </event>
    public event Action<string, object> OnUpdate;
}