using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// Reads a classic PDF file (ISO 32000-1 section 7.5): it locates the last
/// cross-reference table via <c>startxref</c>, follows any <c>/Prev</c> chain of
/// incremental updates, and parses indirect objects on demand.
/// </summary>
public sealed class DocumentReader
{
    private readonly byte[] data;
    private readonly Dictionary<int, XrefEntry> entries = new();
    private readonly Dictionary<int, DocumentObject> cache = new();
    private DictionaryObject trailer = new();

    private DocumentReader(byte[] data)
    {
        this.data = data;
    }

    /// <summary>
    /// Gets the trailer dictionary of the most recent cross-reference section.
    /// </summary>
    public DictionaryObject Trailer => trailer;

    /// <summary>
    /// Gets the number of in-use (non-free) objects across the merged
    /// cross-reference sections.
    /// </summary>
    public int ObjectCount
    {
        get
        {
            var count = 0;
            foreach (var entry in entries.Values)
            {
                if (entry.InUse)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>
    /// Parses a classic PDF document from a byte array.
    /// </summary>
    /// <param name="data">The complete document bytes.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var reader = new DocumentReader(data);
        reader.Load();
        return reader;
    }

    /// <summary>
    /// Parses a classic PDF document from a stream. The stream is fully read
    /// into memory.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Parse(buffer.ToArray());
    }

    /// <summary>
    /// Parses and returns the indirect object with the given object number.
    /// </summary>
    /// <param name="number">The object number.</param>
    /// <returns>The parsed object.</returns>
    public DocumentObject GetObject(int number)
    {
        if (cache.TryGetValue(number, out var cached))
        {
            return cached;
        }

        if (!entries.TryGetValue(number, out var entry) || !entry.InUse)
        {
            throw new DocumentParseException("Object not found.", -1);
        }

        var value = ParseIndirectObject(entry.Offset);
        cache[number] = value;
        return value;
    }

    /// <summary>
    /// Resolves an indirect reference to the object it points at. Non-reference
    /// objects are returned unchanged.
    /// </summary>
    /// <param name="value">The object to resolve.</param>
    /// <returns>The referenced object, or <paramref name="value"/> itself.</returns>
    public DocumentObject Resolve(DocumentObject value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value is ReferenceObject reference)
        {
            return GetObject(reference.ObjectNumber);
        }

        return value;
    }

    private void Load()
    {
        var offset = FindStartXref();
        var visited = new HashSet<long>();
        DictionaryObject? newest = null;
        while (visited.Add(offset))
        {
            var sectionTrailer = ParseXrefSection(offset);
            newest ??= sectionTrailer;
            if (sectionTrailer.TryGetValue("Prev", out var prev) && prev is NumberObject prevNumber)
            {
                offset = prevNumber.IntValue;
            }
            else
            {
                break;
            }
        }

        trailer = newest ?? new DictionaryObject();
    }

    private long FindStartXref()
    {
        var pattern = "startxref";
        for (var i = data.Length - pattern.Length; i >= 0; i--)
        {
            if (Matches(i, pattern))
            {
                var index = i + pattern.Length;
                return ReadLong(ref index);
            }
        }

        throw new DocumentParseException("Missing startxref.", -1);
    }

    private DictionaryObject ParseXrefSection(long offset)
    {
        var index = (int)offset;
        SkipWhitespace(ref index);
        if (!Matches(index, "xref"))
        {
            throw new DocumentParseException("Expected xref.", index);
        }

        index += 4;
        while (true)
        {
            SkipWhitespace(ref index);
            if (Matches(index, "trailer"))
            {
                index += 7;
                return (DictionaryObject)ObjectParser.Parse(data, index);
            }

            var start = (int)ReadLong(ref index);
            var count = (int)ReadLong(ref index);
            for (var i = 0; i < count; i++)
            {
                var entryOffset = ReadLong(ref index);
                ReadLong(ref index);
                SkipWhitespace(ref index);
                var type = data[index];
                index++;
                var number = start + i;
                if (!entries.ContainsKey(number))
                {
                    entries[number] = new XrefEntry(entryOffset, type == (byte)'n');
                }
            }
        }
    }

    private DocumentObject ParseIndirectObject(long offset)
    {
        var lexer = new Lexer(data, (int)offset);
        lexer.Next();
        lexer.Next();
        var keyword = lexer.Next();
        if (keyword.Kind != TokenKind.Keyword || keyword.Text != "obj")
        {
            throw new DocumentParseException("Expected obj.", (int)offset);
        }

        var parser = new ObjectParser(lexer);
        var value = parser.ParseValue();
        if (value is not DictionaryObject dictionary)
        {
            return value;
        }

        var next = parser.NextToken();
        if (next.Kind != TokenKind.Keyword || next.Text != "stream")
        {
            return dictionary;
        }

        var dataStart = lexer.Position;
        if (dataStart < data.Length && data[dataStart] == 13)
        {
            dataStart++;
            if (dataStart < data.Length && data[dataStart] == 10)
            {
                dataStart++;
            }
        }
        else if (dataStart < data.Length && data[dataStart] == 10)
        {
            dataStart++;
        }

        var length = ResolveLength(dictionary);
        var payload = new byte[length];
        Array.Copy(data, dataStart, payload, 0, length);
        var stream = new StreamObject(payload);
        foreach (var key in dictionary.Keys)
        {
            stream.Dictionary[key] = dictionary[key];
        }

        return stream;
    }

    private int ResolveLength(DictionaryObject dictionary)
    {
        if (!dictionary.TryGetValue("Length", out var lengthObject) || lengthObject is null)
        {
            throw new DocumentParseException("Missing stream length.", -1);
        }

        var resolved = Resolve(lengthObject);
        if (resolved is NumberObject number)
        {
            return number.IntValue;
        }

        throw new DocumentParseException("Invalid stream length.", -1);
    }

    private bool Matches(int index, string pattern)
    {
        if (index < 0 || index + pattern.Length > data.Length)
        {
            return false;
        }

        for (var i = 0; i < pattern.Length; i++)
        {
            if (data[index + i] != (byte)pattern[i])
            {
                return false;
            }
        }

        return true;
    }

    private void SkipWhitespace(ref int index)
    {
        while (index < data.Length && Lexer.IsWhitespace(data[index]))
        {
            index++;
        }
    }

    private long ReadLong(ref int index)
    {
        SkipWhitespace(ref index);
        var start = index;
        if (index < data.Length && (data[index] == (byte)'+' || data[index] == (byte)'-'))
        {
            index++;
        }

        while (index < data.Length && data[index] >= (byte)'0' && data[index] <= (byte)'9')
        {
            index++;
        }

        if (index == start)
        {
            throw new DocumentParseException("Expected integer.", start);
        }

        return long.Parse(Encoding.Latin1.GetString(data, start, index - start), CultureInfo.InvariantCulture);
    }

    private readonly struct XrefEntry
    {
        public XrefEntry(long offset, bool inUse)
        {
            Offset = offset;
            InUse = inUse;
        }

        public long Offset { get; }

        public bool InUse { get; }
    }
}
