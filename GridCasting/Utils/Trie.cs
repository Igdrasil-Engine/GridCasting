using System.Diagnostics.CodeAnalysis;

namespace GridCasting.Utils;

/// <summary>
/// Represents a generic prefix tree (Trie) data structure, which maps keys
/// composed of sequences of <typeparamref name="TKey"/> into values of <typeparamref name="TValue"/>.
/// This data structure allows hierarchical storage and efficient retrieval of values associated
/// with a sequence (path) of keys.
/// </summary>
/// <typeparam name="TKey">
/// Specifies the type of the key elements that make up the paths. Must be a non-nullable type.
/// </typeparam>
/// <typeparam name="TValue">
/// Specifies the type of the values that are stored in the trie.
/// </typeparam>
/// <remarks>
/// - The trie supports key sequences of arbitrary length.
/// - Child nodes are stored internally in a dictionary for efficient access.
/// - Enables operations such as setting, retrieving, and removing values at specific paths.
/// - Implements lazy creation for child nodes during insertion.
/// - Allows for weak leaf and overwrite behavior during the modification of its structure.
/// </remarks>
public class Trie<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// Represents the stored value of the current node in the trie. This value
    /// is assigned when a key-path is associated with a specific value in the trie.
    /// It can be null if no value has been set for this node.
    /// </summary>
    private TValue? _value;

    /// <summary>
    /// Indicates whether the current node in the trie serves as a "weak leaf," which means
    /// it is treated as a terminus for key-path lookup but may represent a non-terminal node in the trie structure.
    /// When this flag is set to true, it allows the trie to consider this node as a valid endpoint for key-path retrieval, even if it has child nodes.
    /// This is useful for scenarios where certain paths should be considered complete and retrievable, regardless of whether they have further branches in the trie.
    /// </summary>
    private bool _isWeakLeaf;

    /// <summary>
    /// Represents the collection of child nodes for the current node in the trie.
    /// Each child node is associated with a key and serves as the next level in the trie structure.
    /// This dictionary enables the hierarchical representation of key-paths.
    /// </summary>
    private readonly Dictionary<TKey, Trie<TKey, TValue>> _children = new();

    /// <summary>
    /// Provides access to the value associated with a specific key path in the trie.
    /// If the key path exists, the corresponding value is returned; otherwise, a
    /// KeyNotFoundException is thrown. The indexer facilitates value retrieval
    /// using a sequence of keys, aligning with trie-based key-path structures.
    /// </summary>
    /// <param name="path">An array of keys representing the path to a value in the trie.</param>
    /// <returns>The value associated with the specified key path.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the specified key path does not exist in the trie.
    /// </exception>
    public TValue this[TKey[] path] => TryGetValue(path, out var value)
        ? value
        : throw new KeyNotFoundException($"The given path was not present in the trie: [{string.Join(", ", path)}]");

    /// <summary>
    /// Sets a value in the trie at the specified key path. If the key path does not exist, it will be created.
    /// </summary>
    /// <param name="path">The key path where the value should be set. Each element in the array represents a level in the trie.</param>
    /// <param name="weakLeaf">Indicates whether the node at the specified key path should be marked as a weak leaf.</param>
    /// <param name="value">The value to set at the specified key path.</param>
    /// <param name="rewrite">Indicates whether the value should be overwritten if a value already exists at the specified key path. Defaults to true.</param>
    /// <returns>Returns true if the value was successfully set, otherwise false.</returns>
    public bool Set(TKey[] path, bool weakLeaf, TValue value, bool rewrite = true)
    {
        if (path.Length == 0)
        {
            if (rewrite || _value == null)
            {
                _value = value;
                _isWeakLeaf = weakLeaf;
                return true;
            }
            return false;
        }
        if (!_children.TryGetValue(path[0], out var child))
            _children[path[0]] = child = new Trie<TKey, TValue>();
        return child.Set(path[1..], weakLeaf, value, rewrite);
    }

    /// <summary>
    /// Removes the value associated with the specified key path in the trie.
    /// If the specified path exists and has a corresponding value, the value will be removed.
    /// Intermediate nodes may also be adjusted if they become empty after the removal.
    /// </summary>
    /// <param name="path">The key path where the value should be removed. Each element in the array represents a level in the trie.</param>
    /// <param name="weakLeafDrop">Indicates whether to drop all children of a weak leaf node when it is removed. Defaults to true.</param>
    /// <returns>Returns true if the value was successfully removed, otherwise false.</returns>
    public bool Remove(TKey[] path, bool weakLeafDrop = true)
    {
        if (path.Length == 0 || _isWeakLeaf)
        {
            if (_value == null) return false;
            _value = default;
            // If this node is a weak leaf, we can drop all children as well, since they are not reachable anymore.
            if (_isWeakLeaf && weakLeafDrop)_children.Clear();
            _isWeakLeaf = false;
            return true;
        }
        if (!_children.TryGetValue(path[0], out var child)) return false;
        var childRemoved = child.Remove(path[1..]);
        // Fold branch if empty
        if (child._children.Count == 0)
            _children.Remove(path[0]);
        return childRemoved;
    }

    /// <summary>
    /// Attempts to retrieve a value from the trie at the specified key path.
    /// </summary>
    /// <param name="path">The key path to search within the trie. Each element represents a level in the trie.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key path, if the key path is found; otherwise, contains the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
    /// <returns>Returns true if a value is found at the specified key path; otherwise, false.</returns>
    public bool TryGetValue(TKey[] path, [NotNullWhen(true)] out TValue? value)
    {
        if (path.Length == 0 || _isWeakLeaf)
        {
            value = _value;
            return value != null;
        }
        if (_children.TryGetValue(path[0], out var child)) 
            return child.TryGetValue(path[1..], out value);
        value = default;
        return false;
    }
}