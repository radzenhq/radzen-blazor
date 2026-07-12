using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Documents.Pdf.Objects;

// Packs non-stream indirect objects into a /Type /ObjStm stream
// (ISO 32000-1 section 7.5.7): a header of "number offset" integer pairs
// followed by the concatenated object bodies, Flate-compressed.
internal sealed class ObjectStreamBuilder
{
    private readonly List<(int Number, byte[] Body)> members = [];

    public int Count => members.Count;

    // Serializes the object body immediately and returns the member index
    // used by the type-2 cross-reference entry.
    public int Add(int number, DocumentObject value)
    {
        using var body = new MemoryStream();
        value.Write(body);
        body.WriteByte((byte)'\n');
        members.Add((number, body.ToArray()));
        return members.Count - 1;
    }

    public StreamObject Build()
    {
        using var payload = new MemoryStream();
        var offset = 0L;
        foreach (var (number, memberBody) in members)
        {
            PdfBytes.WriteInteger(payload, number);
            payload.WriteByte((byte)' ');
            PdfBytes.WriteInteger(payload, offset);
            payload.WriteByte((byte)' ');
            offset += memberBody.Length;
        }

        var first = (int)payload.Length;
        foreach (var (_, memberBody) in members)
        {
            payload.Write(memberBody, 0, memberBody.Length);
        }

        var stream = new StreamObject(FlateFilter.Encode(payload.ToArray()));
        stream.Dictionary["Type"] = new NameObject("ObjStm");
        stream.Dictionary["N"] = new NumberObject(members.Count);
        stream.Dictionary["First"] = new NumberObject(first);
        stream.Dictionary["Filter"] = new NameObject("FlateDecode");
        return stream;
    }
}
