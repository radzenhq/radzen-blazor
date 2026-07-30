using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal static class BoxContentLayout
{
    internal readonly struct CellItem
    {
        public LineBox? Line { get; init; }
        public LineBox? MarkerLine { get; init; }
        public Block? MarkerSource { get; init; }
        public Block? Source { get; init; }
        public Image? Image { get; init; }
        public Block? CodeSymbol { get; init; }
        public ImmutableArray<LaidOutCaptionLine>? Caption { get; init; }
        public LaidOutTable? Table { get; init; }
        public Container? Box { get; init; }
        public Measured? BoxContent { get; init; }
        public double BoxOpacity { get; init; }
        public double Indent { get; init; }
        public double Width { get; init; }
        public required double Height { get; init; }
    }

    internal readonly struct Measured
    {
        public required List<CellItem> Items { get; init; }

        public required double Height { get; init; }

        public required LayoutCaptureContext Capture { get; init; }
    }

    public static LaidOutBoxContent Layout(
        BlockCollection blocks,
        Rect contentBox,
        HorizontalAlignment align,
        VerticalAlignment vAlign,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture)
        => Position(
            Measure(blocks, contentBox.Width, align, fonts, measureImage, resolution, capture),
            contentBox,
            align,
            vAlign);

    public static Measured Measure(
        BlockCollection blocks,
        double contentWidth,
        HorizontalAlignment? align,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture)
    {
        var engine = FlowPlacementEngine.ForBox(
            contentWidth,
            align,
            fonts,
            measureImage,
            resolution,
            capture);
        foreach (var block in BlockExpander.ExpandBlocks(blocks, contentWidth, resolution))
        {
            engine.Place(block, 0);
        }

        return new Measured
        {
            Items = engine.Items,
            Height = engine.Cursor,
            Capture = capture,
        };
    }

    public static LaidOutBoxContent Position(
        in Measured measured,
        Rect contentBox,
        HorizontalAlignment align,
        VerticalAlignment vAlign)
    {
        var capture = measured.Capture;
        var factor = vAlign switch
        {
            VerticalAlignment.Middle => 0.5,
            VerticalAlignment.Bottom => 1.0,
            _ => 0.0,
        };
        var offset = (contentBox.Height - measured.Height) * factor;

        var lines = new List<LaidOutLine>();
        var laidImages = new List<LaidOutImage>();
        var laidCodeSymbols = new List<LaidOutCodeSymbol>();
        var nestedTables = new List<LaidOutTablePlacement>();
        var nestedBoxes = new List<LaidOutBox>();
        var order = 0;
        var cursorY = contentBox.Top + offset;
        foreach (var item in measured.Items)
        {
            if (item.MarkerLine is { } marker && item.MarkerSource is { } markerSource)
            {
                lines.Add(new LaidOutLine
                {
                    Line = marker,
                    Source = capture.Source(markerSource),
                    X = contentBox.Left,
                    Y = cursorY,
                    ZOrder = order++,
                });
            }

            if (item.Line is { } line && item.Source is { } source)
            {
                lines.Add(new LaidOutLine
                {
                    Line = line,
                    Source = capture.Source(source),
                    X = contentBox.Left,
                    Y = cursorY,
                    ZOrder = order++,
                });
            }
            else if (item.Image is { } image)
            {
                var availableWidth = Math.Max(0, contentBox.Width - item.Indent);
                laidImages.Add(new LaidOutImage
                {
                    Source = capture.Source(image),
                    Paint = GeometryCapture.Image(image, capture),
                    X = contentBox.Left + item.Indent
                        + HorizontalAlignmentOffset.Of(Effective(image.Alignment, align), availableWidth, item.Width),
                    Y = cursorY,
                    Width = item.Width,
                    Height = item.Height,
                    ZOrder = order++,
                });
            }
            else if (item.CodeSymbol is { } codeSymbol)
            {
                var availableWidth = Math.Max(0, contentBox.Width - item.Indent);
                laidCodeSymbols.Add(new LaidOutCodeSymbol
                {
                    Source = capture.Source(codeSymbol),
                    Modules = CodeSymbolDispatch.Modules(codeSymbol),
                    Width = item.Width,
                    Height = item.Height,
                    Caption = item.Caption,
                    X = contentBox.Left + item.Indent
                        + HorizontalAlignmentOffset.Of(Effective(BlockAlignment(codeSymbol), align), availableWidth, item.Width),
                    Y = cursorY,
                    ZOrder = order++,
                });
            }
            else if (item.Table is { } nested)
            {
                nestedTables.Add(new LaidOutTablePlacement
                {
                    Layout = nested,
                    X = contentBox.Left,
                    Y = cursorY,
                    ZOrder = order++,
                });
            }
            else if (item.Box is { } box && item.BoxContent is { } boxContent)
            {
                var padding = box.Padding.Point;
                var availableWidth = Math.Max(0, contentBox.Width - item.Indent);
                var indent = item.Indent + Math.Max(0, HorizontalAlignmentOffset.Of(box.Alignment, availableWidth, item.Width));
                var innerBox = new Rect(padding, padding, Math.Max(0, item.Width - (2 * padding)), boxContent.Height);
                nestedBoxes.Add(new LaidOutBox
                {
                    Id = capture.Node(),
                    Source = capture.Source(box),
                    Content = Position(boxContent, innerBox, HorizontalAlignment.Left, VerticalAlignment.Top),
                    Bounds = new Rect(contentBox.Left + indent, cursorY, item.Width, item.Height),
                    Style = GeometryCapture.Box(box, item.Width, item.Height, capture),
                    Padding = padding,
                    Opacity = item.BoxOpacity,
                    ZOrder = order++,
                });
            }

            cursorY += item.Height;
        }

        return new LaidOutBoxContent
        {
            Height = measured.Height,
            Lines = [.. lines],
            Images = [.. laidImages],
            CodeSymbols = [.. laidCodeSymbols],
            Tables = [.. nestedTables],
            Boxes = [.. nestedBoxes],
        };
    }

    private static HorizontalAlignment BlockAlignment(Block codeSymbol) => CodeSymbolDispatch.Alignment(codeSymbol);

    private static HorizontalAlignment Effective(HorizontalAlignment blockAlignment, HorizontalAlignment cellAlignment)
        => blockAlignment == HorizontalAlignment.Left ? cellAlignment : blockAlignment;
}
