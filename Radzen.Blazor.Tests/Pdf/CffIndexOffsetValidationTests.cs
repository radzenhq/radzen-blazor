#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// FontCollection.Register(family, Stream) is public, so the CFF bytes reaching CffIndex.Read
// are attacker-controlled. The INDEX offset array is (count+1) attacker-chosen integers that
// GetBytes turns directly into a `new byte[offsets[i+1] - offsets[i]]`, so every one of them
// has to be range-checked, not just the last.
public class CffIndexOffsetValidationTests
{
    // A count=2, offSize=4 INDEX whose two entries are carved out of a 4-byte data block.
    // Raw offsets are 1-based against dataBase = 3 + (count+1)*offSize - 1 = 14, so the
    // data block runs [15, 19) and a well-formed last offset is 5 (absolute 19 == length).
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

    // The separating input: the last offset is valid (so the pre-existing endOffset check
    // passes) while offsets[1] addresses ~2 GB past a 19-byte buffer. GetBytes(0) sizes its
    // allocation from that difference, so accepting the INDEX concedes the allocation.
    [Fact]
    public void Read_IntermediateOffsetPastData_Throws()
    {
        var malformed = BuildIndex(1, 2_000_000_000, 5);

        var ex = Assert.Throws<InvalidDataException>(() => CffIndex.Read(malformed, 0));
        Assert.Contains("offset", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // In range but descending: GetBytes would compute a negative length.
    [Fact]
    public void Read_NonMonotonicOffsets_Throws()
    {
        var malformed = BuildIndex(1, 5, 3);

        Assert.Throws<InvalidDataException>(() => CffIndex.Read(malformed, 0));
    }

    // Offsets are 1-based; 0 addresses the byte before the data block.
    [Fact]
    public void Read_OffsetBelowOne_Throws()
    {
        var malformed = BuildIndex(0, 3, 5);

        Assert.Throws<InvalidDataException>(() => CffIndex.Read(malformed, 0));
    }

    // A 4-byte offset spans the full uint range; accumulating it into an int makes
    // 0xFFFFFFFF read back as -1 and dataBase + (-1) land plausibly inside the buffer.
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

    // Positive control: the real CID-keyed CFF fixture must still parse, and every INDEX
    // entry the font exposes must still be byte-recoverable.
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
