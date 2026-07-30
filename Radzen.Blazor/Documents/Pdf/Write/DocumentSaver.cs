using Radzen.Documents.Pdf.Objects;
using System;
using System.IO;

namespace Radzen.Documents.Pdf.Write;

internal sealed class DocumentSaver(PortableDocument document)
{
    internal void Save(Stream stream)
    {
        var graph = (document.MaterializedGraph ?? new DocumentMaterializer(document).Materialize()).CopyForSerialization();
        var writer = new DocumentWriter(stream, graph)
        {
            Encryption = graph.Encryption,
            UseCompressedStreams = graph.UseCompressedStreams,
        };
        writer.Close();
    }

    internal static XmpMetadata BaseXmp(DocumentInfo info) => DocumentMaterializer.BaseXmp(info);

    internal static DictionaryObject? BuildInfo(DocumentInfo meta, DictionaryObject? source, GraphImporter? importer = null)
        => DocumentMaterializer.BuildInfo(meta, source, importer);

    internal static string PdfDate(DateTimeOffset value) => DocumentMaterializer.PdfDate(value);
}
