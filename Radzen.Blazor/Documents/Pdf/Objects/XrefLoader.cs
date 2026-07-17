using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Objects;

internal sealed class XrefLoader(byte[] data, ReaderLimits limits, StreamDecoder decoder)
{
    private readonly byte[] data = data;
    private readonly ReaderLimits limits = limits;
    private readonly StreamDecoder decoder = decoder;
    private readonly Dictionary<int, XrefEntry> entries = [];

    internal Dictionary<int, XrefEntry> Entries => entries;

    public DictionaryObject Load(IndirectObjectStore store)
    {
        var offset = PdfBytes.FindStartXref(data);
        var visited = new HashSet<long>();
        DictionaryObject? newest = null;
        while (visited.Add(offset))
        {
            var section = ReadXrefSectionAt(offset, store);
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

        return newest ?? throw new DocumentParseException("Missing trailer.", -1);
    }

    private void RequireEntryBudget(int pending)
    {
        if (entries.Count + pending >= limits.MaxXrefEntries)
        {
            throw new DocumentParseException("Cross-reference table exceeds the maximum number of entries.", -1);
        }
    }

    private DictionaryObject ReadXrefSectionAt(long offset, IndirectObjectStore store)
    {
        var index = (int)offset;
        SkipWhitespace(ref index);
        return Matches(index, "xref")
            ? ParseClassicXref(index + 4, store)
            : ParseXrefStreamAt(offset, store);
    }

    private DictionaryObject ParseClassicXref(int index, IndirectObjectStore store)
    {
        var section = new Dictionary<int, XrefEntry>();
        while (true)
        {
            SkipWhitespace(ref index);
            if (Matches(index, "trailer"))
            {
                index += 7;
                var trailerDict = (DictionaryObject)ObjectParser.Parse(data, index, limits);

                foreach (var pair in section)
                {
                    if (pair.Value.InUse && !entries.ContainsKey(pair.Key))
                    {
                        entries[pair.Key] = pair.Value;
                    }
                }

                // ISO 32000-1 7.5.8.4: /XRefStm supersedes the free entries this standard section uses to mask
                // compressed objects, but is consulted before /Prev - so in-use entries of this section win.
                if (trailerDict.TryGetValue("XRefStm", out var hybrid) && hybrid is NumberObject hybridOffset)
                {
                    ParseXrefStreamAt((long)hybridOffset.DoubleValue, store);
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
                var entryGeneration = ReadLong(ref index);
                SkipWhitespace(ref index);
                var type = data[index];
                index++;
                var number = start + i;
                if (!entries.ContainsKey(number) && !section.ContainsKey(number))
                {
                    RequireEntryBudget(section.Count);
                    section[number] = type == (byte)'n'
                        ? new XrefEntry(1, entryOffset, entryGeneration)
                        : new XrefEntry(0, 0, 0);
                }
            }
        }
    }

    private DictionaryObject ParseXrefStreamAt(long offset, IndirectObjectStore store)
    {
        if (!store.TryParseObjectAt(offset, null, out var value, out _) || value is not StreamObject stream)
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
        var decoded = decoder.Decode(dict, stream.Data);

        var widths = (ArrayObject)dict["W"];
        var w0 = ((NumberObject)widths[0]).IntValue;
        var w1 = ((NumberObject)widths[1]).IntValue;
        var w2 = ((NumberObject)widths[2]).IntValue;
        var entryLength = w0 + w1 + w2;

        if (w0 < 0 || w1 < 0 || w2 < 0 || entryLength <= 0)
        {
            throw new DocumentParseException("Invalid cross-reference stream entry width.", -1);
        }

        var size = ((NumberObject)dict["Size"]).IntValue;
        var index = BuildIndex(dict, size);

        var pos = 0;
        for (var s = 0; s + 1 < index.Count; s += 2)
        {
            var start = index[s];
            var count = index[s + 1];

            var available = (decoded.Length - pos) / entryLength;
            if (count > available)
            {
                throw new DocumentParseException("Cross-reference stream is shorter than its /Index declares.", -1);
            }

            for (var i = 0; i < count; i++)
            {
                var field1 = ReadField(decoded, ref pos, w0);
                var field2 = ReadField(decoded, ref pos, w1);
                var field3 = ReadField(decoded, ref pos, w2);
                var type = w0 == 0 ? 1 : (int)field1;
                var number = start + i;
                if (!entries.ContainsKey(number))
                {
                    RequireEntryBudget(0);
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

    private bool Matches(int index, string pattern) => PdfBytes.Matches(data, index, pattern);

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
        var negative = false;
        if (index < data.Length && (data[index] == (byte)'+' || data[index] == (byte)'-'))
        {
            negative = data[index] == (byte)'-';
            index++;
        }

        var digits = index;
        long value = 0;
        while (index < data.Length && data[index] >= (byte)'0' && data[index] <= (byte)'9')
        {
            if (value > (long.MaxValue - (data[index] - '0')) / 10)
            {
                throw new DocumentParseException("Integer is out of range.", start);
            }

            value = (value * 10) + (data[index] - '0');
            index++;
        }

        if (index == digits)
        {
            throw new DocumentParseException("Expected integer.", start);
        }

        return negative ? -value : value;
    }
}

internal readonly struct XrefEntry(byte type, long field2, long field3)
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
