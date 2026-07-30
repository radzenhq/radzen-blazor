using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf;

internal sealed class ResourceNameAllocator<TIdentity, TValue>(
    string prefix,
    IEnumerable<string>? reserved = null,
    IEqualityComparer<TIdentity>? comparer = null)
    where TIdentity : notnull
{
    private readonly List<TValue> values = [];
    private readonly Dictionary<TIdentity, int> indexByIdentity = new(comparer);

    private string Prefix { get; } = ReservePrefix(prefix, reserved);

    public IReadOnlyList<TValue> Values => values;

    public int Count => values.Count;

    public string GetOrAdd(TIdentity identity, Func<string, TValue> create)
        => NameAt(GetOrAddIndex(identity, create));

    public TValue GetOrAddValue(TIdentity identity, Func<string, TValue> create)
        => values[GetOrAddIndex(identity, create)];

    public string Add(Func<string, TValue> create)
    {
        var name = NameAt(values.Count);
        values.Add(create(name));
        return name;
    }

    private int GetOrAddIndex(TIdentity identity, Func<string, TValue> create)
    {
        if (indexByIdentity.TryGetValue(identity, out var existing))
        {
            return existing;
        }

        var index = values.Count;
        Add(create);
        indexByIdentity[identity] = index;
        return index;
    }

    private string NameAt(int index) => Prefix + index.ToString(CultureInfo.InvariantCulture);

    private static string ReservePrefix(string prefix, IEnumerable<string>? reserved)
    {
        if (reserved is null)
        {
            return prefix;
        }

        var candidate = prefix;
        while (StartsAny(candidate, reserved))
        {
            candidate += "z";
        }

        return candidate;
    }

    private static bool StartsAny(string candidate, IEnumerable<string> reserved)
    {
        foreach (var name in reserved)
        {
            if (name.StartsWith(candidate, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

internal static class ResourceNameAllocator
{
    public static string Derive(string name, string suffix) => name + suffix;

    public static string Reserve(string name, IEnumerable<string>? reserved)
    {
        if (reserved is null)
        {
            return name;
        }

        var candidate = name;
        while (ContainsExact(candidate, reserved))
        {
            candidate += "z";
        }

        return candidate;
    }

    private static bool ContainsExact(string candidate, IEnumerable<string> reserved)
    {
        foreach (var name in reserved)
        {
            if (string.Equals(name, candidate, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

internal sealed class ReferenceKeyComparer<T> : IEqualityComparer<T>
    where T : class
{
    public static ReferenceKeyComparer<T> Instance { get; } = new();

    public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

    public int GetHashCode(T value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
}

internal sealed class AlphaComparer : IEqualityComparer<double>
{
    public static AlphaComparer Instance { get; } = new();

    public bool Equals(double x, double y) => x == y;

    public int GetHashCode(double value) => value.GetHashCode();
}
