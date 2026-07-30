using System;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Internal;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// Appends an incremental update (ISO 32000-1 section 7.5.6) to an existing PDF
/// file: new and overridden indirect objects are written after the original
/// end-of-file, followed by a cross-reference section chained to the previous
/// one via <c>/Prev</c>, a trailer, <c>startxref</c> and <c>%%EOF</c>. The
/// original bytes are preserved verbatim as a prefix of the output, which is
/// what digital signatures over a byte range require.
/// </summary>
/// <remarks>
/// The style of the appended cross-reference section matches the original
/// file: a classic <c>xref</c> table and trailer when the original ends with
/// one, or a <c>/Type /XRef</c> cross-reference stream when the original uses
/// one. Overridden objects keep both their object number and their current
/// generation - per section 7.5.6 an updated object reuses its number, and a
/// generation bump is only required when a number from the free list is reused,
/// which this writer never does; new objects are written at generation 0.
/// Output is deterministic: identical inputs produce identical bytes.
/// </remarks>
public sealed class IncrementalUpdateWriter : IObjectWriter
{
    private readonly byte[] original;
    private readonly DocumentReader reader;
    private readonly SortedDictionary<int, DocumentObject> objects = [];
    private readonly Dictionary<int, int> generations = [];
    private readonly long previousStartXref;
    private readonly bool classicXref;
    private readonly int originalMaxNumber;
    private int nextNumber;
    private IReadOnlyDictionary<int, long>? writtenOffsets;

    /// <summary>
    /// Initializes a new instance over the bytes of an existing, valid PDF file.
    /// The bytes are parsed with <see cref="DocumentReader"/> to obtain the
    /// trailer entries (<c>/Root</c>, <c>/ID</c>, <c>/Size</c>) the update must carry.
    /// </summary>
    /// <param name="original">The complete bytes of the existing document.</param>
    public IncrementalUpdateWriter(byte[] original)
        : this(original, DocumentReader.Parse(original ?? throw new ArgumentNullException(nameof(original))))
    {
    }

    /// <summary>
    /// Initializes a new instance over the bytes of an existing, valid PDF file
    /// and an already-parsed reader for those same bytes.
    /// </summary>
    /// <param name="original">The complete bytes of the existing document.</param>
    /// <param name="reader">A reader parsed from <paramref name="original"/>.</param>
    public IncrementalUpdateWriter(byte[] original, DocumentReader reader)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(reader);

        if (reader.IsEncrypted)
        {
            throw new NotSupportedException(
                "Incremental update of an encrypted document is not supported.");
        }

        this.original = original;
        this.reader = reader;

        previousStartXref = PdfBytes.FindStartXref(original);
        classicXref = IsClassicXref(original, previousStartXref);

        originalMaxNumber = reader.Trailer.TryGetValue("Size", out var size) && size is NumberObject sizeNumber
            ? sizeNumber.IntValue - 1
            : throw new DocumentParseException("Trailer is missing /Size.", -1);
        nextNumber = originalMaxNumber + 1;
    }

    /// <summary>
    /// Gets additional trailer entries to carry in the appended trailer (for
    /// example an <c>/Encrypt</c> passthrough). <c>/Root</c>, <c>/Info</c> and
    /// <c>/ID</c> are copied from the original trailer automatically but may be
    /// overridden here; <c>/Size</c> and <c>/Prev</c> are always computed by the
    /// writer and cannot be overridden.
    /// </summary>
    public DictionaryObject Trailer { get; } = new();

    /// <summary>
    /// Registers a new indirect object. It receives the next unused object
    /// number in the chain (starting at the original <c>/Size</c>) with
    /// generation 0.
    /// </summary>
    /// <param name="value">The object to append.</param>
    /// <returns>An indirect reference to the new object.</returns>
    public ReferenceObject Add(DocumentObject value)
        => IndirectObjectRegistration.Add(value, AppendObject);

    private int AppendObject(DocumentObject value)
    {
        var number = nextNumber++;
        objects[number] = value;
        return number;
    }

    /// <summary>
    /// Registers a replacement for an existing indirect object. The replacement
    /// keeps both the object number and the object's current generation; readers
    /// resolve it instead of the original because the appended cross-reference
    /// section is newer.
    /// </summary>
    /// <param name="objectNumber">The number of the object to override.</param>
    /// <param name="value">The replacement object.</param>
    /// <returns>An indirect reference to the overridden object.</returns>
    public ReferenceObject Override(int objectNumber, DocumentObject value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (objectNumber < 1 || objectNumber > originalMaxNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(objectNumber), objectNumber,
                $"Object number must be between 1 and {originalMaxNumber} to override an existing object.");
        }

        // ISO 32000-1 7.3.10: a reference matches on number and generation, so an override keeps G.
        var generation = reader.GenerationOf(objectNumber);
        objects[objectNumber] = value;
        generations[objectNumber] = generation;
        return new ReferenceObject(objectNumber, generation);
    }

    private int GenerationOf(int objectNumber)
        => generations.TryGetValue(objectNumber, out var generation) ? generation : 0;

    /// <summary>
    /// Writes the original bytes followed by the incremental update section and
    /// returns the complete new document.
    /// </summary>
    /// <returns>The bytes of the updated document.</returns>
    public byte[] ToArray()
    {
        using var buffer = new PooledBufferStream(original.Length + (64 * 1024));
        WriteTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>
    /// Gets the absolute byte offset at which the given object was written in the
    /// most recent <see cref="ToArray"/> / <see cref="WriteTo"/>. Valid only after
    /// one of those has run. Used to bound in-place patching (e.g. a signature's
    /// <c>/ByteRange</c> and <c>/Contents</c>) to a single object's own bytes.
    /// </summary>
    /// <param name="reference">A reference returned by <see cref="Add"/> or <see cref="Override"/>.</param>
    /// <returns>The object's absolute start offset in the output.</returns>
    public long OffsetOf(ReferenceObject reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (writtenOffsets is null)
        {
            throw new InvalidOperationException("OffsetOf is only available after ToArray or WriteTo.");
        }

        if (!writtenOffsets.TryGetValue(reference.ObjectNumber, out var offset))
        {
            throw new ArgumentException(
                $"Object {reference.ObjectNumber} was not written by this update.", nameof(reference));
        }

        return offset;
    }

    /// <summary>
    /// Writes the original bytes followed by the incremental update section to
    /// <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (objects.Count == 0)
        {
            throw new InvalidOperationException("The incremental update contains no objects.");
        }

        using var buffer = new CountingBufferedStream(stream);
        buffer.Write(original, 0, original.Length);
        if (original.Length > 0 && original[^1] != (byte)'\n' && original[^1] != (byte)'\r')
        {
            buffer.WriteByte((byte)'\n');
        }

        var offsets = new SortedDictionary<int, long>();
        foreach (var pair in objects)
        {
            offsets[pair.Key] = IndirectObjectFramer.Write(buffer, pair.Key, GenerationOf(pair.Key), pair.Value, WriteContext.None);
        }

        writtenOffsets = offsets;

        long xrefOffset;
        if (classicXref)
        {
            xrefOffset = buffer.Position;
            WriteClassicXref(buffer, offsets);
        }
        else
        {
            xrefOffset = WriteXrefStream(buffer, offsets);
        }

        PdfBytes.WriteAscii(buffer, "startxref\n");
        PdfBytes.WriteInteger(buffer, xrefOffset);
        PdfBytes.WriteAscii(buffer, "\n%%EOF\n");
        buffer.Flush();
    }

    private void WriteClassicXref(CountingBufferedStream buffer, SortedDictionary<int, long> offsets)
    {
        PdfBytes.WriteAscii(buffer, "xref\n");
        foreach (var (start, count) in Subsections(offsets))
        {
            PdfBytes.WriteInteger(buffer, start);
            PdfBytes.WriteAscii(buffer, " ");
            PdfBytes.WriteInteger(buffer, count);
            PdfBytes.WriteAscii(buffer, "\n");
            for (var number = start; number < start + count; number++)
            {
                PdfBytes.WriteXrefEntry(buffer, offsets[number], GenerationOf(number));
            }
        }

        PdfBytes.WriteAscii(buffer, "trailer\n");
        BuildTrailer(nextNumber).Write(buffer);
        PdfBytes.WriteAscii(buffer, "\n");
    }

    private long WriteXrefStream(CountingBufferedStream buffer, SortedDictionary<int, long> offsets)
    {
        var xrefNumber = nextNumber;
        var xrefOffset = buffer.Position;
        offsets[xrefNumber] = xrefOffset;

        var index = new ArrayObject();
        var rows = new List<XrefRow>();
        foreach (var (start, count) in Subsections(offsets))
        {
            index.Add(new NumberObject(start));
            index.Add(new NumberObject(count));
            for (var number = start; number < start + count; number++)
            {
                rows.Add(new XrefRow(1, offsets[number], GenerationOf(number)));
            }
        }

        var xref = XrefStreamPacker.Pack(rows, "Index", index, BuildTrailer(xrefNumber + 1));

        IndirectObjectFramer.Write(buffer, xrefNumber, 0, xref, WriteContext.None);
        return xrefOffset;
    }

    private DictionaryObject BuildTrailer(int size)
    {
        var result = new DictionaryObject();
        foreach (var key in (string[])["Root", "Info", "ID"])
        {
            if (reader.Trailer.TryGetValue(key, out var value) && value is not null)
            {
                result[key] = value;
            }
        }

        foreach (var pair in Trailer)
        {
            result[pair.Key] = pair.Value;
        }

        result["Size"] = new NumberObject(size);
        result["Prev"] = new NumberObject(previousStartXref);
        return result;
    }

    private static IEnumerable<(int Start, int Count)> Subsections(SortedDictionary<int, long> offsets)
    {
        var start = -1;
        var count = 0;
        foreach (var number in offsets.Keys)
        {
            if (start >= 0 && number == start + count)
            {
                count++;
                continue;
            }

            if (start >= 0)
            {
                yield return (start, count);
            }

            start = number;
            count = 1;
        }

        if (start >= 0)
        {
            yield return (start, count);
        }
    }

    private static bool IsClassicXref(byte[] data, long offset)
    {
        var index = (int)offset;
        if (index < 0 || index >= data.Length)
        {
            throw new DocumentParseException("startxref offset is outside the file.", index);
        }

        while (index < data.Length && Lexer.IsWhitespace(data[index]))
        {
            index++;
        }

        return PdfBytes.Matches(data, index, "xref");
    }
}
