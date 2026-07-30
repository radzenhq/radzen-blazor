using Radzen.Documents.Fonts;

namespace Radzen.Documents.Geometry;

internal readonly struct PositionedWatermarkText
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

internal readonly struct PositionedWatermarkImage
{
    public required SourceId Source { get; init; }

    public required ImagePaint Paint { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    public required double Alpha { get; init; }
}

internal sealed record PositionedWatermark
{
    public required double CenterX { get; init; }

    public required double CenterY { get; init; }

    public required double Rotation { get; init; }

    public required double Opacity { get; init; }

    public PositionedWatermarkImage? Image { get; init; }

    public PositionedWatermarkText? Text { get; init; }
}
