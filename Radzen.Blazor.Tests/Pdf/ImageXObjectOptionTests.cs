#nullable enable
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

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

    private static DictionaryObject SingleImageDictionary(Document document)
    {
        var reader = BuildTestSupport.Read(document);
        var images = BuildTestSupport.ImageXObjects(reader);
        return Assert.Single(images).Dictionary;
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

        var dict = SingleImageDictionary(document);

        Assert.True(dict.ContainsKey("Interpolate"), "image XObject is missing /Interpolate");
        Assert.True(Assert.IsType<BooleanObject>(dict["Interpolate"]).Value);
        Assert.Equal("DeviceGray", ((NameObject)dict["ColorSpace"]).Value);
    }

    [Fact]
    public void Interpolate_WhenUnset_OmitsInterpolateKey()
    {
        var document = new Document();
        AddImage(document, "Images/gray.png");

        var dict = SingleImageDictionary(document);

        Assert.False(dict.ContainsKey("Interpolate"), "default image must not carry /Interpolate");
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
