using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

internal readonly struct PositionedLine
{
    public required LineBox Line { get; init; }

    public required Block Source { get; init; }

    public required double Y { get; init; }
}

internal readonly struct PositionedTableFragment
{
    public required LaidOutTable Layout { get; init; }

    public required TableFragment Fragment { get; init; }

    public required double Y { get; init; }
}

internal readonly struct PositionedImage
{
    public required Image Source { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    /// <summary>Horizontal offset of the image from the container left edge (alignment).</summary>
    public double XOffset { get; init; }
}

internal readonly struct PositionedCode
{
    public required Block Source { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    /// <summary>Horizontal offset of the code from the container left edge (alignment).</summary>
    public double XOffset { get; init; }
}

internal sealed class PaginatedPage
{
    public required PageSize Size { get; init; }

    public required Rect ContentBox { get; init; }

    public required int Number { get; init; }

    public required IReadOnlyList<PositionedLine> Lines { get; init; }

    public required IReadOnlyList<PositionedLine> Header { get; init; }

    public required IReadOnlyList<PositionedLine> Footer { get; init; }

    /// <summary>Header band top edge, measured down from the page top.</summary>
    public double HeaderTop { get; init; }

    /// <summary>Footer band top edge, measured down from the page top.</summary>
    public double FooterTop { get; init; }

    public IReadOnlyList<PositionedImage> HeaderImages { get; init; } = [];

    public IReadOnlyList<PositionedImage> FooterImages { get; init; } = [];

    public IReadOnlyList<PositionedTableFragment> HeaderTables { get; init; } = [];

    public IReadOnlyList<PositionedTableFragment> FooterTables { get; init; } = [];

    public IReadOnlyList<PositionedTableFragment> Tables { get; init; } = [];

    public IReadOnlyList<PositionedImage> Images { get; init; } = [];

    public IReadOnlyList<PositionedCode> Codes { get; init; } = [];

    public IReadOnlyList<PositionedCode> HeaderCodes { get; init; } = [];

    public IReadOnlyList<PositionedCode> FooterCodes { get; init; } = [];
}

internal static class Paginator
{
    private const double Eps = 1e-6;

    public static IReadOnlyList<PaginatedPage> Paginate(
        DocumentBuilder document,
        FontCollection fonts,
        System.Func<Image, double, (double Width, double Height)>? measureImage = null)
    {
        var pages = new List<PaginatedPage>();
        foreach (var section in document.Sections)
        {
            PaginateSection(section, fonts, pages, measureImage);
        }

        return pages;
    }

    public static IReadOnlyList<PaginatedPage> Paginate(
        Section section,
        FontCollection fonts,
        System.Func<Image, double, (double Width, double Height)>? measureImage = null)
    {
        var pages = new List<PaginatedPage>();
        PaginateSection(section, fonts, pages, measureImage);
        return pages;
    }

    private static void PaginateSection(
        Section section,
        FontCollection fonts,
        List<PaginatedPage> pages,
        System.Func<Image, double, (double Width, double Height)>? measureImage)
    {
        var (pageWidth, pageHeight) = EffectiveSize(section);
        var left = section.Margins.Left.Point;
        var top = section.Margins.Top.Point;
        var right = section.Margins.Right.Point;
        var bottom = section.Margins.Bottom.Point;

        var contentWidth = pageWidth - left - right;
        var size = new PageSize(Unit.FromPoint(pageWidth), Unit.FromPoint(pageHeight));

        var header = LayoutBand(section.Header, contentWidth, fonts, measureImage);
        var footer = LayoutBand(section.Footer, contentWidth, fonts, measureImage);

        // The header band starts HeaderDistance below the page top and the footer band
        // ends FooterDistance above the page bottom; a band whose extent exceeds its
        // margin shrinks the body so they never overlap.
        var headerDistance = section.HeaderDistance.Point;
        var footerDistance = section.FooterDistance.Point;
        var contentTop = System.Math.Max(top, header.Height > 0 ? headerDistance + header.Height : 0);
        var contentBottom = System.Math.Max(bottom, footer.Height > 0 ? footerDistance + footer.Height : 0);
        var contentBox = new Rect(left, contentTop, contentWidth, pageHeight - contentTop - contentBottom);
        var contentHeight = contentBox.Height;

        List<PositionedLine> current = [];
        List<PositionedTableFragment> currentTables = [];
        List<PositionedImage> currentImages = [];
        List<PositionedCode> currentCodes = [];

        bool HasPageContent() => current.Count > 0 || currentTables.Count > 0 || currentImages.Count > 0 || currentCodes.Count > 0;

        void Flush()
        {
            pages.Add(new PaginatedPage
            {
                Size = size,
                ContentBox = contentBox,
                Number = pages.Count + 1,
                Lines = current,
                Header = header.Lines,
                Footer = footer.Lines,
                HeaderTop = headerDistance,
                FooterTop = pageHeight - footerDistance - footer.Height,
                HeaderImages = header.Images,
                FooterImages = footer.Images,
                HeaderTables = header.Tables,
                FooterTables = footer.Tables,
                Tables = currentTables,
                Images = currentImages,
                Codes = currentCodes,
                HeaderCodes = header.Codes,
                FooterCodes = footer.Codes,
            });
            current = [];
            currentTables = [];
            currentImages = [];
            currentCodes = [];
        }

        double cursor = 0;

        // List blocks expand to hanging-indented marker paragraphs before layout so the rest of
        // the pipeline sees only paragraphs; a section with no lists returns its blocks unchanged.
        var blocks = ExpandBlocks(section.Blocks);

        // A LaidOutTable is expensive (it line-breaks every cell), so each Table block is
        // laid out at most once and shared between the KeepWithNext look-ahead and PlaceTable.
        var tableLayouts = new LaidOutTable?[blocks.Count];

        // A table starts at the current cursor; its first fragment gets the remaining
        // height and only breaks early when the repeating header plus the first body
        // row group cannot fit. Every later fragment starts a fresh page at full height.
        void PlaceTable(int index, Table table)
        {
            var layout = tableLayouts[index] ??= TableLayout.Layout(table, System.Math.Max(0, contentWidth - table.LeftIndent.Point), fonts, measureImage);

            if (HasPageContent() && cursor + TableFirstFragmentHeight(table, layout) > contentHeight + Eps)
            {
                Flush();
                cursor = 0;
            }

            var fragments = TablePaginator.Paginate(layout, table, contentHeight - cursor, contentHeight);
            for (var f = 0; f < fragments.Count; f++)
            {
                if (f > 0)
                {
                    Flush();
                    cursor = 0;
                }

                currentTables.Add(new PositionedTableFragment
                {
                    Layout = layout,
                    Fragment = fragments[f],
                    Y = cursor,
                });
                cursor += fragments[f].Height;
            }
        }

        var broken = new IReadOnlyList<LineBox>?[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] is Paragraph paragraph)
            {
                broken[i] = LineBreaker.Break(paragraph, contentWidth, fonts);
            }
        }

        var startPageCount = pages.Count;

        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block is PageBreak)
            {
                Flush();
                cursor = 0;
                continue;
            }

            if (block is Table table)
            {
                PlaceTable(i, table);
                continue;
            }

            if (block is Image image)
            {
                var (imageWidth, imageHeight) = measureImage is null ? MeasureImage(image, contentWidth) : measureImage(image, contentWidth);
                if (cursor + imageHeight > contentHeight + Eps && HasPageContent())
                {
                    Flush();
                    cursor = 0;
                }

                currentImages.Add(new PositionedImage
                {
                    Source = image,
                    Y = cursor,
                    Width = imageWidth,
                    Height = imageHeight,
                    XOffset = AlignImage(image.Alignment, contentWidth, imageWidth),
                });
                cursor += imageHeight;
                continue;
            }

            if (block is QrCode or Barcode)
            {
                var (codeWidth, codeHeight) = MeasureCode(block);
                if (cursor + codeHeight > contentHeight + Eps && HasPageContent())
                {
                    Flush();
                    cursor = 0;
                }

                currentCodes.Add(new PositionedCode
                {
                    Source = block,
                    Y = cursor,
                    Width = codeWidth,
                    Height = codeHeight,
                    XOffset = AlignImage(CodeAlignment(block), contentWidth, codeWidth),
                });
                cursor += codeHeight;
                continue;
            }

            if (block is not Paragraph para)
            {
                throw new System.NotSupportedException($"Block type '{block.GetType().Name}' is not supported in section content.");
            }

            if (broken[i] is not { } lines)
            {
                continue;
            }

            if (lines.Count == 0)
            {
                cursor += para.SpacingBefore.Point + para.SpacingAfter.Point;
                continue;
            }

            var spacingBefore = para.SpacingBefore.Point;
            var spacingAfter = para.SpacingAfter.Point;
            var offset = 0;
            var first = true;

            while (true)
            {
                var nrem = lines.Count - offset;
                var blockTop = cursor + (first ? spacingBefore : 0);

                var k = 0;
                var y = blockTop;
                while (offset + k < lines.Count && y + lines[offset + k].Height <= contentHeight + Eps)
                {
                    y += lines[offset + k].Height;
                    k++;
                }

                var moveWhole = false;
                int placeCount;

                if (k >= nrem)
                {
                    placeCount = nrem;
                    if (first && para.KeepWithNext && HasPageContent() &&
                        NextBlockFirstHeight(blocks, broken, tableLayouts, i, contentWidth, fonts, measureImage, out var nextSpacingBefore, out var nextHeight))
                    {
                        var afterCursor = blockTop + SumHeights(lines, offset, placeCount) + spacingAfter;
                        if (afterCursor + nextSpacingBefore + nextHeight > contentHeight + Eps)
                        {
                            moveWhole = true;
                        }
                    }
                }
                else if (first && para.KeepTogether)
                {
                    moveWhole = true;
                    placeCount = 0;
                }
                else if (!first)
                {
                    var kept = k;
                    if (nrem - kept < para.Widows)
                    {
                        kept = nrem - para.Widows;
                    }

                    // A continuation break still makes progress: never stall on an empty page
                    // (a line taller than the page is placed alone) and never strand < 1 line.
                    placeCount = kept >= 1 ? kept : (k > 0 || HasPageContent() ? k : 1);
                }
                else if (k < para.Orphans)
                {
                    moveWhole = true;
                    placeCount = 0;
                }
                else
                {
                    var kept = k;
                    if (nrem - kept < para.Widows)
                    {
                        kept = nrem - para.Widows;
                    }

                    if (kept < para.Orphans)
                    {
                        moveWhole = true;
                        placeCount = 0;
                    }
                    else
                    {
                        placeCount = kept;
                    }
                }

                if (moveWhole && !HasPageContent())
                {
                    moveWhole = false;
                    placeCount = k >= nrem ? nrem : (k > 0 ? k : 1);
                }

                if (moveWhole)
                {
                    Flush();
                    cursor = 0;
                    first = true;
                    continue;
                }

                var lineY = blockTop;
                for (var t = 0; t < placeCount; t++)
                {
                    var box = lines[offset + t];
                    current.Add(new PositionedLine { Line = box, Source = para, Y = lineY });
                    lineY += box.Height;
                }

                offset += placeCount;

                if (offset >= lines.Count)
                {
                    cursor = lineY + spacingAfter;
                    break;
                }

                Flush();
                cursor = 0;
                first = false;
            }
        }

        if (HasPageContent() || pages.Count == startPageCount)
        {
            Flush();
        }
    }

    private static (double Width, double Height) MeasureImage(Image image, double availableWidth)
        => ImageDecoder.Measure(image, ImageDecoder.Decode(image.Data), availableWidth);

    internal static (double Width, double Height) MeasureCode(Block block)
        => block switch
        {
            QrCode qr => (qr.Size.Point, qr.Size.Point),
            Barcode barcode => (barcode.Width.Point, barcode.Height.Point + barcode.TextBandHeight),
            _ => (0, 0),
        };

    private static HorizontalAlignment CodeAlignment(Block block)
        => block switch
        {
            QrCode qr => qr.Alignment,
            Barcode barcode => barcode.Alignment,
            _ => HorizontalAlignment.Left,
        };

    internal static IReadOnlyList<Block> ExpandBlocks(BlockCollection blocks)
    {
        var hasList = false;
        foreach (var block in blocks)
        {
            if (block is List)
            {
                hasList = true;
                break;
            }
        }

        if (!hasList)
        {
            return blocks;
        }

        var expanded = new List<Block>(blocks.Count);
        foreach (var block in blocks)
        {
            if (block is List list)
            {
                for (var i = 0; i < list.Items.Count; i++)
                {
                    expanded.Add(ExpandItem(list, i));
                }
            }
            else
            {
                expanded.Add(block);
            }
        }

        return expanded;
    }

    private static Paragraph ExpandItem(List list, int index)
    {
        var item = list.Items[index];

        var itemFont = new Font();
        itemFont.InheritFrom(item.Font);
        itemFont.InheritFrom(list.Font);

        var paragraph = new Paragraph
        {
            LeftIndent = Unit.FromPoint(list.LeftIndent.Point + list.HangingIndent.Point),
            MarkerIndent = list.LeftIndent,
            MarkerText = Marker(list, index),
            EffectiveFont = itemFont,
        };

        foreach (var run in item.Inlines)
        {
            var effective = new Font();
            effective.InheritFrom(run.Font);
            effective.InheritFrom(item.Font);
            effective.InheritFrom(list.Font);
            run.EffectiveFont = effective;
            paragraph.Inlines.Add(run);
        }

        return paragraph;
    }

    private const string BulletGlyph = "\u2022";

    private static string Marker(List list, int index)
        => list.Style == ListStyle.Number
            ? (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "."
            : BulletGlyph;

    private static double AlignImage(HorizontalAlignment alignment, double containerWidth, double imageWidth)
        => alignment switch
        {
            HorizontalAlignment.Center => (containerWidth - imageWidth) / 2.0,
            HorizontalAlignment.Right or HorizontalAlignment.End => containerWidth - imageWidth,
            _ => 0,
        };

    // The required height of the header rows plus the first body ROW GROUP: the minimum a
    // table needs on a page before its first fragment breaks early. The first group is the
    // rowspan closure TablePaginator force-places as one unit, so it must be measured whole
    // or the flush check would let a tall group spill past the page bottom.
    private static double TableFirstFragmentHeight(Table table, LaidOutTable layout)
    {
        double headerHeight = 0;
        List<int> bodies = [];
        for (var r = 0; r < table.Rows.Count; r++)
        {
            if (table.Rows[r].IsHeader)
            {
                headerHeight += layout.RowHeights[r];
            }
            else
            {
                bodies.Add(r);
            }
        }

        if (bodies.Count == 0)
        {
            return headerHeight;
        }

        var reach = new int[table.Rows.Count];
        for (var i = 0; i < reach.Length; i++)
        {
            reach[i] = i;
        }

        foreach (var cell in layout.Cells)
        {
            if (cell.RowSpan <= 1)
            {
                continue;
            }

            var end = cell.Row + cell.RowSpan - 1;
            for (var r = cell.Row; r <= end && r < reach.Length; r++)
            {
                reach[r] = System.Math.Max(reach[r], end);
            }
        }

        var last = 0;
        var groupEnd = reach[bodies[0]];
        var groupHeight = layout.RowHeights[bodies[0]];
        while (last + 1 < bodies.Count && bodies[last + 1] <= groupEnd)
        {
            last++;
            groupEnd = System.Math.Max(groupEnd, reach[bodies[last]]);
            groupHeight += layout.RowHeights[bodies[last]];
        }

        return headerHeight + groupHeight;
    }

    // The height the NEXT block needs at the top of a page: the first line of a
    // paragraph, the header rows plus first body row of a table, or a whole image.
    private static bool NextBlockFirstHeight(
        IReadOnlyList<Block> blocks,
        IReadOnlyList<LineBox>?[] broken,
        LaidOutTable?[] tableLayouts,
        int index,
        double contentWidth,
        FontCollection fonts,
        System.Func<Image, double, (double Width, double Height)>? measureImage,
        out double spacingBefore,
        out double height)
    {
        spacingBefore = 0;
        height = 0;
        var next = index + 1;
        if (next >= blocks.Count)
        {
            return false;
        }

        switch (blocks[next])
        {
            case Paragraph paragraph when broken[next] is { Count: > 0 } lines:
                spacingBefore = paragraph.SpacingBefore.Point;
                height = lines[0].Height;
                return true;
            case Table table:
                var layout = tableLayouts[next] ??= TableLayout.Layout(table, System.Math.Max(0, contentWidth - table.LeftIndent.Point), fonts, measureImage);
                height = TableFirstFragmentHeight(table, layout);
                return true;
            case Image image:
                var (_, imageHeight) = measureImage is null ? MeasureImage(image, contentWidth) : measureImage(image, contentWidth);
                height = imageHeight;
                return true;
            case QrCode or Barcode:
                height = MeasureCode(blocks[next]).Height;
                return true;
            default:
                return false;
        }
    }

    private static double SumHeights(IReadOnlyList<LineBox> lines, int start, int count)
    {
        double sum = 0;
        for (var i = 0; i < count; i++)
        {
            sum += lines[start + i].Height;
        }

        return sum;
    }

    private sealed class BandLayout
    {
        public List<PositionedLine> Lines { get; } = [];

        public List<PositionedImage> Images { get; } = [];

        public List<PositionedCode> Codes { get; } = [];

        public List<PositionedTableFragment> Tables { get; } = [];

        public double Height { get; set; }
    }

    private static BandLayout LayoutBand(
        HeaderFooter band,
        double width,
        FontCollection fonts,
        System.Func<Image, double, (double Width, double Height)>? measureImage)
    {
        var result = new BandLayout();
        var images = result.Images;
        double cursor = 0;
        // Lists expand to marker paragraphs exactly as in section content.
        foreach (var block in ExpandBlocks(band.Blocks))
        {
            if (block is Table table)
            {
                var layout = TableLayout.Layout(table, System.Math.Max(0, width - table.LeftIndent.Point), fonts, measureImage);
                foreach (var fragment in TablePaginator.Paginate(layout, table, double.PositiveInfinity))
                {
                    result.Tables.Add(new PositionedTableFragment
                    {
                        Layout = layout,
                        Fragment = fragment,
                        Y = cursor,
                    });
                    cursor += fragment.Height;
                }

                continue;
            }

            if (block is Image image)
            {
                var (imageWidth, imageHeight) = measureImage is null ? MeasureImage(image, width) : measureImage(image, width);
                images.Add(new PositionedImage
                {
                    Source = image,
                    Y = cursor,
                    Width = imageWidth,
                    Height = imageHeight,
                    XOffset = AlignImage(image.Alignment, width, imageWidth),
                });
                cursor += imageHeight;
                continue;
            }

            if (block is QrCode or Barcode)
            {
                var (codeWidth, codeHeight) = MeasureCode(block);
                result.Codes.Add(new PositionedCode
                {
                    Source = block,
                    Y = cursor,
                    Width = codeWidth,
                    Height = codeHeight,
                    XOffset = AlignImage(CodeAlignment(block), width, codeWidth),
                });
                cursor += codeHeight;
                continue;
            }

            if (block is PageBreak)
            {
                // A header/footer band cannot page-break, so a page break inside one is a no-op.
                continue;
            }

            if (block is not Paragraph paragraph)
            {
                throw new System.NotSupportedException($"Block type '{block.GetType().Name}' is not supported in a header/footer band.");
            }

            var lines = LineBreaker.Break(paragraph, width, fonts);
            var y = cursor + paragraph.SpacingBefore.Point;
            foreach (var box in lines)
            {
                result.Lines.Add(new PositionedLine { Line = box, Source = paragraph, Y = y });
                y += box.Height;
            }

            cursor = y + paragraph.SpacingAfter.Point;
        }

        result.Height = cursor;
        return result;
    }

    private static (double Width, double Height) EffectiveSize(Section section)
    {
        var width = section.PageSize.Width.Point;
        var height = section.PageSize.Height.Point;
        return section.Orientation == PageOrientation.Landscape ? (height, width) : (width, height);
    }
}
