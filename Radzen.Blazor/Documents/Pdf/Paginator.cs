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

        bool HasPageContent() => current.Count > 0 || currentTables.Count > 0 || currentImages.Count > 0;

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
            });
            current = [];
            currentTables = [];
            currentImages = [];
        }

        double cursor = 0;

        // A table starts at the current cursor; its first fragment gets the remaining
        // height and only breaks early when the repeating header plus the first body
        // row cannot fit. Every later fragment starts a fresh page at full height.
        void PlaceTable(Table table)
        {
            var layout = TableLayout.Layout(table, System.Math.Max(0, contentWidth - table.LeftIndent.Point), fonts, measureImage);

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

        var blocks = section.Blocks;
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
                PlaceTable(table);
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
                });
                cursor += imageHeight;
                continue;
            }

            if (block is not Paragraph para || broken[i] is not { } lines)
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
                        NextBlockFirstHeight(blocks, broken, i, contentWidth, fonts, measureImage, out var nextSpacingBefore, out var nextHeight))
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
                    // A continuation line taller than the page still makes progress:
                    // place it alone on an empty page instead of looping forever.
                    placeCount = k > 0 || HasPageContent() ? k : 1;
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

    // The required height of the header rows plus the first body row: the minimum a
    // table needs on a page before its first fragment breaks early.
    private static double TableFirstFragmentHeight(Table table, LaidOutTable layout)
    {
        double headerHeight = 0;
        double firstBodyHeight = 0;
        var seenBody = false;
        for (var r = 0; r < table.Rows.Count; r++)
        {
            if (table.Rows[r].IsHeader)
            {
                headerHeight += layout.RowHeights[r];
            }
            else if (!seenBody)
            {
                firstBodyHeight = layout.RowHeights[r];
                seenBody = true;
            }
        }

        return headerHeight + firstBodyHeight;
    }

    // The height the NEXT block needs at the top of a page: the first line of a
    // paragraph, the header rows plus first body row of a table, or a whole image.
    private static bool NextBlockFirstHeight(
        BlockCollection blocks,
        IReadOnlyList<LineBox>?[] broken,
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
                var layout = TableLayout.Layout(table, System.Math.Max(0, contentWidth - table.LeftIndent.Point), fonts, measureImage);
                height = TableFirstFragmentHeight(table, layout);
                return true;
            case Image image:
                var (_, imageHeight) = measureImage is null ? MeasureImage(image, contentWidth) : measureImage(image, contentWidth);
                height = imageHeight;
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
        foreach (var block in band.Blocks)
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
                });
                cursor += imageHeight;
                continue;
            }

            if (block is not Paragraph paragraph)
            {
                continue;
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
