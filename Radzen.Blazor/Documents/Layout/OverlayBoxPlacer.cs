using System.Collections.Generic;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal static class OverlayBoxPlacer
{
    internal static bool IsSpecial(Container container)
        => container.Layout == ContainerLayout.Overlay;

    internal static (LaidOutBoxContent Content, double Indent, double BoxWidth, double BoxHeight) LayoutOverlay(
        Container container,
        double availableWidth,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture,
        double xOffset = 0)
    {
        var (padding, boxWidth, indent, innerWidth) = Geometry(container, availableWidth, xOffset);

        var lines = new List<LaidOutLine>();
        var images = new List<LaidOutImage>();
        var codeSymbols = new List<LaidOutCodeSymbol>();
        var tables = new List<LaidOutTablePlacement>();
        var boxes = new List<LaidOutBox>();
        var order = 0;
        var innerHeight = 0.0;
        foreach (var child in container.Blocks)
        {
            var single = BlockCollection.Borrowing(child);
            var measured = BoxContentLayout.Measure(
                single,
                innerWidth,
                null,
                fonts,
                measureImage,
                resolution,
                capture);
            innerHeight = Math.Max(innerHeight, measured.Height);
            var contentBox = new Rect(indent + padding.Left, padding.Top, innerWidth, measured.Height);
            var positioned = BoxContentLayout.Position(measured, contentBox, HorizontalAlignment.Left, VerticalAlignment.Top);
            order = Compose(positioned, lines, images, codeSymbols, tables, boxes, order);
        }

        var content = new LaidOutBoxContent
        {
            Height = innerHeight,
            Lines = [.. lines],
            Images = [.. images],
            CodeSymbols = [.. codeSymbols],
            Tables = [.. tables],
            Boxes = [.. boxes],
        };
        return (content, indent, boxWidth, innerHeight + padding.Vertical);
    }

    private static int Compose(
        LaidOutBoxContent child,
        List<LaidOutLine> lines,
        List<LaidOutImage> images,
        List<LaidOutCodeSymbol> codeSymbols,
        List<LaidOutTablePlacement> tables,
        List<LaidOutBox> boxes,
        int order)
    {
        foreach (var line in child.Lines)
        {
            lines.Add(line with { ZOrder = order + line.ZOrder });
        }

        foreach (var image in child.Images)
        {
            images.Add(image with { ZOrder = order + image.ZOrder });
        }

        foreach (var codeSymbol in child.CodeSymbols)
        {
            codeSymbols.Add(codeSymbol with { ZOrder = order + codeSymbol.ZOrder });
        }

        foreach (var table in child.Tables)
        {
            tables.Add(table with { ZOrder = order + table.ZOrder });
        }

        foreach (var box in child.Boxes)
        {
            boxes.Add(box with { ZOrder = order + box.ZOrder });
        }

        return order + child.Lines.Length + child.Images.Length + child.CodeSymbols.Length
            + child.Tables.Length + child.Boxes.Length;
    }

    private static (BoxPadding Padding, double BoxWidth, double Indent, double InnerWidth) Geometry(
        Container container, double availableWidth, double xOffset = 0)
    {
        var padding = container.EffectivePadding;
        var boxWidth = container.Width?.Point ?? availableWidth;
        var indent = xOffset + Math.Max(0, HorizontalAlignmentOffset.Of(container.Alignment, availableWidth, boxWidth));
        var innerWidth = Math.Max(0, boxWidth - padding.Horizontal);
        return (padding, boxWidth, indent, innerWidth);
    }

    internal static BoxContentLayout.Measured MeasureBox(
        Container container,
        double contentWidth,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture)
    {
        var innerWidth = Math.Max(0, (container.Width?.Point ?? contentWidth) - container.EffectivePadding.Horizontal);
        return BoxContentLayout.Measure(
            container.Blocks,
            innerWidth,
            null,
            fonts,
            measureImage,
            resolution,
            capture);
    }

    internal static LaidOutBox BuildBox(
        Container container,
        BoxContentLayout.Measured measured,
        double availableWidth,
        double y,
        int order,
        Matrix? transform,
        LoweringContext? resolution = null,
        double xOffset = 0)
    {
        var (padding, boxWidth, indent, innerWidth) = Geometry(container, availableWidth, xOffset);
        var boxHeight = measured.Height + padding.Vertical;
        var contentBox = new Rect(indent + padding.Left, padding.Top, innerWidth, measured.Height);
        var content = BoxContentLayout.Position(measured, contentBox, HorizontalAlignment.Left, VerticalAlignment.Top);

        return new LaidOutBox
        {
            Id = measured.Capture.Node(),
            Source = measured.Capture.Source(container),
            Content = content,
            Bounds = new Rect(indent, y, boxWidth, boxHeight),
            Style = GeometryCapture.Box(container, boxWidth, boxHeight, measured.Capture),
            Padding = padding,
            Opacity = (resolution?.Opacities ?? OpacityResolver.None).ContainerOpacity(container),
            Transform = transform,
            ZOrder = order,
        };
    }
}
