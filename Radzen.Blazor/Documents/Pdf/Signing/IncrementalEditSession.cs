using System;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Signing;

internal readonly record struct IncrementalEditSession(
    DocumentReader Reader, ReferenceObject RootReference, DictionaryObject Catalog, IncrementalUpdateWriter Writer)
{
    internal static IncrementalEditSession Begin(byte[] pdf, string operation)
    {
        var reader = DocumentReader.Parse(pdf);
        if (reader.IsEncrypted)
        {
            throw new NotSupportedException($"{operation} encrypted documents is not supported.");
        }

        if (!(reader.Trailer.TryGetValue("Root", out var root) && root is ReferenceObject rootRef
            && reader.Resolve(rootRef) is DictionaryObject catalog))
        {
            throw new DocumentParseException("The trailer /Root must reference the document catalog.", -1);
        }

        var writer = new IncrementalUpdateWriter(pdf, reader);
        return new IncrementalEditSession(reader, rootRef, catalog, writer);
    }
}
