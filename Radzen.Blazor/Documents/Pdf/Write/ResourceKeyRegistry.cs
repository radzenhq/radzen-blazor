using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Write;

internal sealed class ResourceKeyRegistry<TIdentity, TValue>(
    string prefix,
    IEqualityComparer<TIdentity>? comparer = null)
    where TIdentity : notnull
{
    private readonly List<TValue> values = [];
    private readonly Dictionary<TIdentity, int> indexByIdentity = new(comparer);

    public IReadOnlyList<TValue> Values => values;

    public int Count => values.Count;

    public string GetOrAdd(TIdentity identity, Func<string, TValue> create)
        => KeyAt(GetOrAddIndex(identity, create));

    public TValue GetOrAddValue(TIdentity identity, Func<string, TValue> create)
        => values[GetOrAddIndex(identity, create)];

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

    private string KeyAt(int index) => prefix + index.ToString(CultureInfo.InvariantCulture);

    public string Add(Func<string, TValue> create)
    {
        var key = prefix + values.Count.ToString(CultureInfo.InvariantCulture);
        values.Add(create(key));
        return key;
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
