#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ToUnicodeSurrogateRangeTests
{
    [Fact]
    public void SurrogatePairBase_IncrementsSupraBmpScalars()
    {
        var (map, _) = ToUnicodeCMap.Parse(Cmap("1 beginbfrange <0003> <0005> <D835DC00> endbfrange"));

        Assert.Equal("\U0001D400", map[0x0003]);
        Assert.Equal("\U0001D401", map[0x0004]);
        Assert.Equal("\U0001D402", map[0x0005]);
    }

    [Fact]
    public void BmpBase_IncrementsSingleCodeUnitAsBefore()
    {
        var (map, _) = ToUnicodeCMap.Parse(Cmap("1 beginbfrange <0003> <0005> <0041> endbfrange"));

        Assert.Equal("A", map[0x0003]);
        Assert.Equal("B", map[0x0004]);
        Assert.Equal("C", map[0x0005]);
    }

    [Fact]
    public void LoneSurrogateBase_DoesNotThrow_YieldsReplacement()
    {
        var (map, _) = ToUnicodeCMap.Parse(Cmap("1 beginbfrange <0003> <0003> <D800> endbfrange"));

        Assert.Equal("\uFFFD", map[0x0003]);
    }

    [Fact]
    public void ExtractText_RecoversSurrogatePairRange()
    {
        var codes = new byte[] { 0x00, 0x03, 0x00, 0x04, 0x00, 0x05 };
        var content = ExtractionSupport.TextRun("F1", 12, 72, 700, codes);
        var document = ExtractionSupport.BuildSinglePage(
            w => Type0Font(w, "1 beginbfrange <0003> <0005> <D835DC00> endbfrange"), content);

        Assert.Equal("\U0001D400\U0001D401\U0001D402", document.Pages[0].ExtractText());
    }

    [Fact]
    public void ExtractText_LoneSurrogateRange_LoadsWithoutThrow()
    {
        var codes = new byte[] { 0x00, 0x03 };
        var content = ExtractionSupport.TextRun("F1", 12, 72, 700, codes);
        var document = ExtractionSupport.BuildSinglePage(
            w => Type0Font(w, "1 beginbfrange <0003> <0003> <D800> endbfrange"), content);

        Assert.Equal("\uFFFD", document.Pages[0].ExtractText());
    }

    private static DictionaryObject Type0Font(DocumentWriter writer, string body) => new()
    {
        ["Type"] = new NameObject("Font"),
        ["Subtype"] = new NameObject("Type0"),
        ["BaseFont"] = new NameObject("AAAAAA+Test"),
        ["Encoding"] = new NameObject("Identity-H"),
        ["ToUnicode"] = writer.Add(new StreamObject(Cmap(body))),
    };

    private static byte[] Cmap(string body) => Encoding.ASCII.GetBytes(
        "/CIDInit /ProcSet findresource begin 12 dict begin begincmap\n" +
        "1 begincodespacerange <0000> <FFFF> endcodespacerange\n" +
        body + "\nendcmap end end\n");
}
