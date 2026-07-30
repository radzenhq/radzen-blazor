#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class CffIndexOffsetValidationTests
{
    private static byte[] BuildIndex(long rawFirst, long rawMiddle, long rawLast)
    {
        var bytes = new byte[19];
        bytes[0] = 0x00;
        bytes[1] = 0x02;
        bytes[2] = 0x04;
        WriteOffset(bytes, 3, rawFirst);
        WriteOffset(bytes, 7, rawMiddle);
        WriteOffset(bytes, 11, rawLast);
        bytes[15] = 0x41;
        bytes[16] = 0x42;
        bytes[17] = 0x43;
        bytes[18] = 0x44;
        return bytes;
    }

    private static void WriteOffset(byte[] bytes, int pos, long value)
    {
        bytes[pos] = (byte)(value >> 24);
        bytes[pos + 1] = (byte)(value >> 16);
        bytes[pos + 2] = (byte)(value >> 8);
        bytes[pos + 3] = (byte)value;
    }

    [Fact]
    public void Read_WellFormedIndex_Parses()
    {
        var index = CffIndex.Read(BuildIndex(1, 3, 5), 0);

        Assert.Equal(2, index.Count);
        Assert.Equal(new byte[] { 0x41, 0x42 }, index.GetBytes(0));
        Assert.Equal(new byte[] { 0x43, 0x44 }, index.GetBytes(1));
        Assert.Equal(19, index.EndOffset);
    }

    [Fact]
    public void Read_IntermediateOffsetPastData_Throws()
    {
        var malformed = BuildIndex(1, 2_000_000_000, 5);

        var ex = Assert.Throws<InvalidDataException>(() => CffIndex.Read(malformed, 0));
        Assert.Contains("offset", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_NonMonotonicOffsets_Throws()
    {
        var malformed = BuildIndex(1, 5, 3);

        Assert.Throws<InvalidDataException>(() => CffIndex.Read(malformed, 0));
    }

    [Fact]
    public void Read_OffsetBelowOne_Throws()
    {
        var malformed = BuildIndex(0, 3, 5);

        Assert.Throws<InvalidDataException>(() => CffIndex.Read(malformed, 0));
    }

    [Fact]
    public void Read_OffsetOverflowingSignedInt_Throws()
    {
        var malformed = BuildIndex(1, 0xFFFFFFFF, 5);

        Assert.Throws<InvalidDataException>(() => CffIndex.Read(malformed, 0));
    }

    [Fact]
    public void Read_LastOffsetPastData_Throws()
    {
        var malformed = BuildIndex(1, 3, 6);

        Assert.Throws<InvalidDataException>(() => CffIndex.Read(malformed, 0));
    }

    [Fact]
    public void Read_RealFont_StillParsesEveryCharString()
    {
        var sfnt = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));
        Assert.True(sfnt.TryGetTable("CFF ", out var cffTable));

        var cff = CffFont.Parse(cffTable);

        Assert.Equal(658, cff.GlyphCount);
        var total = 0;
        for (var gid = 0; gid < cff.GlyphCount; gid++)
        {
            total += cff.GetCharStringBytes(gid).Length;
        }

        Assert.True(total > 0);
    }
}
