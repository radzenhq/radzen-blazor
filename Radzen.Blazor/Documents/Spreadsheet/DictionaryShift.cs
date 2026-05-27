using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

/// <summary>
/// Helpers for rekeying dictionaries during row/column shift operations.
/// </summary>
internal static class DictionaryShift
{
    /// <summary>
    /// Rekeys a dictionary in place by applying <paramref name="remap"/> to every key.
    /// Entries for which <paramref name="remap"/> returns <c>null</c> are dropped.
    /// A two-phase approach (stage, clear, reinsert) prevents corruption when shifted keys overlap existing ones.
    /// </summary>
    public static void Remap<TKey, TValue>(IDictionary<TKey, TValue> dict, Func<TKey, TKey?> remap)
        where TKey : struct
    {
        Remap(dict, remap, onRemap: null);
    }

    /// <summary>
    /// Rekeys a dictionary in place by applying <paramref name="remap"/> to every key.
    /// Entries for which <paramref name="remap"/> returns <c>null</c> are dropped.
    /// <paramref name="onRemap"/> is invoked for every retained entry with its old key, new key, and value,
    /// allowing callers to apply side effects (e.g. updating a stored back-reference).
    /// </summary>
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

    /// <summary>
    /// Rekeys a set in place by applying <paramref name="remap"/> to every element.
    /// Elements for which <paramref name="remap"/> returns <c>null</c> are dropped.
    /// </summary>
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
