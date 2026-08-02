using System;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.3.8.
internal sealed class StreamObject(ReadOnlyMemory<byte> data) : DocumentObject
{
    public ReadOnlyMemory<byte> Data { get; } = data;

    public DictionaryObject Dictionary { get; } = new();

    internal override void Write(Stream stream, WriteContext context)
    {
        var payload = Data;
        var encryptor = context.Encryptor;
        if (encryptor is not null)
        {
            payload = encryptor.EncryptStream(Data, context.ObjectNumber, context.Generation, Dictionary);
        }

        PdfBytes.WriteAscii(stream, "<< /Length ");
        PdfBytes.WriteInteger(stream, payload.Length);

        foreach (var key in Dictionary.Keys)
        {
            if (string.Equals(key, "Length", StringComparison.Ordinal))
            {
                continue;
            }

            stream.WriteByte((byte)' ');
            NameObject.WriteEscaped(stream, key);
            stream.WriteByte((byte)' ');
            Dictionary[key].Write(stream, context);
        }

        PdfBytes.WriteAscii(stream, " >>\nstream\n");
        stream.Write(payload.Span);
        PdfBytes.WriteAscii(stream, "\nendstream");
    }
}
