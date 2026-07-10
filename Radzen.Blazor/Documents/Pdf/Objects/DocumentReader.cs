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
    private readonly Dictionary<int, XrefEntry> entries = [];
    private readonly Dictionary<int, DocumentObject> cache = [];
    private readonly Dictionary<int, ObjectStream> objectStreams = [];
    private DictionaryObject trailer = new();
    private Dictionary<int, long>? scanned;
    private StandardSecurityHandler? security;
    private int encryptObjectNumber = -1;
    private bool decryptionReady;

    private DocumentReader(byte[] data)
    {
        this.data = data;
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
    public static DocumentReader Parse(byte[] data, string? password)
    {
        ArgumentNullException.ThrowIfNull(data);
        var reader = new DocumentReader(data);
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

        if (!entries.TryGetValue(number, out var entry) || !entry.InUse)
        {
            throw new DocumentParseException("Object not found.", -1);
        }

        DocumentObject value;
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

        var handler = new StandardSecurityHandler(encrypt, ReadDocumentId(), Encoding.Latin1.GetBytes(password ?? ""));
        decryptionReady = handler.IsUserPassword || handler.IsOwnerPassword;
        if (!decryptionReady && throwOnFailure)
        {
            throw new InvalidPasswordException();
        }

        security = handler;
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
                var plain = security!.Decrypt(Encoding.Latin1.GetBytes(text.Value), number, generation);
                return new StringObject(Encoding.Latin1.GetString(plain));
            case StreamObject stream:
                var decrypted = security!.Decrypt(stream.Data, number, generation);
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
        catch (DocumentParseException)
        {
            Repair();
        }
    }

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
                    entries[number] = type == (byte)'n'
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
        var size = ((NumberObject)dict["Size"]).IntValue;
        var index = BuildIndex(dict, size);

        var pos = 0;
        for (var s = 0; s + 1 < index.Count; s += 2)
        {
            var start = index[s];
            var count = index[s + 1];
            for (var i = 0; i < count; i++)
            {
                if (pos + entryLength > decoded.Length)
                {
                    break;
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

        var lexer = new Lexer(decoded, 0);
        var members = new List<ObjectStreamMember>(count);
        for (var i = 0; i < count; i++)
        {
            var numberToken = lexer.Next();
            var offsetToken = lexer.Next();
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

    private static byte[] DecodeStreamData(DictionaryObject dictionary, byte[] data)
    {
        var filter = dictionary.TryGetValue("Filter", out var filterObject) ? filterObject : null;
        var names = FilterNames(filter);
        if (names.Count == 0)
        {
            return data;
        }

        var parms = FilterParms(dictionary, names.Count);
        var result = data;
        for (var i = 0; i < names.Count; i++)
        {
            result = ApplyFilter(names[i], result, parms[i]);
        }

        return result;
    }

    private static byte[] ApplyFilter(string name, byte[] data, DictionaryObject? parms)
    {
        if (!string.Equals(name, "FlateDecode", StringComparison.Ordinal)
            && !string.Equals(name, "Fl", StringComparison.Ordinal))
        {
            throw new DocumentParseException($"Unsupported cross-reference filter '{name}'.", -1);
        }

        return ApplyPredictor(FlateFilter.Decode(data), parms);
    }

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

    private static List<string> FilterNames(DocumentObject? filter)
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
                if (item is NameObject entryName)
                {
                    names.Add(entryName.Value);
                }
            }
        }

        return names;
    }

    private static List<DictionaryObject?> FilterParms(DictionaryObject dictionary, int count)
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

        if (source is ArrayObject array)
        {
            for (var i = 0; i < count; i++)
            {
                parms.Add(i < array.Count ? array[i] as DictionaryObject : null);
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

        trailer = new DictionaryObject();
        foreach (var pair in offsets)
        {
            DictionaryObject? dictionary = null;
            try
            {
                var obj = GetObject(pair.Key);
                dictionary = obj as DictionaryObject ?? (obj as StreamObject)?.Dictionary;
            }
            catch (DocumentParseException)
            {
                continue;
            }

            if (dictionary is not null && dictionary.TryGetValue("Type", out var type)
                && type is NameObject name && string.Equals(name.Value, "Catalog", StringComparison.Ordinal))
            {
                trailer["Root"] = new ReferenceObject(pair.Key, 0);
                break;
            }
        }

        trailer["Size"] = new NumberObject(maxNumber + 1);
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
