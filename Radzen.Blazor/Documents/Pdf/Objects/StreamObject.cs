using System;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// A PDF stream object (ISO 32000-1 section 7.3.8): a dictionary followed by
/// raw byte data between the <c>stream</c> and <c>endstream</c> keywords. The
/// <c>/Length</c> entry is emitted automatically from the data byte count.
/// </summary>
public sealed class StreamObject : DocumentObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamObject"/> class.
    /// </summary>
    /// <param name="data">The raw stream data.</param>
    public StreamObject(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Data = data;
    }

    /// <summary>
    /// Gets the raw stream data.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets the stream dictionary. The <c>/Length</c> entry is added
    /// automatically at serialization time and need not be set here.
    /// </summary>
    public DictionaryObject Dictionary { get; } = new();

    internal override void Write(Stream stream, WriteContext context)
    {
        var data = Data;
        var encryptor = context.Encryptor;
        if (encryptor is not null)
        {
            // Pass the dictionary so a /Type /Metadata stream is left plaintext when the
            // writer's /EncryptMetadata flag is false.
            data = encryptor.EncryptStream(Data, context.ObjectNumber, context.Generation, Dictionary);
        }

        PdfBytes.WriteAscii(stream, "<< /Length ");
        PdfBytes.WriteInteger(stream, data.Length);

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
        stream.Write(data, 0, data.Length);
        PdfBytes.WriteAscii(stream, "\nendstream");
    }
}
