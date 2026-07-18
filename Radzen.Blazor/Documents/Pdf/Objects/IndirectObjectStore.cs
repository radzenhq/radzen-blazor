using Radzen.Documents.Pdf.Objects.Encryption;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Radzen.Documents.Pdf.Objects;

internal sealed class IndirectObjectStore(
    byte[] data,
    ReaderLimits limits,
    Dictionary<int, XrefEntry> entries,
    StreamDecoder decoder,
    DocumentRepairer repairer) : IDocumentRepairStore
{
    private readonly byte[] data = data;
    private readonly ReaderLimits limits = limits;
    private readonly Dictionary<int, XrefEntry> entries = entries;
    private readonly ConcurrentDictionary<int, DocumentObject> cache = [];
    private readonly ConcurrentDictionary<int, ObjectStream> objectStreams = [];
    private readonly NullObject nullObject = new();
    private readonly object memberCountsLock = new();
    private Dictionary<int, int>? memberCounts;

    [ThreadStatic]
    private static HashSet<(IndirectObjectStore Store, int Number)>? parsing;
    private readonly StreamDecoder decoder = decoder;
    private readonly DocumentRepairer repairer = repairer;
    private StandardSecurityHandler? security;
    private int encryptObjectNumber = -1;

    internal bool IsEncrypted => security is not null;

    // ISO 32000-1 7.5.7: object-stream members are always generation 0; type-2 Field3 is the member index.
    internal int GenerationOf(int number)
        => entries.TryGetValue(number, out var entry) && entry.Type == 1 ? (int)entry.Field3 : 0;

    public DocumentObject GetObject(int number)
    {
        if (cache.TryGetValue(number, out var cached))
        {
            return cached;
        }

        // ISO 32000-1 7.3.10: a reference to a free or nonexistent object resolves to null.
        if (!entries.TryGetValue(number, out var entry) || !entry.InUse)
        {
            return nullObject;
        }

        var inProgress = parsing ??= [];
        var marker = (this, number);
        if (!inProgress.Add(marker))
        {
            throw new DocumentParseException("Cyclic object reference.", -1);
        }

        DocumentObject value;
        try
        {
            if (entry.Type == 2)
            {
                value = GetCompressedObject(number, (int)entry.Field2, (int)entry.Field3);
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
            inProgress.Remove(marker);
        }

        var published = cache.GetOrAdd(number, value);
        if (entry.Type == 2 && ReferenceEquals(published, value))
        {
            ReleaseDrainedObjectStream((int)entry.Field2);
        }

        return published;
    }

    private void ReleaseDrainedObjectStream(int streamNumber)
    {
        if (objectStreams.TryGetValue(streamNumber, out var container) && container.MemberResolved())
        {
            objectStreams.TryRemove(streamNumber, out _);
        }
    }

    private int MemberCountOf(int streamNumber)
    {
        var counts = memberCounts;
        if (counts is null)
        {
            lock (memberCountsLock)
            {
                counts = memberCounts ??= CountMembersPerStream();
            }
        }

        return counts.TryGetValue(streamNumber, out var count) ? count : 0;
    }

    private Dictionary<int, int> CountMembersPerStream()
    {
        var counts = new Dictionary<int, int>();
        foreach (var pair in entries)
        {
            if (pair.Value.Type != 2)
            {
                continue;
            }

            var stream = (int)pair.Value.Field2;
            counts.TryGetValue(stream, out var count);
            counts[stream] = count + 1;
        }

        return counts;
    }

    public DocumentObject Resolve(DocumentObject value)
    {
        if (value is ReferenceObject reference)
        {
            if (entries.TryGetValue(reference.ObjectNumber, out var entry) && entry.InUse
                && GenerationOf(reference.ObjectNumber) != reference.Generation)
            {
                return nullObject;
            }

            return GetObject(reference.ObjectNumber);
        }

        return value;
    }

    internal IReadOnlyDictionary<DocumentObject, int> BuildObjectNumberIndex()
    {
        var index = new Dictionary<DocumentObject, int>(ReferenceEqualityComparer.Instance);
        foreach (var pair in cache)
        {
            index[pair.Value] = pair.Key;
        }

        return index;
    }

    internal void SetSecurity(StandardSecurityHandler handler, int objectNumber)
    {
        security = handler;
        encryptObjectNumber = objectNumber;

        cache.Clear();
        objectStreams.Clear();
        memberCounts = null;
    }

    private DocumentObject DecryptObject(DocumentObject value, int number, int generation)
    {
        DocumentObject? Decrypt(DocumentObject node)
        {
            switch (node)
            {
                case StringObject text:
                    var plain = security!.DecryptString(Encoding.Latin1.GetBytes(text.Value), number, generation);
                    return new StringObject(Encoding.Latin1.GetString(plain.Span));
                case StreamObject stream:
                    var decrypted = security!.DecryptStream(stream.Data, number, generation, stream.Dictionary);
                    var result = new StreamObject(decrypted);
                    foreach (var key in stream.Dictionary.Keys)
                    {
                        result.Dictionary[key] = CosGraphRewriter.Rewrite(stream.Dictionary[key], Decrypt);
                    }

                    return result;
                default:
                    return null;
            }
        }

        return CosGraphRewriter.Rewrite(value, Decrypt);
    }

    private DocumentObject GetUncompressedObject(int number, long offset, out int generation)
    {
        if (TryParseObjectAt(offset, number, out var value, out generation))
        {
            return value!;
        }

        var offsets = repairer.ScannedOffsets();
        if (offsets.TryGetValue(number, out var recovered) && recovered != offset
            && TryParseObjectAt(recovered, number, out value, out generation))
        {
            return value!;
        }

        throw new DocumentParseException("Object not found at recorded offset.", (int)offset);
    }

    private DocumentObject GetCompressedObject(int expectedNumber, int streamNumber, int index)
    {
        var container = GetObjectStream(streamNumber);
        if (index < 0 || index >= container.Members.Count)
        {
            throw new DocumentParseException("Object stream index out of range.", -1);
        }

        if (container.Members[index].Number != expectedNumber)
        {
            throw new DocumentParseException("Object stream member number does not match the requested object.", -1);
        }

        var offset = container.Members[index].Offset;

        var start = (long)container.First + offset;
        if (offset < 0 || start < 0 || start > container.Data.Length)
        {
            throw new DocumentParseException("Object stream member offset out of range.", -1);
        }

        var lexer = new Lexer(container.Data, (int)start);
        return new ObjectParser(lexer, limits).ParseValue();
    }

    public bool IsObjectStream(int number)
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

    public ObjectStream GetObjectStream(int streamNumber)
    {
        if (objectStreams.TryGetValue(streamNumber, out var cached))
        {
            return cached;
        }

        if (GetObject(streamNumber) is not StreamObject stream)
        {
            throw new DocumentParseException("Object stream is not a stream.", -1);
        }

        var decoded = decoder.Decode(stream.Dictionary, stream.Data);
        var count = ((NumberObject)stream.Dictionary["N"]).IntValue;
        var first = ((NumberObject)stream.Dictionary["First"]).IntValue;

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

        var container = new ObjectStream(decoded, first, members, MemberCountOf(streamNumber));
        return objectStreams.GetOrAdd(streamNumber, container);
    }

    internal bool TryParseObjectAt(long offset, int? expected, out DocumentObject? value, out int generation)
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
        var parser = new ObjectParser(lexer, limits);
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
            length = repairer.RecoverStreamLength(dataStart);
        }

        var stream = new StreamObject(data.AsMemory(dataStart, length));
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

        DocumentObject resolved;
        try
        {
            resolved = Resolve(lengthObject);
        }
        catch (DocumentParseException)
        {
            return -1;
        }

        if (resolved is NumberObject number)
        {
            return number.IntValue;
        }

        if (resolved is NullObject)
        {
            return -1;
        }

        throw new DocumentParseException("Invalid stream length.", -1);
    }

    public void ResetForRepair()
    {
        entries.Clear();
        cache.Clear();
        objectStreams.Clear();
        memberCounts = null;
    }

    public List<int> GetEntryNumbers() => [.. entries.Keys];

    public int EntryCount => entries.Count;

    public bool ContainsEntry(int number) => entries.ContainsKey(number);

    public void SetEntry(int number, XrefEntry entry) => entries[number] = entry;
}

internal sealed class ObjectStream(byte[] data, int first, List<ObjectStreamMember> members, int unresolved)
{
    private int unresolved = unresolved;

    public byte[] Data { get; } = data;

    public int First { get; } = first;

    public List<ObjectStreamMember> Members { get; } = members;

    internal bool MemberResolved() => Interlocked.Decrement(ref unresolved) == 0;
}

internal readonly struct ObjectStreamMember(int number, int offset)
{
    public int Number { get; } = number;

    public int Offset { get; } = offset;
}
