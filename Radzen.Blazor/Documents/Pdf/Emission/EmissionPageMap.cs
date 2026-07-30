using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emission;

internal sealed class EmissionPageMap
{
    private readonly Dictionary<PageEmissionPlan, int> indexes = [];

    private EmissionPageMap()
    {
    }

    public static EmissionPageMap Build(IReadOnlyList<Page> pages)
    {
        var map = new EmissionPageMap();
        for (var index = 0; index < pages.Count; index++)
        {
            if (pages[index].Generated is { } generated)
            {
                map.indexes.TryAdd(generated, index);
            }
        }

        return map;
    }

    public int IndexOf(PageEmissionPlan page, string feature)
        => indexes.TryGetValue(page, out var index)
            ? index
            : throw new InvalidOperationException(
                $"The {feature} of this document references a rendered page that is no longer in Pages. "
                + "Removing a page from a rendered document leaves the structure tree and named destinations "
                + "pointing at content that is gone. Re-render the document after the page change, or drop the "
                + "feature (save without PDF/UA or PDF/A Level A conformance and without anchor destinations).");
}
