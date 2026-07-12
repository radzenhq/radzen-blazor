using Radzen.Documents.Pdf.Objects.Encryption;
using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// Reads a PDF file (ISO 32000-1 section 7.5): it locates the last
/// cross-reference section via <c>startxref</c>, follows any <c>/Prev</c> chain of
/// incremental updates - classic tables, cross-reference streams, or a mix - and
/// parses indirect objects (including objects compressed inside object streams) on
/// demand. When the cross-reference machinery is unusable the reader falls back to
/// scanning the file for <c>N G obj</c> headers and reconstructing the trailer.
/// </summary>
public sealed class DocumentReader
{
    private readonly byte[] data;
    private readonly ReaderLimits limits;
    private readonly Dictionary<int, XrefEntry> entries = [];
    private readonly Dictionary<int, DocumentObject> cache = [];
    private readonly Dictionary<int, ObjectStream> objectStreams = [];
    private readonly NullObject nullObject = new();
    private DictionaryObject trailer = new();
    private readonly HashSet<int> parsing = [];
    private Dictionary<int, long>? scanned;
    private StandardSecurityHandler? security;
    private int encryptObjectNumber = -1;
    private bool decryptionReady;

    private DocumentReader(byte[] data, ReaderLimits limits)
    {
        this.data = data;
        this.limits = limits;
    }

    /// <summary>
    /// Gets the trailer dictionary of the most recent cross-reference section. For
    /// a cross-reference stream this is the stream's own dictionary.
    /// </summary>
    public DictionaryObject Trailer => trailer;

    /// <summary>
    /// Gets the number of in-use (non-free) objects across the merged
    /// cross-reference sections, including objects stored in object streams.
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
    /// Gets a value indicating whether the document is encrypted (its trailer
    /// carries an <c>/Encrypt</c> entry and a security handler was constructed).
    /// </summary>
    public bool IsEncrypted => security is not null;

    /// <summary>
    /// Parses a PDF document from a byte array.
    /// </summary>
    /// <param name="data">The complete document bytes.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(byte[] data) => Parse(data, null);

    /// <summary>
    /// Parses a PDF document from a byte array, supplying a password for an
    /// encrypted document. Opening an encrypted document whose user and owner
    /// passwords both reject the supplied password throws
    /// <see cref="InvalidPasswordException"/>.
    /// </summary>
    /// <param name="data">The complete document bytes.</param>
    /// <param name="password">The user or owner password, or <c>null</c>/empty for none.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(byte[] data, string? password) => Parse(data, password, ReaderLimits.Default);

    /// <summary>
    /// Parses a PDF document from a byte array, supplying a password for an
    /// encrypted document and the resource limits to enforce while reading.
    /// </summary>
    /// <param name="data">The complete document bytes.</param>
    /// <param name="password">The user or owner password, or <c>null</c>/empty for none.</param>
    /// <param name="limits">The resource limits to enforce while reading.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(byte[] data, string? password, ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(limits);
        var reader = new DocumentReader(data, limits);
        reader.Load();
        reader.InitializeSecurity(password, throwOnFailure: true);
        return reader;
    }

    /// <summary>
    /// Parses a PDF document from a stream. The stream is fully read into memory.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(Stream stream) => Parse(stream, null);

    /// <summary>
    /// Parses a PDF document from a stream, supplying a password for an encrypted
    /// document. The stream is fully read into memory.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="password">The user or owner password, or <c>null</c>/empty for none.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(Stream stream, string? password)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Parse(buffer.ToArray(), password);
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

        // An indirect reference to a free or nonexistent object resolves to null
        // (ISO 32000-1 7.3.10), so dangling refs left by incremental updates or
        // annotation deletion never abort a load, save or merge.
        if (!entries.TryGetValue(number, out var entry) || !entry.InUse)
        {
            return nullObject;
        }

        // Guards cyclic references (e.g. a stream /Length pointing back at its own
        // object) that would otherwise recurse without bound.
        if (!parsing.Add(number))
        {
            throw new DocumentParseException("Cyclic object reference.", -1);
        }

        DocumentObject value;
        try
        {
            if (entry.Type == 2)
            {
                value = GetCompressedObject((int)entry.Field2, (int)entry.Field3);
            }
            else
            {
                value = GetUncompressedObject(number, entry.Field2, out var generation);
                if (security is not null && number != encryptObjectNumber)
                {
                    value = DecryptObject(value, number, generation);
                }
            }
        }
        finally
        {
            parsing.Remove(number);
        }

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

    private void InitializeSecurity(string? password, bool throwOnFailure)
    {
        if (!trailer.TryGetValue("Encrypt", out var encryptObject) || encryptObject is null)
        {
            return;
        }

        if (encryptObject is ReferenceObject reference)
        {
            encryptObjectNumber = reference.ObjectNumber;
        }

        if (Resolve(encryptObject) is not DictionaryObject encrypt)
        {
            return;
        }

        var handler = new StandardSecurityHandler(encrypt, ReadDocumentId(), password ?? "");
        decryptionReady = handler.IsUserPassword || handler.IsOwnerPassword;
        if (!decryptionReady && throwOnFailure)
        {
            throw new InvalidPasswordException();
        }

        security = handler;

        // Objects cached before the handler existed were not decrypted.
        cache.Clear();
        objectStreams.Clear();
    }

    private byte[] ReadDocumentId()
    {
        if (trailer.TryGetValue("ID", out var id) && id is ArrayObject array
            && array.Count > 0 && array[0] is StringObject first)
        {
            return Encoding.Latin1.GetBytes(first.Value);
        }

        return [];
    }

    private DocumentObject DecryptObject(DocumentObject value, int number, int generation)
    {
        switch (value)
        {
            case StringObject text:
                var plain = security!.DecryptString(Encoding.Latin1.GetBytes(text.Value), number, generation);
                return new StringObject(Encoding.Latin1.GetString(plain));
            case StreamObject stream:
                var decrypted = security!.DecryptStream(stream.Data, number, generation, stream.Dictionary);
                var result = new StreamObject(decrypted);
                foreach (var key in stream.Dictionary.Keys)
                {
                    result.Dictionary[key] = DecryptObject(stream.Dictionary[key], number, generation);
                }

                return result;
            case DictionaryObject dictionary:
                var mapped = new DictionaryObject();
                foreach (var key in dictionary.Keys)
                {
                    mapped[key] = DecryptObject(dictionary[key], number, generation);
                }

                return mapped;
            case ArrayObject array:
                var items = new ArrayObject();
                foreach (var item in array)
                {
                    items.Add(DecryptObject(item, number, generation));
                }

                return items;
            default:
                return value;
        }
    }

    private void Load()
    {
        try
        {
            LoadFromXref();
        }
        catch (Exception exception) when (IsRecoverableParseFailure(exception))
        {
            Repair();
        }
    }

    // Malformed cross-reference data throws plain BCL exceptions (a /Type /XRef
    // stream missing /W, mistyped dictionary values, truncated entries, corrupt
    // Flate payloads); all of them mean the xref is unusable and the repair scan
    // should run.
    private static bool IsRecoverableParseFailure(Exception exception)
        => exception is DocumentParseException
            or KeyNotFoundException
            or InvalidCastException
            or ArgumentException
            or OverflowException
            or IndexOutOfRangeException
            or FormatException
            or InvalidOperationException
            or EndOfStreamException
            or InvalidDataException;

    private void LoadFromXref()
    {
        var offset = FindStartXref();
        var visited = new HashSet<long>();
        DictionaryObject? newest = null;
        while (visited.Add(offset))
        {
            var section = ReadXrefSectionAt(offset);
            newest ??= section;
            if (section.TryGetValue("Prev", out var prev) && prev is NumberObject prevNumber)
            {
                offset = prevNumber.IntValue;
            }
            else
            {
                break;
            }
        }

        trailer = newest ?? throw new DocumentParseException("Missing trailer.", -1);
    }

    private long FindStartXref()
    {
        const string pattern = "startxref";
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

    private DictionaryObject ReadXrefSectionAt(long offset)
    {
        var index = (int)offset;
        SkipWhitespace(ref index);
        return Matches(index, "xref")
            ? ParseClassicXref(index + 4)
            : ParseXrefStreamAt(offset);
    }

    private DictionaryObject ParseClassicXref(int index)
    {
        var section = new Dictionary<int, XrefEntry>();
        while (true)
        {
            SkipWhitespace(ref index);
            if (Matches(index, "trailer"))
            {
                index += 7;
                var trailerDict = (DictionaryObject)ObjectParser.Parse(data, index);

                // Hybrid-reference file (ISO 32000-1 7.5.8.4): the /XRefStm
                // cross-reference stream takes precedence over this classic
                // section, which lists compressed objects as free for the
                // benefit of pre-1.5 readers.
                if (trailerDict.TryGetValue("XRefStm", out var hybrid) && hybrid is NumberObject hybridOffset)
                {
                    ParseXrefStreamAt((long)hybridOffset.DoubleValue);
                }

                foreach (var pair in section)
                {
                    if (!entries.ContainsKey(pair.Key))
                    {
                        entries[pair.Key] = pair.Value;
                    }
                }

                return trailerDict;
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
                if (!entries.ContainsKey(number) && !section.ContainsKey(number))
                {
                    section[number] = type == (byte)'n'
                        ? new XrefEntry(1, entryOffset, 0)
                        : new XrefEntry(0, 0, 0);
                }
            }
        }
    }

    private DictionaryObject ParseXrefStreamAt(long offset)
    {
        if (!TryParseObjectAt(offset, null, out var value, out _) || value is not StreamObject stream)
        {
            throw new DocumentParseException("Expected cross-reference stream.", (int)offset);
        }

        if (!(stream.Dictionary.TryGetValue("Type", out var type) && type is NameObject name
            && string.Equals(name.Value, "XRef", StringComparison.Ordinal)))
        {
            throw new DocumentParseException("Not a cross-reference stream.", (int)offset);
        }

        return ParseXrefStream(stream);
    }

    private DictionaryObject ParseXrefStream(StreamObject stream)
    {
        var dict = stream.Dictionary;
        var decoded = DecodeStreamData(dict, stream.Data);

        var widths = (ArrayObject)dict["W"];
        var w0 = ((NumberObject)widths[0]).IntValue;
        var w1 = ((NumberObject)widths[1]).IntValue;
        var w2 = ((NumberObject)widths[2]).IntValue;
        var entryLength = w0 + w1 + w2;

        // A zero or negative total width never advances `pos`, so an attacker-chosen
        // /Size builds an unbounded table; reject it before the fill loop.
        if (w0 < 0 || w1 < 0 || w2 < 0 || entryLength <= 0)
        {
            throw new DocumentParseException("Invalid cross-reference stream entry width.", -1);
        }

        var size = ((NumberObject)dict["Size"]).IntValue;
        var index = BuildIndex(dict, size);

        var pos = 0;
        var total = 0;
        for (var s = 0; s + 1 < index.Count; s += 2)
        {
            var start = index[s];
            var count = index[s + 1];
            var available = (decoded.Length - pos) / entryLength;
            if (count > available)
            {
                count = available;
            }

            for (var i = 0; i < count; i++)
            {
                if (pos + entryLength > decoded.Length)
                {
                    break;
                }

                if (++total > limits.MaxXrefEntries)
                {
                    throw new DocumentParseException("Cross-reference table exceeds the maximum number of entries.", -1);
                }

                var field1 = ReadField(decoded, ref pos, w0);
                var field2 = ReadField(decoded, ref pos, w1);
                var field3 = ReadField(decoded, ref pos, w2);
                var type = w0 == 0 ? 1 : (int)field1;
                var number = start + i;
                if (!entries.ContainsKey(number))
                {
                    entries[number] = type switch
                    {
                        0 => new XrefEntry(0, 0, 0),
                        2 => new XrefEntry(2, field2, field3),
                        _ => new XrefEntry(1, field2, field3),
                    };
                }
            }
        }

        return dict;
    }

    private static List<int> BuildIndex(DictionaryObject dict, int size)
    {
        var result = new List<int>();
        if (dict.TryGetValue("Index", out var index) && index is ArrayObject array)
        {
            foreach (var item in array)
            {
                result.Add(((NumberObject)item).IntValue);
            }
        }
        else
        {
            result.Add(0);
            result.Add(size);
        }

        return result;
    }

    private static long ReadField(byte[] data, ref int pos, int width)
    {
        long value = 0;
        for (var i = 0; i < width; i++)
        {
            value = (value << 8) | data[pos];
            pos++;
        }

        return value;
    }

    private DocumentObject GetUncompressedObject(int number, long offset, out int generation)
    {
        if (TryParseObjectAt(offset, number, out var value, out generation))
        {
            return value!;
        }

        var offsets = ScannedOffsets();
        if (offsets.TryGetValue(number, out var recovered) && recovered != offset
            && TryParseObjectAt(recovered, number, out value, out generation))
        {
            return value!;
        }

        throw new DocumentParseException("Object not found at recorded offset.", (int)offset);
    }

    private DocumentObject GetCompressedObject(int streamNumber, int index)
    {
        var container = GetObjectStream(streamNumber);
        if (index < 0 || index >= container.Members.Count)
        {
            throw new DocumentParseException("Object stream index out of range.", -1);
        }

        var offset = container.Members[index].Offset;
        var lexer = new Lexer(container.Data, container.First + offset);
        return new ObjectParser(lexer).ParseValue();
    }

    private bool IsObjectStream(int number)
    {
        DocumentObject value;
        try
        {
            value = GetObject(number);
        }
        catch (DocumentParseException)
        {
            return false;
        }

        return value is StreamObject stream
            && stream.Dictionary.TryGetValue("Type", out var type) && type is NameObject name
            && string.Equals(name.Value, "ObjStm", StringComparison.Ordinal);
    }

    private ObjectStream GetObjectStream(int streamNumber)
    {
        if (objectStreams.TryGetValue(streamNumber, out var cached))
        {
            return cached;
        }

        if (GetObject(streamNumber) is not StreamObject stream)
        {
            throw new DocumentParseException("Object stream is not a stream.", -1);
        }

        var decoded = DecodeStreamData(stream.Dictionary, stream.Data);
        var count = ((NumberObject)stream.Dictionary["N"]).IntValue;
        var first = ((NumberObject)stream.Dictionary["First"]).IntValue;

        // A trusted /N would size the member list and drive the fill loop; clamp it
        // to what the decoded payload can hold (each member occupies at least two
        // bytes) and to the configured maximum before allocating.
        if (count < 0)
        {
            count = 0;
        }

        var payload = first >= 0 && first <= decoded.Length ? decoded.Length - first : decoded.Length;
        var available = payload / 2;
        if (count > available)
        {
            count = available;
        }

        if (count > limits.MaxObjectStreamCount)
        {
            count = limits.MaxObjectStreamCount;
        }

        var lexer = new Lexer(decoded, 0);
        var members = new List<ObjectStreamMember>(count);
        for (var i = 0; i < count; i++)
        {
            var numberToken = lexer.Next();
            var offsetToken = lexer.Next();
            if (numberToken.Kind == TokenKind.EndOfData || offsetToken.Kind == TokenKind.EndOfData)
            {
                break;
            }

            members.Add(new ObjectStreamMember((int)numberToken.IntValue, (int)offsetToken.IntValue));
        }

        var container = new ObjectStream(decoded, first, members);
        objectStreams[streamNumber] = container;
        return container;
    }

    private bool TryParseObjectAt(long offset, int? expected, out DocumentObject? value, out int generation)
    {
        value = null;
        generation = 0;
        if (offset < 0 || offset >= data.Length)
        {
            return false;
        }

        try
        {
            var lexer = new Lexer(data, (int)offset);
            var numberToken = lexer.Next();
            if (numberToken.Kind != TokenKind.Integer)
            {
                return false;
            }

            if (expected.HasValue && numberToken.IntValue != expected.Value)
            {
                return false;
            }

            var generationToken = lexer.Next();
            if (generationToken.Kind != TokenKind.Integer)
            {
                return false;
            }

            generation = (int)generationToken.IntValue;

            var keyword = lexer.Next();
            if (keyword.Kind != TokenKind.Keyword || keyword.Text != "obj")
            {
                return false;
            }

            value = ParseBody(lexer);
            return true;
        }
        catch (DocumentParseException)
        {
            return false;
        }
    }

    private DocumentObject ParseBody(Lexer lexer)
    {
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
        if (length < 0 || dataStart + (long)length > data.Length)
        {
            length = RecoverStreamLength(dataStart);
        }

        var payload = new byte[length];
        Array.Copy(data, dataStart, payload, 0, length);
        var stream = new StreamObject(payload);
        foreach (var key in dictionary.Keys)
        {
            stream.Dictionary[key] = dictionary[key];
        }

        return stream;
    }

    // A wrong /Length (negative or past the end of the file) falls back to
    // scanning for the "endstream" keyword instead of throwing a BCL exception.
    private int RecoverStreamLength(int dataStart)
    {
        const string keyword = "endstream";
        for (var i = dataStart; i <= data.Length - keyword.Length; i++)
        {
            if (Matches(i, keyword))
            {
                var end = i;
                if (end > dataStart && data[end - 1] == 10)
                {
                    end--;
                }

                if (end > dataStart && data[end - 1] == 13)
                {
                    end--;
                }

                return end - dataStart;
            }
        }

        throw new DocumentParseException("Invalid stream length.", dataStart);
    }

    private int ResolveLength(DictionaryObject dictionary)
    {
        if (!dictionary.TryGetValue("Length", out var lengthObject) || lengthObject is null)
        {
            throw new DocumentParseException("Missing stream length.", -1);
        }

        DocumentObject resolved;
        try
        {
            resolved = Resolve(lengthObject);
        }
        catch (DocumentParseException)
        {
            // A cyclic or dangling /Length reference; fall back to the endstream scan.
            return -1;
        }

        if (resolved is NumberObject number)
        {
            return number.IntValue;
        }

        // A /Length pointing at a free object now resolves to null; fall back to
        // the endstream scan rather than treating it as a hard failure.
        if (resolved is NullObject)
        {
            return -1;
        }

        throw new DocumentParseException("Invalid stream length.", -1);
    }

    /// <summary>
    /// Decodes the data of a stream object by applying its full <c>/Filter</c>
    /// chain (with <c>/DecodeParms</c> predictors) in order. A stream without a
    /// filter returns its data unchanged.
    /// </summary>
    /// <param name="stream">The stream object to decode.</param>
    /// <returns>The decoded stream bytes.</returns>
    /// <exception cref="DocumentParseException">The chain contains an unsupported filter.</exception>
    public byte[] DecodeStream(StreamObject stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return DecodeStreamData(stream.Dictionary, stream.Data);
    }

    private byte[] DecodeStreamData(DictionaryObject dictionary, byte[] data)
    {
        var filter = dictionary.TryGetValue("Filter", out var filterObject) && filterObject is not null
            ? Resolve(filterObject)
            : null;
        var names = FilterNames(filter);
        if (names.Count == 0)
        {
            return data;
        }

        if (names.Count > limits.MaxFilterChainLength)
        {
            throw new DocumentParseException("Filter chain exceeds the maximum length.", -1);
        }

        var parms = FilterParms(dictionary, names.Count);
        var result = data;
        var inputLength = data.Length;
        for (var i = 0; i < names.Count; i++)
        {
            result = ApplyFilter(names[i], result, parms[i], limits.MaxDecodedStreamBytes);

            // Cumulative per-stream cap plus a secondary expansion-ratio check that
            // only engages once output clears the floor, so small streams are never
            // rejected for a high ratio on tiny input.
            if (result.LongLength > limits.MaxDecodedStreamBytes)
            {
                throw new DocumentParseException("Decoded stream exceeds the maximum allowed size.", -1);
            }

            if (result.LongLength > limits.ExpansionRatioFloorBytes
                && inputLength > 0
                && result.LongLength / inputLength > limits.MaxDecodeExpansionRatio)
            {
                throw new DocumentParseException("Decoded stream expansion ratio exceeds the maximum.", -1);
            }
        }

        return result;
    }

    private static byte[] ApplyFilter(string name, byte[] data, DictionaryObject? parms, long maxOutput) => name switch
    {
        "FlateDecode" or "Fl" => ApplyPredictor(FlateFilter.Decode(data, maxOutput), parms),
        "LZWDecode" or "LZW" => ApplyPredictor(
            LzwFilter.Decode(data, parms is not null ? ParmInt(parms, "EarlyChange", 1) : 1, maxOutput), parms),
        "RunLengthDecode" or "RL" => RunLengthFilter.Decode(data, maxOutput),
        "ASCIIHexDecode" or "AHx" => AsciiHexFilter.Decode(data),
        "ASCII85Decode" or "A85" => Ascii85Filter.Decode(data, maxOutput),
        _ => throw new DocumentParseException($"Unsupported stream filter '{name}'.", -1),
    };

    private static byte[] ApplyPredictor(byte[] data, DictionaryObject? parms)
    {
        if (parms is null)
        {
            return data;
        }

        var predictor = ParmInt(parms, "Predictor", 1);
        if (predictor <= 1)
        {
            return data;
        }

        var columns = ParmInt(parms, "Columns", 1);
        var colors = ParmInt(parms, "Colors", 1);
        var bits = ParmInt(parms, "BitsPerComponent", 8);
        if (predictor >= 10)
        {
            return PngPredictor.Decode(data, colors, bits, columns);
        }

        return predictor == 2 ? TiffPredictor.Decode(data, colors, bits, columns) : data;
    }

    private static int ParmInt(DictionaryObject parms, string key, int fallback)
        => parms.TryGetValue(key, out var value) && value is NumberObject number ? number.IntValue : fallback;

    private List<string> FilterNames(DocumentObject? filter)
    {
        var names = new List<string>();
        if (filter is NameObject name)
        {
            names.Add(name.Value);
        }
        else if (filter is ArrayObject array)
        {
            foreach (var item in array)
            {
                if (Resolve(item) is NameObject entryName)
                {
                    names.Add(entryName.Value);
                }
            }
        }

        return names;
    }

    private List<DictionaryObject?> FilterParms(DictionaryObject dictionary, int count)
    {
        var parms = new List<DictionaryObject?>(count);
        DocumentObject? source = null;
        if (dictionary.TryGetValue("DecodeParms", out var direct))
        {
            source = direct;
        }
        else if (dictionary.TryGetValue("DP", out var abbreviated))
        {
            source = abbreviated;
        }

        if (source is not null)
        {
            source = Resolve(source);
        }

        if (source is ArrayObject array)
        {
            for (var i = 0; i < count; i++)
            {
                parms.Add(i < array.Count ? Resolve(array[i]) as DictionaryObject : null);
            }
        }
        else
        {
            parms.Add(source as DictionaryObject);
            for (var i = 1; i < count; i++)
            {
                parms.Add(null);
            }
        }

        return parms;
    }

    private void Repair()
    {
        entries.Clear();
        cache.Clear();
        objectStreams.Clear();

        var offsets = ScannedOffsets();
        if (offsets.Count == 0)
        {
            throw new DocumentParseException("No recoverable objects found.", -1);
        }

        var maxNumber = 0;
        foreach (var pair in offsets)
        {
            entries[pair.Key] = new XrefEntry(1, pair.Value, 0);
            if (pair.Key > maxNumber)
            {
                maxNumber = pair.Key;
            }
        }

        // The header scan only sees each ObjStm container's "N G obj"; register
        // type-2 entries for its members so compressed objects and /Root resolve.
        foreach (var number in new List<int>(entries.Keys))
        {
            if (!IsObjectStream(number))
            {
                continue;
            }

            ObjectStream container;
            try
            {
                container = GetObjectStream(number);
            }
            catch (DocumentParseException)
            {
                continue;
            }

            for (var index = 0; index < container.Members.Count; index++)
            {
                var member = container.Members[index];
                if (!entries.ContainsKey(member.Number))
                {
                    entries[member.Number] = new XrefEntry(2, number, index);
                    if (member.Number > maxNumber)
                    {
                        maxNumber = member.Number;
                    }
                }
            }
        }

        trailer = new DictionaryObject();

        // Newest catalog wins: scan object numbers in descending order so a stale
        // catalog left behind by an incremental update never shadows the current one.
        var numbers = new List<int>(entries.Keys);
        numbers.Sort();
        numbers.Reverse();
        foreach (var number in numbers)
        {
            DictionaryObject? dictionary = null;
            try
            {
                var obj = GetObject(number);
                dictionary = obj as DictionaryObject ?? (obj as StreamObject)?.Dictionary;
            }
            catch (DocumentParseException)
            {
                continue;
            }

            if (dictionary is not null && dictionary.TryGetValue("Type", out var type)
                && type is NameObject name && string.Equals(name.Value, "Catalog", StringComparison.Ordinal))
            {
                trailer["Root"] = new ReferenceObject(number, 0);
                break;
            }
        }

        trailer["Size"] = new NumberObject(maxNumber + 1);

        var preserved = FindRawTrailer();
        if (preserved is not null)
        {
            foreach (var key in (string[])["Encrypt", "ID", "Info"])
            {
                if (preserved.TryGetValue(key, out var value) && value is not null)
                {
                    trailer[key] = value;
                }
            }

            if (!trailer.TryGetValue("Root", out var root) || root is null)
            {
                if (preserved.TryGetValue("Root", out var preservedRoot) && preservedRoot is not null)
                {
                    trailer["Root"] = preservedRoot;
                }
            }
        }
    }

    // Locates the last parseable trailer dictionary in the raw bytes so a
    // repaired document keeps /Encrypt, /ID and /Info.
    private DictionaryObject? FindRawTrailer()
    {
        const string pattern = "trailer";
        for (var i = data.Length - pattern.Length; i >= 0; i--)
        {
            if (!Matches(i, pattern))
            {
                continue;
            }

            try
            {
                if (ObjectParser.Parse(data, i + pattern.Length) is DictionaryObject dictionary)
                {
                    return dictionary;
                }
            }
            catch (DocumentParseException)
            {
            }
        }

        return null;
    }

    private Dictionary<int, long> ScannedOffsets()
    {
        scanned ??= ScanObjects();
        return scanned;
    }

    private Dictionary<int, long> ScanObjects()
    {
        var map = new Dictionary<int, long>();
        for (var i = 0; i < data.Length; i++)
        {
            if (!IsDigit(data[i]))
            {
                continue;
            }

            if (i > 0 && !Lexer.IsWhitespace(data[i - 1]) && !Lexer.IsDelimiter(data[i - 1]))
            {
                continue;
            }

            var p = i;
            while (p < data.Length && IsDigit(data[p]))
            {
                p++;
            }

            if (!int.TryParse(Encoding.Latin1.GetString(data, i, p - i),
                NumberStyles.None, CultureInfo.InvariantCulture, out var objectNumber))
            {
                continue;
            }

            var afterNumber = p;
            p = SkipSpaces(p);
            if (p == afterNumber)
            {
                continue;
            }

            var genStart = p;
            while (p < data.Length && IsDigit(data[p]))
            {
                p++;
            }

            if (p == genStart)
            {
                continue;
            }

            var afterGen = p;
            p = SkipSpaces(p);
            if (p == afterGen || !Matches(p, "obj"))
            {
                continue;
            }

            var after = p + 3;
            if (after < data.Length && !Lexer.IsWhitespace(data[after]) && !Lexer.IsDelimiter(data[after]))
            {
                continue;
            }

            map[objectNumber] = i;
            i = p + 2;
        }

        return map;
    }

    private int SkipSpaces(int index)
    {
        while (index < data.Length && Lexer.IsWhitespace(data[index]))
        {
            index++;
        }

        return index;
    }

    private static bool IsDigit(byte b) => b >= (byte)'0' && b <= (byte)'9';

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

    private readonly struct XrefEntry(byte type, long field2, long field3)
    {
        public XrefEntry(int type, long field2, long field3)
            : this((byte)type, field2, field3)
        {
        }

        public byte Type { get; } = type;

        public long Field2 { get; } = field2;

        public long Field3 { get; } = field3;

        public bool InUse => Type != 0;
    }

    private sealed class ObjectStream(byte[] data, int first, List<ObjectStreamMember> members)
    {
        public byte[] Data { get; } = data;

        public int First { get; } = first;

        public List<ObjectStreamMember> Members { get; } = members;
    }

    private readonly struct ObjectStreamMember(int number, int offset)
    {
        public int Number { get; } = number;

        public int Offset { get; } = offset;
    }
}
