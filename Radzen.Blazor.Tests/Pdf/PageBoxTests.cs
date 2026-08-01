#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class PageBoxTests
{
    private static PortableDocument WithBoxes()
    {
        var document = new PortableDocument();
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
        var reloaded = PortableDocument.LoadFromStream(stream);
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
        var reloaded = PortableDocument.LoadFromStream(stream);
        reloaded.Pages[0].TrimBox = PdfRect.FromSize(0, 0, 595, 842);
        var reader = DocumentReader.Parse(reloaded.ToArray());
        Assert.Equal(new[] { 0.0, 0, 595, 842 }, Box(reader, ContentTestHelpers.Kid(reader, 0), "TrimBox"));
    }

    [Fact]
    public void NoBoxes_EmitsNothing_AndByteIdentical()
    {
        PortableDocument Plain()
        {
            var document = new PortableDocument();
            document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (b) Tj ET"));
            return document;
        }

        var bytes = Plain().ToArray();
        Assert.Equal(bytes, Plain().ToArray());
        var page = ContentTestHelpers.Kid(DocumentReader.Parse(bytes), 0);
        Assert.False(page.ContainsKey("BleedBox"));
        Assert.False(page.ContainsKey("TrimBox"));
        Assert.False(page.ContainsKey("ArtBox"));
    }

    [Fact]
    public void NonFiniteAuxiliaryBox_IsRejectedAtAssignment()
    {
        var page = new PortableDocument().Pages.Add(PageSizes.A4);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => page.BleedBox = new PdfRect(0, 0, double.NaN, 100));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => page.TrimBox = new PdfRect(0, 0, 100, double.PositiveInfinity));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => page.ArtBox = new PdfRect(double.NegativeInfinity, 0, 100, 100));
    }
}
