#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class ImageXObjectOptionTests
{
    private static Image AddImage(Document document, string resource)
    {
        var section = document.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open(resource));
        image.Width = Unit.FromPoint(48);
        image.Height = Unit.FromPoint(48);
        return image;
    }

    private static string SingleImageXObject(Document document)
    {
        var emission = Emit(new DocumentRenderer().Render(document));
        var entry = Shaped(
            "page /Resources with one image XObject",
            @"/XObject << /(\S+) (\d+) 0 R >>",
            Line(emission, "/Type /Page "));

        var image = IndirectObject(emission, entry.Groups[2].Value);
        Carries($"image XObject {entry.Groups[2].Value} 0 R", "/Subtype /Image", image);
        return image;
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void OpacityRejectsNonFiniteAndOutOfRangeValues(double value)
    {
        var document = new Document();
        var image = AddImage(document, "Images/gray.png");

        Assert.Throws<ArgumentOutOfRangeException>(() => image.Opacity = value);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void OpacityAcceptsDocumentedEndpoints(double value)
    {
        var document = new Document();
        var image = AddImage(document, "Images/gray.png");

        image.Opacity = value;

        Assert.Equal(value, image.Opacity);
    }

    [Fact]
    public void Interpolate_WhenSet_EmitsInterpolateTrueOnXObject()
    {
        var document = new Document();
        AddImage(document, "Images/gray.png").Interpolate = true;

        var image = SingleImageXObject(document);

        Carries("image XObject", "/Interpolate true", image);
        Carries("image XObject", "/ColorSpace /DeviceGray", image);
    }

    [Fact]
    public void Interpolate_WhenUnset_OmitsInterpolateKey()
    {
        var document = new Document();
        AddImage(document, "Images/gray.png");

        Lacks("default image XObject", "/Interpolate", SingleImageXObject(document));
    }

    [Fact]
    public void DefaultImage_IsByteIdentical_AcrossBuilds()
    {
        static byte[] Build()
        {
            var document = new Document();
            AddImage(document, "Images/rgb.png");
            return new DocumentRenderer().ToArray(document);
        }

        Assert.Equal(Build(), Build());
    }
}
