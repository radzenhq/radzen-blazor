using System;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Internal;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.5.6: new and overridden objects are appended after the original
// end-of-file, followed by a cross-reference section chained via /Prev. An updated
// object reuses its number; a generation bump is only required when a number from the
// free list is reused.
internal sealed class IncrementalUpdateWriter : IObjectWriter
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

    public IncrementalUpdateWriter(byte[] original)
        : this(original, DocumentReader.Parse(original ?? throw new ArgumentNullException(nameof(original))))
    {
    }

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

    public DictionaryObject Trailer { get; } = new();

    public ReferenceObject Add(DocumentObject value)
        => IndirectObjectRegistration.Add(value, AppendObject);

    private int AppendObject(DocumentObject value)
    {
        var number = nextNumber++;
        objects[number] = value;
        return number;
    }

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

    public byte[] ToArray()
    {
        using var buffer = new PooledBufferStream(original.Length + (64 * 1024));
        WriteTo(buffer);
        return buffer.ToArray();
    }

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
