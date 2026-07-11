#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Fonts.Sfnt;

// A single parsed sfnt (TrueType/OpenType) font face. Retains the whole source
// buffer so any table can be served raw via TryGetTable (GSUB/GPOS shaper seam).
internal sealed class SfntFont
{
    private const uint TtcTag = 0x74746366; // 'ttcf'

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
            : HorizontalMetrics.Parse(data, 0, 0);

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

    public ushort GetGlyphId(int codepoint) => cmap?.GetGlyphId(codepoint) ?? 0;

    public ushort GetAdvanceWidth(ushort glyphId) => metrics.GetAdvanceWidth(glyphId);

    public bool TryGetTable(string tag, out byte[] data)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (directory.TryGet(tag, out var record))
        {
            var result = new byte[record.Length];
            Array.Copy(this.data, (int)record.Offset, result, 0, (int)record.Length);
            data = result;
            return true;
        }

        data = null!;
        return false;
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
            reader.ReadUInt16(); // majorVersion
            reader.ReadUInt16(); // minorVersion
            var numFonts = reader.ReadUInt32();

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

        return new SfntReader(data).ReadUInt16At((int)head.Offset + 18);
    }

    private ushort ReadMaxp()
    {
        if (!directory.TryGet("maxp", out var maxp))
        {
            throw new InvalidDataException("Required 'maxp' table is missing.");
        }

        return new SfntReader(data).ReadUInt16At((int)maxp.Offset + 4);
    }

    private ushort ReadHhea()
    {
        if (!directory.TryGet("hhea", out var hhea))
        {
            throw new InvalidDataException("Required 'hhea' table is missing.");
        }

        var reader = new SfntReader(data, (int)hhea.Offset + 4);
        Ascent = reader.ReadInt16();
        Descent = reader.ReadInt16();
        LineGap = reader.ReadInt16();
        return reader.ReadUInt16At((int)hhea.Offset + 34);
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
        var fsSelection = reader.ReadUInt16At((int)os2.Offset + 62);
        Italic = (fsSelection & 0x01) != 0;
        Bold = (fsSelection & 0x20) != 0;

        if (version >= 2)
        {
            CapHeight = reader.ReadInt16At((int)os2.Offset + 88);
        }
    }
}
