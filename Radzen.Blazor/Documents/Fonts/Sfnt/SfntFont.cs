using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Radzen.Documents.Fonts.Sfnt;

internal sealed class SfntFont
{
    private const uint TtcTag = 0x74746366;

    internal const int MaxCollectionFaces = 256;

    private readonly byte[] data;
    private readonly TableDirectory directory;
    private readonly NameTable names;
    private readonly HorizontalMetrics metrics;
    private readonly Cmap? cmap;

    private SfntFont(byte[] data, int directoryOffset)
    {
        this.data = data;
        directory = new TableDirectory(data, directoryOffset);

        UnitsPerEm = ReadHead();
        GlyphCount = ReadMaxp();

        var horizontalHeader = ReadHhea();
        Ascent = horizontalHeader.Ascent;
        Descent = horizontalHeader.Descent;
        LineGap = horizontalHeader.LineGap;

        names = TryGetValidated("name", NameTableHeaderLength, out var name)
            ? NameTable.Parse(data, (int)name.Offset)
            : new NameTable();

        metrics = HorizontalMetrics.Parse(
            data,
            (int)RequireTable("hmtx", (uint)horizontalHeader.NumberOfHMetrics * 4).Offset,
            horizontalHeader.NumberOfHMetrics);

        cmap = TryGetValidated("cmap", CmapHeaderLength, out var cmapTable)
            ? Cmap.Parse(data, (int)cmapTable.Offset)
            : null;

        ItalicAngle = ReadPost();

        var os2 = ReadOs2();
        FsType = os2.FsType;
        Bold = os2.Bold;
        Italic = os2.Italic;
        CapHeight = os2.CapHeight;

        IsCff = HasTable("CFF ");
    }

    private const uint NameTableHeaderLength = 6;

    private const uint CmapHeaderLength = 4;

    private const uint PostItalicAngleLength = 8;

    private const uint Os2SelectionLength = 64;

    private const uint Os2CapHeightLength = 90;

    public ushort UnitsPerEm { get; }

    public ushort GlyphCount { get; }

    public string FamilyName => names.FamilyName;

    public string SubfamilyName => names.SubfamilyName;

    public string PostScriptName => names.PostScriptName;

    public short Ascent { get; }

    public short Descent { get; }

    public short LineGap { get; }

    public short CapHeight { get; }

    public double ItalicAngle { get; }

    public bool IsCff { get; }

    public bool Bold { get; }

    public bool Italic { get; }

    public ushort FsType { get; }

    public bool EmbeddingRestricted => (FsType & 0x0002) != 0;

    public void EnsureEmbeddable(bool allowRestricted)
    {
        if (EmbeddingRestricted && !allowRestricted)
        {
            throw new InvalidOperationException(
                $"The font '{PostScriptName}' has OS/2 fsType 0x{FsType:X4} (Restricted License Embedding) and must not be embedded. "
                + "Pass the embedding opt-in override to embed it anyway if you hold a license that permits it.");
        }
    }

    public bool IsVariable => HasTable("fvar");

    public bool IsColorFont => HasTable("COLR") || HasTable("sbix") || HasTable("SVG ");

    public void EnsureRenderable(bool allowDegraded)
    {
        if (allowDegraded)
        {
            return;
        }

        if (IsVariable)
        {
            throw new NotSupportedException(
                $"The font '{PostScriptName}' is a variable font; axis selection is not supported, so only its default instance "
                + "would be embedded. Set AllowDegradedFonts to embed the default instance anyway.");
        }

        if (IsColorFont)
        {
            throw new NotSupportedException(
                $"The font '{PostScriptName}' is a color font (COLR/sbix/SVG); color glyphs are not supported and would render as "
                + "monochrome outlines or missing. Set AllowDegradedFonts to embed it anyway.");
        }
    }

    public ushort GetGlyphId(int codepoint) => cmap?.GetGlyphId(codepoint) ?? 0;

    public ushort GetAdvanceWidth(ushort glyphId) => metrics.GetAdvanceWidth(glyphId);

    public double AdvanceInUserSpace(ushort glyphId, double size)
        => FontMetric.Scale(GetAdvanceWidth(glyphId), size, UnitsPerEm);

    public double KerningInUserSpace(ushort left, ushort right, double size)
        => FontMetric.Scale(GetKerning(left, right), size, UnitsPerEm);

    private Dictionary<int, int>? kerning;

    public int GetKerning(ushort left, ushort right)
    {
        var cache = Volatile.Read(ref kerning);
        if (cache is null)
        {
            var parsed = HasTable("kern") && TryGetTable("kern", out var table)
                ? KernTable.Parse(table)
                : [];
            cache = Interlocked.CompareExchange(ref kerning, parsed, null) ?? parsed;
        }

        return cache.TryGetValue(FontMetric.PairKey(left, right), out var value) ? value : 0;
    }

    public bool TryGetTable(string tag, out byte[] data)
    {
        if (TryGetValidatedRecord(tag, out var record))
        {
            var result = new byte[record.Length];
            Array.Copy(this.data, (int)record.Offset, result, 0, (int)record.Length);
            data = result;
            return true;
        }

        data = null!;
        return false;
    }

    public bool TryGetTableMemory(string tag, out ReadOnlyMemory<byte> table)
    {
        if (TryGetValidatedRecord(tag, out var record))
        {
            table = data.AsMemory((int)record.Offset, (int)record.Length);
            return true;
        }

        table = default;
        return false;
    }

    private bool TryGetValidatedRecord(string tag, out TableRecord record)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (!directory.TryGet(tag, out record))
        {
            return false;
        }

        ValidateTableExtent(record.Offset, record.Length, tag);
        return true;
    }

    private void ValidateTableExtent(uint offset, uint length, string tag)
    {
        if ((long)offset + length > data.Length)
        {
            throw new InvalidDataException($"Font table '{tag}' extends past the end of the font.");
        }
    }

    public static SfntFont Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        RequireHeader(data);
        if (IsCollection(data))
        {
            throw new InvalidDataException("Font is a TrueType collection; use ParseCollection or Parse(byte[], familyName).");
        }

        return new SfntFont(data, 0);
    }

    public static SfntFont Parse(byte[] data, string familyName)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(familyName);

        return SelectFace(ParseCollection(data), familyName, bold: false, italic: false);
    }

    public static SfntFont SelectFace(IReadOnlyList<SfntFont> faces, string family, bool bold, bool italic)
    {
        SfntFont? regular = null;
        SfntFont? named = null;
        foreach (var face in faces)
        {
            if (!string.Equals(face.FamilyName, family, StringComparison.Ordinal))
            {
                continue;
            }

            if (face.Bold == bold && face.Italic == italic)
            {
                return face;
            }

            if (!face.Bold && !face.Italic)
            {
                regular ??= face;
            }

            named ??= face;
        }

        return regular ?? named ?? throw new InvalidDataException($"No font face with family name '{family}' was found.");
    }

    public static IReadOnlyList<SfntFont> ParseCollection(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        RequireHeader(data);

        if (IsCollection(data))
        {
            var reader = new SfntReader(data, 4);
            reader.ReadUInt16();
            reader.ReadUInt16();
            var numFonts = reader.ReadUInt32();

            if (numFonts > MaxCollectionFaces)
            {
                throw new InvalidDataException(
                    $"TrueType collection face count {numFonts} exceeds the supported maximum of {MaxCollectionFaces}.");
            }

            if (12 + ((long)numFonts * 4) > data.Length)
            {
                throw new InvalidDataException("TrueType collection font count exceeds the header bounds.");
            }

            var faces = new List<SfntFont>((int)numFonts);
            for (var i = 0; i < numFonts; i++)
            {
                var faceOffset = reader.ReadUInt32();
                faces.Add(new SfntFont(data, (int)faceOffset));
            }

            return faces;
        }

        return [new SfntFont(data, 0)];
    }

    internal static bool IsCollection(ReadOnlySpan<byte> data)
        => SfntReader.TryReadTagValue(data, out var tag) && tag == TtcTag;

    private static void RequireHeader(ReadOnlySpan<byte> data)
    {
        if (!SfntReader.TryReadTagValue(data, out _))
        {
            throw new InvalidDataException("Font data is too short to contain a valid header.");
        }
    }

    private ushort ReadHead()
    {
        var head = RequireTable("head", 20);
        return new SfntReader(data).ReadUInt16At((int)head.Offset + 18);
    }

    private ushort ReadMaxp()
    {
        var maxp = RequireTable("maxp", 6);
        return new SfntReader(data).ReadUInt16At((int)maxp.Offset + 4);
    }

    private readonly record struct HorizontalHeader(short Ascent, short Descent, short LineGap, int NumberOfHMetrics);

    private HorizontalHeader ReadHhea()
    {
        var hhea = RequireTable("hhea", 36);
        var reader = new SfntReader(data, (int)hhea.Offset + 4);
        var ascent = reader.ReadInt16();
        var descent = reader.ReadInt16();
        var lineGap = reader.ReadInt16();
        return new HorizontalHeader(
            ascent, descent, lineGap, reader.ReadUInt16At((int)hhea.Offset + 34));
    }

    private void ValidateRequiredTable(TableRecord record, string tag, uint minimumLength)
    {
        ValidateTableExtent(record.Offset, record.Length, tag);
        if (record.Length < minimumLength)
        {
            throw new InvalidDataException($"Required font table '{tag}' is truncated.");
        }
    }

    private TableRecord RequireTable(string tag, uint minimumLength)
    {
        if (!directory.TryGet(tag, out var record))
        {
            throw new InvalidDataException($"Required '{tag}' table is missing.");
        }

        ValidateRequiredTable(record, tag, minimumLength);
        return record;
    }

    private double ReadPost()
        => TryGetValidated("post", PostItalicAngleLength, out var post)
            ? new SfntReader(data, (int)post.Offset + 4).ReadInt32() / 65536.0
            : 0;

    private readonly record struct StyleFlags(ushort FsType, bool Bold, bool Italic, short CapHeight);

    private StyleFlags ReadOs2()
    {
        if (!TryGetValidated("OS/2", Os2SelectionLength, out var os2))
        {
            var macStyle = new SfntReader(data).ReadUInt16At((int)RequireTable("head", 46).Offset + 44);
            return new StyleFlags(0, (macStyle & 0x01) != 0, (macStyle & 0x02) != 0, 0);
        }

        var reader = new SfntReader(data);
        var version = reader.ReadUInt16At((int)os2.Offset);
        var fsType = reader.ReadUInt16At((int)os2.Offset + 8);
        var fsSelection = reader.ReadUInt16At((int)os2.Offset + 62);
        var capHeight = (short)0;

        if (version >= 2)
        {
            ValidateRequiredTable(os2, "OS/2", Os2CapHeightLength);
            capHeight = reader.ReadInt16At((int)os2.Offset + 88);
        }

        return new StyleFlags(fsType, (fsSelection & 0x20) != 0, (fsSelection & 0x01) != 0, capHeight);
    }

    private bool HasTable(string tag) => TryGetValidated(tag, 0, out _);

    private bool TryGetValidated(string tag, uint minimumLength, out TableRecord record)
    {
        if (!directory.TryGet(tag, out record))
        {
            return false;
        }

        ValidateRequiredTable(record, tag, minimumLength);
        return true;
    }
}
