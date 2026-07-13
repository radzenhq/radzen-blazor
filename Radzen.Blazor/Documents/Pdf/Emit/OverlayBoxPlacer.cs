using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

// Places container blocks as first-class boxes: overlay containers (children stacked at the
// box origin) and Stack containers (measured content), plus the shared box/image geometry
// helpers. The section body, header/footer bands and the keep-with-next look-ahead all route
// through here so a container lays out identically wherever it appears.
internal static class OverlayBoxPlacer
{
    internal static bool IsSpecial(Container container)
        => container.Layout == ContainerLayout.Overlay;

    // Lays out an overlay container as a first-class box: each child is measured and
    // positioned independently at the box top-left (inset by the padding), and the
    // results are merged in declaration order (nested tables/boxes reordered onto one
    // increasing sequence so emission interleaves them in that order). The box inner
    // height is the tallest child's; content positions are box-local (the emitter shifts
    // them by the box Y).
    internal static (LaidOutBoxContent Content, double Indent, double BoxWidth, double BoxHeight) LayoutOverlay(
        Container container,
        double availableWidth,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution)
    {
        var padding = container.Padding.Point;
        var boxWidth = container.Width?.Point ?? availableWidth;
        var indent = Math.Max(0, AlignImage(container.Alignment, availableWidth, boxWidth));
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

    // Appends one overlay child's nested tables and boxes onto the shared sequence,
    // renumbering their Order to keep declaration order (later children on top) while
    // preserving each child's own table/box interleave.
    private static int MergeNested(
        LaidOutBoxContent child,
        List<LaidOutNestedTable> tables,
        List<LaidOutNestedBox> boxes,
        int order)
    {
        var ti = 0;
        var bi = 0;
        while (ti < child.Tables.Count || bi < child.Boxes.Count)
        {
            if (bi >= child.Boxes.Count || (ti < child.Tables.Count && child.Tables[ti].Order <= child.Boxes[bi].Order))
            {
                tables.Add(child.Tables[ti++] with { Order = order++ });
            }
            else
            {
                boxes.Add(child.Boxes[bi++] with { Order = order++ });
            }
        }

        return order;
    }

    internal static double AlignImage(HorizontalAlignment alignment, double containerWidth, double imageWidth)
        => alignment switch
        {
            HorizontalAlignment.Center => (containerWidth - imageWidth) / 2.0,
            HorizontalAlignment.Right or HorizontalAlignment.End => containerWidth - imageWidth,
            _ => 0,
        };

    // Measures a Stack container's content at the box's inner width (box width minus the
    // padding on both sides); content measures with no inherited alignment.
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

    // Positions a measured Stack container as a first-class box at y. Content is
    // positioned box-local (Y from the box top); the emitter shifts it by the box's
    // page Y, with left/top content alignment defaults.
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
        var indent = Math.Max(0, AlignImage(container.Alignment, availableWidth, boxWidth));
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
