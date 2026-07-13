using System;
using System.Collections.Generic;

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
        var (pageWidth, pageHeight) = BandLayouter.EffectiveSize(section);
        var left = section.Margins.Left.Point;
        var top = section.Margins.Top.Point;
        var right = section.Margins.Right.Point;
        var bottom = section.Margins.Bottom.Point;

        var contentWidth = pageWidth - left - right;
        var size = new PageSize(Unit.FromPoint(pageWidth), Unit.FromPoint(pageHeight));

        var header = BandLayouter.Layout(section.Header, contentWidth, fonts, measureImage, resolution);
        var footer = BandLayouter.Layout(section.Footer, contentWidth, fonts, measureImage, resolution);

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
        var blocks = BlockExpander.ExpandBlocks(section.Blocks, contentWidth, keepSpecialContainers: true, tocPages, fonts, resolution);

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

            if (HasPageContent() && cursor + NextBlockHeightResolver.TableFirstFragmentHeight(table, layout) > contentHeight + Eps)
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
            var indent = Math.Max(0, OverlayBoxPlacer.AlignImage(container.Alignment, contentWidth, boxWidth));
            var measured = boxMeasures[index] ??= OverlayBoxPlacer.MeasureBox(container, contentWidth, fonts, measureImage, resolution);
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

            currentBoxes.Add(OverlayBoxPlacer.BuildBox(container, measured, contentWidth, cursor, order++, transform));
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
            var (content, indent, boxWidth, boxHeight) = OverlayBoxPlacer.LayoutOverlay(container, contentWidth, fonts, measureImage, resolution);

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
                XOffset = OverlayBoxPlacer.AlignImage(image.Alignment, contentWidth, imageWidth),
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
                XOffset = OverlayBoxPlacer.AlignImage(CodeAlignment(block), contentWidth, codeWidth),
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
                    NextBlockHeightResolver.NextBlockFirstHeight(blocks, broken, tableLayouts, boxMeasures, i, contentWidth, fonts, measureImage, resolution, out var nextSpacingBefore, out var nextHeight))
                {
                    hasNextBlock = true;
                    afterCursor = blockTop + NextBlockHeightResolver.SumHeights(lines, offset, nrem) + spacingAfter;
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
            if (OverlayBoxPlacer.IsSpecial(container))
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

    internal static (double Width, double Height) MeasureImage(Image image, double availableWidth)
        => ImageDecoder.Measure(image, ImageDecoder.Decode(image.Data), availableWidth);

    internal static (double Width, double Height) MeasureCode(Block block, StyleResolution resolution) => CodeBlockDispatch.Measure(block, resolution);

    internal static HorizontalAlignment CodeAlignment(Block block) => CodeBlockDispatch.Alignment(block);

    // Pre-breaks each paragraph into its lines at the content width; a non-paragraph block
    // has no lines to break (Default returns null).
    private sealed class LineBreakVisitor(double contentWidth, FontCollection fonts, StyleResolution resolution)
        : BlockVisitor<Nothing, IReadOnlyList<LineBox>?>
    {
        protected override IReadOnlyList<LineBox>? Default(Block block, Nothing context) => null;

        public override IReadOnlyList<LineBox>? Visit(Paragraph paragraph, Nothing context)
            => LineBreaker.Break(paragraph, contentWidth, fonts, resolution.Alignment(paragraph), resolution);
    }
    // Forwards to BlockExpander so external callers (BoxContentLayout) keep using
    // Paginator.ExpandBlocks; the expansion itself lives in BlockExpander.
    internal static IReadOnlyList<Block> ExpandBlocks(
        BlockCollection blocks,
        double availableWidth,
        bool keepSpecialContainers = false,
        IReadOnlyDictionary<string, int>? tocPages = null,
        FontCollection? fonts = null,
        StyleResolution? resolution = null)
        => BlockExpander.ExpandBlocks(blocks, availableWidth, keepSpecialContainers, tocPages, fonts, resolution);
}
