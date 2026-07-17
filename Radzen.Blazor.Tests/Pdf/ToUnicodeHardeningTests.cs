#nullable enable
using System.Text;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Hardening for the /ToUnicode CMap parser against an incremental bfrange whose
// span dwarfs the source codespace (e.g. <0000> <7fffffff>), which would
// otherwise materialize billions of dictionary entries and exhaust memory from a
// tiny stream. The oversized range is rejected quickly with DocumentParseException;
// ordinary bfrange and bfchar sections still map correctly.
public class ToUnicodeHardeningTests
{
    [Fact]
    public void HugeIncrementalBfrange_ThrowsFast()
    {
        var cmap = Cmap("1 beginbfrange <0000> <7fffffff> <0041> endbfrange");
        Assert.Throws<DocumentParseException>(() => ToUnicodeCMap.Parse(cmap));
    }

    [Fact]
    public void FullCodespaceIncrementalBfrange_ThrowsFast()
    {
        // A range spanning the whole 2-byte codespace via matching 4-byte codes still
        // exceeds MaxCMapEntries and must be rejected rather than filling 2.1B entries.
        var cmap = Cmap("1 beginbfrange <00000000> <7fffffff> <0041> endbfrange");
        Assert.Throws<DocumentParseException>(() => ToUnicodeCMap.Parse(cmap));
    }

    // A singleton bfrange ending at int.MaxValue declares span 1 and passes both the codespace
    // and total-entry caps, then an int counter wraps and the loop never terminates. The
    // endpoint must be rejected.
    [Fact]
    public void SingletonIncrementalBfrangeAtIntMaxValue_ThrowsFast()
    {
        var cmap = Cmap("1 beginbfrange <7fffffff> <7fffffff> <0041> endbfrange");
        Assert.Throws<DocumentParseException>(() => ToUnicodeCMap.Parse(cmap));
    }

    // A high but non-wrapping singleton bfrange still maps: the wrap guard rejects only
    // int.MaxValue, not any large source code.
    [Fact]
    public void HighSingletonIncrementalBfrange_StillMaps()
    {
        var (map, _) = ToUnicodeCMap.Parse(Cmap("1 beginbfrange <7ffffffe> <7ffffffe> <0041> endbfrange"));

        Assert.Equal("A", map[0x7ffffffe]);
    }

    [Fact]
    public void NormalBfrange_StillMapsCorrectly()
    {
        var (map, _) = ToUnicodeCMap.Parse(Cmap("1 beginbfrange <0003> <0005> <0041> endbfrange"));

        Assert.Equal("A", map[0x0003]);
        Assert.Equal("B", map[0x0004]);
        Assert.Equal("C", map[0x0005]);
    }

    [Fact]
    public void Bfchar_StillMapsCorrectly()
    {
        var (map, _) = ToUnicodeCMap.Parse(Cmap("2 beginbfchar <0003> <0041> <0009> <0062> endbfchar"));

        Assert.Equal("A", map[0x0003]);
        Assert.Equal("b", map[0x0009]);
    }

    [Fact]
    public void MixedWidthCodespaceRange_Throws()
    {
        // A CMap declaring both a 1-byte and a 2-byte codespace range cannot be decoded at
        // one fixed width; ReverseFont would garble every code of the other width, so reject.
        var cmap = Encoding.ASCII.GetBytes(
            "begincmap\n" +
            "2 begincodespacerange <00> <80> <8140> <FCFC> endcodespacerange\n" +
            "endcmap\n");

        Assert.Throws<DocumentParseException>(() => ToUnicodeCMap.Parse(cmap));
    }

    private static byte[] Cmap(string body) => Encoding.ASCII.GetBytes(
        "/CIDInit /ProcSet findresource begin 12 dict begin begincmap\n" +
        "1 begincodespacerange <0000> <FFFF> endcodespacerange\n" +
        body + "\nendcmap end end\n");
}
