using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;


internal static class Paginator
{
    public static ImmutableArray<LaidOutPage> Paginate(
        Section section,
        FontCollection fonts,
        LoweringResult resolution,
        LayoutCaptureContext capture,
        Func<Image, double, (double Width, double Height)>? measureImage = null,
        IReadOnlyDictionary<string, int>? tocPages = null,
        int pageNumberOffset = 0,
        int sectionIndex = 0)
    {
        var pages = new List<LaidOutPage>();
        PaginateSection(
            section,
            fonts,
            pages,
            measureImage,
            resolution,
            capture,
            tocPages,
            pageNumberOffset,
            sectionIndex);
        return [.. pages];
    }

    private static void PaginateSection(
        Section section,
        FontCollection fonts,
        List<LaidOutPage> pages,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringResult resolution,
        LayoutCaptureContext capture,
        IReadOnlyDictionary<string, int>? tocPages,
        int pageNumberOffset,
        int sectionIndex)
    {
        var context = new PaginationContext(
            fonts,
            pages,
            measureImage,
            resolution,
            capture,
            pageNumberOffset,
            sectionIndex);
        context.Initialize(section, tocPages);
        var engine = FlowPlacementEngine.ForPages(context);
        for (var i = 0; i < context.Blocks.Count; i++)
        {
            context.PrepareBlock(i);
            engine.Place(context.Blocks[i], i);
        }

        context.Finish();
    }

}
