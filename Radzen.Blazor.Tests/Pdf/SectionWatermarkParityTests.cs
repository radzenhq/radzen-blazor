#nullable enable
using System.Reflection;
using System;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class SectionWatermarkParityTests
{
    private static Paragraph Text(string text)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(text);
        return paragraph;
    }

    [Fact]
    public void SectionWatermarkImage_HonoursInterpolateOption()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var watermark = new Watermark { Opacity = 1 };
        var image = watermark.SetImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Interpolate = true;
        section.Watermark = watermark;
        section.Blocks.Add(Text("Body"));

        var reader = BuildTestSupport.Read(document);
        var stream = Assert.Single(BuildTestSupport.ImageXObjects(reader));
        Assert.True(stream.Dictionary.TryGetValue("Interpolate", out var interpolate));
        Assert.True(interpolate is BooleanObject { Value: true });
    }

    [Fact]
    public void SectionWatermark_RejectsOutOfRangeOpacity()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Watermark { Text = "DRAFT", Opacity = 2 });

    [Fact]
    public void SectionWatermark_RejectsNonFiniteRotation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Watermark { Text = "DRAFT", Rotation = double.NaN });
    }

    [Fact]
    public void DecodeWatermark_ReflectsReplacedImage()
    {
        var images = new ImageRegistry(ImageDecoders.Default);
        var watermark = new Watermark();
        var first = watermark.SetImage(PdfTestResources.Open("Images/rgb.jpg"));
        var decodedFirst = images.DecodeWatermark(
            new SourceId(0),
            Paint(first)).Image;

        var second = watermark.SetImage(PdfTestResources.Open("Images/gray.jpg"));
        var decodedSecond = images.DecodeWatermark(
            new SourceId(1),
            Paint(second)).Image;

        Assert.NotSame(decodedFirst, decodedSecond);
    }

    private static ImagePaint Paint(Image image) => new()
    {
        Data = new SceneImageData(image.Data),
        Opacity = image.Opacity,
        Interpolate = image.Interpolate,
    };
}
