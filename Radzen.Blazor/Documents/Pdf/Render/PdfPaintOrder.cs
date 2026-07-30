using System.Collections.Immutable;

namespace Radzen.Documents.Pdf.Render;

internal enum PaintPhase
{
    Fill,
    Stroke,
    Image,
    Text,
    Watermark,
}

internal enum PageLayerKind
{
    Body,
    Header,
    Footer,
}

internal enum PaintContent
{
    Lines,
    TablesAndBoxesByOrder,
    ImagesAndCodeSymbols,
}

internal static class PdfPaintOrder
{
    public static ImmutableArray<PaintPhase> Phases { get; } =
    [
        PaintPhase.Fill,
        PaintPhase.Stroke,
        PaintPhase.Image,
        PaintPhase.Text,
        PaintPhase.Watermark,
    ];

    public static ImmutableArray<PageLayerKind> Layers { get; } =
    [
        PageLayerKind.Body,
        PageLayerKind.Header,
        PageLayerKind.Footer,
    ];

    public static ImmutableArray<PaintContent> BodyContent { get; } =
    [
        PaintContent.Lines,
        PaintContent.TablesAndBoxesByOrder,
        PaintContent.ImagesAndCodeSymbols,
    ];

    public static ImmutableArray<PaintContent> BandContent { get; } =
    [
        PaintContent.Lines,
        PaintContent.ImagesAndCodeSymbols,
        PaintContent.TablesAndBoxesByOrder,
    ];

    public static ImmutableArray<PaintContent> ContentOf(PageLayerKind layer)
        => layer == PageLayerKind.Body ? BodyContent : BandContent;
}
