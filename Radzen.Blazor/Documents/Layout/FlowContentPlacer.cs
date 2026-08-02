using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal static class FlowContentPlacer
{
    public static LaidOutImage Image(
        Image image,
        double width,
        double y,
        double imageWidth,
        double imageHeight,
        LayoutCaptureContext capture,
        int zOrder,
        double xOffset = 0)
        => new()
        {
            Source = capture.Source(image),
            Paint = GeometryCapture.Image(image, capture),
            Y = y,
            Width = imageWidth,
            Height = imageHeight,
            X = xOffset + HorizontalAlignmentOffset.Of(image.Alignment, width, imageWidth),
            ZOrder = zOrder,
        };

    public static LaidOutCodeSymbol CodeSymbol(
        Block block,
        double width,
        double y,
        double codeSymbolWidth,
        double codeSymbolHeight,
        Fonts.FontCollection fonts,
        LoweringResult? resolution,
        LayoutCaptureContext capture,
        int zOrder,
        double xOffset = 0)
        => new()
        {
            Source = capture.Source(block),
            Modules = CodeSymbolDispatch.Modules(block),
            Foreground = CodeSymbolDispatch.Foreground(block),
            Y = y,
            Width = codeSymbolWidth,
            Height = codeSymbolHeight,
            X = xOffset + HorizontalAlignmentOffset.Of(CodeSymbolDispatch.Alignment(block), width, codeSymbolWidth),
            Caption = CodeSymbolDispatch.Caption(block, fonts, resolution, capture),
            ZOrder = zOrder,
        };

    public static LaidOutTableFragment Table(
        Table source,
        LaidOutTable layout,
        LaidOutTableSlice fragment,
        double y,
        int order,
        LayoutCaptureContext capture)
        => TableFragmentJoin.Join(
            capture.Source(source),
            layout,
            fragment,
            y,
            order);
}
