using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Output;

namespace Radzen.Documents.Pdf.Write;

internal sealed class PageOutputMap
{
    private readonly Dictionary<PageOutput, int> indexes = [];
    private readonly List<PageOutput?> byPageIndex = [];
    private readonly List<PageOutput> planned = [];

    private PageOutputMap()
    {
    }

    public IReadOnlyList<PageOutput> Planned => planned;

    public static PageOutputMap Build(IReadOnlyList<Page> pages)
    {
        var map = new PageOutputMap();
        for (var index = 0; index < pages.Count; index++)
        {
            var plan = pages[index].OutputIdentity;
            map.byPageIndex.Add(plan);
            if (plan is not null)
            {
                map.indexes.TryAdd(plan, index);
                map.planned.Add(plan);
            }
        }

        return map;
    }

    public PageOutput? PlanAt(int pageIndex) => byPageIndex[pageIndex];

    public int IndexOf(PageOutput page, string feature)
        => indexes.TryGetValue(page, out var index)
            ? index
            : throw new InvalidOperationException(
                $"The {feature} of this document references a rendered page that is no longer in Pages. "
                + "Removing a page from a rendered document leaves the structure tree and named destinations "
                + "pointing at content that is gone. Re-render the document after the page change, or drop the "
                + "feature (save without PDF/UA or PDF/A Level A conformance and without anchor destinations).");
}
