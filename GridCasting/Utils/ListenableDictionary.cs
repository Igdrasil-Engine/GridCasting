using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace GridCasting.Utils;

/// <summary>
/// ListenableDictionary is a wrapper around a standard dictionary that extends its functionality
/// by providing event-based notifications for certain operations such as adding, removing, or updating entries.
/// </summary>
/// <typeparam name="TKey">The type of keys maintained in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values maintained in the dictionary.</typeparam>
public class ListenableDictionary<TKey, TValue>(IDictionary<TKey, TValue> baseDictionary) : IDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents an event triggered when the <see cref="ListenableDictionary{TKey, TValue}"/> is cleared using the <c>Clear</c> method.
    /// Subscribing to this event allows monitoring actions where all dictionary entries are removed at once.
    /// </summary>
    public event Action? OnClear;

    /// <summary>
    /// Represents an event triggered when an entry is removed from the <see cref="ListenableDictionary{TKey, TValue}"/>
    /// using the <c>Remove</c> method. Subscribing to this event allows monitoring the removal of specific keys from the dictionary.
    /// </summary>
    public event Action<TKey>? OnRemove;

    /// <summary>
    /// Represents an event triggered whenever an entry in the <see cref="ListenableDictionary{TKey, TValue}"/> is updated or replaced.
    /// Subscribing to this event allows monitoring changes to existing key-value pairs within the dictionary,
    /// providing the updated key and corresponding value.
    /// </summary>
    public event Action<TKey, TValue>? OnUpdate;

    /// <summary>
    /// Returns an enumerator that iterates through the ListenableDictionary.
    /// </summary>
    /// <returns>
    /// An enumerator for the entries in the dictionary.
    /// </returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => baseDictionary.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the ListenableDictionary as a non-generic collection.
    /// </summary>
    /// <returns>
    /// An enumerator for the entries in the dictionary as a non-generic IEnumerable.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Removes all entries from the ListenableDictionary.
    /// </summary>
    /// <remarks>
    /// This operation clears the underlying dictionary and triggers the <see cref="OnClear"/> event if any subscribers are registered.
    /// </remarks>
    public void Clear()
    {
        baseDictionary.Clear();
        OnClear?.Invoke();
    }

    /// <summary>
    /// Gets the total number of key-value pairs contained within the <see cref="ListenableDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <value>
    /// An integer representing the count of elements currently stored in the dictionary.
    /// </value>
    /// <remarks>
    /// This property retrieves the count directly from the underlying base dictionary.
    /// The count updates dynamically as items are added or removed.
    /// </remarks>
    public int Count => baseDictionary.Count;

    /// <summary>
    /// Indicates whether the <see cref="ListenableDictionary{TKey, TValue}"/> is read-only.
    /// </summary>
    /// <remarks>
    /// A read-only dictionary does not allow adding, removing, or modifying its elements.
    /// This property reflects the underlying dictionary's read-only status.
    /// </remarks>
    public bool IsReadOnly => baseDictionary.IsReadOnly;

    /// <summary>
    /// Adds the specified key and value to the ListenableDictionary.
    /// </summary>
    /// <param name="key">The key of the element to add to the dictionary.</param>
    /// <param name="value">The value of the element to add to the dictionary.</param>
    public void Add(TKey key, TValue value)
    {
        baseDictionary.Add(key, value);
        OnUpdate?.Invoke(key, value);
    }

    /// <summary>
    /// Determines whether the ListenableDictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the dictionary.</param>
    /// <returns>
    /// true if the ListenableDictionary contains an element with the specified key; otherwise, false.
    /// </returns>
    public bool ContainsKey(TKey key) => baseDictionary.ContainsKey(key);

    /// <summary>
    /// Removes the value with the specified key from the ListenableDictionary.
    /// </summary>
    /// <param name="key">The key of the element to remove from the dictionary.</param>
    /// <returns>
    /// true if the element is successfully removed; otherwise, false.
    /// This method also returns false if the key was not found in the dictionary.
    /// </returns>
    public bool Remove(TKey key)
    {
        var result = baseDictionary.Remove(key);
        if (result) OnRemove?.Invoke(key);
        return result;
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key from the ListenableDictionary.
    /// </summary>
    /// <param name="key">The key whose value to retrieve.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key,
    /// if the key is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns>
    /// <c>true</c> if the ListenableDictionary contains an element with the specified key; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) =>
        baseDictionary.TryGetValue(key, out value);

    /// <summary>
    /// Gets or sets the value associated with the specified key in the ListenableDictionary.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <value>
    /// The value associated with the specified key. If the key does not exist, a get operation throws a KeyNotFoundException.
    /// A set operation will update the value and trigger the <see cref="OnUpdate"/> event.
    /// </value>
    /// <exception cref="KeyNotFoundException">Thrown when attempting to get a value for a key that does not exist.</exception>
    /// <remarks>
    /// Setting a value will replace the existing value if the key is already present, and notify subscribers via the <see cref="OnUpdate"/> event.
    /// </remarks>
    public TValue this[TKey key]
    {
        get => baseDictionary[key];
        set
        {
            baseDictionary[key] = value;
            OnUpdate?.Invoke(key, value);
        }
    }

    /// <summary>
    /// Gets a collection containing the keys in the <see cref="ListenableDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// The returned collection reflects the current state of the dictionary and provides a way to iterate through all the keys.
    /// </remarks>
    public ICollection<TKey> Keys => baseDictionary.Keys;

    /// <summary>
    /// Gets a collection containing the values in the <see cref="ListenableDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// The returned collection directly reflects the values present in the underlying dictionary at the time of access.
    /// Changes to the ListenableDictionary will be reflected in this collection.
    /// </remarks>
    public ICollection<TValue> Values => baseDictionary.Values;

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => baseDictionary.Contains(item);
    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index) => baseDictionary.CopyTo(array, index);
    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        if (!baseDictionary.TryGetValue(item.Key, out var value)) return false;
        if (!EqualityComparer<TValue>.Default.Equals(value, item.Value)) return false;
        Remove(item.Key);
        return true;
    }
} 