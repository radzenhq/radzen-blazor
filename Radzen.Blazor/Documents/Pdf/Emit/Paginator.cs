using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Emit;


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
    /// Placement sequence within the section body, shared with <see cref="PositionedBox.Order"/>
    /// so page emission can interleave boxes and table fragments in document order.
    /// </summary>
    public int Order { get; init; }
}

// A container placed as a first-class box (section body or header/footer band): the
// decoration paints through BoxRenderer and the laid-out content through
// TableEmitter.EmitBoxContent. Bounds is in content space (X from the content-box left,
// Y from the content-box top, Y == Bounds.Top). Style carries no ExtGState - the box's
// own opacity is registered per page by the emitter. A Stack container stacks its content
// vertically; an overlay container overlays its children at the box top-left (both routed
// here via PlaceBox and PlaceSpecialContainer respectively).
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
        Func<Image, double, (double Width, double Height)>? measureImage = null,
        StyleResolution? resolution = null)
    {
        var pages = new List<PaginatedPage>();
        foreach (var section in document.Sections)
        {
            PaginateSection(section, fonts, pages, measureImage, resolution ?? new StyleResolution());
        }

        return pages;
    }

    public static IReadOnlyList<PaginatedPage> Paginate(
        Section section,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage = null,
        StyleResolution? resolution = null,
        IReadOnlyDictionary<string, int>? tocPages = null)
    {
        var pages = new List<PaginatedPage>();
        PaginateSection(section, fonts, pages, measureImage, resolution ?? new StyleResolution(), tocPages);
        return pages;
    }

    private static void PaginateSection(
        Section section,
        FontCollection fonts,
        List<PaginatedPage> pages,
        Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution,
        IReadOnlyDictionary<string, int>? tocPages = null)
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
        var contentTop = Math.Max(top, header.Height > 0 ? headerDistance + header.Height : 0);
        var contentBottom = Math.Max(bottom, footer.Height > 0 ? footerDistance + footer.Height : 0);
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
        // Containers pass through intact: Stack containers are placed as first-class boxes by
        // PlaceBox and overlay ones by PlaceSpecialContainer.
        var blocks = ExpandBlocks(section.Blocks, contentWidth, keepSpecialContainers: true, tocPages, fonts, resolution);

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
            var layout = tableLayouts[index] ??= TableLayout.Layout(table, Math.Max(0, contentWidth - table.LeftIndent.Point), fonts, measureImage, resolution);

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
            var indent = Math.Max(0, AlignImage(container.Alignment, contentWidth, boxWidth));
            var measured = boxMeasures[index] ??= MeasureBox(container, contentWidth, fonts, measureImage, resolution);
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

        // An overlay container is ONE unbreakable first-class box: its decoration
        // (background/gradient/borders/rounded corners/shadow) paints through the same
        // BoxStyle/BoxEmitter/BoxRenderer path a Stack container uses, and its children
        // share the box origin - each laid out from the box top-left (inset by the
        // padding) and merged in declaration order (later children on top within each
        // draw kind) so the box height is the tallest child's. A rotated overlay
        // additionally carries a page-space rotation about the box center.
        void PlaceSpecialContainer(Container container)
        {
            var (content, indent, boxWidth, boxHeight) = LayoutOverlay(container, contentWidth, fonts, measureImage, resolution);

            if (HasPageContent() && cursor + boxHeight > contentHeight + Eps)
            {
                Flush();
                cursor = 0;
            }

            Matrix? transform = null;
            if (container.Rotation != 0)
            {
                var centerX = left + indent + boxWidth / 2;
                var centerY = pageHeight - contentBox.Y - cursor - boxHeight / 2;
                transform = Matrix.Translate(-centerX, -centerY)
                    * Matrix.Rotate(container.Rotation)
                    * Matrix.Translate(centerX, centerY);
            }

            currentBoxes.Add(new PositionedBox
            {
                Source = container,
                Content = content,
                Bounds = new Rect(indent, cursor, boxWidth, boxHeight),
                Style = BoxStyle.FromContainer(container),
                Y = cursor,
                Opacity = container.Opacity,
                Transform = transform,
                Order = order++,
            });
            cursor += boxHeight;
        }

        var broken = new IReadOnlyList<LineBox>?[blocks.Count];
        var breaker = new LineBreakVisitor(contentWidth, fonts, resolution);
        for (var i = 0; i < blocks.Count; i++)
        {
            broken[i] = blocks[i].Accept(breaker, default);
        }

        var startPageCount = pages.Count;

        // Placement bodies stay local functions closing over the running pagination state
        // (cursor, the current-page lists, Flush/HasPageContent); the block dispatch itself
        // moves to the polymorphic SectionPlacer so each block kind routes to its own body.
        void PlaceBreak()
        {
            Flush();
            cursor = 0;
        }

        void PlaceImage(Image image)
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
        }

        void PlaceCode(Block block)
        {
            var (codeWidth, codeHeight) = MeasureCode(block, resolution);
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
        }

        void PlaceParagraph(int i, Paragraph para)
        {
            if (broken[i] is not { } lines)
            {
                return;
            }

            if (lines.Count == 0)
            {
                cursor += para.SpacingBefore.Point + para.SpacingAfter.Point;
                return;
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

                // The keep-with-next look-ahead is the only branch that needs paginator
                // state (it force-fills the shared layout caches for the next block); its
                // gated result feeds the pure placement policy. Kept identical to the old
                // inline `first && KeepWithNext && HasPageContent && NextBlockFirstHeight`.
                var hasNextBlock = false;
                double afterCursor = 0;
                double nextLeadingHeight = 0;
                if (k >= nrem && first && para.KeepWithNext && HasPageContent() &&
                    NextBlockFirstHeight(blocks, broken, tableLayouts, boxMeasures, i, contentWidth, fonts, measureImage, resolution, out var nextSpacingBefore, out var nextHeight))
                {
                    hasNextBlock = true;
                    afterCursor = blockTop + SumHeights(lines, offset, nrem) + spacingAfter;
                    nextLeadingHeight = nextSpacingBefore + nextHeight;
                }

                var decision = LinePlacer.Decide(new LinePlacementRequest
                {
                    LinesThatFit = k,
                    RemainingLines = nrem,
                    IsFirst = first,
                    HasPageContent = HasPageContent(),
                    Widows = para.Widows,
                    Orphans = para.Orphans,
                    KeepTogether = para.KeepTogether,
                    KeepWithNext = para.KeepWithNext,
                    HasNextBlock = hasNextBlock,
                    AfterCursor = afterCursor,
                    NextBlockLeadingHeight = nextLeadingHeight,
                    ContentHeight = contentHeight,
                });

                var moveWhole = decision.MoveWhole;
                var placeCount = decision.PlaceCount;

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

        var placer = new SectionPlacer(PlaceBreak, PlaceTable, PlaceSpecialContainer, PlaceBox, PlaceImage, PlaceCode, PlaceParagraph);
        for (var i = 0; i < blocks.Count; i++)
        {
            blocks[i].Accept(placer, i);
        }

        if (HasPageContent() || pages.Count == startPageCount)
        {
            Flush();
        }
    }

    // Routes each section-body block to its placement function. A block kind not valid as
    // direct section content (e.g. a List that escaped expansion) fails loud through Default,
    // exactly as the former is-not-Paragraph throw did.
    private sealed class SectionPlacer(
        Action placeBreak,
        Action<int, Table> placeTable,
        Action<Container> placeSpecial,
        Action<int, Container> placeBox,
        Action<Image> placeImage,
        Action<Block> placeCode,
        Action<int, Paragraph> placeParagraph)
        : BlockVisitor<int, Nothing>
    {
        protected override Nothing Default(Block block, int index)
            => throw new NotSupportedException($"Block type '{block.GetType().Name}' is not supported in section content.");

        public override Nothing Visit(PageBreak block, int index)
        {
            placeBreak();
            return default;
        }

        public override Nothing Visit(Table table, int index)
        {
            placeTable(index, table);
            return default;
        }

        public override Nothing Visit(Container container, int index)
        {
            if (IsSpecial(container))
            {
                placeSpecial(container);
            }
            else
            {
                placeBox(index, container);
            }

            return default;
        }

        public override Nothing Visit(Image image, int index)
        {
            placeImage(image);
            return default;
        }

        public override Nothing Visit(QrCode block, int index)
        {
            placeCode(block);
            return default;
        }

        public override Nothing Visit(Barcode block, int index)
        {
            placeCode(block);
            return default;
        }

        public override Nothing Visit(Paragraph para, int index)
        {
            placeParagraph(index, para);
            return default;
        }
    }

    private static (double Width, double Height) MeasureImage(Image image, double availableWidth)
        => ImageDecoder.Measure(image, ImageDecoder.Decode(image.Data), availableWidth);

    internal static (double Width, double Height) MeasureCode(Block block, StyleResolution resolution) => CodeBlockDispatch.Measure(block, resolution);

    private static HorizontalAlignment CodeAlignment(Block block) => CodeBlockDispatch.Alignment(block);

    // Pre-breaks each paragraph into its lines at the content width; a non-paragraph block
    // has no lines to break (Default returns null).
    private sealed class LineBreakVisitor(double contentWidth, FontCollection fonts, StyleResolution resolution)
        : BlockVisitor<Nothing, IReadOnlyList<LineBox>?>
    {
        protected override IReadOnlyList<LineBox>? Default(Block block, Nothing context) => null;

        public override IReadOnlyList<LineBox>? Visit(Paragraph paragraph, Nothing context)
            => LineBreaker.Break(paragraph, contentWidth, fonts, resolution.Alignment(paragraph), resolution);
    }

    internal static IReadOnlyList<Block> ExpandBlocks(
        BlockCollection blocks,
        double availableWidth,
        bool keepSpecialContainers = false,
        IReadOnlyDictionary<string, int>? tocPages = null,
        FontCollection? fonts = null,
        StyleResolution? resolution = null)
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
        var visitor = new ExpandVisitor(expanded, availableWidth, keepSpecialContainers, tocPages, fonts, resolution ?? new StyleResolution());
        foreach (var block in blocks)
        {
            block.Accept(visitor, default);
        }

        return expanded;
    }

    // Lowers lists to marker paragraphs and a table of contents to entry paragraphs, keeps
    // containers first-class (rejecting overlay/rotated ones outside direct section content),
    // and passes every other block through unchanged (Default).
    private sealed class ExpandVisitor(
        List<Block> expanded,
        double availableWidth,
        bool keepSpecialContainers,
        IReadOnlyDictionary<string, int>? tocPages,
        FontCollection? fonts,
        StyleResolution resolution)
        : BlockVisitor<Nothing, Nothing>
    {
        protected override Nothing Default(Block block, Nothing context)
        {
            expanded.Add(block);
            return default;
        }

        public override Nothing Visit(List list, Nothing context)
        {
            ExpandList(list, expanded, 0, null, resolution);
            return default;
        }

        public override Nothing Visit(Container container, Nothing context)
        {
            // A Stack container is a first-class box: the section body and the
            // header/footer bands place it as a first-class box and cell/box content
            // nests it as a first-class nested box (BoxContentLayout). Overlay and
            // rotated containers are only allowed as direct section content
            // (keepSpecialContainers: true), where PlaceSpecialContainer/PlaceBox
            // handle them - nested content cannot host a page-space transform.
            if (!keepSpecialContainers && (IsSpecial(container) || container.Rotation != 0))
            {
                throw new NotSupportedException(
                    "Overlay and rotated containers are only supported as direct section content.");
            }

            expanded.Add(container);
            return default;
        }

        public override Nothing Visit(TableOfContents toc, Nothing context)
        {
            if (!keepSpecialContainers)
            {
                throw new NotSupportedException(
                    "A table of contents is only supported as direct section content.");
            }

            ExpandTableOfContents(toc, expanded, availableWidth, tocPages, fonts);
            return default;
        }
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
        IReadOnlyDictionary<string, int>? tocPages,
        FontCollection? fonts)
    {
        if (fonts is null)
        {
            throw new InvalidOperationException("A table of contents requires font metrics to lower.");
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
        IReadOnlyDictionary<string, int>? tocPages,
        FontCollection fonts)
    {
        var indent = toc.LevelIndent.Point * entry.Level;
        var max = availableWidth - indent;
        var reserve = fonts.MeasureText(TocPagePlaceholder, toc.Font) + 2;
        var stop = Math.Max(0, max - reserve);

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
            var count = (int)Math.Floor((stop - TocLeaderGap - textWidth - spaceWidth) / leaderWidth);
            if (count >= 1)
            {
                paragraph.Inlines.Add(" " + new string(toc.Leader, count)).Font.InheritFrom(toc.Font);
            }
        }

        paragraph.Inlines.Add("\t").Font.InheritFrom(toc.Font);

        var number = tocPages is not null && tocPages.TryGetValue(entry.Anchor, out var page)
            ? page.ToString(CultureInfo.InvariantCulture)
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

    // Lays out an overlay container as a first-class box: each child is measured and
    // positioned independently at the box top-left (inset by the padding), and the
    // results are merged in declaration order (nested tables/boxes reordered onto one
    // increasing sequence so emission interleaves them in that order). The box inner
    // height is the tallest child's; content positions are box-local (the emitter shifts
    // them by the box Y).
    private static (LaidOutBoxContent Content, double Indent, double BoxWidth, double BoxHeight) LayoutOverlay(
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

    // Each nesting level shifts the marker column by the parent's LeftIndent + HangingIndent and
    // inherits the parent item's resolved font, so nested runs cascade item -> list -> parent item.
    private static void ExpandList(List list, List<Block> expanded, double indent, Font? inherited, StyleResolution resolution)
    {
        for (var i = 0; i < list.Items.Count; i++)
        {
            var paragraph = ExpandItem(list, i, indent, inherited, resolution);
            expanded.Add(paragraph);
            if (list.Items[i].NestedList is { } nested)
            {
                ExpandList(nested, expanded, indent + list.LeftIndent.Point + list.HangingIndent.Point, resolution.ParagraphFont(paragraph), resolution);
            }
        }
    }

    private static Paragraph ExpandItem(List list, int index, double indent, Font? inherited, StyleResolution resolution)
    {
        var item = list.Items[index];

        // StyleResolver resolves the marker and run fonts through the full cascade (including the
        // surrounding cell/row/table context and the Normal default) and stores them in the
        // per-save StyleResolution; fall back to the item/list cascade only when the resolver has
        // not run (nested items always take this path). The resolved fonts live in the resolution
        // (keyed by the shared run and by this synthesized paragraph), never on the model.
        var itemFont = resolution.ItemFont(item) ?? ItemFont(item, list, inherited);
        var paragraph = new Paragraph
        {
            LeftIndent = Unit.FromPoint(indent + list.LeftIndent.Point + list.HangingIndent.Point),
            MarkerIndent = Unit.FromPoint(indent + list.LeftIndent.Point),
            MarkerText = Marker(list, index),
        };
        resolution.SetParagraphFont(paragraph, itemFont);

        // Null unless the tree was built for tagged output; carries the item's Lbl/LBody so the
        // synthesized paragraph tags its marker and content into the right structure elements.
        if (resolution.ListItemElements(item) is { } elements)
        {
            resolution.SetListParagraphElements(paragraph, elements.Label, elements.Body);
        }

        foreach (var run in item.Inlines)
        {
            resolution.SetRunFont(run, resolution.RunFont(run) ?? RunFont(run, item, list, inherited));
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
            ? (index + 1).ToString(CultureInfo.InvariantCulture) + "."
            : BulletGlyph;

    private static double AlignImage(HorizontalAlignment alignment, double containerWidth, double imageWidth)
        => alignment switch
        {
            HorizontalAlignment.Center => (containerWidth - imageWidth) / 2.0,
            HorizontalAlignment.Right or HorizontalAlignment.End => containerWidth - imageWidth,
            _ => 0,
        };

    // Measures a Stack container's content at the box's inner width (box width minus the
    // padding on both sides); content measures with no inherited alignment.
    private static BoxContentLayout.Measured MeasureBox(
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

    // The required height of the header rows plus the first body ROW GROUP: the minimum a
    // table needs on a page before its first fragment breaks early. The first group is the
    // rowspan closure TablePaginator force-places as one unit, so it must be measured whole
    // or the flush check would let a tall group spill past the page bottom.
    private static double TableFirstFragmentHeight(Table table, LaidOutTable layout)
        => TablePaginator.FirstBodyGroupHeight(layout, table);

    private readonly struct NextBlockHeight
    {
        public bool Found { get; init; }

        public double SpacingBefore { get; init; }

        public double Height { get; init; }
    }

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
        Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution,
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

        var visitor = new NextHeightVisitor(broken, tableLayouts, boxMeasures, contentWidth, fonts, measureImage, resolution);
        var result = blocks[next].Accept(visitor, next);
        spacingBefore = result.SpacingBefore;
        height = result.Height;
        return result.Found;
    }

    // Resolves the leading height of the block at the given index, keyed off the same
    // per-section layout caches the main loop shares. A block kind that a page can start
    // mid-way (or that never breaks meaningfully here) reports no minimum (Default).
    private sealed class NextHeightVisitor(
        IReadOnlyList<LineBox>?[] broken,
        LaidOutTable?[] tableLayouts,
        BoxContentLayout.Measured?[] boxMeasures,
        double contentWidth,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution)
        : BlockVisitor<int, NextBlockHeight>
    {
        protected override NextBlockHeight Default(Block block, int next) => default;

        public override NextBlockHeight Visit(Paragraph paragraph, int next)
            => broken[next] is { Count: > 0 } lines
                ? new NextBlockHeight { Found = true, SpacingBefore = paragraph.SpacingBefore.Point, Height = lines[0].Height }
                : default;

        public override NextBlockHeight Visit(Table table, int next)
        {
            var layout = tableLayouts[next] ??= TableLayout.Layout(table, Math.Max(0, contentWidth - table.LeftIndent.Point), fonts, measureImage, resolution);
            return new NextBlockHeight { Found = true, Height = TableFirstFragmentHeight(table, layout) };
        }

        public override NextBlockHeight Visit(Container container, int next)
        {
            if (IsSpecial(container))
            {
                return default;
            }

            // A Stack container never splits, so its first height is the whole box.
            var measured = boxMeasures[next] ??= MeasureBox(container, contentWidth, fonts, measureImage, resolution);
            return new NextBlockHeight { Found = true, Height = measured.Height + (2 * container.Padding.Point) };
        }

        public override NextBlockHeight Visit(Image image, int next)
        {
            var (_, imageHeight) = measureImage is null ? MeasureImage(image, contentWidth) : measureImage(image, contentWidth);
            return new NextBlockHeight { Found = true, Height = imageHeight };
        }

        public override NextBlockHeight Visit(QrCode block, int next) => VisitCode(block);

        public override NextBlockHeight Visit(Barcode block, int next) => VisitCode(block);

        private NextBlockHeight VisitCode(Block block) => new() { Found = true, Height = MeasureCode(block, resolution).Height };
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
        Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution)
    {
        var result = new BandLayout();
        var visitor = new BandVisitor(result, width, fonts, measureImage, resolution);
        // Lists expand to marker paragraphs exactly as in section content.
        foreach (var block in ExpandBlocks(band.Blocks, width, resolution: resolution))
        {
            block.Accept(visitor, default);
        }

        result.Height = visitor.Cursor;
        return result;
    }

    // Lays a header/footer band out at a running cursor: a band never page-breaks, so a
    // container/table/image/code/paragraph places whole and a page break is a no-op. Any
    // other block type is unsupported in a band (Default fails loud).
    private sealed class BandVisitor(
        BandLayout result,
        double width,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution)
        : BlockVisitor<Nothing, Nothing>
    {
        // Placement sequence shared by band table fragments and band boxes so page
        // emission can interleave them in document order.
        private int order;

        public double Cursor { get; private set; }

        protected override Nothing Default(Block block, Nothing context)
            => throw new NotSupportedException($"Block type '{block.GetType().Name}' is not supported in a header/footer band.");

        public override Nothing Visit(Container container, Nothing context)
        {
            // A Stack container in a band is a first-class box, like the section body;
            // a band never page-breaks, so the box places whole at the running cursor.
            var measured = MeasureBox(container, width, fonts, measureImage, resolution);
            var box = BuildBox(container, measured, width, Cursor, order++, transform: null);
            result.Boxes.Add(box);
            Cursor += box.Bounds.Height;
            return default;
        }

        public override Nothing Visit(Table table, Nothing context)
        {
            var layout = TableLayout.Layout(table, Math.Max(0, width - table.LeftIndent.Point), fonts, measureImage, resolution);
            var tableOrder = order++;
            foreach (var fragment in TablePaginator.Paginate(layout, table, double.PositiveInfinity))
            {
                result.Tables.Add(new PositionedTableFragment
                {
                    Layout = layout,
                    Fragment = fragment,
                    Y = Cursor,
                    Order = tableOrder,
                });
                Cursor += fragment.Height;
            }

            return default;
        }

        public override Nothing Visit(Image image, Nothing context)
        {
            var (imageWidth, imageHeight) = measureImage is null ? MeasureImage(image, width) : measureImage(image, width);
            result.Images.Add(new PositionedImage
            {
                Source = image,
                Y = Cursor,
                Width = imageWidth,
                Height = imageHeight,
                XOffset = AlignImage(image.Alignment, width, imageWidth),
            });
            Cursor += imageHeight;
            return default;
        }

        public override Nothing Visit(QrCode block, Nothing context) => VisitCode(block);

        public override Nothing Visit(Barcode block, Nothing context) => VisitCode(block);

        private Nothing VisitCode(Block block)
        {
            var (codeWidth, codeHeight) = MeasureCode(block, resolution);
            result.Codes.Add(new PositionedCode
            {
                Source = block,
                Y = Cursor,
                Width = codeWidth,
                Height = codeHeight,
                XOffset = AlignImage(CodeAlignment(block), width, codeWidth),
            });
            Cursor += codeHeight;
            return default;
        }

        // A header/footer band cannot page-break, so a page break inside one is a no-op.
        public override Nothing Visit(PageBreak block, Nothing context) => default;

        public override Nothing Visit(Paragraph paragraph, Nothing context)
        {
            var lines = LineBreaker.Break(paragraph, width, fonts, resolution.Alignment(paragraph), resolution);
            var y = Cursor + paragraph.SpacingBefore.Point;
            foreach (var box in lines)
            {
                result.Lines.Add(new PositionedLine { Line = box, Source = paragraph, Y = y });
                y += box.Height;
            }

            Cursor = y + paragraph.SpacingAfter.Point;
            return default;
        }
    }

    private static (double Width, double Height) EffectiveSize(Section section)
    {
        var width = section.PageSize.Width.Point;
        var height = section.PageSize.Height.Point;
        return section.Orientation == PageOrientation.Landscape ? (height, width) : (width, height);
    }
}
