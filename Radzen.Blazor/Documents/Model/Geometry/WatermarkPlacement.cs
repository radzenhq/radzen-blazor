using Radzen.Documents.Fonts;

namespace Radzen.Documents.Geometry;

internal readonly struct LaidOutWatermarkText
{
    public required string Text { get; init; }

    public required FontPaint Font { get; init; }

    public required double Size { get; init; }

    public required Color Color { get; init; }

    public required double X { get; init; }

    public required double Baseline { get; init; }

    public required CapturedGlyphRun GlyphRun { get; init; }

    public double? AlphaOverride { get; init; }
}

internal readonly struct LaidOutWatermarkImage
{
    public required SourceId Source { get; init; }

    public required ImagePaint Paint { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    public required double Alpha { get; init; }
}

internal sealed record LaidOutWatermark
{
    public required LaidOutNodeId Id { get; init; }

    public required double CenterX { get; init; }

    public required double CenterY { get; init; }

    public required double Rotation { get; init; }

    public required double Opacity { get; init; }

    public LaidOutWatermarkImage? Image { get; init; }

    public LaidOutWatermarkText? Text { get; init; }
}
