#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class SfntParserTests
{
    private static SfntFont LiberationSansRegular()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));

    [Fact]
    public void LiberationSansRegular_HeadAndNames()
    {
        var font = LiberationSansRegular();

        Assert.Equal(2048, (int)font.UnitsPerEm);
        Assert.Equal(2620, (int)font.GlyphCount);
        Assert.Equal("Liberation Sans", font.FamilyName);
        Assert.Equal("Regular", font.SubfamilyName);
        Assert.Equal("LiberationSans", font.PostScriptName);
    }

    [Fact]
    public void LiberationSansRegular_Metrics()
    {
        var font = LiberationSansRegular();

        Assert.Equal(1854, (int)font.Ascent);
        Assert.Equal(-434, (int)font.Descent);
        Assert.Equal(67, (int)font.LineGap);
        Assert.Equal(1409, (int)font.CapHeight);
        Assert.Equal(0d, (double)font.ItalicAngle);
    }

    [Fact]
    public void LiberationSansRegular_IsTrueTypeNotCff()
    {
        Assert.False(LiberationSansRegular().IsCff);
    }

    [Fact]
    public void LiberationSansRegular_StyleFlagsFalse()
    {
        var font = LiberationSansRegular();

        Assert.False(font.Bold);
        Assert.False(font.Italic);
    }

    [Fact]
    public void LiberationSansBold_BoldFlagTrue()
    {
        var font = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf"));

        Assert.True(font.Bold);
        Assert.False(font.Italic);
        Assert.Equal("Bold", font.SubfamilyName);
    }

    [Fact]
    public void LiberationSansBoldItalic_BothFlagsTrue()
    {
        var font = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-BoldItalic.ttf"));

        Assert.True(font.Bold);
        Assert.True(font.Italic);
        Assert.Equal(-12d, (double)font.ItalicAngle);
    }

    [Fact]
    public void LiberationSerif_FamilyNameDiffers()
    {
        var font = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf"));

        Assert.Equal("Liberation Serif", font.FamilyName);
        Assert.False(font.Bold);
        Assert.False(font.Italic);
    }

    [Theory]
    [InlineData('A', 36)]
    [InlineData('z', 93)]
    [InlineData('0', 19)]
    [InlineData(' ', 3)]
    [InlineData('W', 58)]
    [InlineData(0x0411, 962)]
    [InlineData(0x044F, 1024)]
    public void LiberationSansRegular_CmapFormat4_GlyphIds(int codepoint, int expectedGid)
    {
        Assert.Equal(expectedGid, (int)LiberationSansRegular().GetGlyphId(codepoint));
    }

    [Fact]
    public void LiberationSansRegular_UnmappedCodepointReturnsNotdef()
    {
        Assert.Equal(0, (int)LiberationSansRegular().GetGlyphId(0x2603));
    }

    [Theory]
    [InlineData(36, 1366)]
    [InlineData(3, 569)]
    [InlineData(58, 1933)]
    public void LiberationSansRegular_AdvanceWidths(int gid, int expected)
    {
        Assert.Equal(expected, (int)LiberationSansRegular().GetAdvanceWidth((ushort)gid));
    }

    private static SfntFont NotoSubset()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));

    [Fact]
    public void NotoSubset_IsCffOpenType()
    {
        var font = NotoSubset();

        Assert.True(font.IsCff);
        Assert.Equal(1000, (int)font.UnitsPerEm);
        Assert.Equal(658, (int)font.GlyphCount);
        Assert.Equal("Noto Sans SC", font.FamilyName);
    }

    [Theory]
    [InlineData(0x53D1, 401)]
    [InlineData(0x7968, 418)]
    [InlineData('A', 34)]
    [InlineData(0x0431, 223)]
    [InlineData(' ', 1)]
    public void NotoSubset_GlyphIds(int codepoint, int expectedGid)
    {
        Assert.Equal(expectedGid, (int)NotoSubset().GetGlyphId(codepoint));
    }

    [Theory]
    [InlineData(401, 1000)]
    [InlineData(418, 1000)]
    [InlineData(34, 608)]
    [InlineData(223, 608)]
    [InlineData(1, 224)]
    public void NotoSubset_AdvanceWidths(int gid, int expected)
    {
        Assert.Equal(expected, (int)NotoSubset().GetAdvanceWidth((ushort)gid));
    }

    [Fact]
    public void NotoSubset_GlyphBeyondNumberOfHMetricsReusesLastAdvance()
    {
        Assert.Equal(1000, (int)NotoSubset().GetAdvanceWidth(657));
    }

    [Theory]
    [InlineData("GSUB", 5910)]
    [InlineData("GPOS", 14804)]
    [InlineData("BASE", 210)]
    public void NotoSubset_TryGetTable_ReturnsRawBytesMatchingDirectoryLength(string tag, int length)
    {
        Assert.True(NotoSubset().TryGetTable(tag, out var data));
        Assert.Equal(length, data.Length);
    }

    [Fact]
    public void NotoSubset_TryGetTable_MissingTableReturnsFalse()
    {
        Assert.False(NotoSubset().TryGetTable("GDEF", out var data));
        Assert.Null(data);
    }

    [Fact]
    public void Parse_TruncatedFile_Throws()
    {
        var full = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        var truncated = new byte[4];
        Array.Copy(full, truncated, 4);

        Assert.ThrowsAny<Exception>(() => SfntFont.Parse(truncated));
    }

    [Fact]
    public void Parse_BadMagic_Throws()
    {
        Assert.ThrowsAny<Exception>(() => SfntFont.Parse(new byte[16]));
    }

    [Theory]
    [InlineData("head", 19u)]
    [InlineData("maxp", 5u)]
    [InlineData("hhea", 35u)]
    public void Parse_RequiredTableDeclaredLengthCannotBeTruncated(string tag, uint length)
    {
        var bytes = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        var count = (bytes[4] << 8) | bytes[5];
        for (var i = 0; i < count; i++)
        {
            var record = 12 + i * 16;
            if (Encoding.ASCII.GetString(bytes, record, 4) != tag)
            {
                continue;
            }

            bytes[record + 12] = (byte)(length >> 24);
            bytes[record + 13] = (byte)(length >> 16);
            bytes[record + 14] = (byte)(length >> 8);
            bytes[record + 15] = (byte)length;
            break;
        }

        Assert.Throws<InvalidDataException>(() => SfntFont.Parse(bytes));
    }

    [Fact]
    public void TableRecordPastEnd_Throws()
    {
        var font = SfntFont.Parse(MinimalSfnt(bogusTableLength: 0xFFFF_FFF0u));
        Assert.Throws<InvalidDataException>(() => font.TryGetTable("TEST", out _));
    }

    [Fact]
    public void InBoundsTable_StillReturned()
    {
        var font = SfntFont.Parse(MinimalSfnt(bogusTableLength: 4));
        Assert.True(font.TryGetTable("TEST", out var table));
        Assert.Equal(4, table.Length);
    }

    [Fact]
    public void WithoutHorizontalMetrics_Throws()
    {
        var error = Assert.Throws<InvalidDataException>(
            () => SfntFont.Parse(MinimalSfnt(bogusTableLength: 4, numberOfHMetrics: 0)));

        Assert.Contains("no horizontal metrics", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TableRecordPastEnd_MemoryVariant_Throws()
    {
        var font = SfntFont.Parse(MinimalSfnt(bogusTableLength: 0xFFFF_FFF0u));
        Assert.Throws<InvalidDataException>(() => font.TryGetTableMemory("TEST", out _));
    }

    private static byte[] MinimalSfnt(uint bogusTableLength, int numberOfHMetrics = 1)
    {
        const int numTables = 5;
        var dirLen = 12 + (numTables * 16);
        var head = new byte[54];
        var maxp = new byte[6];
        var hhea = new byte[36];
        hhea[34] = (byte)(numberOfHMetrics >> 8);
        hhea[35] = (byte)numberOfHMetrics;
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
            U32(0);
            U32(offset);
            U32(length);
        }

        U32(0x00010000);
        U16(numTables);
        U16(0); U16(0); U16(0);
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
}
