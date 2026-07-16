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
