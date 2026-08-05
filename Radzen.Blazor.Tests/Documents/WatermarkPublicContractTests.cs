#nullable enable
using System;
using System.IO;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents.Core;

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

    [Fact]
    public void TextAndImageBothRenderWithTheImageBeneathTheText()
    {
        var watermark = new Watermark { Text = "DRAFT" };
        using (var stream = new MemoryStream(Convert.FromBase64String(Pixel)))
        {
            watermark.SetImage(stream);
        }

        var rendered = new DocumentRenderer().Render(DocumentWith(watermark));
        var content = System.Text.Encoding.ASCII.GetString(
            rendered.Output!.Pages[0].ContentArray);

        var image = content.LastIndexOf("Do\n", StringComparison.Ordinal);
        var text = content.LastIndexOf("Tj\n", StringComparison.Ordinal);

        Assert.True(image >= 0);
        Assert.True(text >= 0);
        Assert.True(image < text);
    }

    private static Document DocumentWith(Watermark watermark)
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Watermark = watermark;
        section.Blocks.Add(new Paragraph("body"));
        return document;
    }
}
