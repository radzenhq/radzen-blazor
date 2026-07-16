using System;

namespace Radzen.Documents.Pdf.Emit;

// The laid-out content of a header or footer band: a band never page-breaks, so every block
// places whole at a running cursor and the band's Height is the final cursor.
internal sealed class BandLayout
{
    public PageLayer Content { get; } = new();

    public double Height { get; set; }
}

// Lays out header/footer bands and resolves the section page size. Bands share the section
// body's block/box/table/image placement helpers but run at a single non-breaking cursor.
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
        // Lists expand to marker paragraphs exactly as in section content.
        foreach (var block in BlockExpander.ExpandBlocks(band.Blocks, width, resolution: resolution))
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
                result.Content.Tables.Add(new PositionedTableFragment
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
            var (imageWidth, imageHeight) = measureImage is null ? Paginator.MeasureImage(image, width) : measureImage(image, width);
            result.Content.Images.Add(new PositionedImage
            {
                Source = image,
                Y = Cursor,
                Width = imageWidth,
                Height = imageHeight,
                XOffset = OverlayBoxPlacer.AlignImage(image.Alignment, width, imageWidth),
            });
            Cursor += imageHeight;
            return default;
        }

        public override Nothing Visit(QrCode block, Nothing context) => VisitCode(block);

        public override Nothing Visit(Barcode block, Nothing context) => VisitCode(block);

        private Nothing VisitCode(Block block)
        {
            var (codeWidth, codeHeight) = Paginator.MeasureCode(block, fonts, resolution);
            result.Content.Codes.Add(new PositionedCode
            {
                Source = block,
                Y = Cursor,
                Width = codeWidth,
                Height = codeHeight,
                XOffset = OverlayBoxPlacer.AlignImage(Paginator.CodeAlignment(block), width, codeWidth),
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
                result.Content.Lines.Add(new PositionedLine { Line = box, Source = paragraph, Y = y });
                y += box.Height;
            }

            Cursor = y + paragraph.SpacingAfter.Point;
            return default;
        }
    }

    public static (double Width, double Height) EffectiveSize(Section section)
    {
        var width = section.PageSize.Width.Point;
        var height = section.PageSize.Height.Point;
        return section.Orientation == PageOrientation.Landscape ? (height, width) : (width, height);
    }
}
