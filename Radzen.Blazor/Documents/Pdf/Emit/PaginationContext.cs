using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class PaginationContext
{
    internal const double Eps = 1e-6;
    private readonly List<PaginatedPage> pages;
    private readonly FontCollection fonts;
    private readonly Func<Image, double, (double Width, double Height)>? measureImage;
    private readonly StyleResolution resolution;
    private readonly PageSize size;
    private readonly PageLayer header;
    private readonly PageLayer footer;
    private readonly double headerTop;
    private readonly double footerTop;
    private readonly double left;
    private readonly double pageHeight;
    private readonly double contentTop;
    private readonly double contentWidth;
    private readonly double contentHeight;
    private readonly LaidOutTable?[] tableLayouts;
    private readonly BoxContentLayout.Measured?[] boxMeasures;
    private int order;
    private PageLayer current = new();

    public PaginationContext(
        Section section,
        FontCollection fonts,
        List<PaginatedPage> pages,
        Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution,
        IReadOnlyDictionary<string, int>? tocPages)
    {
        this.pages = pages;
        this.fonts = fonts;
        this.measureImage = measureImage;
        this.resolution = resolution;

        var (pageWidth, effectivePageHeight) = BandLayouter.EffectiveSize(section);
        pageHeight = effectivePageHeight;
        left = section.Margins.Left.Point;
        var top = section.Margins.Top.Point;
        var right = section.Margins.Right.Point;
        var bottom = section.Margins.Bottom.Point;
        contentWidth = pageWidth - left - right;
        size = new PageSize(Unit.FromPoint(pageWidth), Unit.FromPoint(pageHeight));

        var headerLayout = BandLayouter.Layout(section.Header, contentWidth, fonts, measureImage, resolution);
        var footerLayout = BandLayouter.Layout(section.Footer, contentWidth, fonts, measureImage, resolution);
        header = headerLayout.Content;
        footer = footerLayout.Content;

        var headerDistance = section.HeaderDistance.Point;
        var footerDistance = section.FooterDistance.Point;
        contentTop = Math.Max(top, headerLayout.Height > 0 ? headerDistance + headerLayout.Height : 0);
        var contentBottom = Math.Max(bottom, footerLayout.Height > 0 ? footerDistance + footerLayout.Height : 0);
        ContentBox = new Rect(left, contentTop, contentWidth, pageHeight - contentTop - contentBottom);
        contentHeight = ContentBox.Height;
        headerTop = headerDistance;
        footerTop = pageHeight - footerDistance - footerLayout.Height;

        Blocks = BlockExpander.ExpandBlocks(section.Blocks, contentWidth, keepSpecialContainers: true, tocPages, fonts, resolution);
        tableLayouts = new LaidOutTable?[Blocks.Count];
        boxMeasures = new BoxContentLayout.Measured?[Blocks.Count];
        Broken = new IReadOnlyList<LineBox>?[Blocks.Count];
        var breaker = new LineBreakVisitor(contentWidth, fonts, resolution);
        for (var i = 0; i < Blocks.Count; i++)
        {
            Broken[i] = Blocks[i].Accept(breaker, default);
        }

        StartPageCount = pages.Count;
    }

    public IReadOnlyList<Block> Blocks { get; }

    public IReadOnlyList<LineBox>?[] Broken { get; }

    public Rect ContentBox { get; }

    public int StartPageCount { get; }

    public double Cursor { get; private set; }

    public bool HasPageContent => current.HasContent;

    public void Flush()
    {
        pages.Add(new PaginatedPage
        {
            Size = size,
            ContentBox = ContentBox,
            Number = pages.Count + 1,
            Body = current,
            HeaderLayer = header,
            FooterLayer = footer,
            HeaderTop = headerTop,
            FooterTop = footerTop,
        });
        current = new PageLayer();
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

    public void PlaceTable(int index, Table table)
    {
        var layout = tableLayouts[index] ??= TableLayout.Layout(table, Math.Max(0, contentWidth - table.LeftIndent.Point), fonts, measureImage, resolution);

        if (HasPageContent && Cursor + NextBlockHeightResolver.TableFirstFragmentHeight(table, layout) > contentHeight + Eps)
        {
            Flush();
            Cursor = 0;
        }

        var tableOrder = order++;
        var fragments = TablePaginator.Paginate(layout, table, contentHeight - Cursor, contentHeight);
        for (var f = 0; f < fragments.Count; f++)
        {
            if (f > 0)
            {
                Flush();
                Cursor = 0;
            }

            current.Tables.Add(new PositionedTableFragment
            {
                Layout = layout,
                Fragment = fragments[f],
                Y = Cursor,
                Order = tableOrder,
            });
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
        var centerY = pageHeight - contentTop - Cursor - boxHeight / 2;
        return Matrix.Translate(-centerX, -centerY)
            * Matrix.Rotate(container.Rotation)
            * Matrix.Translate(centerX, centerY);
    }

    public void PlaceBox(int index, Container container)
    {
        var padding = container.Padding.Point;
        var boxWidth = container.Width?.Point ?? contentWidth;
        var indent = Math.Max(0, OverlayBoxPlacer.AlignImage(container.Alignment, contentWidth, boxWidth));
        var measured = boxMeasures[index] ??= OverlayBoxPlacer.MeasureBox(container, contentWidth, fonts, measureImage, resolution);
        var boxHeight = measured.Height + (2 * padding);

        if (HasPageContent && Cursor + boxHeight > contentHeight + Eps)
        {
            Flush();
            Cursor = 0;
        }

        var transform = RotationAboutCenter(container, indent, boxWidth, boxHeight);

        current.Boxes.Add(OverlayBoxPlacer.BuildBox(container, measured, contentWidth, Cursor, order++, transform));
        Cursor += boxHeight;
    }

    public void PlaceSpecialContainer(Container container)
    {
        var (content, indent, boxWidth, boxHeight) = OverlayBoxPlacer.LayoutOverlay(container, contentWidth, fonts, measureImage, resolution);

        if (HasPageContent && Cursor + boxHeight > contentHeight + Eps)
        {
            Flush();
            Cursor = 0;
        }

        var transform = RotationAboutCenter(container, indent, boxWidth, boxHeight);

        current.Boxes.Add(new PositionedBox
        {
            Source = container,
            Content = content,
            Bounds = new Rect(indent, Cursor, boxWidth, boxHeight),
            Style = BoxStyle.FromContainer(container),
            Y = Cursor,
            Opacity = container.Opacity,
            Transform = transform,
            Order = order++,
        });
        Cursor += boxHeight;
    }

    public void PlaceImage(Image image)
    {
        var (imageWidth, imageHeight) = measureImage is null ? Paginator.MeasureImage(image, contentWidth) : measureImage(image, contentWidth);
        if (Cursor + imageHeight > contentHeight + Eps && HasPageContent)
        {
            Flush();
            Cursor = 0;
        }

        current.Images.Add(new PositionedImage
        {
            Source = image,
            Y = Cursor,
            Width = imageWidth,
            Height = imageHeight,
            XOffset = OverlayBoxPlacer.AlignImage(image.Alignment, contentWidth, imageWidth),
        });
        Cursor += imageHeight;
    }

    public void PlaceCode(Block block)
    {
        var (codeWidth, codeHeight) = Paginator.MeasureCode(block, fonts, resolution);
        if (Cursor + codeHeight > contentHeight + Eps && HasPageContent)
        {
            Flush();
            Cursor = 0;
        }

        current.Codes.Add(new PositionedCode
        {
            Source = block,
            Y = Cursor,
            Width = codeWidth,
            Height = codeHeight,
            XOffset = OverlayBoxPlacer.AlignImage(Paginator.CodeAlignment(block), contentWidth, codeWidth),
        });
        Cursor += codeHeight;
    }

    public void PlaceParagraph(int index, Paragraph paragraph)
    {
        if (Broken[index] is not { } lines)
        {
            return;
        }

        if (lines.Count == 0)
        {
            Cursor += paragraph.SpacingBefore.Point + paragraph.SpacingAfter.Point;
            return;
        }

        var spacingBefore = paragraph.SpacingBefore.Point;
        var spacingAfter = paragraph.SpacingAfter.Point;
        var offset = 0;
        var first = true;

        while (true)
        {
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
            while (offset + fit < lines.Count && y + lines[offset + fit].Height <= contentHeight + Eps)
            {
                y += lines[offset + fit].Height;
                fit++;
            }

            var hasNextBlock = false;
            double afterCursor = 0;
            double nextLeadingHeight = 0;
            if (fit >= remaining && first && paragraph.KeepWithNext && HasPageContent &&
                NextBlockHeightResolver.NextBlockFirstHeight(Blocks, Broken, tableLayouts, boxMeasures, index, contentWidth, fonts, measureImage, resolution, out var nextSpacingBefore, out var nextHeight))
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
                KeepTogether = paragraph.KeepTogether,
                KeepWithNext = paragraph.KeepWithNext,
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
                current.Lines.Add(new PositionedLine { Line = box, Source = paragraph, Y = lineY });
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

    private sealed class LineBreakVisitor(double contentWidth, FontCollection fonts, StyleResolution resolution)
        : BlockVisitor<Nothing, IReadOnlyList<LineBox>?>
    {
        protected override IReadOnlyList<LineBox>? Default(Block block, Nothing context) => null;

        public override IReadOnlyList<LineBox>? Visit(Paragraph paragraph, Nothing context)
            => LineBreaker.Break(paragraph, contentWidth, fonts, resolution.Alignment(paragraph), resolution);
    }
}
