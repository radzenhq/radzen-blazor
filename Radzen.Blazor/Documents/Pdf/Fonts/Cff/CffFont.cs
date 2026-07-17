using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Fonts.Cff;

// CFF font parser (Adobe Technical Note 5176). Supports name-keyed and CID-keyed
// (ROS) fonts: charset, per-FD Private DICTs, FDArray/FDSelect, and enough Type 2
// charstring interpretation to recover the advance width operand and detect seac.
internal sealed class CffFont
{
    private readonly int[] charset;
    private readonly int[] fdSelect;
    private readonly FdInfo[] fdArray;
    private readonly CffIndex charStrings;
    private readonly CffIndex globalSubrs;
    private readonly int globalBias;
    private readonly bool isCidKeyed;
    private readonly ReaderLimits limits;

    private CffFont(
        string? fontName,
        string? registry,
        string? ordering,
        int supplement,
        bool isCidKeyed,
        double[]? fontMatrix,
        int[] charset,
        int[] fdSelect,
        FdInfo[] fdArray,
        CffIndex charStrings,
        CffIndex globalSubrs,
        ReaderLimits limits)
    {
        this.limits = limits;
        FontMatrix = fontMatrix;
        FontName = fontName;
        Registry = registry;
        Ordering = ordering;
        Supplement = supplement;
        this.isCidKeyed = isCidKeyed;
        this.charset = charset;
        this.fdSelect = fdSelect;
        this.fdArray = fdArray;
        this.charStrings = charStrings;
        this.globalSubrs = globalSubrs;
        globalBias = Bias(globalSubrs.Count);
    }

    public string? FontName { get; }

    public string? Registry { get; }

    public string? Ordering { get; }

    public int Supplement { get; }

    // Top DICT FontMatrix (12 7), or null when the font uses the default 0.001 matrix.
    public double[]? FontMatrix { get; }

    public bool IsCidKeyed => isCidKeyed;

    public int GlyphCount => charStrings.Count;

    public int[] Charset => charset;

    public int FdCount => fdArray.Length;

    public int GetFd(int glyphIndex)
    {
        if (!isCidKeyed)
        {
            return 0;
        }

        return fdSelect[glyphIndex];
    }

    public int GetAdvanceWidth(int glyphIndex)
    {
        var fd = fdArray[GetFd(glyphIndex)];
        var charString = charStrings.GetBytes(glyphIndex);

        var context = new WidthContext(fd, globalSubrs, globalBias, limits.MaxCharstringOperations);
        context.Run(charString);

        var width = context.Width ?? fd.DefaultWidthX;
        return (int)Math.Round(width, MidpointRounding.AwayFromZero);
    }

    internal byte[] GetCharStringBytes(int glyphIndex) => charStrings.GetBytes(glyphIndex);

    // True when the glyph is composed with an endchar seac (4 or 5 operands): its base and
    // accent are addressed through the source charset/Standard Encoding. A compact subset
    // renumbers glyphs and rewrites the charset to identity, which silently breaks that
    // reference, so the subsetter rejects such glyphs rather than emit a wrong composition.
    internal bool UsesSeacEndchar(int glyphIndex)
    {
        var fd = fdArray[GetFd(glyphIndex)];
        var context = new SeacContext(fd, globalSubrs, globalBias, limits.MaxCharstringOperations);
        context.Run(charStrings.GetBytes(glyphIndex));
        return context.Seac;
    }

    internal byte[][] GetGlobalSubrBytes() => Extract(globalSubrs);

    internal byte[][] GetLocalSubrBytes(int fd)
    {
        var subrs = fdArray[fd].LocalSubrs;
        return subrs is null ? [] : Extract(subrs);
    }

    internal double GetDefaultWidthX(int fd) => fdArray[fd].DefaultWidthX;

    internal double GetNominalWidthX(int fd) => fdArray[fd].NominalWidthX;

    internal double[]? GetFdFontMatrix(int fd) => fdArray[fd].FontMatrix;

    private static byte[][] Extract(CffIndex index)
    {
        var result = new byte[index.Count][];
        for (var i = 0; i < index.Count; i++)
        {
            result[i] = index.GetBytes(i);
        }

        return result;
    }

    // limits bounds the charstring walk. The font bytes reach here from the public
    // FontCollection.Register(string, Stream), so they are attacker-controlled.
    public static CffFont Parse(byte[] cffData, ReaderLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(cffData);

        var effectiveLimits = (limits ?? ReaderLimits.Default).Snapshot();

        if (cffData.Length < 4 || cffData[0] != 1)
        {
            throw new InvalidDataException("Not a valid CFF font: unexpected header.");
        }

        var headerSize = cffData[2];

        var nameIndex = CffIndex.Read(cffData, headerSize);
        var topDictIndex = CffIndex.Read(cffData, nameIndex.EndOffset);
        var stringIndex = CffIndex.Read(cffData, topDictIndex.EndOffset);
        var globalSubrs = CffIndex.Read(cffData, stringIndex.EndOffset);

        var fontName = nameIndex.Count > 0 ? Ascii(nameIndex.GetBytes(0)) : null;
        var topDict = CffDict.Parse(topDictIndex.GetBytes(0));

        if (!topDict.TryGetValue(17, out var charStringsOp) || charStringsOp.Length == 0)
        {
            throw new InvalidDataException("CFF Top DICT is missing CharStrings.");
        }

        var charStrings = CffIndex.Read(cffData, (int)charStringsOp[0]);
        var glyphCount = charStrings.Count;

        var fontMatrix = topDict.TryGetValue(1207, out var matrix) && matrix.Length == 6 ? matrix : null;

        var isCidKeyed = topDict.TryGetValue(1230, out var ros) && ros is not null && ros.Length >= 3;
        string? registry = null;
        string? ordering = null;
        var supplement = 0;
        if (isCidKeyed && ros is not null)
        {
            registry = GetString((int)ros[0], stringIndex);
            ordering = GetString((int)ros[1], stringIndex);
            supplement = (int)ros[2];
        }

        var charset = ReadCharset(cffData, topDict, glyphCount);

        FdInfo[] fdArray;
        int[] fdSelect;
        if (isCidKeyed)
        {
            fdArray = ReadFdArray(cffData, topDict);
            fdSelect = ReadFdSelect(cffData, topDict, glyphCount);
        }
        else
        {
            // A name-keyed font carries its FontMatrix only in the Top DICT. Do not also
            // surface it as the FD's matrix: the subsetter writes both, and a viewer that
            // concatenates Top-DICT and FD-DICT matrices would then scale glyphs twice.
            fdArray = [ReadPrivate(cffData, topDict, includeFontMatrix: false)];
            fdSelect = [];
        }

        return new CffFont(fontName, registry, ordering, supplement, isCidKeyed, fontMatrix, charset, fdSelect, fdArray, charStrings, globalSubrs, effectiveLimits);
    }

    private static int[] ReadCharset(byte[] data, Dictionary<int, double[]> topDict, int glyphCount)
    {
        var charset = new int[glyphCount];
        var offset = topDict.TryGetValue(15, out var op) && op.Length > 0 ? (int)op[0] : 0;

        // 0/1/2 are predefined charsets; the CID fonts we target always carry a real offset.
        if (offset <= 2)
        {
            for (var gid = 0; gid < glyphCount; gid++)
            {
                charset[gid] = gid;
            }

            return charset;
        }

        var format = ReadByteAt(data, offset);
        var p = offset + 1;
        var glyph = 1;
        switch (format)
        {
            case 0:
                while (glyph < glyphCount)
                {
                    charset[glyph++] = ReadCard16(data, p);
                    p += 2;
                }

                break;
            case 1:
                while (glyph < glyphCount)
                {
                    var first = ReadCard16(data, p);
                    int left = ReadByteAt(data, p + 2);
                    p += 3;
                    for (var i = 0; i <= left && glyph < glyphCount; i++)
                    {
                        charset[glyph++] = first + i;
                    }
                }

                break;
            case 2:
                while (glyph < glyphCount)
                {
                    var first = ReadCard16(data, p);
                    var left = ReadCard16(data, p + 2);
                    p += 4;
                    for (var i = 0; i <= left && glyph < glyphCount; i++)
                    {
                        charset[glyph++] = first + i;
                    }
                }

                break;
            default:
                throw new InvalidDataException($"Unsupported CFF charset format {format}.");
        }

        return charset;
    }

    private static int[] ReadFdSelect(byte[] data, Dictionary<int, double[]> topDict, int glyphCount)
    {
        if (!topDict.TryGetValue(1237, out var op) || op.Length == 0)
        {
            throw new InvalidDataException("CID-keyed CFF is missing FDSelect.");
        }

        var offset = (int)op[0];
        var result = new int[glyphCount];
        var format = ReadByteAt(data, offset);
        switch (format)
        {
            case 0:
                for (var gid = 0; gid < glyphCount; gid++)
                {
                    result[gid] = ReadByteAt(data, offset + 1 + gid);
                }

                break;
            case 3:
                var nRanges = ReadCard16(data, offset + 1);
                var p = offset + 3;
                for (var r = 0; r < nRanges; r++)
                {
                    var first = ReadCard16(data, p);
                    int fd = ReadByteAt(data, p + 2);
                    var next = ReadCard16(data, p + 3);
                    for (var gid = first; gid < next && gid < glyphCount; gid++)
                    {
                        result[gid] = fd;
                    }

                    p += 3;
                }

                break;
            default:
                throw new InvalidDataException($"Unsupported CFF FDSelect format {format}.");
        }

        return result;
    }

    private static FdInfo[] ReadFdArray(byte[] data, Dictionary<int, double[]> topDict)
    {
        if (!topDict.TryGetValue(1236, out var op) || op.Length == 0)
        {
            throw new InvalidDataException("CID-keyed CFF is missing FDArray.");
        }

        var fdIndex = CffIndex.Read(data, (int)op[0]);
        var result = new FdInfo[fdIndex.Count];
        for (var i = 0; i < fdIndex.Count; i++)
        {
            result[i] = ReadPrivate(data, CffDict.Parse(fdIndex.GetBytes(i)));
        }

        return result;
    }

    private static FdInfo ReadPrivate(byte[] data, Dictionary<int, double[]> dict, bool includeFontMatrix = true)
    {
        var fontMatrix = includeFontMatrix && dict.TryGetValue(1207, out var matrix) && matrix.Length == 6 ? matrix : null;
        if (!dict.TryGetValue(18, out var op))
        {
            return new FdInfo(0, 0, null, 0, fontMatrix);
        }

        if (op.Length < 2)
        {
            throw new InvalidDataException("CFF Private operator requires size and offset operands.");
        }

        var size = (int)op[0];
        var offset = (int)op[1];

        // size/offset come straight from the Top DICT; a hostile size would allocate an
        // arbitrary buffer and the copy would read past the font. Validate against the data.
        if (size < 0 || offset < 0 || (long)offset + size > data.Length)
        {
            throw new InvalidDataException("CFF Private DICT extends past the end of the font.");
        }

        var privateBytes = new byte[size];
        Array.Copy(data, offset, privateBytes, 0, size);
        var privateDict = CffDict.Parse(privateBytes);

        var defaultWidthX = privateDict.TryGetValue(20, out var dw) && dw.Length > 0 ? dw[0] : 0;
        var nominalWidthX = privateDict.TryGetValue(21, out var nw) && nw.Length > 0 ? nw[0] : 0;

        CffIndex? localSubrs = null;
        var localBias = 0;
        if (privateDict.TryGetValue(19, out var subrsOp) && subrsOp.Length > 0)
        {
            localSubrs = CffIndex.Read(data, offset + (int)subrsOp[0]);
            localBias = Bias(localSubrs.Count);
        }

        return new FdInfo(defaultWidthX, nominalWidthX, localSubrs, localBias, fontMatrix);
    }

    private static string GetString(int sid, CffIndex stringIndex)
    {
        if (sid < CffStandardStrings.Strings.Length)
        {
            return CffStandardStrings.Strings[sid];
        }

        return Ascii(stringIndex.GetBytes(sid - CffStandardStrings.Strings.Length));
    }

    private static string Ascii(byte[] bytes) => Encoding.ASCII.GetString(bytes);

    // Attacker-controlled offsets from the Top DICT, and attacker-controlled charstring
    // bytes, reach these raw readers; surface a diagnosable InvalidDataException (as the
    // rest of the parser does) instead of a bare IndexOutOfRangeException from deep inside
    // charset/FDSelect parsing or the charstring walk.
    private static byte ReadByteAt(byte[] data, int offset)
    {
        if (offset < 0 || offset >= data.Length)
        {
            throw new InvalidDataException("CFF read past the end of the font.");
        }

        return data[offset];
    }

    private static int ReadCard16(byte[] data, int offset)
    {
        if (offset < 0 || offset + 2 > data.Length)
        {
            throw new InvalidDataException("CFF read past the end of the font.");
        }

        return BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset));
    }

    private static int Bias(int subrCount) => subrCount < 1240 ? 107 : subrCount < 33900 ? 1131 : 32768;

    private readonly struct FdInfo(double defaultWidthX, double nominalWidthX, CffIndex? localSubrs, int localBias, double[]? fontMatrix)
    {
        public double DefaultWidthX => defaultWidthX;

        public double NominalWidthX => nominalWidthX;

        public CffIndex? LocalSubrs => localSubrs;

        public int LocalBias => localBias;

        public double[]? FontMatrix => fontMatrix;
    }

    // Walks a Type 2 charstring: operand decoding, stem counting, hintmask skipping and
    // subr dispatch. Subclasses supply only the answer they are after, via Visit.
    private abstract class CharstringContext(FdInfo fd, CffIndex globalSubrs, int globalBias, int maxOperations)
    {
        // Type 2 charstring spec section 3.1: the argument stack holds at most 48 entries.
        // This bounds stack depth only. It does not bound the walk: a subr that pops what it
        // pushes leaves the stack at 0 or 1 forever, so a font built from callsubr alone never
        // reaches 48 no matter how many operations it demands. MaxCharstringOperations bounds
        // that count; the two caps guard different quantities and neither implies the other.
        private const int MaxStackEntries = 48;

        protected readonly List<double> stack = [];
        private int hintCount;
        private bool done;

        // Walk-wide, deliberately not reset per Run: the whole point is to bound the product of
        // the nested calls, which a per-invocation counter would never see.
        private int operations;

        protected FdInfo Fd => fd;

        // Called with the operator's operands still on the stack. Returning true ends the walk.
        protected abstract bool Visit(int op);

        public void Run(byte[] cs) => Run(cs, 0);

        private void Run(byte[] cs, int depth)
        {
            if (depth > 10)
            {
                done = true;
                return;
            }

            var i = 0;
            while (i < cs.Length && !done)
            {
                if (++operations > maxOperations)
                {
                    done = true;
                    return;
                }

                int b = cs[i];
                if (b >= 32 || b == 28)
                {
                    i = ReadOperand(cs, i, b);
                    continue;
                }

                i++;
                switch (b)
                {
                    case 1:
                    case 3:
                    case 18:
                    case 23:
                    case 19:
                    case 20:
                        if (Visit(b))
                        {
                            return;
                        }

                        hintCount += stack.Count / 2;
                        stack.Clear();
                        if (b == 19 || b == 20)
                        {
                            i += (hintCount + 7) / 8;
                        }

                        break;
                    case 10:
                        RunSubr(Fd.LocalSubrs, Fd.LocalBias, depth);
                        break;
                    case 11:
                        return;
                    case 29:
                        RunSubr(globalSubrs, globalBias, depth);
                        break;
                    case 12:
                        var escape = ReadByteAt(cs, i);
                        i++;
                        if (!ApplyEscape(escape))
                        {
                            stack.Clear();
                        }

                        break;
                    default:
                        if (Visit(b))
                        {
                            return;
                        }

                        stack.Clear();
                        break;
                }
            }
        }

        protected void Stop() => done = true;

        // Overflow ends the walk rather than throwing: the charstring is malformed, and every
        // other malformed case here degrades to the default width instead of failing a render.
        private void Push(double value)
        {
            if (stack.Count >= MaxStackEntries)
            {
                done = true;
                return;
            }

            stack.Add(value);
        }

        // Type 2 arithmetic escapes leave a result on the stack, so a width operand sitting
        // under one survives to the moveto/endchar that resolves it. Returning false means
        // "not applied" and the caller clears, which is correct for the drawing escapes
        // (12 34 hflex, 12 35 flex, 12 36 hflex1, 12 37 flex1 - they consume their operands)
        // and a conservative fallback elsewhere: an unhandled escape loses the width to
        // defaultWidthX, which is what this walk did for every escape before.
        // Not implemented, as none appears in the width arithmetic this walk exists to read:
        // put(20) get(21) need transient-array state, random(23) has no deterministic answer,
        // and index(29) roll(30) and(3) or(4) not(5) eq(15) ifelse(22) are pure stack but
        // would be untested guesswork here. Add one only with a charstring that needs it.
        private bool ApplyEscape(int escape)
        {
            switch (escape)
            {
                case 9: // abs
                case 14: // neg
                case 26: // sqrt
                    if (stack.Count < 1 || (escape == 26 && stack[^1] < 0))
                    {
                        return false;
                    }

                    var v = stack[^1];
                    stack[^1] = escape switch { 9 => Math.Abs(v), 14 => -v, _ => Math.Sqrt(v) };
                    return true;
                case 10: // add
                case 11: // sub
                case 12: // div
                case 24: // mul
                    if (stack.Count < 2 || (escape == 12 && stack[^1] == 0))
                    {
                        return false;
                    }

                    var y = stack[^1];
                    var x = stack[^2];
                    stack.RemoveAt(stack.Count - 1);
                    stack[^1] = escape switch { 10 => x + y, 11 => x - y, 24 => x * y, _ => x / y };
                    return true;
                case 18: // drop
                    if (stack.Count < 1)
                    {
                        return false;
                    }

                    stack.RemoveAt(stack.Count - 1);
                    return true;
                case 27: // dup
                    if (stack.Count < 1)
                    {
                        return false;
                    }

                    Push(stack[^1]);
                    return true;
                case 28: // exch
                    if (stack.Count < 2)
                    {
                        return false;
                    }

                    (stack[^1], stack[^2]) = (stack[^2], stack[^1]);
                    return true;
                default:
                    return false;
            }
        }

        private void RunSubr(CffIndex? subrs, int bias, int depth)
        {
            if (subrs is null || stack.Count == 0)
            {
                stack.Clear();
                return;
            }

            var index = (int)stack[^1] + bias;
            stack.RemoveAt(stack.Count - 1);
            if (index < 0 || index >= subrs.Count)
            {
                done = true;
                return;
            }

            Run(subrs.GetBytes(index), depth + 1);
        }

        protected int ReadOperand(byte[] cs, int i, int b)
        {
            if (b == 28)
            {
                Push((short)((ReadByteAt(cs, i + 1) << 8) | ReadByteAt(cs, i + 2)));
                return i + 3;
            }

            if (b < 247)
            {
                Push(b - 139);
                return i + 1;
            }

            if (b < 251)
            {
                Push(((b - 247) * 256) + ReadByteAt(cs, i + 1) + 108);
                return i + 2;
            }

            if (b < 255)
            {
                Push((-(b - 251) * 256) - ReadByteAt(cs, i + 1) - 108);
                return i + 2;
            }

            Push((
                (ReadByteAt(cs, i + 1) << 24) | (ReadByteAt(cs, i + 2) << 16) |
                (ReadByteAt(cs, i + 3) << 8) | ReadByteAt(cs, i + 4)) / 65536.0);
            return i + 5;
        }
    }

    // Recovers the optional leading width operand: it sits before the first
    // stem/moveto/endchar operator when the operand count exceeds what that operator
    // consumes, so the first such operator settles the question either way.
    private sealed class WidthContext(FdInfo fd, CffIndex globalSubrs, int globalBias, int maxOperations)
        : CharstringContext(fd, globalSubrs, globalBias, maxOperations)
    {
        public double? Width { get; private set; }

        protected override bool Visit(int op) => op switch
        {
            21 => ResolveWidth(stack.Count > 2),
            4 or 22 => ResolveWidth(stack.Count > 1),
            1 or 3 or 18 or 23 or 19 or 20 or 14 => ResolveWidth(stack.Count % 2 == 1),
            _ => false,
        };

        private bool ResolveWidth(bool hasWidth)
        {
            Width = hasWidth ? Fd.NominalWidthX + stack[0] : Fd.DefaultWidthX;
            Stop();
            return true;
        }
    }

    // Decides whether the charstring terminates in an endchar seac (an endchar with 4 or 5
    // operands, the 5th form carrying a leading width).
    private sealed class SeacContext(FdInfo fd, CffIndex globalSubrs, int globalBias, int maxOperations)
        : CharstringContext(fd, globalSubrs, globalBias, maxOperations)
    {
        public bool Seac { get; private set; }

        protected override bool Visit(int op)
        {
            if (op != 14)
            {
                return false;
            }

            Seac = stack.Count >= 4;
            Stop();
            return true;
        }
    }
}
