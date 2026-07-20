using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.5.7: /Type /ObjStm - a header of "number offset" pairs followed by concatenated bodies, Flate-compressed.
internal sealed class ObjectStreamBuilder
{
    private readonly List<(int Number, byte[] Body)> members = [];

    public int Count => members.Count;

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

        return FlateFilter.EncodeStream(payload.ToArray(), dictionary =>
        {
            dictionary["Type"] = new NameObject("ObjStm");
            dictionary["N"] = new NumberObject(members.Count);
            dictionary["First"] = new NumberObject(first);
        });
    }
}
