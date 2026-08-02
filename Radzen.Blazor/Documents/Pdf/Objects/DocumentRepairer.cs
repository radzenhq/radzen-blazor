using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Documents.Pdf.Objects;

internal sealed class DocumentRepairer(byte[] data, ReaderLimits limits)
{
    private readonly byte[] data = data;
    private readonly ReaderLimits limits = limits;
    private const int UnbalancedSpan = -1;
    private const int EscapedSpan = -2;
    private Dictionary<int, long>? scanned;
    private int[]? endstreamOffsets;
    private Dictionary<int, int>? literalStringSpans;
    private int unbalancedStringFrom = int.MaxValue;

    public DictionaryObject Repair(IndirectObjectStore store)
    {
        store.ResetForRepair();

        var offsets = ScannedOffsets();
        if (offsets.Count == 0)
        {
            throw new DocumentParseException("No recoverable objects found.", -1);
        }

        var maxNumber = 0;
        foreach (var pair in offsets)
        {
            store.SetEntry(pair.Key, new XrefEntry(1, pair.Value, 0));
            if (pair.Key > maxNumber)
            {
                maxNumber = pair.Key;
            }
        }

        foreach (var number in store.GetEntryNumbers())
        {
            if (!store.IsObjectStream(number))
            {
                continue;
            }

            ObjectStream container;
            try
            {
                container = store.GetObjectStream(number);
            }
            catch (DocumentParseException)
            {
                continue;
            }

            for (var index = 0; index < container.Members.Count; index++)
            {
                var member = container.Members[index];
                if (!store.ContainsEntry(member.Number))
                {
                    if (store.EntryCount >= limits.MaxXrefEntries)
                    {
                        throw new DocumentParseException("Recovered cross-reference table exceeds the maximum number of entries.", -1);
                    }

                    store.SetEntry(member.Number, new XrefEntry(2, number, index));
                    if (member.Number > maxNumber)
                    {
                        maxNumber = member.Number;
                    }
                }
            }
        }

        var trailer = new DictionaryObject();

        var numbers = store.GetEntryNumbers();
        numbers.Sort();
        numbers.Reverse();
        foreach (var number in numbers)
        {
            DictionaryObject? dictionary = null;
            try
            {
                var obj = store.GetObject(number);
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

        return trailer;
    }

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
                if (ObjectParser.Parse(data, i + pattern.Length, limits) is DictionaryObject dictionary)
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

    public Dictionary<int, long> ScannedOffsets()
    {
        scanned ??= ScanObjects();
        return scanned;
    }

    private Dictionary<int, long> ScanObjects()
    {
        var map = new Dictionary<int, long>();
        var ends = EndstreamOffsets();
        var i = 0;
        while (i < data.Length)
        {
            var b = data[i];

            if (b == (byte)'(')
            {
                i = SkipLiteralString(i);
                continue;
            }

            if (b == (byte)'%')
            {
                i = Lexer.SkipComment(data, i);
                continue;
            }

            // ISO 32000-1 7.3.8.1: the stream keyword is followed by CRLF or LF, then the stream data.
            if (b == (byte)'s' && Matches(i, "stream") && FollowedByEndOfLine(i + 6))
            {
                i = SkipStreamBody(i + 6, ends);
                continue;
            }

            if (IsDigit(b) && (i == 0 || Lexer.IsWhitespace(data[i - 1]) || Lexer.IsDelimiter(data[i - 1]))
                && TryReadObjectHeader(i, out var objectNumber, out var next))
            {
                if (!map.ContainsKey(objectNumber))
                {
                    if (map.Count >= limits.MaxXrefEntries)
                    {
                        throw new DocumentParseException("Recovered cross-reference table exceeds the maximum number of entries.", -1);
                    }

                    map[objectNumber] = i;
                }
                else if (i < unbalancedStringFrom)
                {
                    map[objectNumber] = i;
                }

                i = next;
                continue;
            }

            i++;
        }

        return map;
    }

    private int SkipLiteralString(int open)
    {
        var spans = literalStringSpans ??= [];
        if (spans.TryGetValue(open, out var cached))
        {
            return cached >= 0 ? cached + 1 : open + 1;
        }

        var stack = new Stack<int>();
        stack.Push(open);
        var escaped = false;
        var index = open + 1;
        while (index < data.Length)
        {
            var c = data[index];
            if (escaped)
            {
                if (c == (byte)'(')
                {
                    spans[index] = EscapedSpan;
                }

                escaped = false;
            }
            else if (c == (byte)'\\')
            {
                escaped = true;
            }
            else if (c == (byte)'(')
            {
                stack.Push(index);
            }
            else if (c == (byte)')')
            {
                var start = stack.Pop();
                spans[start] = index;
                if (stack.Count == 0)
                {
                    return index + 1;
                }
            }

            index++;
        }

        foreach (var start in stack)
        {
            spans[start] = UnbalancedSpan;
        }

        unbalancedStringFrom = Math.Min(unbalancedStringFrom, open);
        return open + 1;
    }

    private static int LowerBound(int[] offsets, int value)
    {
        var lo = 0;
        var hi = offsets.Length;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (offsets[mid] < value)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    private bool FollowedByEndOfLine(int index)
        => index < data.Length && (data[index] == (byte)'\n' || data[index] == (byte)'\r');

    private int SkipStreamBody(int bodyStart, int[] ends)
    {
        var lo = LowerBound(ends, bodyStart);
        return lo < ends.Length ? ends[lo] + "endstream".Length : bodyStart;
    }

    private bool TryReadObjectHeader(int start, out int objectNumber, out int next)
    {
        objectNumber = 0;
        next = start;

        var p = start;
        while (p < data.Length && IsDigit(data[p]))
        {
            p++;
        }

        if (!int.TryParse(Encoding.Latin1.GetString(data, start, p - start),
            NumberStyles.None, CultureInfo.InvariantCulture, out objectNumber))
        {
            return false;
        }

        var afterNumber = p;
        p = SkipSpaces(p);
        if (p == afterNumber)
        {
            return false;
        }

        var genStart = p;
        while (p < data.Length && IsDigit(data[p]))
        {
            p++;
        }

        if (p == genStart)
        {
            return false;
        }

        var afterGen = p;
        p = SkipSpaces(p);
        if (p == afterGen || !Matches(p, "obj"))
        {
            return false;
        }

        var after = p + 3;
        if (after < data.Length && !Lexer.IsWhitespace(data[after]) && !Lexer.IsDelimiter(data[after]))
        {
            return false;
        }

        next = p + 3;
        return true;
    }

    public int RecoverStreamLength(int dataStart)
    {
        var offsets = EndstreamOffsets();
        var lo = LowerBound(offsets, dataStart);

        if (lo >= offsets.Length)
        {
            throw new DocumentParseException("Invalid stream length.", dataStart);
        }

        var end = offsets[lo];
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

    private int[] EndstreamOffsets()
    {
        if (endstreamOffsets is not null)
        {
            return endstreamOffsets;
        }

        const string keyword = "endstream";
        var offsets = new List<int>();
        for (var i = 0; i <= data.Length - keyword.Length; i++)
        {
            if (Matches(i, keyword))
            {
                offsets.Add(i);
                i += keyword.Length - 1;
            }
        }

        endstreamOffsets = [.. offsets];
        return endstreamOffsets;
    }

    private int SkipSpaces(int index) => Lexer.SkipWhitespace(data, index);

    private static bool IsDigit(byte b) => b >= (byte)'0' && b <= (byte)'9';

    private bool Matches(int index, string pattern) => PdfBytes.Matches(data, index, pattern);
}
