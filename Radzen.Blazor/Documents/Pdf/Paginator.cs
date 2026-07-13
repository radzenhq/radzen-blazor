using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


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

    /// <summary>
    /// Page-space transform to apply to every draw this fragment produces (a rotated
    /// overlay container), or <see langword="null"/> for none.
    /// </summary>
    public Matrix? Transform { get; init; }

    /// <summary>
    /// Placement sequence within the section body, shared with <see cref="PositionedBox.Order"/>
    /// so page emission can interleave boxes and table fragments in document order.
    /// </summary>
    public int Order { get; init; }
}

// A Stack container placed as a first-class box (section body or header/footer band):
// the decoration paints through BoxRenderer and the laid-out content through
// TableEmitter.EmitBoxContent. Bounds is in content space (X from the content-box left,
// Y from the content-box top, Y == Bounds.Top). Style carries no ExtGState - the box's
// own opacity is registered per page by the emitter. Overlay containers keep the
// table-lowering path.
internal readonly struct PositionedBox
{
    public required Container Source { get; init; }

    public required LaidOutBoxContent Content { get; init; }

    public required Rect Bounds { get; init; }

    public required BoxStyle Style { get; init; }

    public required double Y { get; init; }

    public required double Opacity { get; init; }

    /// <summary>
    /// Page-space transform to apply to every draw this box produces (a rotated
    /// container), or <see langword="null"/> for none.
    /// </summary>
    public Matrix? Transform { get; init; }

    public int Order { get; init; }
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

    public IReadOnlyList<PositionedBox> HeaderBoxes { get; init; } = [];

    public IReadOnlyList<PositionedBox> FooterBoxes { get; init; } = [];

    public IReadOnlyList<PositionedTableFragment> Tables { get; init; } = [];

    public IReadOnlyList<PositionedBox> Boxes { get; init; } = [];

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
        System.Func<Image, double, (double Width, double Height)>? measureImage = null,
        StyleResolution? resolution = null)
    {
        var pages = new List<PaginatedPage>();
        foreach (var section in document.Sections)
        {
            PaginateSection(section, fonts, pages, measureImage, resolution ?? StyleResolution.Empty);
        }

        return pages;
    }

    public static IReadOnlyList<PaginatedPage> Paginate(
        Section section,
        FontCollection fonts,
        System.Func<Image, double, (double Width, double Height)>? measureImage = null,
        StyleResolution? resolution = null,
        System.Collections.Generic.IReadOnlyDictionary<string, int>? tocPages = null)
    {
        var pages = new List<PaginatedPage>();
        PaginateSection(section, fonts, pages, measureImage, resolution ?? StyleResolution.Empty, tocPages);
        return pages;
    }

    private static void PaginateSection(
        Section section,
        FontCollection fonts,
        List<PaginatedPage> pages,
        System.Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution,
        System.Collections.Generic.IReadOnlyDictionary<string, int>? tocPages = null)
    {
        var (pageWidth, pageHeight) = EffectiveSize(section);
        var left = section.Margins.Left.Point;
        var top = section.Margins.Top.Point;
        var right = section.Margins.Right.Point;
        var bottom = section.Margins.Bottom.Point;

        var contentWidth = pageWidth - left - right;
        var size = new PageSize(Unit.FromPoint(pageWidth), Unit.FromPoint(pageHeight));

        var header = LayoutBand(section.Header, contentWidth, fonts, measureImage, resolution);
        var footer = LayoutBand(section.Footer, contentWidth, fonts, measureImage, resolution);

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
        List<PositionedBox> currentBoxes = [];
        List<PositionedImage> currentImages = [];
        List<PositionedCode> currentCodes = [];

        bool HasPageContent() => current.Count > 0 || currentTables.Count > 0 || currentBoxes.Count > 0 || currentImages.Count > 0 || currentCodes.Count > 0;

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
                HeaderBoxes = header.Boxes,
                FooterBoxes = footer.Boxes,
                Tables = currentTables,
                Boxes = currentBoxes,
                Images = currentImages,
                Codes = currentCodes,
                HeaderCodes = header.Codes,
                FooterCodes = footer.Codes,
            });
            current = [];
            currentTables = [];
            currentBoxes = [];
            currentImages = [];
            currentCodes = [];
        }

        double cursor = 0;

        // List blocks expand to hanging-indented marker paragraphs before layout so the rest of
        // the pipeline sees only paragraphs; a section with no lists returns its blocks unchanged.
        // Containers stay unlowered: Stack containers are placed as first-class boxes by
        // PlaceBox and overlay ones by PlaceSpecialContainer.
        var blocks = ExpandBlocks(section.Blocks, contentWidth, keepSpecialContainers: true, tocPages, fonts);

        // A LaidOutTable is expensive (it line-breaks every cell), so each Table block is
        // laid out at most once and shared between the KeepWithNext look-ahead and PlaceTable.
        var tableLayouts = new LaidOutTable?[blocks.Count];

        // Stack container content is likewise measured at most once and shared between the
        // KeepWithNext look-ahead and PlaceBox.
        var boxMeasures = new BoxContentLayout.Measured?[blocks.Count];

        // Placement sequence shared by table fragments and boxes so page emission can
        // interleave them in document order.
        var order = 0;

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

            var tableOrder = order++;
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
                    Order = tableOrder,
                });
                cursor += fragments[f].Height;
            }
        }

        // A Stack container is ONE unbreakable unit placed as a first-class box: its content
        // lays out through BoxContentLayout (the cell primitive) at the box's inner width,
        // and the whole box moves to the next page when it does not fit - it never splits.
        // A rotated container additionally carries a page-space rotation about the box center.
        void PlaceBox(int index, Container container)
        {
            var padding = container.Padding.Point;
            var boxWidth = container.Width?.Point ?? contentWidth;
            var indent = System.Math.Max(0, AlignImage(container.Alignment, contentWidth, boxWidth));
            var measured = boxMeasures[index] ??= MeasureBox(container, contentWidth, fonts, measureImage);
            var boxHeight = measured.Height + (2 * padding);

            if (HasPageContent() && cursor + boxHeight > contentHeight + Eps)
            {
                Flush();
                cursor = 0;
            }

            Matrix? transform = null;
            if (container.Rotation != 0)
            {
                var centerX = left + indent + boxWidth / 2;
                var centerY = pageHeight - contentTop - cursor - boxHeight / 2;
                transform = Matrix.Translate(-centerX, -centerY)
                    * Matrix.Rotate(container.Rotation)
                    * Matrix.Translate(centerX, centerY);
            }

            currentBoxes.Add(BuildBox(container, measured, contentWidth, cursor, order++, transform));
            cursor += boxHeight;
        }

        // An overlay container is placed as one unit that never breaks across pages.
        // Overlay children each lower to their own single-cell table sharing the box
        // origin (declaration order = paint order); the box decoration, when present, is a
        // separate empty table sized to the tallest child so it spans the whole box. A
        // rotated overlay additionally carries a page-space rotation about the box center.
        void PlaceSpecialContainer(Container container)
        {
            var placements = new List<(Table Table, LaidOutTable Layout)>();
            var boxWidth = container.Width?.Point ?? contentWidth;
            var indent = System.Math.Max(0, AlignImage(container.Alignment, contentWidth, boxWidth));
            var boxLeft = left + indent;

            var boxHeight = 2 * container.Padding.Point;
            foreach (var child in container.Blocks)
            {
                var table = OverlayChildTable(container, child, boxWidth, indent);
                var layout = TableLayout.Layout(table, boxWidth, fonts, measureImage);
                boxHeight = System.Math.Max(boxHeight, layout.Height);
                placements.Add((table, layout));
            }

            if (container.Background is not null || container.Borders.Top.IsSet
                || container.Borders.Right.IsSet || container.Borders.Bottom.IsSet
                || container.Borders.Left.IsSet)
            {
                var decoration = OverlayDecorationTable(container, boxWidth, indent, boxHeight);
                placements.Insert(0, (decoration, TableLayout.Layout(decoration, boxWidth, fonts, measureImage)));
            }

            if (HasPageContent() && cursor + boxHeight > contentHeight + Eps)
            {
                Flush();
                cursor = 0;
            }

            Matrix? transform = null;
            if (container.Rotation != 0)
            {
                var centerX = boxLeft + boxWidth / 2;
                var centerY = pageHeight - contentBox.Y - cursor - boxHeight / 2;
                transform = Matrix.Translate(-centerX, -centerY)
                    * Matrix.Rotate(container.Rotation)
                    * Matrix.Translate(centerX, centerY);
            }

            var boxOrder = order++;
            foreach (var (table, layout) in placements)
            {
                foreach (var fragment in TablePaginator.Paginate(layout, table, double.PositiveInfinity))
                {
                    currentTables.Add(new PositionedTableFragment
                    {
                        Layout = layout,
                        Fragment = fragment,
                        Y = cursor,
                        Transform = transform,
                        Order = boxOrder,
                    });
                }
            }

            cursor += boxHeight;
        }

        var broken = new IReadOnlyList<LineBox>?[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] is Paragraph paragraph)
            {
                broken[i] = LineBreaker.Break(paragraph, contentWidth, fonts, resolution.Alignment(paragraph));
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

            if (block is Container container)
            {
                if (IsSpecial(container))
                {
                    PlaceSpecialContainer(container);
                }
                else
                {
                    PlaceBox(i, container);
                }

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

                // A SpacingBefore taller than the page would drop the first line below the bottom
                // and lose it; on an otherwise-empty page clamp it so the first line still fits.
                if (first && !HasPageContent())
                {
                    var maxTop = contentHeight - lines[offset].Height;
                    if (maxTop >= 0 && blockTop > maxTop)
                    {
                        blockTop = maxTop;
                    }
                }

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
                        NextBlockFirstHeight(blocks, broken, tableLayouts, boxMeasures, i, contentWidth, fonts, measureImage, out var nextSpacingBefore, out var nextHeight))
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

                // A page must always make progress: placing zero lines (e.g. Orphans=0 with a
                // Widows pull-up) would flush a spurious blank page, so treat it as move-whole.
                if (placeCount == 0 && !moveWhole)
                {
                    moveWhole = true;
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

    internal static IReadOnlyList<Block> ExpandBlocks(
        BlockCollection blocks,
        double availableWidth,
        bool keepSpecialContainers = false,
        System.Collections.Generic.IReadOnlyDictionary<string, int>? tocPages = null,
        FontCollection? fonts = null)
    {
        var needsExpansion = false;
        foreach (var block in blocks)
        {
            if (block is List or Container or TableOfContents)
            {
                needsExpansion = true;
                break;
            }
        }

        if (!needsExpansion)
        {
            return blocks;
        }

        var expanded = new List<Block>(blocks.Count);
        foreach (var block in blocks)
        {
            if (block is List list)
            {
                ExpandList(list, expanded, 0, null);
            }
            else if (block is Container container)
            {
                // A Stack container is never lowered anymore: the section body and the
                // header/footer bands place it as a first-class box and cell/box content
                // nests it as a first-class nested box (BoxContentLayout). Overlay and
                // rotated containers are only allowed as direct section content
                // (keepSpecialContainers: true), where PlaceSpecialContainer/PlaceBox
                // handle them - nested content cannot host a page-space transform.
                if (!keepSpecialContainers && (IsSpecial(container) || container.Rotation != 0))
                {
                    throw new System.NotSupportedException(
                        "Overlay and rotated containers are only supported as direct section content.");
                }

                expanded.Add(container);
            }
            else if (block is TableOfContents toc)
            {
                if (!keepSpecialContainers)
                {
                    throw new System.NotSupportedException(
                        "A table of contents is only supported as direct section content.");
                }

                ExpandTableOfContents(toc, expanded, availableWidth, tocPages, fonts);
            }
            else
            {
                expanded.Add(block);
            }
        }

        return expanded;
    }

    // The page-number column is sized for this placeholder (plus a small safety margin) and
    // pass 1 renders it in place of the not-yet-known number, so the wrap fit of every entry
    // line is identical in both layout passes regardless of the resolved digits.
    private const string TocPagePlaceholder = "0000";

    private const double TocLeaderGap = 2.0;

    // A stop far beyond any line keeps a tab off the default 36pt grid when the entry text
    // reaches past the page-number stop: the number word then wraps in both passes alike
    // instead of depending on its (pass-varying) width against the grid.
    private const double TocSentinelStop = 100000;

    // Lowers a TableOfContents to one Paragraph per entry: linked text, a measured run of
    // leader characters and the page number right-aligned at a tab stop. See the remarks on
    // TableOfContents for why entries lower to paragraphs rather than a table.
    private static void ExpandTableOfContents(
        TableOfContents toc,
        List<Block> expanded,
        double availableWidth,
        System.Collections.Generic.IReadOnlyDictionary<string, int>? tocPages,
        FontCollection? fonts)
    {
        if (fonts is null)
        {
            throw new System.InvalidOperationException("A table of contents requires font metrics to lower.");
        }

        foreach (var entry in toc.Entries)
        {
            expanded.Add(LowerTocEntry(toc, entry, availableWidth, tocPages, fonts));
        }
    }

    private static Paragraph LowerTocEntry(
        TableOfContents toc,
        TocEntry entry,
        double availableWidth,
        System.Collections.Generic.IReadOnlyDictionary<string, int>? tocPages,
        FontCollection fonts)
    {
        var indent = toc.LevelIndent.Point * entry.Level;
        var max = availableWidth - indent;
        var reserve = fonts.MeasureText(TocPagePlaceholder, toc.Font) + 2;
        var stop = System.Math.Max(0, max - reserve);

        var paragraph = new Paragraph { LeftIndent = Unit.FromPoint(indent) };
        paragraph.Font.InheritFrom(toc.Font);
        paragraph.TabStops.AddTabStop(Unit.FromPoint(stop), TabAlignment.Right);
        paragraph.TabStops.AddTabStop(Unit.FromPoint(TocSentinelStop));

        var text = SanitizeTocText(entry.Text);
        var textRun = paragraph.Inlines.Add(text);
        textRun.LinkToAnchor = entry.Anchor;
        textRun.Font.InheritFrom(toc.Font);

        var leaderWidth = fonts.MeasureText(toc.Leader.ToString(), toc.Font);
        if (leaderWidth > 0)
        {
            var textWidth = fonts.MeasureText(text, toc.Font);
            var spaceWidth = fonts.MeasureText(" ", toc.Font);
            var count = (int)System.Math.Floor((stop - TocLeaderGap - textWidth - spaceWidth) / leaderWidth);
            if (count >= 1)
            {
                paragraph.Inlines.Add(" " + new string(toc.Leader, count)).Font.InheritFrom(toc.Font);
            }
        }

        paragraph.Inlines.Add("\t").Font.InheritFrom(toc.Font);

        var number = tocPages is not null && tocPages.TryGetValue(entry.Anchor, out var page)
            ? page.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : TocPagePlaceholder;
        var numberRun = paragraph.Inlines.Add(number);
        numberRun.LinkToAnchor = entry.Anchor;
        numberRun.Font.InheritFrom(toc.Font);

        return paragraph;
    }

    // Tabs and line breaks in entry text would defeat the single-line tab layout; they flatten
    // to spaces.
    private static string SanitizeTocText(string text)
    {
        if (text.IndexOfAny(['\t', '\r', '\n']) < 0)
        {
            return text;
        }

        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is '\t' or '\r' or '\n')
            {
                chars[i] = ' ';
            }
        }

        return new string(chars);
    }

    private static bool IsSpecial(Container container)
        => container.Layout == ContainerLayout.Overlay;

    private static Table OverlayChildTable(Container container, Block child, double boxWidth, double indent)
    {
        var table = new Table { LeftIndent = Unit.FromPoint(indent) };
        table.Columns.Add(Unit.FromPoint(boxWidth));
        var cell = table.Rows.Add().Cells[0];
        cell.Padding = container.Padding;
        cell.Blocks.Add(child);
        return table;
    }

    // Background and borders of an overlay box are drawn by an empty single-cell table
    // whose height is forced through the top padding (empty content + padding = box height).
    private static Table OverlayDecorationTable(Container container, double boxWidth, double indent, double boxHeight)
    {
        var table = new Table { LeftIndent = Unit.FromPoint(indent) };
        table.Columns.Add(Unit.FromPoint(boxWidth));
        var cell = table.Rows.Add().Cells[0];
        cell.Background = container.Background;
        cell.CornerRadius = container.CornerRadius;
        cell.PaddingTop = Unit.FromPoint(boxHeight);
        CopyEdge(container.Borders.Top, cell.Borders.Top);
        CopyEdge(container.Borders.Right, cell.Borders.Right);
        CopyEdge(container.Borders.Bottom, cell.Borders.Bottom);
        CopyEdge(container.Borders.Left, cell.Borders.Left);
        return table;
    }

    private static void CopyEdge(Border source, Border target)
    {
        target.Width = source.Width;
        target.Color = source.Color;
        target.Style = source.Style;
    }

    // Each nesting level shifts the marker column by the parent's LeftIndent + HangingIndent and
    // inherits the parent item's resolved font, so nested runs cascade item -> list -> parent item.
    private static void ExpandList(List list, List<Block> expanded, double indent, Font? inherited)
    {
        for (var i = 0; i < list.Items.Count; i++)
        {
            var paragraph = ExpandItem(list, i, indent, inherited);
            expanded.Add(paragraph);
            if (list.Items[i].NestedList is { } nested)
            {
                ExpandList(nested, expanded, indent + list.LeftIndent.Point + list.HangingIndent.Point, paragraph.EffectiveFont);
            }
        }
    }

    private static Paragraph ExpandItem(List list, int index, double indent, Font? inherited)
    {
        var item = list.Items[index];

        // StyleResolver resolves the marker and run fonts through the full cascade (including the
        // surrounding cell/row/table context and the Normal default); fall back to the item/list
        // fonts only when the resolver has not run (nested items always take this path).
        var paragraph = new Paragraph
        {
            LeftIndent = Unit.FromPoint(indent + list.LeftIndent.Point + list.HangingIndent.Point),
            MarkerIndent = Unit.FromPoint(indent + list.LeftIndent.Point),
            MarkerText = Marker(list, index),
            EffectiveFont = StyleResolver.ItemFont(item) ?? ItemFont(item, list, inherited),
            // Null unless the tree was built for tagged output; carries the item's Lbl/LBody.
            ListLabelElement = item.LabelElement,
            ListBodyElement = item.BodyElement,
        };

        foreach (var run in item.Inlines)
        {
            run.EffectiveFont ??= RunFont(run, item, list, inherited);
            paragraph.Inlines.Add(run);
        }

        return paragraph;
    }

    private static Font ItemFont(ListItem item, List list, Font? inherited)
    {
        var font = new Font();
        font.InheritFrom(item.Font);
        font.InheritFrom(list.Font);
        if (inherited != null)
        {
            font.InheritFrom(inherited);
        }

        return font;
    }

    private static Font RunFont(Run run, ListItem item, List list, Font? inherited)
    {
        var font = new Font();
        font.InheritFrom(run.Font);
        font.InheritFrom(item.Font);
        font.InheritFrom(list.Font);
        if (inherited != null)
        {
            font.InheritFrom(inherited);
        }

        return font;
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

    // Measures a Stack container's content at the box's inner width (box width minus the
    // padding on both sides), with the same null alignment the lowered single-cell table
    // resolved for its synthetic cell.
    private static BoxContentLayout.Measured MeasureBox(
        Container container,
        double contentWidth,
        FontCollection fonts,
        System.Func<Image, double, (double Width, double Height)>? measureImage)
    {
        var innerWidth = System.Math.Max(0, (container.Width?.Point ?? contentWidth) - (2 * container.Padding.Point));
        return BoxContentLayout.Measure(container.Blocks, innerWidth, null, fonts, measureImage);
    }

    // Positions a measured Stack container as a first-class box at y. Content is
    // positioned box-local (Y from the box top); the emitter shifts it by the box's
    // page Y. Align/vAlign match the lowered single-cell table's defaults.
    private static PositionedBox BuildBox(
        Container container,
        BoxContentLayout.Measured measured,
        double availableWidth,
        double y,
        int order,
        Matrix? transform)
    {
        var padding = container.Padding.Point;
        var boxWidth = container.Width?.Point ?? availableWidth;
        var indent = System.Math.Max(0, AlignImage(container.Alignment, availableWidth, boxWidth));
        var innerWidth = System.Math.Max(0, boxWidth - (2 * padding));
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

    // The required height of the header rows plus the first body ROW GROUP: the minimum a
    // table needs on a page before its first fragment breaks early. The first group is the
    // rowspan closure TablePaginator force-places as one unit, so it must be measured whole
    // or the flush check would let a tall group spill past the page bottom.
    private static double TableFirstFragmentHeight(Table table, LaidOutTable layout)
        => TablePaginator.FirstBodyGroupHeight(layout, table);

    // The height the NEXT block needs at the top of a page: the first line of a
    // paragraph, the header rows plus first body row of a table, or a whole image.
    private static bool NextBlockFirstHeight(
        IReadOnlyList<Block> blocks,
        IReadOnlyList<LineBox>?[] broken,
        LaidOutTable?[] tableLayouts,
        BoxContentLayout.Measured?[] boxMeasures,
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
            case Container container when !IsSpecial(container):
                // A Stack container never splits, so its first height is the whole box.
                var measured = boxMeasures[next] ??= MeasureBox(container, contentWidth, fonts, measureImage);
                height = measured.Height + (2 * container.Padding.Point);
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

        public List<PositionedBox> Boxes { get; } = [];

        public double Height { get; set; }
    }

    private static BandLayout LayoutBand(
        HeaderFooter band,
        double width,
        FontCollection fonts,
        System.Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution)
    {
        var result = new BandLayout();
        var images = result.Images;
        double cursor = 0;
        // Placement sequence shared by band table fragments and band boxes so page
        // emission can interleave them in document order.
        var order = 0;
        // Lists expand to marker paragraphs exactly as in section content.
        foreach (var block in ExpandBlocks(band.Blocks, width))
        {
            // A Stack container in a band is a first-class box, like the section body;
            // a band never page-breaks, so the box places whole at the running cursor.
            if (block is Container container)
            {
                var measured = MeasureBox(container, width, fonts, measureImage);
                var box = BuildBox(container, measured, width, cursor, order++, transform: null);
                result.Boxes.Add(box);
                cursor += box.Bounds.Height;
                continue;
            }

            if (block is Table table)
            {
                var layout = TableLayout.Layout(table, System.Math.Max(0, width - table.LeftIndent.Point), fonts, measureImage);
                var tableOrder = order++;
                foreach (var fragment in TablePaginator.Paginate(layout, table, double.PositiveInfinity))
                {
                    result.Tables.Add(new PositionedTableFragment
                    {
                        Layout = layout,
                        Fragment = fragment,
                        Y = cursor,
                        Order = tableOrder,
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

            var lines = LineBreaker.Break(paragraph, width, fonts, resolution.Alignment(paragraph));
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
