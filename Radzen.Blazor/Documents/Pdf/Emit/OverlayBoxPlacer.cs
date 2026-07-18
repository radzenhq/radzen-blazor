using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal static class OverlayBoxPlacer
{
    internal static bool IsSpecial(Container container)
        => container.Layout == ContainerLayout.Overlay;

    internal static (LaidOutBoxContent Content, double Indent, double BoxWidth, double BoxHeight) LayoutOverlay(
        Container container,
        double availableWidth,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution)
    {
        var padding = container.Padding.Point;
        var boxWidth = container.Width?.Point ?? availableWidth;
        var indent = Math.Max(0, HorizontalAlignmentOffset.Of(container.Alignment, availableWidth, boxWidth));
        var innerWidth = Math.Max(0, boxWidth - (2 * padding));

        var lines = new List<LaidOutLine>();
        var images = new List<LaidOutImage>();
        var codes = new List<LaidOutCode>();
        var tables = new List<LaidOutNestedTable>();
        var boxes = new List<LaidOutNestedBox>();
        var order = 0;
        var innerHeight = 0.0;
        foreach (var child in container.Blocks)
        {
            var single = new BlockCollection { child };
            var measured = BoxContentLayout.Measure(single, innerWidth, null, fonts, measureImage, resolution);
            innerHeight = Math.Max(innerHeight, measured.Height);
            var contentBox = new Rect(indent + padding, padding, innerWidth, measured.Height);
            var positioned = BoxContentLayout.Position(measured, contentBox, HorizontalAlignment.Left, VerticalAlignment.Top);
            lines.AddRange(positioned.Lines);
            images.AddRange(positioned.Images);
            codes.AddRange(positioned.Codes);
            order = MergeNested(positioned, tables, boxes, order);
        }

        var content = new LaidOutBoxContent
        {
            Height = innerHeight,
            Lines = lines,
            Images = images,
            Codes = codes,
            Tables = tables,
            Boxes = boxes,
        };
        return (content, indent, boxWidth, innerHeight + (2 * padding));
    }

    private static int MergeNested(
        LaidOutBoxContent child,
        List<LaidOutNestedTable> tables,
        List<LaidOutNestedBox> boxes,
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

    internal static BoxContentLayout.Measured MeasureBox(
        Container container,
        double contentWidth,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution)
    {
        var innerWidth = Math.Max(0, (container.Width?.Point ?? contentWidth) - (2 * container.Padding.Point));
        return BoxContentLayout.Measure(container.Blocks, innerWidth, null, fonts, measureImage, resolution);
    }

    internal static PositionedBox BuildBox(
        Container container,
        BoxContentLayout.Measured measured,
        double availableWidth,
        double y,
        int order,
        Matrix? transform)
    {
        var padding = container.Padding.Point;
        var boxWidth = container.Width?.Point ?? availableWidth;
        var indent = Math.Max(0, HorizontalAlignmentOffset.Of(container.Alignment, availableWidth, boxWidth));
        var innerWidth = Math.Max(0, boxWidth - (2 * padding));
        var boxHeight = measured.Height + (2 * padding);
        var contentBox = new Rect(indent + padding, padding, innerWidth, measured.Height);
        var content = BoxContentLayout.Position(measured, contentBox, HorizontalAlignment.Left, VerticalAlignment.Top);

        return new PositionedBox
        {
            Source = container,
            Content = content,
            Bounds = new Rect(indent, y, boxWidth, boxHeight),
            Style = BoxStyle.FromContainer(container),
            Y = y,
            Opacity = container.Opacity,
            Transform = transform,
            Order = order,
        };
    }
}
