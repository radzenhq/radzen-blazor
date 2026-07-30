using System.Collections.Immutable;

namespace Radzen.Documents.Geometry;

internal readonly struct LaidOutLine
{
    public required LineBox Line { get; init; }

    public required SourceId Source { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public int ZOrder { get; init; }
}

internal readonly struct LaidOutImage
{
    public required SourceId Source { get; init; }

    public required ImagePaint Paint { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    public int ZOrder { get; init; }
}

internal readonly struct LaidOutCodeSymbol
{
    public required SourceId Source { get; init; }

    public required ImmutableArray<CodeSymbolModule> Modules { get; init; }

    public required double Width { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Height { get; init; }

    public ImmutableArray<LaidOutCaptionLine>? Caption { get; init; }

    public int ZOrder { get; init; }
}

internal readonly struct LaidOutTablePlacement
{
    public required LaidOutTable Layout { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public int ZOrder { get; init; }
}

internal sealed record LaidOutBox
{
    public required LaidOutNodeId Id { get; init; }

    public required SourceId Source { get; init; }

    public required LaidOutBoxContent Content { get; init; }

    public required Rect Bounds { get; init; }

    public required BoxStyle Style { get; init; }

    public required double Padding { get; init; }

    public required double Opacity { get; init; }

    public Matrix? Transform { get; init; }

    public int ZOrder { get; init; }
}

internal sealed record LaidOutCell
{
    public required LaidOutNodeId Id { get; init; }

    public required SourceId Source { get; init; }

    public required int Row { get; init; }

    public required int Column { get; init; }

    public required int ColumnSpan { get; init; }

    public required int RowSpan { get; init; }

    public required Rect Bounds { get; init; }

    public required Rect ContentBox { get; init; }

    public required BoxStyle Decoration { get; init; }

    public required double Opacity { get; init; }

    public required ImmutableArray<LaidOutLine> Lines { get; init; }

    public ImmutableArray<LaidOutImage> Images { get; init; } = [];

    public ImmutableArray<LaidOutCodeSymbol> CodeSymbols { get; init; } = [];

    public ImmutableArray<LaidOutTablePlacement> Tables { get; init; } = [];

    public ImmutableArray<LaidOutBox> Boxes { get; init; } = [];
}

internal sealed class LaidOutTable
{
    public required LaidOutNodeId Id { get; init; }

    public required ImmutableArray<double> ColumnWidths { get; init; }

    public required ImmutableArray<double> RowHeights { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    public required ImmutableArray<LaidOutCell> Cells { get; init; }

    public required TableDecoration Decoration { get; init; }

    public required SourceId Source { get; init; }
}
