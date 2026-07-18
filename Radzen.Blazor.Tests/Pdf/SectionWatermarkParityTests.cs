#nullable enable
using System;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
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
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var watermark = new Watermark { Opacity = 1 };
        var image = watermark.SetImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Interpolate = true;
        section.Watermark = watermark;
        section.Blocks.Add(Text("Body"));

        var reader = BuildTestSupport.Read(builder);
        var stream = Assert.Single(BuildTestSupport.ImageXObjects(reader));
        Assert.True(stream.Dictionary.TryGetValue("Interpolate", out var interpolate));
        Assert.True(interpolate is BooleanObject { Value: true });
    }

    [Fact]
    public void SectionWatermark_RejectsOutOfRangeOpacity()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Watermark = new Watermark { Text = "DRAFT", Opacity = 2 };
        section.Blocks.Add(Text("Body"));

        Assert.Throws<ArgumentOutOfRangeException>(() => BuildTestSupport.Read(builder));
    }

    [Fact]
    public void SectionWatermark_RejectsNonFiniteRotation()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Watermark = new Watermark { Text = "DRAFT", Rotation = double.NaN };
        section.Blocks.Add(Text("Body"));

        Assert.Throws<ArgumentOutOfRangeException>(() => BuildTestSupport.Read(builder));
    }

    [Fact]
    public void DecodeImage_ReflectsReplacedImage()
    {
        var watermark = new Watermark();
        var first = watermark.SetImage(PdfTestResources.Open("Images/rgb.jpg"));
        var decodedFirst = watermark.DecodeImage(first);

        var second = watermark.SetImage(PdfTestResources.Open("Images/gray.jpg"));
        var decodedSecond = watermark.DecodeImage(second);

        Assert.NotSame(decodedFirst, decodedSecond);
    }
}
