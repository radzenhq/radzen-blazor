#nullable enable
using System.Text;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

using Radzen.Documents.Pdf;
namespace Radzen.Blazor.Pdf.Tests;

public class ToUnicodeHardeningTests
{
    [Fact]
    public void HugeIncrementalBfrange_ThrowsFast()
    {
        var cmap = Cmap("1 beginbfrange <0000> <7fffffff> <0041> endbfrange");
        Assert.Throws<DocumentParseException>(() => ToUnicodeCMap.Parse(cmap));
    }

    [Fact]
    public void HighSingletonIncrementalBfrange_StillMaps()
    {
        var (map, _) = ToUnicodeCMap.Parse(Cmap("1 beginbfrange <7ffffffe> <7ffffffe> <0041> endbfrange"));

        Assert.Equal("A", map[0x7ffffffe]);
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
        var cmap = Encoding.ASCII.GetBytes(
            "begincmap\n" +
            "2 begincodespacerange <00> <80> <8140> <FCFC> endcodespacerange\n" +
            "endcmap\n");

        Assert.Throws<DocumentParseException>(() => ToUnicodeCMap.Parse(cmap));
    }

    [Fact]
    public void HonorsTightenedMaxCMapEntries()
    {
        var cmap = Encoding.ASCII.GetBytes(
            "1 begincodespacerange <0000> <FFFF> endcodespacerange\n" +
            "1 beginbfrange <0003> <012C> <0041> endbfrange\n");
        var tight = new ReaderLimits { MaxCMapEntries = 100 };
        Assert.Throws<DocumentParseException>(() => ToUnicodeCMap.Parse(cmap, tight));

        var (map, _) = ToUnicodeCMap.Parse(cmap);
        Assert.Equal("A", map[0x0003]);
    }

    private static byte[] Cmap(string body) => Encoding.ASCII.GetBytes(
        "/CIDInit /ProcSet findresource begin 12 dict begin begincmap\n" +
        "1 begincodespacerange <0000> <FFFF> endcodespacerange\n" +
        body + "\nendcmap end end\n");
}
