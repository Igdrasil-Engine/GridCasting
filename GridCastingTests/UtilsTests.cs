using GridCasting.Models.Grid;
using GridCasting.Utils;
#if NET8_0_OR_GREATER
using FVector2 = IgdrasilEngine.Engine.Math.Vectors.FVector2;
#else
using FVector2 = UnityEngine.Vector2;
#endif

namespace GridCastingTests;

public class UtilsTests
{
    [Test]
    public void ListenableDictionaryRaisesEventsForMutations()
    {
        var dictionary = new ListenableDictionary<string, int>(new Dictionary<string, int>());
        var updates = new List<(string Key, int Value)>();
        var removedKeys = new List<string>();
        var clearCount = 0;

        dictionary.OnUpdate += (key, value) => updates.Add((key, value));
        dictionary.OnRemove += key => removedKeys.Add(key);
        dictionary.OnClear += () => clearCount++;

        dictionary.Add("mana", 10);
        dictionary["mana"] = 7;
        var removed = dictionary.Remove("mana");
        dictionary.Add("cooldown", 1);
        dictionary.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(updates, Is.EqualTo(new[] { ("mana", 10), ("mana", 7), ("cooldown", 1) }));
            Assert.That(removed, Is.True);
            Assert.That(removedKeys, Is.EqualTo(new[] { "mana" }));
            Assert.That(clearCount, Is.EqualTo(1));
            Assert.That(dictionary.Count, Is.Zero);
        });
    }

    [Test]
    public void ListenableDictionaryCollectionRemoveChecksKeyAndValue()
    {
        var dictionary = new ListenableDictionary<string, int>(new Dictionary<string, int>
        {
            ["mana"] = 10
        });
        ICollection<KeyValuePair<string, int>> collection = dictionary;
        var removedKeys = new List<string>();
        dictionary.OnRemove += key => removedKeys.Add(key);

        var wrongValueRemoved = collection.Remove(new KeyValuePair<string, int>("mana", 7));
        var missingKeyRemoved = collection.Remove(new KeyValuePair<string, int>("health", 10));
        var removed = collection.Remove(new KeyValuePair<string, int>("mana", 10));

        Assert.Multiple(() =>
        {
            Assert.That(wrongValueRemoved, Is.False);
            Assert.That(missingKeyRemoved, Is.False);
            Assert.That(removed, Is.True);
            Assert.That(removedKeys, Is.EqualTo(new[] { "mana" }));
            Assert.That(dictionary.ContainsKey("mana"), Is.False);
        });
    }

    [Test]
    public void GridExposesNodesByIndexAndEnumeration()
    {
        var grid = new Grid();
        var first = new GridNode(new FVector2(0f, 0f));
        var second = new GridNode(new FVector2(1f, 0f));

        first.Connections.Add(second);
        grid.Nodes.AddRange([first, second]);

        Assert.Multiple(() =>
        {
            Assert.That(grid[0], Is.SameAs(first));
            Assert.That(grid.ToArray(), Is.EqualTo(new[] { first, second }));
            Assert.That(first.Position, Is.EqualTo(new FVector2(0f, 0f)));
            Assert.That(first.Connections, Is.EqualTo(new GridNode?[] { second }));
        });
    }
}
