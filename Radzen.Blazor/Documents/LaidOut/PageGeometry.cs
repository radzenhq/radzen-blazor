using System.Collections.Immutable;
using Radzen.Documents.Core;

namespace Radzen.Documents.LaidOut;

internal readonly record struct ImagePaint
{
    public required SceneImageData Data { get; init; }

    public required double Opacity { get; init; }

    public required bool Interpolate { get; init; }

    public string? MediaType => Data.MediaType;
}

internal readonly struct LaidOutTableFragment
{
    public required SourceId Source { get; init; }

    public required LaidOutTable Layout { get; init; }

    public required LaidOutTableSlice Fragment { get; init; }

    public required ImmutableArray<LaidOutRow> Rows { get; init; }

    public required Rect Bounds { get; init; }

    public int ZOrder { get; init; }
}

internal readonly struct LaidOutCaptionLine
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

internal readonly record struct LaidOutLink
{
    public required double Left { get; init; }

    public required double Top { get; init; }

    public required double Right { get; init; }

    public required double Bottom { get; init; }

    public string? Uri { get; init; }

    public string? Anchor { get; init; }

    public string? Text { get; init; }

    public SourceId Source { get; init; }
}

internal readonly record struct LaidOutAnchor
{
    public required string Name { get; init; }

    public required double Top { get; init; }
}

internal sealed record LaidOutLayer : ILaidOutContent<LaidOutTableFragment>
{
    public ImmutableArray<LaidOutLine> Lines { get; init; } = [];

    public ImmutableArray<LaidOutImage> Images { get; init; } = [];

    public ImmutableArray<LaidOutCodeSymbol> CodeSymbols { get; init; } = [];

    public ImmutableArray<LaidOutTableFragment> Tables { get; init; } = [];

    public ImmutableArray<LaidOutBox> Boxes { get; init; } = [];

}

internal sealed record LaidOutPage
{
    public required PageSize Size { get; init; }

    public required Rect ContentBox { get; init; }

    public required LaidOutLayer Body { get; init; }

    public required LaidOutLayer HeaderLayer { get; init; }

    public required LaidOutLayer FooterLayer { get; init; }

    public LaidOutWatermark? Watermark { get; init; }

    public ImmutableArray<LaidOutLink> Links { get; init; } = [];

    public ImmutableArray<LaidOutAnchor> Anchors { get; init; } = [];

    public double HeaderTop { get; init; }

    public double FooterTop { get; init; }

}
