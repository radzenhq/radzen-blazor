using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// Writes a COS object model to a stream as a classic PDF file (ISO 32000-1
/// section 7.5): file header, indirect object bodies, a cross-reference table,
/// and a trailer.
/// </summary>
/// <remarks>
/// <see cref="Add(DocumentObject)"/> registers an object and immediately
/// returns its indirect reference; the object may be mutated afterwards.
/// Object bodies are serialized only when <see cref="Close"/> is called.
/// </remarks>
public sealed class DocumentWriter
{
    private static readonly byte[] Header =
    {
        (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-', (byte)'1', (byte)'.', (byte)'7', (byte)'\n',
        (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n',
    };

    private readonly Stream stream;
    private readonly List<DocumentObject> objects = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentWriter"/> class.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    public DocumentWriter(Stream stream)
    {
        this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    /// <summary>
    /// Gets the trailer dictionary. Entries such as <c>/Root</c> are written
    /// verbatim; <c>/Size</c> is set automatically by <see cref="Close"/>.
    /// </summary>
    public DictionaryObject Trailer { get; } = new();

    /// <summary>
    /// Registers <paramref name="value"/> as an indirect object and returns a
    /// reference to it. The object body is serialized later by <see cref="Close"/>.
    /// </summary>
    /// <param name="value">The object to register.</param>
    /// <returns>An indirect reference to the registered object.</returns>
    public ReferenceObject Add(DocumentObject value)
    {
        ArgumentNullException.ThrowIfNull(value);

        objects.Add(value);
        return new ReferenceObject(objects.Count, 0);
    }

    /// <summary>
    /// Serializes all registered objects, the cross-reference table, and the
    /// trailer to the destination stream.
    /// </summary>
    public void Close()
    {
        using var buffer = new MemoryStream();
        buffer.Write(Header, 0, Header.Length);

        var offsets = new long[objects.Count];
        for (var i = 0; i < objects.Count; i++)
        {
            offsets[i] = buffer.Position;
            PdfBytes.WriteAscii(buffer, string.Create(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n"));
            objects[i].Write(buffer);
            PdfBytes.WriteAscii(buffer, "\nendobj\n");
        }

        var xrefOffset = buffer.Position;
        var size = objects.Count + 1;
        PdfBytes.WriteAscii(buffer, string.Create(CultureInfo.InvariantCulture, $"xref\n0 {size}\n"));
        PdfBytes.WriteAscii(buffer, "0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            PdfBytes.WriteAscii(buffer, string.Create(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n"));
        }

        Trailer["Size"] = new NumberObject(size);
        PdfBytes.WriteAscii(buffer, "trailer\n");
        Trailer.Write(buffer);
        PdfBytes.WriteAscii(buffer, string.Create(CultureInfo.InvariantCulture, $"\nstartxref\n{xrefOffset}\n%%EOF\n"));

        buffer.Position = 0;
        buffer.CopyTo(stream);
        stream.Flush();
    }
}
