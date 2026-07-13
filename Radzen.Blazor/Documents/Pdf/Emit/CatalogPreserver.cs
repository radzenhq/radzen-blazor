using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

// Carries a loaded document's page tree and catalog features across a re-save:
// marks removed pages so stale references collapse, and copies catalog-level
// entries this writer does not build itself onto the new catalog.
internal sealed class CatalogPreserver(Document document)
{
    // Catalog keys this writer builds itself; a preserved source catalog must not
    // overwrite them (or drag in a duplicate sub-graph for them).
    private static readonly HashSet<string> ManagedCatalogKeys = new(StringComparer.Ordinal)
    {
        "Type", "Pages", "AcroForm", "Names", "AF", "OutputIntents", "MarkInfo", "StructTreeRoot",
    };

    // Marks every source page node that was loaded but removed from Pages before
    // saving so preserved destinations and annotation /P links that still point at
    // them collapse to null instead of resurrecting the page (and its content).
    public HashSet<DictionaryObject> PruneRemovedPages(GraphImporter importer)
    {
        var removed = new HashSet<DictionaryObject>();
        var kept = new HashSet<Page>(document.Pages);
        foreach (var pair in document.sourcePages)
        {
            if (!kept.Contains(pair.Key))
            {
                importer.Prune(pair.Value);
                removed.Add(pair.Value);
            }
        }

        return removed;
    }

    // Carries catalog-level features a loaded document declared - /Outlines,
    // /PageLabels, /OpenAction, /ViewerPreferences, /Metadata, /Lang - that this
    // writer does not build itself. Page-referencing entries repoint onto the new
    // page tree; entries pointing at a removed page collapse to null.
    public void PreserveCatalog(GraphImporter importer, DictionaryObject catalog, ReferenceObject pagesRef)
    {
        var source = document.source;
        var sourceCatalog = document.sourceCatalog;
        if (sourceCatalog is null || source is null)
        {
            return;
        }

        if (source.GetDictionary(sourceCatalog, "Pages") is { } sourcePagesNode)
        {
            importer.Seed(sourcePagesNode, pagesRef);
        }

        foreach (var key in sourceCatalog.Keys)
        {
            if (ManagedCatalogKeys.Contains(key) || catalog.ContainsKey(key))
            {
                continue;
            }

            // A conforming save (PDF/A or PDF/UA) writes its own XMP and overwrites
            // catalog["Metadata"]; importing the source stream first would leave it
            // orphaned. Keep the source's XMP only when neither is requested.
            if (string.Equals(key, "Metadata", StringComparison.Ordinal)
                && (document.Conformance != PdfAConformance.None || document.PdfUA))
            {
                continue;
            }

            catalog[key] = importer.ImportValue(sourceCatalog[key]);
        }
    }
}
