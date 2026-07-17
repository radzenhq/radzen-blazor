using System;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// A PDF stream object (ISO 32000-1 section 7.3.8): a dictionary followed by
/// raw byte data between the <c>stream</c> and <c>endstream</c> keywords. The
/// <c>/Length</c> entry is emitted automatically from the data byte count.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StreamObject"/> class with the
/// given raw stream data.
/// </remarks>
public sealed class StreamObject(ReadOnlyMemory<byte> data) : DocumentObject
{
    /// <summary>
    /// Gets the raw stream data. A parsed stream windows the file buffer rather than
    /// owning a copy, so the payload must never be written through.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; } = data;

    /// <summary>
    /// Gets the stream dictionary. The <c>/Length</c> entry is added
    /// automatically at serialization time and need not be set here.
    /// </summary>
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
