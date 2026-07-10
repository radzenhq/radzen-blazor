#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// L5 test 2 + 5 + 6: Build() drives the layout engine to a real physical Document.
// The page count matches the Paginator over the same builder; an Image block decodes
// to an XObject scaled to its box; and a base-14 paragraph round trips with no
// registered fonts (zero configuration).
public class BuildEndToEndTests
{
    [Fact]
    public void PageCount_MatchesPaginator()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var lh = PaginationSupport.LineHeight(builder.Fonts, 12);

        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(PaginationSupport.HeightForLines(lh, 5)));
        section.Margin = Unit.FromPoint(0);

        for (var i = 0; i < 13; i++)
        {
            BuildTestSupport.AddText(section, $"Line{i}", BuildTestSupport.Latin);
        }

        var expected = Paginator.Paginate(builder, builder.Fonts).Count;
        Assert.True(expected > 1, "content spans multiple pages");

        var built = builder.Build();
        Assert.Equal(expected, built.Pages.Count);

        // 13 single lines, 5 per page -> 3 pages, and each page carries its own dimensions.
        Assert.Equal(3, built.Pages.Count);
        Assert.Equal(200, built.Pages[0].Width.Point, 3);
    }

    [Fact]
    public void FirstAndLastPageBodyTextSurvivesRoundTrip()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var lh = PaginationSupport.LineHeight(builder.Fonts, 12);

        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(PaginationSupport.HeightForLines(lh, 5)));
        section.Margin = Unit.FromPoint(0);
        for (var i = 0; i < 13; i++)
        {
            BuildTestSupport.AddText(section, $"Line{i}", BuildTestSupport.Latin);
        }

        var text = BuildTestSupport.Reload(builder).ExtractText();
        var first = text.IndexOf("Line0", System.StringComparison.Ordinal);
        var last = text.IndexOf("Line12", System.StringComparison.Ordinal);
        Assert.True(first >= 0, "first line present");
        Assert.True(last >= 0, "last line present");
        Assert.True(first < last, "lines preserved in flow order");
    }

    [Fact]
    public void ImageBlock_DecodesToScaledXObject()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(200);
        image.Height = Unit.FromPoint(100);

        var original = PdfTestResources.ReadAllBytes("Images/rgb.jpg");
        var reader = BuildTestSupport.Read(builder);

        var images = BuildTestSupport.ImageXObjects(reader);
        Assert.Single(images);

        var dict = images[0].Dictionary;
        Assert.Equal("Image", BuildTestSupport.Name(reader, dict, "Subtype"));
        Assert.Equal(64, BuildTestSupport.Int(dict, "Width"));
        Assert.Equal(64, BuildTestSupport.Int(dict, "Height"));
        Assert.Equal("DeviceRGB", BuildTestSupport.Name(reader, dict, "ColorSpace"));
        Assert.Equal(8, BuildTestSupport.Int(dict, "BitsPerComponent"));
        Assert.Equal("DCTDecode", BuildTestSupport.Name(reader, dict, "Filter"));

        // JPEG passthrough: the stored bytes are the untouched DCT data.
        Assert.Equal(original, images[0].Data);
    }

    [Fact]
    public void ImageBlock_IsPaintedAtRequestedSize()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(200);
        image.Height = Unit.FromPoint(100);

        var reader = BuildTestSupport.Read(builder);
        var leaves = BuildTestSupport.PageLeaves(reader);
        Assert.Single(leaves);

        var content = Encoding.Latin1.GetString(BuildTestSupport.Content(reader, leaves[0].Page));

        // Conventional image placement: `w 0 0 h x y cm ... Do`, the CTM scaling the
        // unit image square to the requested 200x100 point box.
        Assert.Matches(
            @"200(?:\.0+)?\s+0(?:\.0+)?\s+0(?:\.0+)?\s+100(?:\.0+)?\s+[-\d.]+\s+[-\d.]+\s+cm",
            content);
        Assert.Matches(@"/\w+\s+Do", content);
    }

    [Fact]
    public void SaveToStream_MatchesBuildThenSave()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Convenience", BuildTestSupport.Latin);

        using var stream = new System.IO.MemoryStream();
        builder.SaveToStream(stream);
        var viaSave = stream.ToArray();

        using var expected = new System.IO.MemoryStream();
        builder.Build().SaveToStream(expected);

        Assert.NotEmpty(viaSave);
        Assert.Equal(expected.ToArray().Length, viaSave.Length);

        using var buffer = new System.IO.MemoryStream(viaSave);
        var reloaded = Document.LoadFromStream(buffer);
        Assert.Contains("Convenience", reloaded.ExtractText(), System.StringComparison.Ordinal);
    }

    [Fact]
    public void Base14ZeroConfig_RoundTripsHelvetica()
    {
        const string text = "Helvetica Sample";
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, text, "Helvetica");

        var reloaded = BuildTestSupport.Reload(builder);
        Assert.Equal(1, reloaded.Pages.Count);
        Assert.Contains(text, reloaded.ExtractText(), System.StringComparison.Ordinal);
    }

    [Fact]
    public void Base14ZeroConfig_EmitsNamedType1Font()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Helvetica Sample", "Helvetica");

        var reader = BuildTestSupport.Read(builder);
        Assert.Empty(BuildTestSupport.Type0Fonts(reader));

        var fonts = BuildTestSupport.Fonts(reader);
        Assert.Single(fonts);
        Assert.Equal("Type1", BuildTestSupport.Name(reader, fonts[0], "Subtype"));
        Assert.Equal("Helvetica", BuildTestSupport.Name(reader, fonts[0], "BaseFont"));
    }
}
