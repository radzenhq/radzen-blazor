#nullable enable
using Xunit;
using Radzen.Documents.Pdf.Fonts.Cff;

namespace Radzen.Blazor.Pdf.Tests;

// Unit tests for the low-level CFF INDEX reader on hand-built bytes. This isolates
// the trickiest CFF primitive (Card16 count, offSize, (count+1) 1-based offsets, then
// packed object data) independently of any real font. Layout per CFF spec section 5.
public class CffIndexTests
{
    // INDEX of {"AB","C",""}, offSize 1:
    //   count  = 00 03
    //   offSize= 01
    //   offsets= 01 03 04 04   (1-based, relative to the byte before the data block)
    //   data   = 41 42 43      ("ABC")
    private static readonly byte[] ThreeEntryIndex =
    {
        0x00, 0x03,
        0x01,
        0x01, 0x03, 0x04, 0x04,
        0x41, 0x42, 0x43,
    };

    [Fact]
    public void Read_Count()
    {
        var index = CffIndex.Read(ThreeEntryIndex, 0);

        Assert.Equal(3, index.Count);
    }

    [Fact]
    public void Read_EntryBytes()
    {
        var index = CffIndex.Read(ThreeEntryIndex, 0);

        Assert.Equal(new byte[] { 0x41, 0x42 }, index.GetBytes(0));
        Assert.Equal(new byte[] { 0x43 }, index.GetBytes(1));
        Assert.Equal(new byte[] { }, index.GetBytes(2));
    }

    [Fact]
    public void Read_EndOffsetIsPastIndex()
    {
        var index = CffIndex.Read(ThreeEntryIndex, 0);

        Assert.Equal(ThreeEntryIndex.Length, index.EndOffset);
    }

    [Fact]
    public void Read_HonorsStartOffset()
    {
        var prefixed = new byte[2 + ThreeEntryIndex.Length];
        prefixed[0] = 0xAA;
        prefixed[1] = 0xBB;
        ThreeEntryIndex.CopyTo(prefixed, 2);

        var index = CffIndex.Read(prefixed, 2);

        Assert.Equal(3, index.Count);
        Assert.Equal(new byte[] { 0x41, 0x42 }, index.GetBytes(0));
        Assert.Equal(prefixed.Length, index.EndOffset);
    }

    [Fact]
    public void Read_EmptyIndex()
    {
        // count = 0; an empty INDEX is just a Card16 count of 0 with no offSize/offsets/data.
        var empty = new byte[] { 0x00, 0x00 };

        var index = CffIndex.Read(empty, 0);

        Assert.Equal(0, index.Count);
        Assert.Equal(2, index.EndOffset);
    }
}
