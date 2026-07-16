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
        "Type", "Pages", "AcroForm", "AF", "OutputIntents", "MarkInfo", "StructTreeRoot",
    };

    // /Names is a container of independent branches rather than a single managed entry.
    // Only /EmbeddedFiles is this writer's: the loader turns it into document.Attachments
    // and the AttachmentWriter re-emits it, so importing it too would orphan a duplicate.
    private static readonly HashSet<string> ManagedNameTreeBranches = new(StringComparer.Ordinal)
    {
        "EmbeddedFiles",
    };

    // Marks every source page node that was loaded but removed from Pages before
    // saving so preserved destinations and annotation /P links that still point at
    // them collapse to null instead of resurrecting the page (and its content).
    public HashSet<DictionaryObject> PruneRemovedPages(GraphImporter importer)
    {
        var removed = new HashSet<DictionaryObject>();
        if (document.Loaded is not { } loaded)
        {
            return removed;
        }

        var kept = new HashSet<Page>(document.Pages);
        foreach (var pair in loaded.SourcePages)
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
        var source = document.Loaded?.Source;
        var sourceCatalog = document.Loaded?.SourceCatalog;
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

            if ((string.Equals(key, "Outlines", StringComparison.Ordinal) && document.OutlineChanged)
                || (string.Equals(key, "PageLabels", StringComparison.Ordinal) && document.PageLabelsChanged)
                || (string.Equals(key, "Metadata", StringComparison.Ordinal) && document.Xmp.IsModified))
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

            if (string.Equals(key, "Names", StringComparison.Ordinal))
            {
                PreserveNames(importer, source, catalog, sourceCatalog);
                continue;
            }

            catalog[key] = importer.ImportValue(sourceCatalog[key]);
        }

    }

    // Carries the source /Names branches this writer does not build itself - /Dests
    // above all, without which every re-emitted link annotation keeps a /D (name) that
    // resolves to nothing. Page references inside an imported branch repoint onto the
    // new page tree (kept pages are seeded, removed ones pruned to null), so a
    // preserved destination still lands on its own page after renumbering. The tree is
    // left inline so the attachment and navigation writers can still merge into it.
    private static void PreserveNames(GraphImporter importer, DocumentReader source, DictionaryObject catalog, DictionaryObject sourceCatalog)
    {
        if (source.GetDictionary(sourceCatalog, "Names") is not { } names)
        {
            return;
        }

        var tree = new DictionaryObject();
        foreach (var branch in names.Keys)
        {
            if (!ManagedNameTreeBranches.Contains(branch))
            {
                tree[branch] = importer.ImportValue(names[branch]);
            }
        }

        if (tree.Keys.Count > 0)
        {
            catalog["Names"] = tree;
        }
    }
}
