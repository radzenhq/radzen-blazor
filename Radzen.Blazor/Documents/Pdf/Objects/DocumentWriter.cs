using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects.Encryption;

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
/// <remarks>
/// Initializes a new instance of the <see cref="DocumentWriter"/> class.
/// </remarks>
/// <param name="stream">The destination stream.</param>
public sealed class DocumentWriter(Stream stream) : IObjectWriter
{
    private static readonly byte[] HeaderSuffix =
    [
        (byte)'\n', (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n',
    ];

    /// <summary>
    /// Gets or sets the PDF version written in the file header (e.g. "1.7" or
    /// "2.0"). PDF/A-4 requires "2.0".
    /// </summary>
    public string Version { get; set; } = "1.7";

    private byte[] BuildHeader()
    {
        var header = new byte[5 + Version.Length + HeaderSuffix.Length];
        header[0] = (byte)'%';
        header[1] = (byte)'P';
        header[2] = (byte)'D';
        header[3] = (byte)'F';
        header[4] = (byte)'-';
        for (var i = 0; i < Version.Length; i++)
        {
            header[5 + i] = (byte)Version[i];
        }
        Array.Copy(HeaderSuffix, 0, header, 5 + Version.Length, HeaderSuffix.Length);
        return header;
    }

    private readonly Stream stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private readonly List<DocumentObject> objects = [];

    /// <summary>
    /// Gets the trailer dictionary. Entries such as <c>/Root</c> are written
    /// verbatim; <c>/Size</c> is set automatically by <see cref="Close"/>.
    /// </summary>
    public DictionaryObject Trailer { get; } = new();

    /// <summary>
    /// Gets or sets standard PDF encryption options. When non-null, <see cref="Close"/>
    /// writes an <c>/Encrypt</c> dictionary and a document <c>/ID</c>, and encrypts
    /// every string and stream. When null the output is unencrypted.
    /// </summary>
    public EncryptionOptions? Encryption { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Close"/> packs eligible
    /// non-stream objects into a Flate-compressed <c>/Type /ObjStm</c> object stream
    /// and writes a <c>/Type /XRef</c> cross-reference stream (ISO 32000-1 sections
    /// 7.5.7 and 7.5.8) instead of the classic cross-reference table and trailer.
    /// Defaults to <c>false</c>, which keeps the classic output unchanged.
    /// </summary>
    public bool UseCompressedStreams { get; set; }

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

    internal DocumentObject? Resolve(ReferenceObject reference)
        => reference.ObjectNumber >= 1 && reference.ObjectNumber <= objects.Count
            ? objects[reference.ObjectNumber - 1]
            : null;

    /// <summary>
    /// Serializes all registered objects, the cross-reference table, and the
    /// trailer to the destination stream.
    /// </summary>
    public void Close()
    {
        using var buffer = new CountingBufferedStream(stream);
        var header = BuildHeader();
        buffer.Write(header, 0, header.Length);

        var (encryption, encryptNumber) = PrepareEncryption();

        if (UseCompressedStreams)
        {
            CloseCompressed(buffer, encryption, encryptNumber);
            buffer.Flush();
            return;
        }

        var offsets = new long[objects.Count];
        for (var i = 0; i < objects.Count; i++)
        {
            offsets[i] = WriteIndirectObject(buffer, i + 1, objects[i], encryption, encryptNumber);
        }

        var xrefOffset = buffer.Position;
        var size = objects.Count + 1;
        PdfBytes.WriteAscii(buffer, "xref\n0 ");
        PdfBytes.WriteInteger(buffer, size);
        PdfBytes.WriteAscii(buffer, "\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            PdfBytes.WriteXrefEntry(buffer, offset);
        }

        Trailer["Size"] = new NumberObject(size);
        PdfBytes.WriteAscii(buffer, "trailer\n");
        Trailer.Write(buffer);
        PdfBytes.WriteAscii(buffer, "\nstartxref\n");
        PdfBytes.WriteInteger(buffer, xrefOffset);
        PdfBytes.WriteAscii(buffer, "\n%%EOF\n");

        buffer.Flush();
    }

    private static long WriteIndirectObject(CountingBufferedStream buffer, int number, DocumentObject value, EncryptionWriter? encryption, int encryptNumber)
    {
        var context = encryption is not null && number != encryptNumber
            ? new WriteContext(encryption, number, 0)
            : WriteContext.None;
        return IndirectObjectFramer.Write(buffer, number, 0, value, context);
    }

    private void CloseCompressed(CountingBufferedStream buffer, EncryptionWriter? encryption, int encryptNumber)
    {
        var builder = new ObjectStreamBuilder();
        var count = objects.Count;
        var offsets = new long[count];
        var packedIndex = new int[count];

        // Object stream contents are not individually encrypted (ISO 32000-1 7.6.1).
        for (var i = 0; i < count; i++)
        {
            packedIndex[i] = objects[i] is not StreamObject && i + 1 != encryptNumber
                ? builder.Add(i + 1, objects[i])
                : -1;
        }

        var objStmNumber = builder.Count > 0 ? count + 1 : -1;
        var xrefNumber = builder.Count > 0 ? count + 2 : count + 1;

        for (var i = 0; i < count; i++)
        {
            if (packedIndex[i] < 0)
            {
                offsets[i] = WriteIndirectObject(buffer, i + 1, objects[i], encryption, encryptNumber);
            }
        }

        long objStmOffset = 0;
        if (objStmNumber > 0)
        {
            objStmOffset = WriteIndirectObject(buffer, objStmNumber, builder.Build(), encryption, encryptNumber);
        }

        var xrefOffset = buffer.Position;
        WriteXrefStream(buffer, xrefNumber, xrefOffset, offsets, packedIndex, objStmNumber, objStmOffset);

        PdfBytes.WriteAscii(buffer, "startxref\n");
        PdfBytes.WriteInteger(buffer, xrefOffset);
        PdfBytes.WriteAscii(buffer, "\n%%EOF\n");
    }

    private void WriteXrefStream(CountingBufferedStream buffer, int xrefNumber, long xrefOffset, long[] offsets, int[] packedIndex, int objStmNumber, long objStmOffset)
    {
        var size = xrefNumber + 1;
        var rows = new XrefRow[size];

        rows[0] = new XrefRow(0, 0, 65535);
        for (var i = 0; i < offsets.Length; i++)
        {
            var number = i + 1;
            rows[number] = packedIndex[i] >= 0
                ? new XrefRow(2, objStmNumber, packedIndex[i])
                : new XrefRow(1, offsets[i], 0);
        }

        if (objStmNumber > 0)
        {
            rows[objStmNumber] = new XrefRow(1, objStmOffset, 0);
        }

        rows[xrefNumber] = new XrefRow(1, xrefOffset, 0);

        var xref = XrefStreamPacker.Pack(rows, "Size", new NumberObject(size), Trailer);

        // Cross-reference streams are never encrypted (ISO 32000-1 7.5.8.2).
        WriteIndirectObject(buffer, xrefNumber, xref, null, -1);
    }

    private (EncryptionWriter? Writer, int EncryptNumber) PrepareEncryption()
    {
        if (Encryption is null)
        {
            return (null, -1);
        }

        if (Encryption.Material is null)
        {
            throw new InvalidOperationException(
                "EncryptionOptions.Material must be set to write an encrypted document; the library generates no randomness of its own.");
        }

        var sequence = new MaterialSequence(Encryption.Material);
        var documentId = sequence.Next(16);
        var writer = EncryptionWriter.Build(Encryption, documentId, sequence, out var dictionary);
        var reference = Add(dictionary);
        Trailer["Encrypt"] = reference;

        var id = new StringObject(Encoding.Latin1.GetString(documentId));
        Trailer["ID"] = new ArrayObject { id, id };

        return (writer, reference.ObjectNumber);
    }
}
