using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// Recovers a document whose cross-reference machinery is unusable: it scans the raw
/// bytes for <c>N G obj</c> headers to rebuild the xref, recovers streams whose
/// <c>/Length</c> is wrong or truncated, and reconstructs a trailer. The scan and
/// endstream caches are also consulted by the reader's normal object-retrieval path
/// (a single object recorded at the wrong offset, a stream with a bogus length).
/// </summary>
internal sealed class DocumentRepairer(byte[] data, ReaderLimits limits)
{
    private readonly byte[] data = data;
    private readonly ReaderLimits limits = limits;
    private Dictionary<int, long>? scanned;
    private int[]? endstreamOffsets;

    public DictionaryObject Repair(IDocumentRepairStore store)
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

        // The header scan only sees each ObjStm container's "N G obj"; register
        // type-2 entries for its members so compressed objects and /Root resolve.
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

        // Newest catalog wins: scan object numbers in descending order so a stale
        // catalog left behind by an incremental update never shadows the current one.
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

            if (!map.ContainsKey(objectNumber) && map.Count >= limits.MaxXrefEntries)
            {
                throw new DocumentParseException("Recovered cross-reference table exceeds the maximum number of entries.", -1);
            }

            map[objectNumber] = i;
            i = p + 2;
        }

        return map;
    }

    // A wrong /Length (negative or past the end of the file) falls back to the nearest
    // "endstream" keyword at or after the payload start. The keyword positions are
    // precomputed in a single pass and binary-searched, so a hostile file full of streams
    // with bogus lengths cannot force a quadratic per-stream scan to end-of-file.
    public int RecoverStreamLength(int dataStart)
    {
        var offsets = EndstreamOffsets();
        var lo = 0;
        var hi = offsets.Length;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (offsets[mid] < dataStart)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

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

    private int SkipSpaces(int index)
    {
        while (index < data.Length && Lexer.IsWhitespace(data[index]))
        {
            index++;
        }

        return index;
    }

    private static bool IsDigit(byte b) => b >= (byte)'0' && b <= (byte)'9';

    private bool Matches(int index, string pattern) => PdfBytes.Matches(data, index, pattern);
}

/// <summary>
/// Exposes only the object-store operations needed while repairing a document.
/// </summary>
internal interface IDocumentRepairStore
{
    void ResetForRepair();

    List<int> GetEntryNumbers();

    int EntryCount { get; }

    bool ContainsEntry(int number);

    void SetEntry(int number, XrefEntry entry);

    DocumentObject GetObject(int number);

    bool IsObjectStream(int number);

    ObjectStream GetObjectStream(int streamNumber);
}
