using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf.Emit;

namespace Radzen.Documents.Pdf;

internal static class PageOperations
{
    // The re-parse runs under the limits the source itself was loaded with, so a host that
    // tightened them against hostile input keeps that budget for imported content instead of
    // silently falling back to the defaults.
    internal static Document Snapshot(Document source)
    {
        var bytes = source.ToArray();
        var limits = source.Loaded?.Source?.Limits ?? ReaderLimits.Default;
        return Document.LoadFromStream(new MemoryStream(bytes, writable: false), limits);
    }

    // A page still holding exactly the bytes, resources and rotation it was loaded with
    // re-serializes from those alone, so DocumentMerger can copy it straight off the live
    // source and the snapshot round trip is pure waste. Everything else (authored or edited
    // content, a queued overlay, modeled annotations, document-level form fields) only turns
    // into bytes when the source is saved, so it still needs the snapshot.
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

    internal static Document Extract(Document snapshot, int offset, int length)
    {
        var result = new Document();
        Import(result, snapshot, offset, length);
        return result;
    }

    internal static IReadOnlyList<Page> Import(Document target, Document snapshot, int offset, int length)
    {
        var imported = new List<Page>(length);
        for (var index = offset; index < offset + length; index++)
        {
            var source = snapshot.Pages[index];
            var page = DocumentMerger.AppendPage(target, snapshot, source);
            page.BleedBox = source.BleedBox;
            page.TrimBox = source.TrimBox;
            page.ArtBox = source.ArtBox;
            imported.Add(page);
        }

        return imported;
    }
}
