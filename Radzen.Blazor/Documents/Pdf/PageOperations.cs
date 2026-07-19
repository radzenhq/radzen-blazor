using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf.Emit;

namespace Radzen.Documents.Pdf;

internal static class PageOperations
{
    internal static Document Snapshot(Document source)
    {
        var bytes = source.ToArray();
        var limits = source.Loaded?.Source?.Limits ?? ReaderLimits.Default;
        return Document.LoadFromStream(new MemoryStream(bytes, writable: false), limits);
    }

    internal static bool CanImportDirectly(Document target, Document source, int offset, int length)
    {
        if (ReferenceEquals(target, source) || source.FormFields.Count > 0
            || source.Loaded is not { Source: not null } state)
        {
            return false;
        }

        for (var index = offset; index < offset + length; index++)
        {
            var page = source.Pages[index];
            if (!page.ContentIsIntact || page.Annotations.Count > 0
                || !state.SourceResources.ContainsKey(page)
                || page.Rotate != (state.SourceRotations.TryGetValue(page, out var rotation) ? rotation : 0))
            {
                return false;
            }
        }

        return true;
    }

    internal static IReadOnlyList<Page> ImportIsolated(Document target, Document source, int offset, int length)
    {
        var staging = new Document();
        Import(staging, source, offset, length);
        return Import(target, Snapshot(staging), 0, length);
    }

    internal static Document Extract(Document snapshot, int offset, int length)
    {
        var result = new Document();
        Import(result, snapshot, offset, length);
        return result;
    }

    internal static IReadOnlyList<Page> Import(Document target, Document snapshot, int offset, int length)
    {
        var imported = new List<Page>(length);
        var targetPageOffset = target.Pages.Count;
        for (var index = offset; index < offset + length; index++)
        {
            var source = snapshot.Pages[index];
            var page = DocumentMerger.AppendPage(
                target, snapshot, source, offset, length, targetPageOffset);
            imported.Add(page);
        }

        return imported;
    }
}
