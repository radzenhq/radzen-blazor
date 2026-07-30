using System.Collections.Immutable;

namespace Radzen.Documents.Geometry;

internal readonly struct LaidOutFragmentRow
{
    public required int SourceRow { get; init; }

    public required bool IsHeader { get; init; }

    public required double Y { get; init; }

    public required double Height { get; init; }
}

internal sealed record LaidOutTableSlice
{
    public required LaidOutNodeId Id { get; init; }

    public required int Number { get; init; }

    public required ImmutableArray<LaidOutFragmentRow> Rows { get; init; }

    public required int HeaderRowCount { get; init; }

    public required bool ContainsRepeatedHeaders { get; init; }

    public required double Height { get; init; }
}
