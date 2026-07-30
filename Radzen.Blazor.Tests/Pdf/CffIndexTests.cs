#nullable enable
using Xunit;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// CFF spec section 5.
public class CffIndexTests
{
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
        var empty = new byte[] { 0x00, 0x00 };

        var index = CffIndex.Read(empty, 0);

        Assert.Equal(0, index.Count);
        Assert.Equal(2, index.EndOffset);
    }
}
