using System.Collections.Immutable;

namespace Radzen.Documents.Geometry;

internal readonly struct FragmentRow
{
    public required int SourceRow { get; init; }

    public required bool IsHeader { get; init; }

    public required double Y { get; init; }

    public required double Height { get; init; }
}

internal sealed record TableFragment
{
    public required int Number { get; init; }

    public required ImmutableArray<FragmentRow> Rows { get; init; }

    public required int HeaderRowCount { get; init; }

    public required double Height { get; init; }
}
