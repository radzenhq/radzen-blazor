using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class CatalogPreserver(Document document)
{
    private static readonly HashSet<string> ManagedCatalogKeys = new(StringComparer.Ordinal)
    {
        "Type", "Pages", "AcroForm", "AF", "OutputIntents", "MarkInfo", "StructTreeRoot",
    };

    private static readonly HashSet<string> ManagedNameTreeBranches = new(StringComparer.Ordinal)
    {
        "EmbeddedFiles",
    };

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

            if (string.Equals(key, "Metadata", StringComparison.Ordinal)
                && (document.Conformance != PdfAConformance.None || document.IsPdfUa))
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

        if (document.Structure is null && document.HasPreservableStructureGraph)
        {
            catalog["StructTreeRoot"] = importer.ImportValue(sourceCatalog["StructTreeRoot"]!);
            if (sourceCatalog.TryGetValue("MarkInfo", out var markInfo))
            {
                catalog["MarkInfo"] = importer.ImportValue(markInfo!);
            }
        }
    }

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
