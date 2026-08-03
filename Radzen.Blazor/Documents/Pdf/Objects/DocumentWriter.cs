using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Radzen.Documents.Internal;
using Radzen.Documents.Pdf.Crypto;
using Radzen.Documents.Pdf.Objects.Encryption;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.5: file header, indirect object bodies, cross-reference table, trailer.
internal sealed class DocumentWriter : IObjectWriter
{
    private static readonly byte[] HeaderSuffix =
    [
        (byte)'\n', (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n',
    ];

    public string Version
    {
        get => graph.Version;
        set => graph.Version = value;
    }

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

    private readonly Stream stream;
    private readonly DocumentObjectGraph graph;

    public DocumentWriter(Stream stream)
        : this(stream, new DocumentObjectGraph())
    {
    }

    internal DocumentWriter(Stream stream, DocumentObjectGraph graph)
    {
        this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    internal DocumentObjectGraph Graph => graph;

    public DictionaryObject Trailer => graph.Trailer;

    public EncryptionOptions? Encryption { get; set; }

    // ISO 32000-1 7.5.7 and 7.5.8: object streams and cross-reference streams.
    public bool UseCompressedStreams { get; set; }

    public ReferenceObject Add(DocumentObject value)
        => graph.Add(value);

    internal DocumentObject? Resolve(ReferenceObject reference)
        => graph.Resolve(reference);

    public void Close()
    {
        var prepared = PrepareEncryption(Encryption?.AesProvider, memoizeMaterial: false);
        WriteBody(stream, prepared);
    }

    public async ValueTask CloseAsync()
    {
        if (Encryption is null)
        {
            Close();
            return;
        }

        var pump = new AesCbcPump(Encryption.AesProvider);
        var prepared = await PrepareEncryptionAsync(pump).ConfigureAwait(false);

        for (var attempt = 0; attempt < 64; attempt++)
        {
            pump.Mode = AesCbcPump.Stage.Collect;
            Rewind(prepared);
            using var scratch = new PooledBufferStream(64 * 1024);
            WriteBody(scratch, prepared);
            if (pump.PendingCount == 0)
            {
                pump.Mode = AesCbcPump.Stage.PassThrough;
                stream.Write(scratch.WrittenSpan);
                return;
            }

            await pump.ResolvePendingAsync().ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            "The supplied IAesCbcProvider did not settle after 64 encryption passes.");
    }

    private static void Rewind(PreparedEncryption prepared)
    {
        if (prepared.Material is { } material)
        {
            material.Position = prepared.MaterialPosition;
        }
    }

    private readonly record struct PreparedEncryption(
        EncryptionWriter? Writer, int EncryptNumber, MaterialSequence? Material, int MaterialPosition);

    private void WriteBody(Stream target, PreparedEncryption prepared)
    {
        using var buffer = new CountingBufferedStream(target);
        var header = BuildHeader();
        buffer.Write(header, 0, header.Length);

        var (encryption, encryptNumber) = (prepared.Writer, prepared.EncryptNumber);

        if (UseCompressedStreams)
        {
            CloseCompressed(buffer, encryption, encryptNumber);
            buffer.Flush();
            return;
        }

        var offsets = new long[graph.Objects.Count];
        for (var i = 0; i < graph.Objects.Count; i++)
        {
            offsets[i] = WriteIndirectObject(buffer, i + 1, graph.Objects[i], encryption, encryptNumber);
        }

        var xrefOffset = buffer.Position;
        var size = graph.Objects.Count + 1;
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
        var count = graph.Objects.Count;
        var offsets = new long[count];
        var packedIndex = new int[count];

        // Object stream contents are not individually encrypted (ISO 32000-1 7.6.1).
        for (var i = 0; i < count; i++)
        {
            packedIndex[i] = graph.Objects[i] is not StreamObject && i + 1 != encryptNumber
                ? builder.Add(i + 1, graph.Objects[i])
                : -1;
        }

        var objStmNumber = builder.Count > 0 ? count + 1 : -1;
        var xrefNumber = builder.Count > 0 ? count + 2 : count + 1;

        for (var i = 0; i < count; i++)
        {
            if (packedIndex[i] < 0)
            {
                offsets[i] = WriteIndirectObject(buffer, i + 1, graph.Objects[i], encryption, encryptNumber);
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

    private PreparedEncryption PrepareEncryption(IAesCbcProvider? provider, bool memoizeMaterial)
    {
        if (Encryption is null)
        {
            return new PreparedEncryption(null, -1, null, 0);
        }

        var sequence = NewSequence(memoizeMaterial);
        var documentId = sequence.Next(16);
        var writer = EncryptionWriter.Build(Encryption, documentId, sequence, provider, out var dictionary);
        return Register(writer, dictionary, documentId, sequence);
    }

    private async ValueTask<PreparedEncryption> PrepareEncryptionAsync(IAesCbcProvider provider)
    {
        var sequence = NewSequence(memoizeMaterial: true);
        var documentId = sequence.Next(16);
        var built = await EncryptionWriter.BuildAsync(Encryption!, documentId, sequence, provider)
            .ConfigureAwait(false);
        return Register(built.Writer, built.Dictionary, documentId, sequence);
    }

    private MaterialSequence NewSequence(bool memoizeMaterial)
    {
        var material = Encryption!.Material ?? RandomEncryptionMaterial.Instance;
        return new MaterialSequence(memoizeMaterial ? new MemoizedEncryptionMaterial(material) : material);
    }

    private PreparedEncryption Register(
        EncryptionWriter writer, DictionaryObject dictionary, byte[] documentId, MaterialSequence sequence)
    {
        var reference = Add(dictionary);
        Trailer["Encrypt"] = reference;

        var id = new StringObject(Encoding.Latin1.GetString(documentId));
        Trailer["ID"] = new ArrayObject { id, id };

        return new PreparedEncryption(writer, reference.ObjectNumber, sequence, sequence.Position);
    }
}

internal static class IndirectObjectRegistration
{
    public static ReferenceObject Add(DocumentObject value, Func<DocumentObject, int> append)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ReferenceObject(append(value), 0);
    }
}
