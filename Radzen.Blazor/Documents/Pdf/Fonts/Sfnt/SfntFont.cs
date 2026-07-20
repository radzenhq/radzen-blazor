using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Fonts.Sfnt;

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

        var numberOfHMetrics = ReadHhea();
        names = directory.TryGet("name", out var name)
            ? NameTable.Parse(data, (int)name.Offset)
            : new NameTable();

        metrics = directory.TryGet("hmtx", out var hmtx)
            ? HorizontalMetrics.Parse(data, (int)hmtx.Offset, numberOfHMetrics)
            : throw new InvalidDataException("Required 'hmtx' table is missing.");

        cmap = directory.TryGet("cmap", out var cmapTable)
            ? Cmap.Parse(data, (int)cmapTable.Offset)
            : null;

        ReadPost();
        ReadOs2();

        IsCff = directory.Contains("CFF ");
    }

    public ushort UnitsPerEm { get; }

    public ushort GlyphCount { get; }

    public string FamilyName => names.FamilyName;

    public string SubfamilyName => names.SubfamilyName;

    public string PostScriptName => names.PostScriptName;

    public short Ascent { get; private set; }

    public short Descent { get; private set; }

    public short LineGap { get; private set; }

    public short CapHeight { get; private set; }

    public double ItalicAngle { get; private set; }

    public bool IsCff { get; }

    public bool Bold { get; private set; }

    public bool Italic { get; private set; }

    public ushort FsType { get; private set; }

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

    public bool IsVariable => directory.Contains("fvar");

    public bool IsColorFont => directory.Contains("COLR") || directory.Contains("sbix") || directory.Contains("SVG ");

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
        kerning ??= directory.Contains("kern") && TryGetTable("kern", out var table)
            ? KernTable.Parse(table)
            : [];
        return kerning.TryGetValue(FontMetric.PairKey(left, right), out var value) ? value : 0;
    }

    public bool TryGetTable(string tag, out byte[] data)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (directory.TryGet(tag, out var record))
        {
            ValidateTableExtent(record.Offset, record.Length, tag);
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
        ArgumentNullException.ThrowIfNull(tag);
        if (directory.TryGet(tag, out var record))
        {
            ValidateTableExtent(record.Offset, record.Length, tag);
            table = data.AsMemory((int)record.Offset, (int)record.Length);
            return true;
        }

        table = default;
        return false;
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
        if (ReadTagValue(data) == TtcTag)
        {
            throw new InvalidDataException("Font is a TrueType collection; use ParseCollection or Parse(byte[], familyName).");
        }

        return new SfntFont(data, 0);
    }

    public static SfntFont Parse(byte[] data, string familyName)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(familyName);

        var faces = ParseCollection(data);
        foreach (var face in faces)
        {
            if (string.Equals(face.FamilyName, familyName, StringComparison.Ordinal))
            {
                return face;
            }
        }

        throw new InvalidDataException($"No font face with family name '{familyName}' was found.");
    }

    public static IReadOnlyList<SfntFont> ParseCollection(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (ReadTagValue(data) == TtcTag)
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

    private static uint ReadTagValue(byte[] data)
    {
        if (data.Length < 4)
        {
            throw new InvalidDataException("Font data is too short to contain a valid header.");
        }

        return ((uint)data[0] << 24) | ((uint)data[1] << 16) | ((uint)data[2] << 8) | data[3];
    }

    private ushort ReadHead()
    {
        if (!directory.TryGet("head", out var head))
        {
            throw new InvalidDataException("Required 'head' table is missing.");
        }

        ValidateRequiredTable(head, "head", 20);

        return new SfntReader(data).ReadUInt16At((int)head.Offset + 18);
    }

    private ushort ReadMaxp()
    {
        if (!directory.TryGet("maxp", out var maxp))
        {
            throw new InvalidDataException("Required 'maxp' table is missing.");
        }

        ValidateRequiredTable(maxp, "maxp", 6);

        return new SfntReader(data).ReadUInt16At((int)maxp.Offset + 4);
    }

    private ushort ReadHhea()
    {
        if (!directory.TryGet("hhea", out var hhea))
        {
            throw new InvalidDataException("Required 'hhea' table is missing.");
        }

        ValidateRequiredTable(hhea, "hhea", 36);

        var reader = new SfntReader(data, (int)hhea.Offset + 4);
        Ascent = reader.ReadInt16();
        Descent = reader.ReadInt16();
        LineGap = reader.ReadInt16();
        return reader.ReadUInt16At((int)hhea.Offset + 34);
    }

    private void ValidateRequiredTable(TableRecord record, string tag, uint minimumLength)
    {
        ValidateTableExtent(record.Offset, record.Length, tag);
        if (record.Length < minimumLength)
        {
            throw new InvalidDataException($"Required font table '{tag}' is truncated.");
        }
    }

    private void ReadPost()
    {
        if (!directory.TryGet("post", out var post))
        {
            return;
        }

        var reader = new SfntReader(data, (int)post.Offset + 4);
        ItalicAngle = reader.ReadInt32() / 65536.0;
    }

    private void ReadOs2()
    {
        if (!directory.TryGet("OS/2", out var os2))
        {
            if (directory.TryGet("head", out var head))
            {
                var macStyle = new SfntReader(data).ReadUInt16At((int)head.Offset + 44);
                Bold = (macStyle & 0x01) != 0;
                Italic = (macStyle & 0x02) != 0;
            }

            return;
        }

        var reader = new SfntReader(data);
        var version = reader.ReadUInt16At((int)os2.Offset);
        FsType = reader.ReadUInt16At((int)os2.Offset + 8);
        var fsSelection = reader.ReadUInt16At((int)os2.Offset + 62);
        Italic = (fsSelection & 0x01) != 0;
        Bold = (fsSelection & 0x20) != 0;

        if (version >= 2)
        {
            CapHeight = reader.ReadInt16At((int)os2.Offset + 88);
        }
    }
}
