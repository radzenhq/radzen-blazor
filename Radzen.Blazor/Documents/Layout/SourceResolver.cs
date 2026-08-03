using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal sealed class SourceResolver
{
    private readonly ImmutableArray<object> values;
    private readonly Dictionary<object, SourceId> ids;

    public SourceResolver(IReadOnlyList<object> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var builder = ImmutableArray.CreateBuilder<object>(sources.Count);
        var map = new Dictionary<object, SourceId>(sources.Count, ReferenceEqualityComparer.Instance);
        for (var index = 0; index < sources.Count; index++)
        {
            builder.Add(sources[index]);
            map[sources[index]] = new SourceId(index);
        }

        values = builder.MoveToImmutable();
        ids = map;
    }

    public int Count => values.Length;

    public bool Knows(SourceId id) => id.Value >= 0 && id.Value < values.Length;

    public object Resolve(SourceId id)
        => Knows(id)
            ? values[id.Value]
            : throw new ArgumentOutOfRangeException(
                nameof(id),
                $"Source id {id.Value} was not produced by the layout pass this resolver was built from.");

    public T Resolve<T>(SourceId id) where T : class => (T)Resolve(id);

    public SourceId? Of(object source)
        => source is not null && ids.TryGetValue(source, out var id) ? id : null;
}
