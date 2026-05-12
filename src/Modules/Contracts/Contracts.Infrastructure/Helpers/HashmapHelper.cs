using System.Reflection;

namespace Contracts.Infrastructure.Helpers;

public static class HashmapReflectionExtensions
{
    public static IEnumerable<KeyValuePair<TK, TV>> GetEntries<TK, TV>(
        this Hashmap<TK, TV> hashmap,
        HashmapOptions<TK, TV> options)
    {
        var field = typeof(Hashmap<TK, TV>)
            .BaseType?
            .GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);

        if (field?.GetValue(hashmap) is not SortedDictionary<Bits, Cell> map)
            yield break;

        var deserializeKey = options.Deserializers?.Key;
        var deserializeValue = options.Deserializers?.Value;

        if (deserializeKey is null || deserializeValue is null)
            yield break;

        foreach (var kv in map)
        {
            yield return new KeyValuePair<TK, TV>(
                deserializeKey(kv.Key),
                deserializeValue(kv.Value));
        }
    }
}