using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

// The keep-with-next look-ahead. Shares the per-section layout caches with the main placement
// loop and warms them as a side effect, so a later placement reuses the measurement.
internal static class NextBlockHeightResolver
{
    // The required height of the header rows plus the first body ROW GROUP: the minimum a
    // table needs on a page before its first fragment breaks early. The first group is the
    // rowspan closure TablePaginator force-places as one unit, so it must be measured whole
    // or the flush check would let a tall group spill past the page bottom.
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

    // Default reports no minimum: a block kind a page can start mid-way, or that never
    // breaks meaningfully here.
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
            if (OverlayBoxPlacer.IsSpecial(container))
            {
                return default;
            }

            // A Stack container never splits, so its first height is the whole box.
            var measured = boxMeasures[next] ??= OverlayBoxPlacer.MeasureBox(container, contentWidth, fonts, measureImage, resolution);
            return new NextBlockHeight { Found = true, Height = measured.Height + (2 * container.Padding.Point) };
        }

        public override NextBlockHeight Visit(Image image, int next)
        {
            var (_, imageHeight) = measureImage is null ? Paginator.MeasureImage(image, contentWidth) : measureImage(image, contentWidth);
            return new NextBlockHeight { Found = true, Height = imageHeight };
        }

        public override NextBlockHeight Visit(QrCode block, int next) => VisitCode(block);

        public override NextBlockHeight Visit(Barcode block, int next) => VisitCode(block);

        private NextBlockHeight VisitCode(Block block) => new() { Found = true, Height = Paginator.MeasureCode(block, fonts, resolution).Height };
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
