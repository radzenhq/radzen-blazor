using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

internal static class DictionaryShift
{
    public static void Remap<TKey, TValue>(IDictionary<TKey, TValue> dict, Func<TKey, TKey?> remap)
        where TKey : struct
    {
        Remap(dict, remap, onRemap: null);
    }

    // Two-phase (stage, clear, reinsert) prevents corruption when shifted keys overlap existing ones.
    public static void Remap<TKey, TValue>(
        IDictionary<TKey, TValue> dict,
        Func<TKey, TKey?> remap,
        Action<TKey, TKey, TValue>? onRemap)
        where TKey : struct
    {
        var staging = new List<(TKey oldKey, TKey newKey, TValue value)>(dict.Count);

        foreach (var kvp in dict)
        {
            var newKey = remap(kvp.Key);
            if (newKey.HasValue)
            {
                staging.Add((kvp.Key, newKey.Value, kvp.Value));
            }
        }

        dict.Clear();

        foreach (var (oldKey, newKey, value) in staging)
        {
            dict[newKey] = value;
            onRemap?.Invoke(oldKey, newKey, value);
        }
    }

    public static void Remap<T>(ISet<T> set, Func<T, T?> remap) where T : struct
    {
        var staging = new List<T>(set.Count);

        foreach (var item in set)
        {
            var mapped = remap(item);
            if (mapped.HasValue)
            {
                staging.Add(mapped.Value);
            }
        }

        set.Clear();

        foreach (var item in staging)
        {
            set.Add(item);
        }
    }
}
