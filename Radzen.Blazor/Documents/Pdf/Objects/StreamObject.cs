using System;
using System.IO;
using Radzen.Documents.Pdf.Objects.Encryption;

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

    /// <inheritdoc />
    public override void Write(Stream stream)
    {
        var data = Data;
        var encryptor = EncryptionWriter.Current;
        if (encryptor is not null)
        {
            data = encryptor.EncryptStream(Data, EncryptionWriter.CurrentObjectNumber, 0);
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
            Dictionary[key].Write(stream);
        }

        PdfBytes.WriteAscii(stream, " >>\nstream\n");
        stream.Write(data, 0, data.Length);
        PdfBytes.WriteAscii(stream, "\nendstream");
    }
}
