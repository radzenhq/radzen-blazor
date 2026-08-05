#nullable enable
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Documents.Core;
namespace Radzen.Blazor.Pdf.Tests;

public class BuildEndToEndTests
{
    [Fact]
    public void PageCount_MatchesPaginator()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var lh = PaginationSupport.LineHeight(12);

        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(PaginationSupport.HeightForLines(lh, 5)));
        section.Margins.SetAll(Unit.FromPoint(0));

        for (var i = 0; i < 13; i++)
        {
            BuildTestSupport.AddText(section, $"Line{i}", BuildTestSupport.Latin);
        }

        var expected = DocumentLayouter.Layout(document).Pages.Length;
        Assert.True(expected > 1, "content spans multiple pages");

        var built = new DocumentRenderer().Render(document);
        Assert.Equal(expected, built.Pages.Count);

        Assert.Equal(3, built.Pages.Count);
        Assert.Equal(200, built.Pages[0].Width.Point, 3);
    }

    [Fact]
    public void FirstAndLastPageBodyTextSurvivesRoundTrip()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var lh = PaginationSupport.LineHeight(12);

        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(PaginationSupport.HeightForLines(lh, 5)));
        section.Margins.SetAll(Unit.FromPoint(0));
        for (var i = 0; i < 13; i++)
        {
            BuildTestSupport.AddText(section, $"Line{i}", BuildTestSupport.Latin);
        }

        var text = BuildTestSupport.Reload(document).ExtractText();
        var first = text.IndexOf("Line0", StringComparison.Ordinal);
        var last = text.IndexOf("Line12", StringComparison.Ordinal);
        Assert.True(first >= 0, "first line present");
        Assert.True(last >= 0, "last line present");
        Assert.True(first < last, "lines preserved in flow order");
    }

    [Fact]
    public void ImageBlock_DecodesToScaledXObject()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var image = section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));
        image.Width = Unit.FromPoint(200);
        image.Height = Unit.FromPoint(100);

        var original = PdfTestResources.ReadAllBytes("Images/rgb.jpg");
        var emission = Emit(new DocumentRenderer().Render(document));

        var images = Regex.Matches(emission, @"\n(\d+) 0 obj\n(<<[^\n]*/Subtype /Image[^\n]*)\n");
        Assert.True(images.Count == 1, $"Expected 1 image XObject, found {images.Count}.");

        var dictionary = images[0].Groups[2].Value;
        Carries("image xobject", "/Width 64", dictionary);
        Carries("image xobject", "/Height 64", dictionary);
        Carries("image xobject", "/ColorSpace /DeviceRGB", dictionary);
        Carries("image xobject", "/BitsPerComponent 8", dictionary);
        Carries("image xobject", "/Filter /DCTDecode", dictionary);

        Carries(
            "image xobject",
            Encoding.Latin1.GetString(original),
            IndirectObject(emission, images[0].Groups[1].Value));
    }

    [Fact]
    public void ImageBlock_IsPaintedAtRequestedSize()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var image = section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));
        image.Width = Unit.FromPoint(200);
        image.Height = Unit.FromPoint(100);

        var reader = BuildTestSupport.Read(document);
        var leaves = BuildTestSupport.PageLeaves(reader);
        Assert.Single(leaves);

        var content = BuildTestSupport.Content(reader, leaves[0].Page);
        var placement = Assert.Single(
            ContentStreamTokenizer.Parse(content),
            operation => operation.Operator == "cm" && operation.Num(0) == 200);

        Assert.Equal(0, placement.Num(1));
        Assert.Equal(0, placement.Num(2));
        Assert.Equal(100, placement.Num(3));
        Assert.NotEmpty(ContentOperationTestHelpers.ResourceNames(content, "Do"));
    }

    [Fact]
    public void SaveToStream_MatchesBuildThenSave()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Convenience", BuildTestSupport.Latin);

        using var stream = new MemoryStream();
        new DocumentRenderer().SaveToStream(document, stream);
        var viaSave = stream.ToArray();

        using var expected = new MemoryStream();
        new DocumentRenderer().Render(document).SaveToStream(expected);

        Assert.NotEmpty(viaSave);
        Assert.Equal(expected.ToArray(), viaSave);

        using var buffer = new MemoryStream(viaSave);
        var reloaded = PortableDocument.LoadFromStream(buffer);
        Assert.Contains("Convenience", reloaded.ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Base14ZeroConfig_RoundTripsHelvetica()
    {
        const string text = "Helvetica Sample";
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, text, "Helvetica");

        var reloaded = BuildTestSupport.Reload(document);
        Assert.Equal(1, reloaded.Pages.Count);
        Assert.Contains(text, reloaded.ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Base14ZeroConfig_EmitsNamedType1Font()
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Helvetica Sample", "Helvetica");

        var emission = Emit(new DocumentRenderer().Render(document));
        Lacks("emission", "/Subtype /Type0", emission);

        var font = Shaped(
            "page /Font resource",
            @"/Font << /\w+ (<< /Type /Font [^>]*>>) >>",
            Line(emission, "/Type /Page "));

        Carries("font", "/Subtype /Type1", font.Groups[1].Value);
        Carries("font", "/BaseFont /Helvetica", font.Groups[1].Value);
    }
}
