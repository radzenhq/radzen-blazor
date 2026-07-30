using System;
using System.Collections.Generic;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal sealed class BlockLayoutCache(
    int count,
    double contentWidth,
    FontCollection fonts,
    Func<Image, double, (double Width, double Height)>? measureImage,
    LoweringContext lowering,
    LayoutCaptureContext capture)
{
    private readonly LaidOutTable?[] tables = new LaidOutTable?[count];
    private readonly BoxContentLayout.Measured?[] boxes = new BoxContentLayout.Measured?[count];

    internal LaidOutTable Table(int index, Table table)
        => tables[index] ??= LoweredBlockDispatch.LayoutTable(
            table,
            contentWidth,
            fonts,
            measureImage,
            lowering,
            capture);

    internal BoxContentLayout.Measured Box(int index, Container container)
        => boxes[index] ??= OverlayBoxPlacer.MeasureBox(
            container,
            Math.Max(0, contentWidth - lowering.BlockIndent(container)),
            fonts,
            measureImage,
            lowering,
            capture);
}

internal static class NextBlockHeightResolver
{
    internal static double TableFirstFragmentHeight(Table table, LaidOutTable layout)
        => TablePaginator.FirstBodyGroupHeight(layout, table);

    private readonly struct NextBlockHeight
    {
        public bool Found { get; init; }

        public double SpacingBefore { get; init; }

        public double Height { get; init; }
    }

    internal static bool NextBlockFirstHeight(
        IReadOnlyList<Block> blocks,
        IReadOnlyList<LineBox>?[] broken,
        BlockLayoutCache layouts,
        int index,
        double contentWidth,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
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

        var visitor = new NextHeightVisitor(
            broken,
            layouts,
            contentWidth,
            fonts,
            measureImage,
            resolution);
        var result = LoweredBlockDispatch.Dispatch(blocks[next], visitor, next);
        spacingBefore = result.SpacingBefore;
        height = result.Height;
        return result.Found;
    }

    private sealed class NextHeightVisitor(
        IReadOnlyList<LineBox>?[] broken,
        BlockLayoutCache layouts,
        double contentWidth,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution)
        : ILoweredBlockHandler<int, NextBlockHeight>
    {
        public NextBlockHeight Paragraph(Paragraph paragraph, int next)
            => broken[next] is { Count: > 0 } lines
                ? new NextBlockHeight { Found = true, SpacingBefore = resolution.Format(paragraph).SpacingBefore.Point, Height = lines[0].Height }
                : default;

        public NextBlockHeight Table(Table table, int next)
            => new() { Found = true, Height = TableFirstFragmentHeight(table, layouts.Table(next, table)) };

        public NextBlockHeight Container(Container container, int next)
        {
            if (OverlayBoxPlacer.IsSpecial(container))
            {
                return default;
            }

            var measured = layouts.Box(next, container);
            return new NextBlockHeight { Found = true, Height = measured.Height + (2 * container.Padding.Point) };
        }

        public NextBlockHeight Image(Image image, int next)
        {
            var availableWidth = Math.Max(0, contentWidth - resolution.BlockIndent(image));
            var (_, imageHeight) = measureImage is null
                ? Paginator.MeasureImage(image, availableWidth)
                : measureImage(image, availableWidth);
            return new NextBlockHeight { Found = true, Height = imageHeight };
        }

        public NextBlockHeight CodeSymbol(Block block, int next)
            => new() { Found = true, Height = Paginator.MeasureCodeSymbol(block, fonts, resolution).Height };

        public NextBlockHeight PageBreak(PageBreak pageBreak, int next) => default;
    }

    internal static double SumHeights(IReadOnlyList<LineBox> lines, int start, int count)
    {
        double sum = 0;
        for (var i = 0; i < count; i++)
        {
            sum += lines[start + i].Height;
        }

        return sum;
    }
}
