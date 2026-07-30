using System.Collections.Immutable;

namespace Radzen.Documents.Geometry;

internal readonly struct TableDecoration
{
    public required double LeftIndent { get; init; }

    public required double CornerRadius { get; init; }

    public required BoxStyle Frame { get; init; }

    public required ImmutableArray<Color?> RowBackgrounds { get; init; }

    public Color? RowBackground(int row)
        => row >= 0 && row < RowBackgrounds.Length ? RowBackgrounds[row] : null;

}

internal readonly struct PlacedCell
{
    public required LaidOutCell Cell { get; init; }

    public required double Delta { get; init; }
}

internal readonly struct PlacedRow
{
    public required int SourceRow { get; init; }

    public required bool IsHeader { get; init; }

    public required double Y { get; init; }

    public required double Height { get; init; }

    public Color? Background { get; init; }

    public required ImmutableArray<PlacedCell> Cells { get; init; }
}
