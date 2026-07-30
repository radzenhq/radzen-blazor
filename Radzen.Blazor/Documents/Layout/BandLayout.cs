using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal sealed class BandLayout(LayoutCaptureContext capture)
{
    public PageLayerBuilder Content { get; } = new(capture);

    public double Height { get; set; }
}

internal static class BandLayouter
{
    public static BandLayout Layout(
        HeaderFooter band,
        double width,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture)
    {
        var result = new BandLayout(capture);
        var visitor = new BandVisitor(result, width, fonts, measureImage, resolution, capture);
        foreach (var block in BlockExpander.ExpandBlocks(band.Blocks, width, resolution))
        {
            LoweredBlockDispatch.Dispatch(block, visitor, default(Nothing));
        }

        result.Height = visitor.Cursor;
        return result;
    }

    private sealed class BandVisitor(
        BandLayout result,
        double width,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture)
        : ILoweredBlockHandler<Nothing, Nothing>
    {
        private int order;

        public double Cursor { get; private set; }

        private void PlaceMarker(Block block)
        {
            if (resolution.ListMarker(block) is { } marker)
            {
                result.Content.Lines.Add(new LaidOutLine
                {
                    Line = LineLayouter.MarkerLine(marker, fonts, capture),
                    Source = capture.Source(block),
                    X = 0,
                    Y = Cursor,
                });
            }
        }

        public Nothing Container(Container container, Nothing context)
        {
            var indent = resolution.BlockIndent(container);
            var availableWidth = Math.Max(0, width - indent);
            var measured = OverlayBoxPlacer.MeasureBox(
                container,
                availableWidth,
                fonts,
                measureImage,
                resolution,
                capture);
            var box = OverlayBoxPlacer.BuildBox(
                container,
                measured,
                availableWidth,
                Cursor,
                order++,
                transform: null,
                resolution,
                indent);
            PlaceMarker(container);
            result.Content.Boxes.Add(box);
            Cursor += box.Bounds.Height;
            return default;
        }

        public Nothing Table(Table table, Nothing context)
        {
            var layout = LoweredBlockDispatch.LayoutTable(
                table,
                width,
                fonts,
                measureImage,
                resolution,
                capture);
            var tableOrder = order++;
            PlaceMarker(table);
            foreach (var fragment in TablePaginator.Paginate(
                layout, table, double.PositiveInfinity, capture))
            {
                result.Content.Tables.Add(FlowContentPlacer.Table(
                    table,
                    layout,
                    fragment,
                    Cursor,
                    tableOrder,
                    capture));
                Cursor += fragment.Height;
            }

            return default;
        }

        public Nothing Image(Image image, Nothing context)
        {
            var indent = resolution.BlockIndent(image);
            var availableWidth = Math.Max(0, width - indent);
            var (imageWidth, imageHeight) = FlowContentPlacer.MeasureImage(image, availableWidth, measureImage);
            PlaceMarker(image);
            result.Content.Images.Add(FlowContentPlacer.Image(
                image,
                availableWidth,
                Cursor,
                imageWidth,
                imageHeight,
                capture,
                indent));
            Cursor += imageHeight;
            return default;
        }

        public Nothing CodeSymbol(Block block, Nothing context)
        {
            var indent = resolution.BlockIndent(block);
            var availableWidth = Math.Max(0, width - indent);
            var (codeSymbolWidth, codeSymbolHeight) = Paginator.MeasureCodeSymbol(block, fonts, resolution);
            PlaceMarker(block);
            result.Content.CodeSymbols.Add(FlowContentPlacer.CodeSymbol(
                block,
                availableWidth,
                Cursor,
                codeSymbolWidth,
                codeSymbolHeight,
                fonts,
                resolution,
                capture,
                indent));
            Cursor += codeSymbolHeight;
            return default;
        }

        public Nothing PageBreak(PageBreak block, Nothing context) => default;

        public Nothing Paragraph(Paragraph paragraph, Nothing context)
        {
            var lines = LineBreaker.Break(
                paragraph,
                width,
                fonts,
                resolution.Alignment(paragraph),
                resolution,
                capture);
            var format = resolution.Format(paragraph);
            var y = Cursor + format.SpacingBefore.Point;
            foreach (var box in lines)
            {
                result.Content.Lines.Add(new LaidOutLine
                {
                    Line = box,
                    Source = capture.Source(paragraph),
                    X = 0,
                    Y = y,
                });
                y += box.Height;
            }

            Cursor = y + format.SpacingAfter.Point;
            return default;
        }
    }

    public static (double Width, double Height) EffectiveSize(Section section)
    {
        var (width, height) = section.PageSize.Effective(section.Orientation);
        return (width.Point, height.Point);
    }
}
