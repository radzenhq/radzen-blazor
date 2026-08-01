using System.Collections.Immutable;

namespace Radzen.Documents.Pdf.Render;

internal enum PaintPhase
{
    Fills,
    StraightStrokes,
    RoundedStrokes,
    Images,
    Text,
    Watermark,
}

internal static class PdfPaintOrder
{
    public static ImmutableArray<PaintPhase> Phases { get; } =
    [
        PaintPhase.Fills,
        PaintPhase.StraightStrokes,
        PaintPhase.RoundedStrokes,
        PaintPhase.Images,
        PaintPhase.Text,
        PaintPhase.Watermark,
    ];
}
