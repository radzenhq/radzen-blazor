using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

internal readonly struct LaidOutBoxContent
{
    public required double Height { get; init; }

    public required IReadOnlyList<LaidOutLine> Lines { get; init; }

    public required IReadOnlyList<LaidOutImage> Images { get; init; }

    public required IReadOnlyList<LaidOutCode> Codes { get; init; }

    public required IReadOnlyList<LaidOutNestedTable> Tables { get; init; }
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
        System.Func<Image, double, (double Width, double Height)>? measureImage)
        => Position(Measure(blocks, contentBox.Width, align, fonts, measureImage), contentBox, align, vAlign);

    public static Measured Measure(
        BlockCollection blocks,
        double contentWidth,
        HorizontalAlignment? align,
        FontCollection fonts,
        System.Func<Image, double, (double Width, double Height)>? measureImage)
    {
        var items = new List<CellItem>();
        double height = 0;
        // Lists expand to marker paragraphs exactly as in section content.
        foreach (var block in Paginator.ExpandBlocks(blocks, contentWidth))
        {
            if (block is Paragraph paragraph)
            {
                var spacingBefore = paragraph.SpacingBefore.Point;
                if (spacingBefore > 0)
                {
                    items.Add(new CellItem { Height = spacingBefore });
                    height += spacingBefore;
                }

                foreach (var line in LineBreaker.Break(paragraph, contentWidth, fonts, align))
                {
                    items.Add(new CellItem { Line = line, Source = block, Height = line.Height });
                    height += line.Height;
                }

                var spacingAfter = paragraph.SpacingAfter.Point;
                if (spacingAfter > 0)
                {
                    items.Add(new CellItem { Height = spacingAfter });
                    height += spacingAfter;
                }
            }
            else if (block is Image image)
            {
                var (imageWidth, imageHeight) = measureImage is null
                    ? ImageDecoder.Measure(image, ImageDecoder.Decode(image.Data), contentWidth)
                    : measureImage(image, contentWidth);
                items.Add(new CellItem { Image = image, Width = imageWidth, Height = imageHeight });
                height += imageHeight;
            }
            else if (block is QrCode or Barcode)
            {
                var (codeWidth, codeHeight) = Paginator.MeasureCode(block);
                items.Add(new CellItem { Code = block, Width = codeWidth, Height = codeHeight });
                height += codeHeight;
            }
            else if (block is Table nested)
            {
                var layout = TableLayout.Layout(nested, System.Math.Max(0, contentWidth - nested.LeftIndent.Point), fonts, measureImage);
                items.Add(new CellItem { Table = layout, Height = layout.Height });
                height += layout.Height;
            }
            else if (block is PageBreak)
            {
                // A cell cannot break across pages by itself, so a page break inside one is a no-op.
            }
            else
            {
                throw new System.NotSupportedException($"Block type '{block.GetType().Name}' is not supported inside a table cell.");
            }
        }

        return new Measured { Items = items, Height = height };
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
        };
    }

    private static HorizontalAlignment BlockAlignment(Block code) => code switch
    {
        QrCode qr => qr.Alignment,
        Barcode barcode => barcode.Alignment,
        _ => HorizontalAlignment.Left,
    };

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
