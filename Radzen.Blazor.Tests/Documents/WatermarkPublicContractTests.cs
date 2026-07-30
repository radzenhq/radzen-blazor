#nullable enable
using System;
using System.IO;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;
using ModelDocument = Radzen.Documents.Document;

namespace Radzen.Blazor.Documents.Tests;

public class WatermarkPublicContractTests
{
    private const string Pixel =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    [Fact]
    public void DefaultsMatchThePublicWatermarkContract()
    {
        var watermark = new Watermark();

        Assert.Null(watermark.Text);
        Assert.Null(watermark.Image);
        Assert.Equal(Unit.Parse("72pt"), watermark.Font.Size);
        Assert.Equal(0.15, watermark.Opacity);
        Assert.Equal(45, watermark.Rotation);
    }

    [Fact]
    public void SetImageBuffersAndReturnsTheAssignedImage()
    {
        Image image;
        using (var stream = new MemoryStream(Convert.FromBase64String(Pixel)))
        {
            var watermark = new Watermark();
            image = watermark.SetImage(stream);
            Assert.Same(image, watermark.Image);
        }

        Assert.NotNull(image);
    }

    [Fact]
    public void ImagePropertyCanBeCleared()
    {
        var watermark = new Watermark();
        using var stream = new MemoryStream(Convert.FromBase64String(Pixel));
        watermark.SetImage(stream);

        watermark.Image = null;

        Assert.Null(watermark.Image);
    }

    [Fact]
    public void SetImageRejectsNull()
        => Assert.Throws<ArgumentNullException>(() => new Watermark().SetImage(null!));

    [Fact]
    public void FiniteRotationPassesRenderTimeValidation()
    {
        var document = DocumentWith(new Watermark { Text = "DRAFT", Rotation = -720.5 });

        Assert.Single(new DocumentRenderer().Render(document).Pages);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteRotationIsRejectedWhenAssigned(double rotation)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Watermark { Text = "DRAFT", Rotation = rotation });

    private static ModelDocument DocumentWith(Watermark watermark)
    {
        var document = new ModelDocument();
        var section = document.Sections.Add();
        section.Watermark = watermark;
        section.Blocks.AddParagraph("body");
        return document;
    }
}
