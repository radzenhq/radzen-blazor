using System;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal static class FlowContentPlacer
{
    public static (double Width, double Height) MeasureImage(
        Image image,
        double width,
        Func<Image, double, (double Width, double Height)>? measureImage)
        => measureImage is null ? Paginator.MeasureImage(image, width) : measureImage(image, width);

    public static PositionedImage Image(
        Image image,
        double width,
        double y,
        double imageWidth,
        double imageHeight,
        LayoutCaptureContext capture,
        double xOffset = 0)
        => new()
        {
            Source = capture.Source(image),
            Paint = GeometryCapture.Image(image, capture),
            Y = y,
            Width = imageWidth,
            Height = imageHeight,
            XOffset = xOffset + HorizontalAlignmentOffset.Of(image.Alignment, width, imageWidth),
        };

    public static PositionedCodeSymbol CodeSymbol(
        Block block,
        double width,
        double y,
        double codeSymbolWidth,
        double codeSymbolHeight,
        Fonts.FontCollection fonts,
        LoweringContext? resolution,
        LayoutCaptureContext capture,
        double xOffset = 0)
        => new()
        {
            Source = capture.Source(block),
            Modules = CodeSymbolDispatch.Modules(block),
            Y = y,
            Width = codeSymbolWidth,
            Height = codeSymbolHeight,
            XOffset = xOffset + HorizontalAlignmentOffset.Of(Paginator.CodeSymbolAlignment(block), width, codeSymbolWidth),
            Caption = CodeSymbolDispatch.Caption(block, fonts, resolution),
        };

    public static PositionedTableFragment Table(
        Table source,
        LaidOutTable layout,
        TableFragment fragment,
        double y,
        int order,
        LayoutCaptureContext capture)
        => TableFragmentJoin.Join(capture.Source(source), layout, fragment, y, order);
}
