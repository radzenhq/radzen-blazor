#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// Untrusted-parse allocation hardening (fx-hardening lane): every attacker-controlled
// 32-bit length/count is validated against the remaining bytes and a ReaderLimits cap
// BEFORE allocating, so malformed fonts/images/streams raise a recoverable typed
// exception rather than OOM, IndexOutOfRangeException, or silent wrong output. Each
// guard is paired with a positive control proving valid input is unaffected, and the
// threading fixes are pinned by proving a caller-tightened ReaderLimits is honored.
public class FxHardeningTests
{
    // --- Cmap format 12 numGroups drives three uint[] before any bounds check ----

    [Fact]
    public void CmapFormat12_OversizedNumGroups_Throws()
    {
        Assert.Throws<InvalidDataException>(() => Cmap.Parse(Format12Cmap(numGroups: 0x4000_0000)));
    }

    [Fact]
    public void CmapFormat12_ValidGroups_StillParse()
    {
        var mapper = Cmap.Parse(Format12Cmap(numGroups: 2));
        Assert.Equal(5, (int)mapper.GetGlyphId(0x1F600));
        Assert.Equal(6, (int)mapper.GetGlyphId(0x1F601));
    }

    // A whole cmap table with one format 12 subtable; numGroups is written verbatim so a
    // hostile value can be injected while the buffer stays tiny.
    private static byte[] Format12Cmap(long numGroups)
    {
        var bytes = new List<byte>();
        void U16(int v) { bytes.Add((byte)(v >> 8)); bytes.Add((byte)v); }
        void U32(long v)
        {
            bytes.Add((byte)(v >> 24));
            bytes.Add((byte)(v >> 16));
            bytes.Add((byte)(v >> 8));
            bytes.Add((byte)v);
        }

        U16(0);   // version
        U16(1);   // numTables
        U16(3);   // platformID
        U16(10);  // encodingID
        U32(12);  // subtable offset

        U16(12);  // format
        U16(0);   // reserved
        U32(16 + 2 * 12); // length (only the two real groups written below)
        U32(0);   // language
        U32(numGroups);
        U32(0x1F600); U32(0x1F600); U32(5);
        U32(0x1F601); U32(0x1F603); U32(6);
        return bytes.ToArray();
    }

    // --- CFF Private DICT size sizes a byte[] and copies before any bounds check ----

    [Fact]
    public void CffPrivateDictSizePastEnd_Throws()
    {
        var data = CffFixtureBuilder.Build([0x8B, 20], ThreeCharStrings(), (cs, priv) =>
        {
            var dict = new List<byte>();
            CffFixtureBuilder.Int5(dict, cs);
            dict.Add(17);
            CffFixtureBuilder.Int5(dict, 0x7FFF_FFFF); // Private DICT size runs off the font
            CffFixtureBuilder.Int5(dict, priv);
            dict.Add(18);
            return [.. dict];
        });

        Assert.Throws<InvalidDataException>(() => CffFont.Parse(data));
    }

    [Fact]
    public void CffValidPrivateDict_StillParses()
    {
        var font = CffFont.Parse(CffFixtureBuilder.Build([0x8B, 20], ThreeCharStrings()));
        Assert.Equal(3, font.GlyphCount);
    }

    private static byte[][] ThreeCharStrings() => [[0x0E], [0x0E], [239, 0x0E]];

    // --- SfntFont.TryGetTable sizes/copies a table record before any bounds check ----

    [Fact]
    public void SfntTableRecordPastEnd_Throws()
    {
        var font = SfntFont.Parse(MinimalSfnt(bogusTableLength: 0xFFFF_FFF0u));
        Assert.Throws<InvalidDataException>(() => font.TryGetTable("TEST", out _));
    }

    [Fact]
    public void SfntInBoundsTable_StillReturned()
    {
        var font = SfntFont.Parse(MinimalSfnt(bogusTableLength: 4));
        Assert.True(font.TryGetTable("TEST", out var table));
        Assert.Equal(4, table.Length);
    }

    [Fact]
    public void SfntTableRecordPastEnd_MemoryVariant_Throws()
    {
        // The zero-copy accessor must fail with the same diagnosable exception as its
        // byte[] twin, not an opaque ArgumentOutOfRangeException from AsMemory.
        var font = SfntFont.Parse(MinimalSfnt(bogusTableLength: 0xFFFF_FFF0u));
        Assert.Throws<InvalidDataException>(() => font.TryGetTableMemory("TEST", out _));
    }

    [Theory]
    [InlineData(new byte[] { 29 })]             // bare 32-bit integer prefix, no payload
    [InlineData(new byte[] { 28 })]             // bare 16-bit integer prefix, no payload
    [InlineData(new byte[] { 12 })]             // escape operator with no second byte
    [InlineData(new byte[] { 0xF7 })]           // 2-byte positive integer, second byte missing
    public void CffDictTruncatedOperand_ThrowsInvalidData(byte[] dict)
    {
        Assert.Throws<InvalidDataException>(() => CffDict.Parse(dict));
    }

    // Minimal TrueType sfnt carrying the required head/maxp/hhea tables plus one "TEST"
    // table whose record length is caller-supplied so an out-of-range record can be forged.
    private static byte[] MinimalSfnt(uint bogusTableLength)
    {
        const int numTables = 5;
        var dirLen = 12 + (numTables * 16);
        var head = new byte[54];
        var maxp = new byte[6];
        var hhea = new byte[36];
        var hmtx = new byte[4];
        var test = new byte[] { 1, 2, 3, 4 };

        var headOffset = dirLen;
        var maxpOffset = headOffset + head.Length;
        var hheaOffset = maxpOffset + maxp.Length;
        var hmtxOffset = hheaOffset + hhea.Length;
        var testOffset = hmtxOffset + hmtx.Length;

        var buffer = new List<byte>();
        void U16(int v) { buffer.Add((byte)(v >> 8)); buffer.Add((byte)v); }
        void U32(long v)
        {
            buffer.Add((byte)(v >> 24));
            buffer.Add((byte)(v >> 16));
            buffer.Add((byte)(v >> 8));
            buffer.Add((byte)v);
        }

        void Record(string tag, long offset, long length)
        {
            buffer.AddRange(Encoding.ASCII.GetBytes(tag));
            U32(0); // checksum (ignored)
            U32(offset);
            U32(length);
        }

        U32(0x00010000); // sfnt version
        U16(numTables);
        U16(0); U16(0); U16(0); // searchRange/entrySelector/rangeShift
        Record("head", headOffset, head.Length);
        Record("maxp", maxpOffset, maxp.Length);
        Record("hhea", hheaOffset, hhea.Length);
        Record("hmtx", hmtxOffset, hmtx.Length);
        Record("TEST", testOffset, bogusTableLength);

        buffer.AddRange(head);
        buffer.AddRange(maxp);
        buffer.AddRange(hhea);
        buffer.AddRange(hmtx);
        buffer.AddRange(test);
        return buffer.ToArray();
    }

    // --- ImageDecoder: truncated PNG IDAT, and threaded MaxImagePixels ----

    [Fact]
    public void PngTruncatedIdat_Throws()
    {
        using var ms = new MemoryStream();
        ms.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        WriteChunk(ms, "IHDR", Ihdr(4, 4, 8, 6));       // 4x4 RGBA -> needs 4 scanlines
        WriteChunk(ms, "IDAT", FlateFilter.Encode(new byte[17])); // only one scanline
        WriteChunk(ms, "IEND", []);

        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(ms.ToArray()));
    }

    [Fact]
    public void ImageDecoder_HonorsTightenedMaxImagePixels()
    {
        var png = PdfTestResources.ReadAllBytes("Images/rgb.png"); // 64x64 = 4096 pixels
        var tight = new ReaderLimits { MaxImagePixels = 100 };
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(png, tight));
        Assert.NotNull(ImageDecoder.Decode(png)); // default limits accept it
    }

    private static byte[] Ihdr(int width, int height, byte bitDepth, byte colorType)
    {
        var body = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(0), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(4), (uint)height);
        body[8] = bitDepth;
        body[9] = colorType;
        return body;
    }

    private static void WriteChunk(Stream stream, string type, byte[] body)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)body.Length);
        stream.Write(length);
        stream.Write(Encoding.ASCII.GetBytes(type));
        stream.Write(body);
        stream.Write(stackalloc byte[4]); // CRC placeholder
    }

    // --- ASCII85 5-tuple overflows uint ----

    [Fact]
    public void Ascii85_TupleOverflows32Bits_Throws()
    {
        // Five 'u' (0x75) encode 84*(85^4+...+1) = 0x1_08C5_9DC4 > 0xFFFFFFFF.
        var data = Encoding.ASCII.GetBytes("uuuuu~>");
        Assert.Throws<InvalidDataException>(() => Ascii85Filter.Decode(data));
    }

    [Fact]
    public void Ascii85_MaxValidTuple_StillDecodes()
    {
        // "s8W-!" is exactly 0xFFFFFFFF.
        var decoded = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("s8W-!~>"));
        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, decoded);
    }

    // --- LZW accepts a code beyond the next dictionary slot (KwKwK abuse) ----

    [Fact]
    public void Lzw_CodeBeyondNextSlot_Throws()
    {
        // clear, 'A', then code 300 while the next slot is only 258.
        var bomb = PackLzw([256, 65, 300]);
        Assert.Throws<DocumentParseException>(() => LzwFilter.Decode(bomb, 1, 1 << 20));
    }

    [Fact]
    public void Lzw_ValidKwKwK_StillDecodes()
    {
        // clear, 'A'(65), then code 258 == nextCode is the legal KwKwK ("AA") -> "AAA".
        var stream = PackLzw([256, 65, 258]);
        Assert.Equal(new byte[] { 65, 65, 65 }, LzwFilter.Decode(stream, 1, 1 << 20));
    }

    private static byte[] PackLzw(List<int> codes)
    {
        var bits = new List<bool>();
        var width = 9;
        var nextCode = 258;
        foreach (var code in codes)
        {
            for (var i = width - 1; i >= 0; i--)
            {
                bits.Add(((code >> i) & 1) != 0);
            }

            if (code == 256)
            {
                width = 9;
                nextCode = 258;
            }
            else if (code != 257)
            {
                nextCode++;
                if (nextCode + 1 == (1 << width) && width < 12)
                {
                    width++;
                }
            }
        }

        var bytes = new byte[(bits.Count + 7) / 8];
        for (var i = 0; i < bits.Count; i++)
        {
            if (bits[i])
            {
                bytes[i >> 3] |= (byte)(1 << (7 - (i & 7)));
            }
        }

        return bytes;
    }

    // --- ObjStm member offset -> raw IndexOutOfRangeException on post-Load GetObject ----

    [Fact]
    public void ObjStmNegativeMemberOffset_ThrowsDocumentParseException()
    {
        var reader = DocumentReader.Parse(ObjStmWithMemberOffset(-100000));
        var exception = Record.Exception(() => reader.GetObject(1));
        Assert.IsType<DocumentParseException>(exception);
    }

    private static byte[] ObjStmWithMemberOffset(int memberOffset)
    {
        var header = $"1 {memberOffset} ";
        var body = "42";
        var stmData = header + body;
        var first = header.Length;

        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        var offset4 = pdf.Position;
        pdf.Append($"4 0 obj\n<< /Type /ObjStm /N 1 /First {first} /Length {stmData.Length} >>\nstream\n")
            .Append(stmData)
            .Append("\nendstream\nendobj\n");

        var offset5 = pdf.Position;
        var payload = new byte[12];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(2, 4, 0));            // obj 1: compressed, ObjStm 4 index 0
        Copy(payload, 4, FixturePdf.XrefStreamEntry(1, (int)offset4, 0)); // obj 4
        Copy(payload, 8, FixturePdf.XrefStreamEntry(1, (int)offset5, 0)); // obj 5

        pdf.Append("5 0 obj\n<< /Type /XRef /Size 6 /Index [1 1 4 2] /W [1 2 1] /Root 4 0 R /Length 12 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offset5 + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static void Copy(byte[] target, int at, byte[] source)
    {
        for (var i = 0; i < source.Length; i++)
        {
            target[at + i] = source[i];
        }
    }

    // --- Classic xref table has no entry cap unlike the xref-stream path ----
    // The cap applies both to ParseClassicXref and the repair scan, so recovery
    // cannot bypass a caller-tightened budget. Defaults still use the xref directly.

    [Fact]
    public void ClassicXref_TightenedMaxXrefEntries_Throws()
    {
        var file = ClassicXrefFile();
        var tight = new ReaderLimits { MaxXrefEntries = 2 }; // below the 4 declared entries

        Assert.Throws<DocumentParseException>(() => DocumentReader.Parse(file, null, tight));
    }

    [Fact]
    public void ClassicXref_DefaultLimits_ParseViaXref()
    {
        Assert.Equal(3, DocumentReader.Parse(ClassicXrefFile()).ObjectCount);
    }

    private static byte[] ClassicXrefFile()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n(a string)\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 4\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append("trailer\n<< /Size 4 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    // --- ObjectParser uses the caller's ReaderLimits, not the default ----

    [Fact]
    public void ObjectParser_HonorsTightenedNestingDepth()
    {
        var bytes = Encoding.Latin1.GetBytes("[[[1]]]");
        var tight = new ReaderLimits { MaxObjectNestingDepth = 2 };
        Assert.Throws<DocumentParseException>(() => ObjectParser.Parse(bytes, 0, tight));
        Assert.NotNull(ObjectParser.Parse(bytes, 0)); // default depth accepts it
    }

    // --- ToUnicodeCMap uses the caller's ReaderLimits, not the default ----

    [Fact]
    public void ToUnicode_HonorsTightenedMaxCMapEntries()
    {
        var cmap = Encoding.ASCII.GetBytes(
            "1 begincodespacerange <0000> <FFFF> endcodespacerange\n" +
            "1 beginbfrange <0003> <012C> <0041> endbfrange\n"); // 298-entry incremental range
        var tight = new ReaderLimits { MaxCMapEntries = 100 };
        Assert.Throws<DocumentParseException>(() => ToUnicodeCMap.Parse(cmap, tight));

        var (map, _) = ToUnicodeCMap.Parse(cmap); // default cap accepts it
        Assert.Equal("A", map[0x0003]);
    }
}
