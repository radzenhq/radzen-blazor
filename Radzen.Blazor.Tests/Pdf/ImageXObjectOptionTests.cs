#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Opt-in keys stamped on the image XObject dictionary: /Interpolate, the /ImageMask
// stencil transform, and /Mask colour-key masking. Each test builds a document through
// the public DocumentBuilder API, round-trips the bytes through DocumentReader and asserts
// the exact ISO 32000-1 construct on the reloaded XObject.
public class ImageXObjectOptionTests
{
    private static Image AddImage(DocumentBuilder builder, string resource)
    {
        var section = builder.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open(resource));
        image.Width = Unit.FromPoint(48);
        image.Height = Unit.FromPoint(48);
        return image;
    }

    private static DictionaryObject SingleImageDictionary(DocumentBuilder builder)
    {
        var reader = BuildTestSupport.Read(builder);
        var images = BuildTestSupport.ImageXObjects(reader);
        return Assert.Single(images).Dictionary;
    }

    [Fact]
    public void Interpolate_WhenSet_EmitsInterpolateTrueOnXObject()
    {
        var builder = new DocumentBuilder();
        AddImage(builder, "Images/gray.png").Interpolate = true;

        var dict = SingleImageDictionary(builder);

        Assert.True(dict.ContainsKey("Interpolate"), "image XObject is missing /Interpolate");
        Assert.True(Assert.IsType<BooleanObject>(dict["Interpolate"]).Value);
        // The colour image keeps its colour space; the flag is purely additive.
        Assert.Equal("DeviceGray", ((NameObject)dict["ColorSpace"]).Value);
    }

    [Fact]
    public void Interpolate_WhenUnset_OmitsInterpolateKey()
    {
        var builder = new DocumentBuilder();
        AddImage(builder, "Images/gray.png");

        var dict = SingleImageDictionary(builder);

        Assert.False(dict.ContainsKey("Interpolate"), "default image must not carry /Interpolate");
    }
}
