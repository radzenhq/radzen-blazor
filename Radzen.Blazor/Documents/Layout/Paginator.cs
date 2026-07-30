using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;


internal static class Paginator
{
    public static ImmutableArray<LaidOutPage> Paginate(
        Section section,
        FontCollection fonts,
        LoweringContext resolution,
        Func<Image, double, (double Width, double Height)>? measureImage = null,
        IReadOnlyDictionary<string, int>? tocPages = null,
        LayoutCaptureContext? capture = null,
        int pageNumberOffset = 0)
    {
        capture ??= new LayoutCaptureContext();
        var pages = new List<LaidOutPage>();
        PaginateSection(
            section,
            fonts,
            pages,
            measureImage,
            resolution,
            capture,
            tocPages,
            pageNumberOffset);
        return [.. pages];
    }

    private static void PaginateSection(
        Section section,
        FontCollection fonts,
        List<LaidOutPage> pages,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture,
        IReadOnlyDictionary<string, int>? tocPages = null,
        int pageNumberOffset = 0)
    {
        var context = new PaginationContext(
            section,
            fonts,
            pages,
            measureImage,
            resolution,
            capture,
            tocPages,
            pageNumberOffset);
        var placer = new SectionPlacer(context);
        for (var i = 0; i < context.Blocks.Count; i++)
        {
            context.PrepareBlock(i);
            LoweredBlockDispatch.Dispatch(context.Blocks[i], placer, i);
        }

        context.Finish();
    }

    private sealed class SectionPlacer(PaginationContext context)
        : ILoweredBlockHandler<int, Nothing>
    {
        public Nothing PageBreak(PageBreak block, int index)
        {
            context.PlaceBreak();
            return default;
        }

        public Nothing Table(Table table, int index)
        {
            context.PlaceTable(index, table);
            return default;
        }

        public Nothing Container(Container container, int index)
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

        public Nothing Image(Image image, int index)
        {
            context.PlaceImage(image);
            return default;
        }

        public Nothing CodeSymbol(Block block, int index)
        {
            context.PlaceCodeSymbol(block);
            return default;
        }

        public Nothing Paragraph(Paragraph para, int index)
        {
            context.PlaceParagraph(index, para);
            return default;
        }
    }

    internal static (double Width, double Height) MeasureImage(Image image, double availableWidth)
        => ImageProbe.Measure(image, availableWidth);

    internal static (double Width, double Height) MeasureCodeSymbol(Block block, FontCollection fonts, LoweringContext resolution) => CodeSymbolDispatch.Measure(block, fonts, resolution);

    internal static HorizontalAlignment CodeSymbolAlignment(Block block) => CodeSymbolDispatch.Alignment(block);

}
