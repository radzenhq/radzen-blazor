using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

internal readonly struct LaidOutBoxContent
{
    public required double Height { get; init; }

    public required IReadOnlyList<LaidOutLine> Lines { get; init; }

    public required IReadOnlyList<LaidOutImage> Images { get; init; }

    public required IReadOnlyList<LaidOutCode> Codes { get; init; }

    public required IReadOnlyList<LaidOutNestedTable> Tables { get; init; }

    public required IReadOnlyList<LaidOutNestedBox> Boxes { get; init; }
}

// Measures and positions a block sequence inside a fixed-width content box. Table cells
// and (later) containers share this single primitive. Split into Measure/Position so
// TableLayout can measure every cell once, solve row heights, and only then position -
// a single-shot Layout would re-run measurement (and the measureImage callback) per cell.
internal static class BoxContentLayout
{
    internal readonly struct CellItem
    {
        public LineBox? Line { get; init; }
        public Block? Source { get; init; }
        public Image? Image { get; init; }
        public Block? Code { get; init; }
        public LaidOutTable? Table { get; init; }
        public Container? Box { get; init; }
        public Measured? BoxContent { get; init; }
        public double Width { get; init; }
        public required double Height { get; init; }
    }

    internal readonly struct Measured
    {
        public required List<CellItem> Items { get; init; }

        public required double Height { get; init; }
    }

    public static LaidOutBoxContent Layout(
        BlockCollection blocks,
        Rect contentBox,
        HorizontalAlignment align,
        VerticalAlignment vAlign,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage)
        => Position(Measure(blocks, contentBox.Width, align, fonts, measureImage), contentBox, align, vAlign);

    public static Measured Measure(
        BlockCollection blocks,
        double contentWidth,
        HorizontalAlignment? align,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage)
    {
        var visitor = new MeasureVisitor(contentWidth, align, fonts, measureImage);
        // Lists expand to marker paragraphs exactly as in section content.
        foreach (var block in Paginator.ExpandBlocks(blocks, contentWidth))
        {
            block.Accept(visitor, default);
        }

        return new Measured { Items = visitor.Items, Height = visitor.Height };
    }

    // Measures each block into the flat CellItem list, accumulating the running height.
    // A page break inside a cell is a no-op (a cell cannot break across pages by itself);
    // any other unsupported block type fails loud through Default. Lists and special
    // containers never reach here - ExpandBlocks expands or rejects them first.
    private sealed class MeasureVisitor(
        double contentWidth,
        HorizontalAlignment? align,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage)
        : BlockVisitor<Nothing, Nothing>
    {
        public List<CellItem> Items { get; } = [];

        public double Height { get; private set; }

        protected override Nothing Default(Block block, Nothing context)
            => throw new NotSupportedException($"Block type '{block.GetType().Name}' is not supported inside a table cell.");

        public override Nothing Visit(Paragraph paragraph, Nothing context)
        {
            var spacingBefore = paragraph.SpacingBefore.Point;
            if (spacingBefore > 0)
            {
                Items.Add(new CellItem { Height = spacingBefore });
                Height += spacingBefore;
            }

            foreach (var line in LineBreaker.Break(paragraph, contentWidth, fonts, align))
            {
                Items.Add(new CellItem { Line = line, Source = paragraph, Height = line.Height });
                Height += line.Height;
            }

            var spacingAfter = paragraph.SpacingAfter.Point;
            if (spacingAfter > 0)
            {
                Items.Add(new CellItem { Height = spacingAfter });
                Height += spacingAfter;
            }

            return default;
        }

        public override Nothing Visit(Image image, Nothing context)
        {
            var (imageWidth, imageHeight) = measureImage is null
                ? ImageDecoder.Measure(image, ImageDecoder.Decode(image.Data), contentWidth)
                : measureImage(image, contentWidth);
            Items.Add(new CellItem { Image = image, Width = imageWidth, Height = imageHeight });
            Height += imageHeight;
            return default;
        }

        public override Nothing Visit(QrCode block, Nothing context) => VisitCode(block);

        public override Nothing Visit(Barcode block, Nothing context) => VisitCode(block);

        private Nothing VisitCode(Block block)
        {
            var (codeWidth, codeHeight) = Paginator.MeasureCode(block);
            Items.Add(new CellItem { Code = block, Width = codeWidth, Height = codeHeight });
            Height += codeHeight;
            return default;
        }

        public override Nothing Visit(Table nested, Nothing context)
        {
            var layout = TableLayout.Layout(nested, Math.Max(0, contentWidth - nested.LeftIndent.Point), fonts, measureImage);
            Items.Add(new CellItem { Table = layout, Height = layout.Height });
            Height += layout.Height;
            return default;
        }

        public override Nothing Visit(Container container, Nothing context)
        {
            // A Stack container nests as a first-class box (ExpandBlocks throws for
            // overlay/rotated ones before this point): its content measures at the box's
            // inner width and the box adds the padding on both axes, exactly like the
            // synthetic single-cell table it used to lower to.
            var padding = container.Padding.Point;
            var boxWidth = container.Width?.Point ?? contentWidth;
            var inner = Measure(container.Blocks, Math.Max(0, boxWidth - (2 * padding)), null, fonts, measureImage);
            var boxHeight = inner.Height + (2 * padding);
            Items.Add(new CellItem { Box = container, BoxContent = inner, Width = boxWidth, Height = boxHeight });
            Height += boxHeight;
            return default;
        }

        public override Nothing Visit(PageBreak block, Nothing context) => default;
    }

    public static LaidOutBoxContent Position(
        in Measured measured,
        Rect contentBox,
        HorizontalAlignment align,
        VerticalAlignment vAlign)
    {
        var factor = vAlign switch
        {
            VerticalAlignment.Middle => 0.5,
            VerticalAlignment.Bottom => 1.0,
            _ => 0.0,
        };
        var offset = (contentBox.Height - measured.Height) * factor;

        var lines = new List<LaidOutLine>();
        var laidImages = new List<LaidOutImage>();
        var laidCodes = new List<LaidOutCode>();
        var nestedTables = new List<LaidOutNestedTable>();
        var nestedBoxes = new List<LaidOutNestedBox>();
        // Shared placement sequence so emission interleaves nested tables and boxes in
        // document order.
        var order = 0;
        var cursorY = contentBox.Top + offset;
        foreach (var item in measured.Items)
        {
            if (item.Line is { } line && item.Source is { } source)
            {
                lines.Add(new LaidOutLine
                {
                    Line = line,
                    Source = source,
                    X = contentBox.Left,
                    Y = cursorY,
                });
            }
            else if (item.Image is { } image)
            {
                laidImages.Add(new LaidOutImage
                {
                    Source = image,
                    X = contentBox.Left + ((contentBox.Width - item.Width) * AlignFactor(image.Alignment, align)),
                    Y = cursorY,
                    Width = item.Width,
                    Height = item.Height,
                });
            }
            else if (item.Code is { } code)
            {
                laidCodes.Add(new LaidOutCode
                {
                    Source = code,
                    X = contentBox.Left + ((contentBox.Width - item.Width) * AlignFactor(BlockAlignment(code), align)),
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
                // The box honors its OWN alignment (never the cell's), like the LeftIndent
                // the lowered single-cell table used to carry; content is positioned
                // box-local (X/Y from the box's top-left corner) with the lowered cell's
                // default alignment, and the emitter shifts it by the box position.
                var padding = box.Padding.Point;
                var indent = Math.Max(0, (contentBox.Width - item.Width) * AlignFactor(box.Alignment, HorizontalAlignment.Left));
                var innerBox = new Rect(padding, padding, Math.Max(0, item.Width - (2 * padding)), boxContent.Height);
                nestedBoxes.Add(new LaidOutNestedBox
                {
                    Source = box,
                    Content = Position(boxContent, innerBox, HorizontalAlignment.Left, VerticalAlignment.Top),
                    Bounds = new Rect(contentBox.Left + indent, cursorY, item.Width, item.Height),
                    Style = BoxStyle.FromContainer(box),
                    Radius = BoxRenderer.ClampRadius(box.CornerRadius.Point, item.Width, item.Height),
                    Opacity = box.Opacity,
                    Order = order++,
                });
            }

            cursorY += item.Height;
        }

        return new LaidOutBoxContent
        {
            Height = measured.Height,
            Lines = lines,
            Images = laidImages,
            Codes = laidCodes,
            Tables = nestedTables,
            Boxes = nestedBoxes,
        };
    }

    private static HorizontalAlignment BlockAlignment(Block code) => CodeBlockDispatch.Alignment(code);

    // Non-text content honors its OWN alignment, falling back to the cell's only when the
    // block leaves it at the default Left. The factor matches how text resolves alignment
    // (Right/End flush right, Center centered, Left/Start/Justify flush left).
    private static double AlignFactor(HorizontalAlignment blockAlignment, HorizontalAlignment cellAlignment)
        => (blockAlignment == HorizontalAlignment.Left ? cellAlignment : blockAlignment) switch
        {
            HorizontalAlignment.Right or HorizontalAlignment.End => 1.0,
            HorizontalAlignment.Center => 0.5,
            _ => 0.0,
        };
}
