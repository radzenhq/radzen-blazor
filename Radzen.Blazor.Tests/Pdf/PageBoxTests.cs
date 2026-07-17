#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class PageBoxTests
{
    private static Document WithBoxes()
    {
        var document = new Document();
        var page = document.Pages.Add(PageSizes.A4);
        page.SetContent(Encoding.ASCII.GetBytes("BT (b) Tj ET"));
        page.BleedBox = PdfRect.FromSize(10, 10, 575, 821);
        page.TrimBox = PdfRect.FromSize(20, 20, 555, 801);
        page.ArtBox = PdfRect.FromSize(30, 30, 535, 781);
        return document;
    }

    private static double[] Box(DocumentReader reader, DictionaryObject page, string key)
    {
        var array = Assert.IsType<ArrayObject>(reader.Resolve(page[key]));
        var result = new double[array.Count];
        for (var i = 0; i < array.Count; i++)
        {
            result[i] = Assert.IsType<NumberObject>(reader.Resolve(array[i])).DoubleValue;
        }

        return result;
    }

    [Fact]
    public void Setters_WriteBoxesToPageNode()
    {
        var reader = DocumentReader.Parse(WithBoxes().ToArray());
        var page = ContentTestHelpers.Kid(reader, 0);

        Assert.Equal(new[] { 10.0, 10, 585, 831 }, Box(reader, page, "BleedBox"));
        Assert.Equal(new[] { 20.0, 20, 575, 821 }, Box(reader, page, "TrimBox"));
        Assert.Equal(new[] { 30.0, 30, 565, 811 }, Box(reader, page, "ArtBox"));
    }

    [Fact]
    public void Boxes_PreservedAcrossLoadThenSave()
    {
        var save1 = WithBoxes().ToArray();

        using var stream = new MemoryStream(save1);
        var reloaded = Document.LoadFromStream(stream);
        var save2 = reloaded.ToArray();

        var reader = DocumentReader.Parse(save2);
        var page = ContentTestHelpers.Kid(reader, 0);
        Assert.Equal(new[] { 10.0, 10, 585, 831 }, Box(reader, page, "BleedBox"));
        Assert.Equal(new[] { 20.0, 20, 575, 821 }, Box(reader, page, "TrimBox"));
        Assert.Equal(new[] { 30.0, 30, 565, 811 }, Box(reader, page, "ArtBox"));
    }

    [Fact]
    public void UserBox_OverridesPreservedSourceBox()
    {
        using var stream = new MemoryStream(WithBoxes().ToArray());
        var reloaded = Document.LoadFromStream(stream);
        reloaded.Pages[0].TrimBox = PdfRect.FromSize(0, 0, 595, 842);
        var reader = DocumentReader.Parse(reloaded.ToArray());
        Assert.Equal(new[] { 0.0, 0, 595, 842 }, Box(reader, ContentTestHelpers.Kid(reader, 0), "TrimBox"));
    }

    [Fact]
    public void NoBoxes_EmitsNothing_AndByteIdentical()
    {
        Document Plain()
        {
            var document = new Document();
            document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (b) Tj ET"));
            return document;
        }

        var bytes = Plain().ToArray();
        Assert.Equal(bytes, Plain().ToArray());
        var text = Encoding.Latin1.GetString(bytes);
        Assert.DoesNotContain("/BleedBox", text);
        Assert.DoesNotContain("/TrimBox", text);
        Assert.DoesNotContain("/ArtBox", text);
    }
}
