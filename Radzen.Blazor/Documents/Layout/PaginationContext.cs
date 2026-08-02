using System.Collections.Generic;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Core;

namespace Radzen.Documents.Layout;

internal sealed class PaginationContext
{
    private readonly List<LaidOutPage> pages;
    private readonly FontCollection fonts;
    private readonly LoweringResult resolution;
    private readonly LayoutCaptureContext capture;
    private LaidOutWatermark? watermark;
    private PageSize size;
    private LaidOutLayer header = null!;
    private LaidOutLayer footer = null!;
    private double headerTop;
    private double footerTop;
    private double left;
    private double pageHeight;
    private double contentTop;
    private double contentWidth;
    private double contentHeight;
    private BlockLayoutCache blockLayouts = null!;
    private readonly int sectionIndex;
    private BlockHeightHandler? blockHeightHandler;
    private int order;
    private PageLayerBuilder current;

    public PaginationContext(
        FontCollection fonts,
        List<LaidOutPage> pages,
        LoweringResult resolution,
        LayoutCaptureContext capture,
        int sectionIndex)
    {
        this.pages = pages;
        this.fonts = fonts;
        this.resolution = resolution;
        this.capture = capture;
        this.sectionIndex = sectionIndex;
        current = new PageLayerBuilder();
        StartPageCount = pages.Count;
    }

    public void Initialize(
        Section section,
        IReadOnlyDictionary<string, int>? tocPages)
    {
        var (pageWidth, effectivePageHeight) = BandLayouter.EffectiveSize(section);
        pageHeight = effectivePageHeight;
        left = section.Margins.Left.Point;
        var top = section.Margins.Top.Point;
        var right = section.Margins.Right.Point;
        var bottom = section.Margins.Bottom.Point;
        contentWidth = pageWidth - left - right;
        RequirePositiveContentBox(contentWidth, pageHeight - top - bottom);
        size = new PageSize(Unit.FromPoint(pageWidth), Unit.FromPoint(pageHeight));
        watermark = WatermarkPlanner.Plan(section.Watermark, size, fonts, capture);

        var headerLayout = BandLayouter.Layout(
            section.Header,
            contentWidth,
            fonts,
            resolution,
            capture);
        var footerLayout = BandLayouter.Layout(
            section.Footer,
            contentWidth,
            fonts,
            resolution,
            capture);
        header = headerLayout.Content.Build();
        footer = footerLayout.Content.Build();

        var headerDistance = section.HeaderDistance.Point;
        var footerDistance = section.FooterDistance.Point;
        contentTop = Math.Max(top, headerLayout.Height > 0 ? headerDistance + headerLayout.Height : 0);
        var contentBottom = Math.Max(bottom, footerLayout.Height > 0 ? footerDistance + footerLayout.Height : 0);
        ContentBox = new Rect(left, contentTop, contentWidth, pageHeight - contentTop - contentBottom);
        RequirePositiveContentBox(ContentBox.Width, ContentBox.Height);
        contentHeight = ContentBox.Height;
        headerTop = headerDistance;
        footerTop = pageHeight - footerDistance - footerLayout.Height;

        Blocks = BlockExpander.ExpandBlocks(section.Blocks, contentWidth, resolution, keepSpecialContainers: true, tocPages, fonts);
        blockLayouts = new BlockLayoutCache(
            Blocks.Count,
            contentWidth,
            fonts,
            resolution,
            capture);
        Broken = new IReadOnlyList<LineBox>?[Blocks.Count];
        var breaker = new LineBreakVisitor(contentWidth, fonts, resolution, capture);
        for (var i = 0; i < Blocks.Count; i++)
        {
            Broken[i] = Blocks[i].Accept(breaker, default);
        }
    }

    private void RequirePositiveContentBox(double width, double height)
    {
        if (width > 0 && height > 0)
        {
            return;
        }

        throw new InvalidOperationException(FormattableString.Invariant(
            $"Section {sectionIndex} has an impossible content box of {width:0.###} x {height:0.###} points; its margins, header distance and footer distance must leave a positive content width and height."));
    }

    public ExpandedBlocks Blocks { get; private set; } = null!;

    public IReadOnlyList<LineBox>?[] Broken { get; private set; } = null!;

    public Rect ContentBox { get; private set; }

    public int StartPageCount { get; }

    public double Cursor { get; private set; }

    internal FontCollection Fonts => fonts;

    internal LoweringResult Resolution => resolution;

    internal LayoutCaptureContext Capture => capture;

    public bool HasPageContent => current.HasContent;

    public void Flush()
    {
        pages.Add(new LaidOutPage
        {
            Size = size,
            ContentBox = ContentBox,
            Body = current.Build(),
            HeaderLayer = header,
            FooterLayer = footer,
            HeaderTop = headerTop,
            FooterTop = footerTop,
            Watermark = watermark,
        });
        current = new PageLayerBuilder();
    }

    public void Finish()
    {
        if (HasPageContent || pages.Count == StartPageCount)
        {
            Flush();
        }
    }

    public void PlaceBreak()
    {
        Flush();
        Cursor = 0;
    }

    private void EnsureFits(double height)
    {
        if (HasPageContent && Cursor + height > contentHeight + LayoutTolerance.Epsilon)
        {
            Flush();
            Cursor = 0;
        }
    }

    public void PrepareBlock(int index)
    {
        foreach (var range in Blocks.ListItemRanges)
        {
            if (!range.KeepTogether || range.Start != index || !HasPageContent)
            {
                continue;
            }

            double height = 0;
            for (var blockIndex = range.Start; blockIndex <= range.End; blockIndex++)
            {
                height += BlockHeight(blockIndex);
            }

            if (height <= contentHeight + LayoutTolerance.Epsilon)
            {
                EnsureFits(height);
            }
        }
    }

    private double BlockHeight(int index)
        => LoweredBlockDispatch.Dispatch(
            Blocks[index],
            blockHeightHandler ??= new BlockHeightHandler(this),
            index);

    private double AvailableWidth(Block block)
        => Math.Max(0, contentWidth - resolution.BlockIndent(block));

    private LaidOutTable TableLayoutFor(int index, Table table)
        => blockLayouts.Table(index, table);

    private void PlaceMarker(Block block)
    {
        if (block is not Paragraph && resolution.ListMarker(block) is { } marker)
        {
            current.Lines.Add(new LaidOutLine
            {
                Line = LineLayouter.MarkerLine(marker, fonts, capture),
                Source = capture.Source(block),
                X = 0,
                Y = Cursor,
                ZOrder = order++,
            });
        }
    }

    public void PlaceTable(int index, Table table)
    {
        var layout = TableLayoutFor(index, table);

        EnsureFits(NextBlockHeightResolver.TableFirstFragmentHeight(table, layout));
        PlaceMarker(table);

        var tableOrder = order++;
        var fragments = TablePaginator.Paginate(
            layout, table, contentHeight - Cursor, contentHeight, capture);
        for (var f = 0; f < fragments.Count; f++)
        {
            if (f > 0)
            {
                Flush();
                Cursor = 0;
            }

            current.Tables.Add(FlowContentPlacer.Table(
                table,
                layout,
                fragments[f],
                Cursor,
                tableOrder,
                capture));
            Cursor += fragments[f].Height;
        }
    }

    private Matrix? RotationAboutCenter(Container container, double indent, double boxWidth, double boxHeight)
    {
        if (container.Rotation == 0)
        {
            return null;
        }

        var centerX = left + indent + boxWidth / 2;
        var centerY = contentTop + Cursor + boxHeight / 2;
        return Matrix.Translate(-centerX, -centerY)
            * Matrix.Rotate(-container.Rotation)
            * Matrix.Translate(centerX, centerY);
    }

    public void PlaceBox(int index, Container container)
    {
        var listIndent = resolution.BlockIndent(container);
        var availableWidth = AvailableWidth(container);
        var padding = container.EffectivePadding;
        var boxWidth = container.Width?.Point ?? availableWidth;
        var indent = listIndent + Math.Max(0, HorizontalAlignmentOffset.Of(container.Alignment, availableWidth, boxWidth));
        var measured = blockLayouts.Box(index, container);
        var boxHeight = measured.Height + padding.Vertical;

        EnsureFits(boxHeight);
        PlaceMarker(container);

        var transform = RotationAboutCenter(container, indent, boxWidth, boxHeight);

        current.Boxes.Add(OverlayBoxPlacer.BuildBox(
            container,
            measured,
            availableWidth,
            Cursor,
            order++,
            transform,
            resolution,
            listIndent));
        Cursor += boxHeight;
    }

    public void PlaceSpecialContainer(Container container)
    {
        var listIndent = resolution.BlockIndent(container);
        var (content, indent, boxWidth, boxHeight) = OverlayBoxPlacer.LayoutOverlay(
            container,
            AvailableWidth(container),
            fonts,
            resolution,
            capture,
            listIndent);

        EnsureFits(boxHeight);
        PlaceMarker(container);

        var transform = RotationAboutCenter(container, indent, boxWidth, boxHeight);

        current.Boxes.Add(new LaidOutBox
        {
            Source = capture.Source(container),
            Content = content,
            Bounds = new Rect(indent, Cursor, boxWidth, boxHeight),
            Style = GeometryCapture.Box(container, boxWidth, boxHeight, capture),
            Padding = container.EffectivePadding,
            Opacity = (resolution?.Opacities ?? OpacityResolver.None).ContainerOpacity(container),
            Transform = transform,
            ZOrder = order++,
        });
        Cursor += boxHeight;
    }

    public void PlaceImage(Image image)
    {
        var indent = resolution.BlockIndent(image);
        var availableWidth = AvailableWidth(image);
        var (imageWidth, imageHeight) = capture.Probes.Measure(image, availableWidth);
        EnsureFits(imageHeight);
        PlaceMarker(image);

        current.Images.Add(FlowContentPlacer.Image(
            image,
            availableWidth,
            Cursor,
            imageWidth,
            imageHeight,
            capture,
            order++,
            indent));
        Cursor += imageHeight;
    }

    public void PlaceCodeSymbol(Block block)
    {
        var indent = resolution.BlockIndent(block);
        var availableWidth = AvailableWidth(block);
        var (codeSymbolWidth, codeSymbolHeight) = CodeSymbolDispatch.Measure(
            block,
            capture,
            fonts,
            resolution);
        EnsureFits(codeSymbolHeight);
        PlaceMarker(block);

        current.CodeSymbols.Add(FlowContentPlacer.CodeSymbol(
            block,
            availableWidth,
            Cursor,
            codeSymbolWidth,
            codeSymbolHeight,
            fonts,
            resolution,
            capture,
            order++,
            indent));
        Cursor += codeSymbolHeight;
    }

    public void PlaceParagraph(int index, Paragraph paragraph)
    {
        if (Broken[index] is not { } lines)
        {
            return;
        }

        var format = resolution.Format(paragraph);

        if (lines.Count == 0)
        {
            Cursor += format.SpacingBefore.Point + format.SpacingAfter.Point;
            return;
        }

        var spacingBefore = format.SpacingBefore.Point;
        var spacingAfter = format.SpacingAfter.Point;
        var offset = 0;
        var first = true;
        var progress = new LayoutProgressGuard(
            FormattableString.Invariant($"Paragraph pagination over {lines.Count} lines"));

        while (true)
        {
            progress.Reached(offset);

            var remaining = lines.Count - offset;
            var blockTop = Cursor + (first ? spacingBefore : 0);
            if (first && !HasPageContent)
            {
                var maxTop = contentHeight - lines[offset].Height;
                if (maxTop >= 0 && blockTop > maxTop)
                {
                    blockTop = maxTop;
                }
            }

            var fit = 0;
            var y = blockTop;
            while (offset + fit < lines.Count && y + lines[offset + fit].Height <= contentHeight + LayoutTolerance.Epsilon)
            {
                y += lines[offset + fit].Height;
                fit++;
            }

            var hasNextBlock = false;
            double afterCursor = 0;
            double nextLeadingHeight = 0;
            if (fit >= remaining && first && format.KeepWithNext && HasPageContent &&
                NextBlockHeightResolver.NextBlockFirstHeight(
                    Blocks,
                    Broken,
                    blockLayouts,
                    index,
                    contentWidth,
                    fonts,
                    resolution,
                    out var nextSpacingBefore,
                    out var nextHeight))
            {
                hasNextBlock = true;
                afterCursor = blockTop + NextBlockHeightResolver.SumHeights(lines, offset, remaining) + spacingAfter;
                nextLeadingHeight = nextSpacingBefore + nextHeight;
            }

            var decision = LinePlacer.Decide(new LinePlacementRequest
            {
                LinesThatFit = fit,
                RemainingLines = remaining,
                IsFirst = first,
                HasPageContent = HasPageContent,
                Widows = paragraph.Widows,
                Orphans = paragraph.Orphans,
                KeepTogether = format.KeepTogether,
                KeepWithNext = format.KeepWithNext,
                HasNextBlock = hasNextBlock,
                AfterCursor = afterCursor,
                NextBlockLeadingHeight = nextLeadingHeight,
                ContentHeight = contentHeight,
            });

            if (decision.MoveWhole)
            {
                Flush();
                Cursor = 0;
                first = true;
                continue;
            }

            var lineY = blockTop;
            for (var lineIndex = 0; lineIndex < decision.PlaceCount; lineIndex++)
            {
                var box = lines[offset + lineIndex];
                current.Lines.Add(new LaidOutLine
                {
                    Line = box,
                    Source = capture.Source(paragraph),
                    X = 0,
                    Y = lineY,
                    ZOrder = order++,
                });
                lineY += box.Height;
            }

            offset += decision.PlaceCount;
            if (offset >= lines.Count)
            {
                Cursor = lineY + spacingAfter;
                break;
            }

            Flush();
            Cursor = 0;
            first = false;
        }
    }

    private sealed class LineBreakVisitor(
        double contentWidth,
        FontCollection fonts,
        LoweringResult resolution,
        LayoutCaptureContext capture)
        : BlockVisitor<Nothing, IReadOnlyList<LineBox>?>
    {
        protected override IReadOnlyList<LineBox>? Default(Block block, Nothing context) => null;

        public override IReadOnlyList<LineBox>? Visit(Paragraph paragraph, Nothing context)
            => LineLayouter.Layout(
                paragraph,
                contentWidth,
                fonts,
                capture,
                resolution.Alignment(paragraph),
                resolution);
    }

    private sealed class BlockHeightHandler(PaginationContext owner)
        : ILoweredBlockHandler<int, double>
    {
        public double Paragraph(Paragraph paragraph, int index)
        {
            var format = owner.resolution.Format(paragraph);
            return format.SpacingBefore.Point
                + (owner.Broken[index] is { } lines ? NextBlockHeightResolver.SumHeights(lines, 0, lines.Count) : 0)
                + format.SpacingAfter.Point;
        }

        public double Table(Table table, int index) => owner.TableLayoutFor(index, table).Height;

        public double Container(Container container, int index)
        {
            if (OverlayBoxPlacer.IsSpecial(container))
            {
                return OverlayBoxPlacer.LayoutOverlay(
                    container,
                    owner.AvailableWidth(container),
                    owner.fonts,
                    owner.resolution,
                    capture: owner.capture).BoxHeight;
            }

            var measured = owner.blockLayouts.Box(index, container);
            return measured.Height + container.EffectivePadding.Vertical;
        }

        public double Image(Image image, int index)
            => owner.capture.Probes.Measure(image, owner.AvailableWidth(image)).Height;

        public double CodeSymbol(Block block, int index)
            => CodeSymbolDispatch.Measure(
                block,
                owner.capture,
                owner.fonts,
                owner.resolution).Height;

        public double PageBreak(PageBreak pageBreak, int index) => double.PositiveInfinity;
    }
}
