using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Emit;

// Registers a resource, dedups it by identity and hands back a prefix+ordinal key (GS0, P1, ...).
// Shared by the generated (PagePlan) and loaded (ContentWriter) worlds; what varies is the
// identity a caller dedups by, not the mechanic. The ordinal is the entry count, so one instance
// mints consecutive keys no matter which identity domain reached it - the interleaving of plain
// and soft-mask states through a single registry is what keeps GS numbering stable.
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

    // For callers whose registered value carries its own key and is needed back.
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

    // Appends an entry that opts out of reuse: it consumes an ordinal but is never handed back.
    public string Add(Func<string, TValue> create)
    {
        var key = prefix + values.Count.ToString(CultureInfo.InvariantCulture);
        values.Add(create(key));
        return key;
    }
}

// Dedups by instance, so a brush reused across elements shares one pattern while two equal-valued
// brushes stay distinct. Explicit rather than EqualityComparer<T>.Default: were the identity type
// to gain an Equals override, the default would silently start merging entries and move bytes.
internal sealed class ReferenceKeyComparer<T> : IEqualityComparer<T>
    where T : class
{
    public static ReferenceKeyComparer<T> Instance { get; } = new();

    public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

    public int GetHashCode(T value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
}

// Dedups alpha by IEEE ==, reproducing the linear scan this replaced: -0 matches 0, and NaN
// matches nothing (including itself), so a NaN alpha appends a fresh entry per call.
internal sealed class AlphaComparer : IEqualityComparer<double>
{
    public static AlphaComparer Instance { get; } = new();

    public bool Equals(double x, double y) => x == y;

    public int GetHashCode(double value) => value.GetHashCode();
}
