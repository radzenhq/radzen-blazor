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
        IReadOnlyDictionary<string, int>? tocPages = null,
        int sectionIndex = 0)
    {
        var pages = new List<LaidOutPage>();
        PaginateSection(
            section,
            fonts,
            pages,
            resolution,
            capture,
            tocPages,
            sectionIndex);
        return [.. pages];
    }

    private static void PaginateSection(
        Section section,
        FontCollection fonts,
        List<LaidOutPage> pages,
        LoweringResult resolution,
        LayoutCaptureContext capture,
        IReadOnlyDictionary<string, int>? tocPages,
        int sectionIndex)
    {
        var context = new PaginationContext(
            fonts,
            pages,
            resolution,
            capture,
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
