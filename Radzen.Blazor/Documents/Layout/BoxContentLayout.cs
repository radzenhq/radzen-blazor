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
        public ImmutableArray<PositionedCaptionLine>? Caption { get; init; }
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
        LayoutCaptureContext? capture = null)
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
        LayoutCaptureContext? capture = null)
    {
        capture ??= new LayoutCaptureContext();
        var visitor = new MeasureVisitor(contentWidth, align, fonts, measureImage, resolution, capture);
        foreach (var block in BlockExpander.ExpandBlocks(blocks, contentWidth, resolution))
        {
            BlockLayoutDispatch.Dispatch(block, visitor, default(Nothing));
        }

        return new Measured
        {
            Items = visitor.Items,
            Height = visitor.Height,
            Capture = capture,
        };
    }

    private sealed class MeasureVisitor(
        double contentWidth,
        HorizontalAlignment? align,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture)
        : IBlockLayoutHandler<Nothing, Nothing>
    {
        public List<CellItem> Items { get; } = [];

        public double Height { get; private set; }

        public Nothing Unsupported(Block block, Nothing context)
            => throw new NotSupportedException($"Block type '{block.GetType().Name}' is not supported inside a table cell.");

        private LineBox? Marker(Block block)
            => resolution.ListMarker(block) is { } marker
                ? LineLayouter.MarkerLine(marker, fonts, capture)
                : null;

        public Nothing Paragraph(Paragraph paragraph, Nothing context)
        {
            var format = resolution.Format(paragraph);
            var spacingBefore = format.SpacingBefore.Point;
            if (spacingBefore > 0)
            {
                Items.Add(new CellItem { Height = spacingBefore });
                Height += spacingBefore;
            }

            foreach (var line in LineBreaker.Break(
                paragraph,
                contentWidth,
                fonts,
                resolution.Alignment(paragraph) ?? align,
                resolution,
                capture))
            {
                Items.Add(new CellItem { Line = line, Source = paragraph, Height = line.Height });
                Height += line.Height;
            }

            var spacingAfter = format.SpacingAfter.Point;
            if (spacingAfter > 0)
            {
                Items.Add(new CellItem { Height = spacingAfter });
                Height += spacingAfter;
            }

            return default;
        }

        public Nothing Image(Image image, Nothing context)
        {
            var indent = resolution.BlockIndent(image);
            var (imageWidth, imageHeight) = FlowContentPlacer.MeasureImage(
                image,
                Math.Max(0, contentWidth - indent),
                measureImage);
            Items.Add(new CellItem
            {
                MarkerLine = Marker(image),
                MarkerSource = image,
                Image = image,
                Indent = indent,
                Width = imageWidth,
                Height = imageHeight,
            });
            Height += imageHeight;
            return default;
        }

        public Nothing CodeSymbol(Block block, Nothing context)
        {
            var (codeSymbolWidth, codeSymbolHeight) = Paginator.MeasureCodeSymbol(block, fonts, resolution);
            Items.Add(new CellItem
            {
                CodeSymbol = block,
                MarkerLine = Marker(block),
                MarkerSource = block,
                Caption = CodeSymbolDispatch.Caption(block, fonts, resolution),
                Indent = resolution.BlockIndent(block),
                Width = codeSymbolWidth,
                Height = codeSymbolHeight,
            });
            Height += codeSymbolHeight;
            return default;
        }

        public Nothing Table(Table nested, Nothing context)
        {
            var layout = BlockLayoutDispatch.LayoutTable(
                nested,
                contentWidth,
                fonts,
                measureImage,
                resolution,
                capture);
            Items.Add(new CellItem
            {
                MarkerLine = Marker(nested),
                MarkerSource = nested,
                Table = layout,
                Height = layout.Height,
            });
            Height += layout.Height;
            return default;
        }

        public Nothing Container(Container container, Nothing context)
        {
            var indent = resolution.BlockIndent(container);
            var availableWidth = Math.Max(0, contentWidth - indent);
            var padding = container.Padding.Point;
            var boxWidth = container.Width?.Point ?? availableWidth;
            var inner = Measure(
                container.Blocks,
                Math.Max(0, boxWidth - (2 * padding)),
                null,
                fonts,
                measureImage,
                resolution,
                capture);
            var boxHeight = inner.Height + (2 * padding);
            Items.Add(new CellItem
            {
                Box = container,
                MarkerLine = Marker(container),
                MarkerSource = container,
                BoxContent = inner,
                BoxOpacity = resolution.Opacities.ContainerOpacity(container),
                Indent = indent,
                Width = boxWidth,
                Height = boxHeight,
            });
            Height += boxHeight;
            return default;
        }

        public Nothing PageBreak(PageBreak block, Nothing context) => default;
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
        var nestedTables = new List<LaidOutNestedTable>();
        var nestedBoxes = new List<LaidOutNestedBox>();
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
                    Caption = item.Caption,
                    X = contentBox.Left + item.Indent
                        + HorizontalAlignmentOffset.Of(Effective(BlockAlignment(codeSymbol), align), availableWidth, item.Width),
                    Y = cursorY,
                });
            }
            else if (item.Table is { } nested)
            {
                nestedTables.Add(new LaidOutNestedTable
                {
                    Layout = nested,
                    X = contentBox.Left,
                    Y = cursorY,
                    Order = order++,
                });
            }
            else if (item.Box is { } box && item.BoxContent is { } boxContent)
            {
                var padding = box.Padding.Point;
                var availableWidth = Math.Max(0, contentBox.Width - item.Indent);
                var indent = item.Indent + Math.Max(0, HorizontalAlignmentOffset.Of(box.Alignment, availableWidth, item.Width));
                var innerBox = new Rect(padding, padding, Math.Max(0, item.Width - (2 * padding)), boxContent.Height);
                nestedBoxes.Add(new LaidOutNestedBox
                {
                    Source = capture.Source(box),
                    Content = Position(boxContent, innerBox, HorizontalAlignment.Left, VerticalAlignment.Top),
                    Bounds = new Rect(contentBox.Left + indent, cursorY, item.Width, item.Height),
                    Style = GeometryCapture.Box(box, item.Width, item.Height),
                    Radius = BoxStyle.ClampRadius(box.CornerRadius.Point, item.Width, item.Height),
                    Padding = padding,
                    Opacity = item.BoxOpacity,
                    Order = order++,
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
