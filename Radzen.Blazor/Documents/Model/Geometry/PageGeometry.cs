using System.Collections.Immutable;

namespace Radzen.Documents.Geometry;

internal readonly record struct ImagePaint
{
    public required SceneImageData Data { get; init; }

    public required double Opacity { get; init; }

    public required bool Interpolate { get; init; }
}

internal readonly struct PositionedLine
{
    public required LineBox Line { get; init; }

    public required SourceId Source { get; init; }

    public required double Y { get; init; }
}

internal readonly struct PositionedTableFragment
{
    public required SourceId Source { get; init; }

    public required LaidOutTable Layout { get; init; }

    public required TableFragment Fragment { get; init; }

    public required ImmutableArray<PlacedRow> Rows { get; init; }

    public required Rect Bounds { get; init; }

    public required double Y { get; init; }

    public int Order { get; init; }
}

internal readonly struct PositionedBox
{
    public required SourceId Source { get; init; }

    public required LaidOutBoxContent Content { get; init; }

    public required Rect Bounds { get; init; }

    public required BoxStyle Style { get; init; }

    public required double Y { get; init; }

    public required double Padding { get; init; }

    public required double Opacity { get; init; }

    public Matrix? Transform { get; init; }

    public int Order { get; init; }
}

internal readonly struct PositionedImage
{
    public required SourceId Source { get; init; }

    public required ImagePaint Paint { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    public double XOffset { get; init; }
}

internal readonly struct PositionedCaptionLine
{
    public required LineBox Line { get; init; }

    public required double Y { get; init; }
}

internal readonly struct CodeSymbolModule
{
    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }
}

internal readonly struct PositionedCodeSymbol
{
    public required SourceId Source { get; init; }

    public required ImmutableArray<CodeSymbolModule> Modules { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    public double XOffset { get; init; }

    public ImmutableArray<PositionedCaptionLine>? Caption { get; init; }
}

internal readonly record struct PositionedLink
{
    public required double Left { get; init; }

    public required double Top { get; init; }

    public required double Right { get; init; }

    public required double Bottom { get; init; }

    public string? Uri { get; init; }

    public string? Anchor { get; init; }

    public SourceId Source { get; init; }
}

internal readonly record struct PositionedAnchor
{
    public required string Name { get; init; }

    public required double Top { get; init; }
}

internal sealed record PageLayer
{
    public ImmutableArray<PositionedLine> Lines { get; init; } = [];

    public ImmutableArray<PositionedImage> Images { get; init; } = [];

    public ImmutableArray<PositionedCodeSymbol> CodeSymbols { get; init; } = [];

    public ImmutableArray<PositionedTableFragment> Tables { get; init; } = [];

    public ImmutableArray<PositionedBox> Boxes { get; init; } = [];

    public bool HasContent => Lines.Length > 0 || Images.Length > 0 || CodeSymbols.Length > 0 || Tables.Length > 0 || Boxes.Length > 0;
}

internal sealed record PaginatedPage
{
    public required PageSize Size { get; init; }

    public required Rect ContentBox { get; init; }

    public required int Number { get; init; }

    public required PageLayer Body { get; init; }

    public required PageLayer HeaderLayer { get; init; }

    public required PageLayer FooterLayer { get; init; }

    public PositionedWatermark? Watermark { get; init; }

    public ImmutableArray<PositionedLink> Links { get; init; } = [];

    public ImmutableArray<PositionedAnchor> Anchors { get; init; } = [];

    public double HeaderTop { get; init; }

    public double FooterTop { get; init; }

}
