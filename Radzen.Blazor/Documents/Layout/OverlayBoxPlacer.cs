using System;
using System.Collections.Generic;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

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
        double xOffset = 0,
        LayoutCaptureContext? capture = null)
    {
        capture ??= new LayoutCaptureContext();
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
            var contentBox = new Rect(indent + padding, padding, innerWidth, measured.Height);
            var positioned = BoxContentLayout.Position(measured, contentBox, HorizontalAlignment.Left, VerticalAlignment.Top);
            lines.AddRange(positioned.Lines);
            images.AddRange(positioned.Images);
            codeSymbols.AddRange(positioned.CodeSymbols);
            order = MergeNested(positioned, tables, boxes, order);
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
        return (content, indent, boxWidth, innerHeight + (2 * padding));
    }

    private static int MergeNested(
        LaidOutBoxContent child,
        List<LaidOutTablePlacement> tables,
        List<LaidOutBox> boxes,
        int order)
    {
        var cursor = OrderedMerge.ByOrder(child.Tables, static t => t.Order, child.Boxes, static b => b.Order);
        while (cursor.MoveNext())
        {
            if (cursor.IsTable)
            {
                tables.Add(child.Tables[cursor.TableIndex] with { Order = order++ });
            }
            else
            {
                boxes.Add(child.Boxes[cursor.BoxIndex] with { Order = order++ });
            }
        }

        return order;
    }

    private static (double Padding, double BoxWidth, double Indent, double InnerWidth) Geometry(
        Container container, double availableWidth, double xOffset = 0)
    {
        var padding = container.Padding.Point;
        var boxWidth = container.Width?.Point ?? availableWidth;
        var indent = xOffset + Math.Max(0, HorizontalAlignmentOffset.Of(container.Alignment, availableWidth, boxWidth));
        var innerWidth = Math.Max(0, boxWidth - (2 * padding));
        return (padding, boxWidth, indent, innerWidth);
    }

    internal static BoxContentLayout.Measured MeasureBox(
        Container container,
        double contentWidth,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext? capture = null)
    {
        var innerWidth = Math.Max(0, (container.Width?.Point ?? contentWidth) - (2 * container.Padding.Point));
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
        var boxHeight = measured.Height + (2 * padding);
        var contentBox = new Rect(indent + padding, padding, innerWidth, measured.Height);
        var content = BoxContentLayout.Position(measured, contentBox, HorizontalAlignment.Left, VerticalAlignment.Top);

        return new LaidOutBox
        {
            Id = measured.Capture.Node(),
            Source = measured.Capture.Source(container),
            Content = content,
            Bounds = new Rect(indent, y, boxWidth, boxHeight),
            Style = GeometryCapture.Box(container, boxWidth, boxHeight),
            Padding = padding,
            Opacity = (resolution?.Opacities ?? OpacityResolver.None).ContainerOpacity(container),
            Transform = transform,
            Order = order,
        };
    }
}
