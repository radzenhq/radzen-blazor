using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emission;

internal sealed class EmissionPageMap
{
    private readonly Dictionary<PageEmissionPlan, int> indexes = [];
    private readonly List<PageEmissionPlan?> byPageIndex = [];
    private readonly List<PageEmissionPlan> planned = [];

    private EmissionPageMap()
    {
    }

    public IReadOnlyList<PageEmissionPlan> Planned => planned;

    public static EmissionPageMap Build(IReadOnlyList<Page> pages)
    {
        var map = new EmissionPageMap();
        for (var index = 0; index < pages.Count; index++)
        {
            var plan = pages[index].EmissionIdentity;
            map.byPageIndex.Add(plan);
            if (plan is not null)
            {
                map.indexes.TryAdd(plan, index);
                map.planned.Add(plan);
            }
        }

        return map;
    }

    public PageEmissionPlan? PlanAt(int pageIndex) => byPageIndex[pageIndex];

    public int IndexOf(PageEmissionPlan page, string feature)
        => indexes.TryGetValue(page, out var index)
            ? index
            : throw new InvalidOperationException(
                $"The {feature} of this document references a rendered page that is no longer in Pages. "
                + "Removing a page from a rendered document leaves the structure tree and named destinations "
                + "pointing at content that is gone. Re-render the document after the page change, or drop the "
                + "feature (save without PDF/UA or PDF/A Level A conformance and without anchor destinations).");
}
