using System;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal static class FlowContentPlacer
{
    public static (double Width, double Height) MeasureImage(
        Image image,
        double width,
        Func<Image, double, (double Width, double Height)>? measureImage)
        => measureImage is null ? ImageProbe.Measure(image, width) : measureImage(image, width);

    public static LaidOutImage Image(
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
            X = xOffset + HorizontalAlignmentOffset.Of(image.Alignment, width, imageWidth),
        };

    public static LaidOutCodeSymbol CodeSymbol(
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
            X = xOffset + HorizontalAlignmentOffset.Of(Paginator.CodeSymbolAlignment(block), width, codeSymbolWidth),
            Caption = CodeSymbolDispatch.Caption(block, fonts, resolution, capture),
        };

    public static LaidOutTableFragment Table(
        Table source,
        LaidOutTable layout,
        LaidOutTableSlice fragment,
        double y,
        int order,
        LayoutCaptureContext capture)
        => TableFragmentJoin.Join(
            capture.Node(),
            capture.Source(source),
            layout,
            fragment,
            y,
            order);
}
