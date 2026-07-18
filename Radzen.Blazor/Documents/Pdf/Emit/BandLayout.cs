using System;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class BandLayout
{
    public PageLayer Content { get; } = new();

    public double Height { get; set; }
}

internal static class BandLayouter
{
    public static BandLayout Layout(
        HeaderFooter band,
        double width,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution)
    {
        var result = new BandLayout();
        var visitor = new BandVisitor(result, width, fonts, measureImage, resolution);
        foreach (var block in BlockExpander.ExpandBlocks(band.Blocks, width, resolution: resolution))
        {
            block.Accept(visitor, default);
        }

        result.Height = visitor.Cursor;
        return result;
    }

    private sealed class BandVisitor(
        BandLayout result,
        double width,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        StyleResolution resolution)
        : BlockVisitor<Nothing, Nothing>
    {
        private int order;

        public double Cursor { get; private set; }

        protected override Nothing Default(Block block, Nothing context)
            => throw new NotSupportedException($"Block type '{block.GetType().Name}' is not supported in a header/footer band.");

        public override Nothing Visit(Container container, Nothing context)
        {
            var measured = OverlayBoxPlacer.MeasureBox(container, width, fonts, measureImage, resolution);
            var box = OverlayBoxPlacer.BuildBox(container, measured, width, Cursor, order++, transform: null);
            result.Content.Boxes.Add(box);
            Cursor += box.Bounds.Height;
            return default;
        }

        public override Nothing Visit(Table table, Nothing context)
        {
            var layout = TableLayout.Layout(table, Math.Max(0, width - table.LeftIndent.Point), fonts, measureImage, resolution);
            var tableOrder = order++;
            foreach (var fragment in TablePaginator.Paginate(layout, table, double.PositiveInfinity))
            {
                result.Content.Tables.Add(FlowContentPlacer.Table(layout, fragment, Cursor, tableOrder));
                Cursor += fragment.Height;
            }

            return default;
        }

        public override Nothing Visit(Image image, Nothing context)
        {
            var (imageWidth, imageHeight) = FlowContentPlacer.MeasureImage(image, width, measureImage);
            result.Content.Images.Add(FlowContentPlacer.Image(image, width, Cursor, imageWidth, imageHeight));
            Cursor += imageHeight;
            return default;
        }

        public override Nothing Visit(QrCode block, Nothing context) => VisitCode(block);

        public override Nothing Visit(Barcode block, Nothing context) => VisitCode(block);

        private Nothing VisitCode(Block block)
        {
            var (codeWidth, codeHeight) = Paginator.MeasureCode(block, fonts, resolution);
            result.Content.Codes.Add(FlowContentPlacer.Code(block, width, Cursor, codeWidth, codeHeight));
            Cursor += codeHeight;
            return default;
        }

        public override Nothing Visit(PageBreak block, Nothing context) => default;

        public override Nothing Visit(Paragraph paragraph, Nothing context)
        {
            var lines = LineBreaker.Break(paragraph, width, fonts, resolution.Alignment(paragraph), resolution);
            var y = Cursor + paragraph.SpacingBefore.Point;
            foreach (var box in lines)
            {
                result.Content.Lines.Add(new PositionedLine { Line = box, Source = paragraph, Y = y });
                y += box.Height;
            }

            Cursor = y + paragraph.SpacingAfter.Point;
            return default;
        }
    }

    public static (double Width, double Height) EffectiveSize(Section section)
    {
        var (width, height) = section.PageSize.Effective(section.Orientation);
        return (width.Point, height.Point);
    }
}
