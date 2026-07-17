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

    public int Order { get; init; }
}

internal readonly struct PositionedBox
{
    public required Container Source { get; init; }

    public required LaidOutBoxContent Content { get; init; }

    public required Rect Bounds { get; init; }

    public required BoxStyle Style { get; init; }

    public required double Y { get; init; }

    public required double Opacity { get; init; }

    public Matrix? Transform { get; init; }

    public int Order { get; init; }
}

internal readonly struct PositionedImage
{
    public required Image Source { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    public double XOffset { get; init; }
}

internal readonly struct PositionedCode
{
    public required Block Source { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    public double XOffset { get; init; }
}

internal sealed class PageLayer
{
    public List<PositionedLine> Lines { get; } = [];

    public List<PositionedImage> Images { get; } = [];

    public List<PositionedCode> Codes { get; } = [];

    public List<PositionedTableFragment> Tables { get; } = [];

    public List<PositionedBox> Boxes { get; } = [];

    public bool HasContent => Lines.Count > 0 || Images.Count > 0 || Codes.Count > 0 || Tables.Count > 0 || Boxes.Count > 0;
}

internal sealed class PaginatedPage
{
    public required PageSize Size { get; init; }

    public required Rect ContentBox { get; init; }

    public required int Number { get; init; }

    public required PageLayer Body { get; init; }

    public required PageLayer HeaderLayer { get; init; }

    public required PageLayer FooterLayer { get; init; }

    internal IReadOnlyList<PositionedLine> Lines => Body.Lines;

    internal IReadOnlyList<PositionedLine> Header => HeaderLayer.Lines;

    internal IReadOnlyList<PositionedLine> Footer => FooterLayer.Lines;

    internal IReadOnlyList<PositionedImage> Images => Body.Images;

    internal IReadOnlyList<PositionedCode> Codes => Body.Codes;

    internal IReadOnlyList<PositionedTableFragment> Tables => Body.Tables;

    internal IReadOnlyList<PositionedBox> Boxes => Body.Boxes;

    internal IReadOnlyList<PositionedImage> HeaderImages => HeaderLayer.Images;

    internal IReadOnlyList<PositionedCode> HeaderCodes => HeaderLayer.Codes;

    internal IReadOnlyList<PositionedTableFragment> HeaderTables => HeaderLayer.Tables;

    internal IReadOnlyList<PositionedBox> HeaderBoxes => HeaderLayer.Boxes;

    internal IReadOnlyList<PositionedImage> FooterImages => FooterLayer.Images;

    internal IReadOnlyList<PositionedCode> FooterCodes => FooterLayer.Codes;

    internal IReadOnlyList<PositionedTableFragment> FooterTables => FooterLayer.Tables;

    internal IReadOnlyList<PositionedBox> FooterBoxes => FooterLayer.Boxes;

    public double HeaderTop { get; init; }

    public double FooterTop { get; init; }

}

internal static class Paginator
{
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
        var context = new PaginationContext(section, fonts, pages, measureImage, resolution, tocPages);
        var placer = new SectionPlacer(context);
        for (var i = 0; i < context.Blocks.Count; i++)
        {
            context.Blocks[i].Accept(placer, i);
        }

        context.Finish();
    }

    private sealed class SectionPlacer(PaginationContext context)
        : BlockVisitor<int, Nothing>
    {
        protected override Nothing Default(Block block, int index)
            => throw new NotSupportedException($"Block type '{block.GetType().Name}' is not supported in section content.");

        public override Nothing Visit(PageBreak block, int index)
        {
            context.PlaceBreak();
            return default;
        }

        public override Nothing Visit(Table table, int index)
        {
            context.PlaceTable(index, table);
            return default;
        }

        public override Nothing Visit(Container container, int index)
        {
            if (OverlayBoxPlacer.IsSpecial(container))
            {
                context.PlaceSpecialContainer(container);
            }
            else
            {
                context.PlaceBox(index, container);
            }

            return default;
        }

        public override Nothing Visit(Image image, int index)
        {
            context.PlaceImage(image);
            return default;
        }

        public override Nothing Visit(QrCode block, int index)
        {
            context.PlaceCode(block);
            return default;
        }

        public override Nothing Visit(Barcode block, int index)
        {
            context.PlaceCode(block);
            return default;
        }

        public override Nothing Visit(Paragraph para, int index)
        {
            context.PlaceParagraph(index, para);
            return default;
        }
    }

    internal static (double Width, double Height) MeasureImage(Image image, double availableWidth)
        => ImageDecoder.Measure(image, ImageDecoder.Decode(image.Data), availableWidth);

    internal static (double Width, double Height) MeasureCode(Block block, FontCollection fonts, StyleResolution resolution) => CodeBlockDispatch.Measure(block, fonts, resolution);

    internal static HorizontalAlignment CodeAlignment(Block block) => CodeBlockDispatch.Alignment(block);

    internal static IReadOnlyList<Block> ExpandBlocks(
        BlockCollection blocks,
        double availableWidth,
        bool keepSpecialContainers = false,
        IReadOnlyDictionary<string, int>? tocPages = null,
        FontCollection? fonts = null,
        StyleResolution? resolution = null)
        => BlockExpander.ExpandBlocks(blocks, availableWidth, keepSpecialContainers, tocPages, fonts, resolution);
}
