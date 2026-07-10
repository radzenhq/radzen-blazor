using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

internal sealed class PositionedLine
{
    public required LineBox Line { get; init; }

    public required Block Source { get; init; }

    public required double Y { get; init; }
}

internal sealed class PositionedTableFragment
{
    public required LaidOutTable Layout { get; init; }

    public required TableFragment Fragment { get; init; }

    public required double Y { get; init; }
}

internal sealed class PositionedImage
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

    public IReadOnlyList<PositionedImage> HeaderImages { get; init; } = [];

    public IReadOnlyList<PositionedImage> FooterImages { get; init; } = [];

    public IReadOnlyList<PositionedTableFragment> Tables { get; init; } = [];

    public IReadOnlyList<PositionedImage> Images { get; init; } = [];
}

internal static class Paginator
{
    private const double Eps = 1e-6;

    public static IReadOnlyList<PaginatedPage> Paginate(
        DocumentBuilder document,
        FontCollection fonts,
        System.Func<Image, (double Width, double Height)>? measureImage = null)
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
        System.Func<Image, (double Width, double Height)>? measureImage = null)
    {
        var pages = new List<PaginatedPage>();
        PaginateSection(section, fonts, pages, measureImage);
        return pages;
    }

    private static void PaginateSection(
        Section section,
        FontCollection fonts,
        List<PaginatedPage> pages,
        System.Func<Image, (double Width, double Height)>? measureImage)
    {
        var (pageWidth, pageHeight) = EffectiveSize(section);
        var left = section.Margins.Left.Point;
        var top = section.Margins.Top.Point;
        var right = section.Margins.Right.Point;
        var bottom = section.Margins.Bottom.Point;

        var contentBox = new Rect(left, top, pageWidth - left - right, pageHeight - top - bottom);
        var size = new PageSize(Unit.FromPoint(pageWidth), Unit.FromPoint(pageHeight));
        var contentHeight = contentBox.Height;
        var contentWidth = contentBox.Width;

        var (header, headerImages) = LayoutBand(section.Header, contentWidth, fonts, measureImage);
        var (footer, footerImages) = LayoutBand(section.Footer, contentWidth, fonts, measureImage);

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
                Header = header,
                Footer = footer,
                HeaderImages = headerImages,
                FooterImages = footerImages,
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
            var layout = TableLayout.Layout(table, contentWidth, fonts, measureImage);

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

            if (HasPageContent() && cursor + headerHeight + firstBodyHeight > contentHeight + Eps)
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
                var (imageWidth, imageHeight) = measureImage is null ? MeasureImage(image) : measureImage(image);
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
                        NextFirstLine(blocks, broken, i, out var nextSpacingBefore, out var nextHeight))
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
                    placeCount = k;
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

    private static (double Width, double Height) MeasureImage(Image image)
        => ImageDecoder.Measure(image, ImageDecoder.Decode(image.Data));

    private static bool NextFirstLine(
        BlockCollection blocks,
        IReadOnlyList<LineBox>?[] broken,
        int index,
        out double spacingBefore,
        out double height)
    {
        spacingBefore = 0;
        height = 0;
        var next = index + 1;
        if (next >= blocks.Count || blocks[next] is not Paragraph paragraph ||
            broken[next] is not { Count: > 0 } lines)
        {
            return false;
        }

        spacingBefore = paragraph.SpacingBefore.Point;
        height = lines[0].Height;
        return true;
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

    private static (List<PositionedLine> Lines, List<PositionedImage> Images) LayoutBand(
        HeaderFooter band,
        double width,
        FontCollection fonts,
        System.Func<Image, (double Width, double Height)>? measureImage)
    {
        List<PositionedLine> result = [];
        List<PositionedImage> images = [];
        double cursor = 0;
        foreach (var block in band.Blocks)
        {
            if (block is Image image)
            {
                var (imageWidth, imageHeight) = measureImage is null ? MeasureImage(image) : measureImage(image);
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
                result.Add(new PositionedLine { Line = box, Source = paragraph, Y = y });
                y += box.Height;
            }

            cursor = y + paragraph.SpacingAfter.Point;
        }

        return (result, images);
    }

    private static (double Width, double Height) EffectiveSize(Section section)
    {
        var width = section.PageSize.Width.Point;
        var height = section.PageSize.Height.Point;
        return section.Orientation == PageOrientation.Landscape ? (height, width) : (width, height);
    }
}
