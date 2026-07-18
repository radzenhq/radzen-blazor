#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class WatermarkImageOptionsTests
{
    private static byte[] Build(bool sectionWatermark, Watermark watermark)
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Body");
        section.Blocks.Add(paragraph);
        if (sectionWatermark)
        {
            section.Watermark = watermark;
            return builder.ToArray();
        }

        var document = builder.Build();
        document.AddWatermark(watermark);
        return document.ToArray();
    }

    private static string Content(DocumentReader reader)
    {
        var page = Assert.Single(PdfPageContentTestHelper.PageLeaves(reader, assertStructure: true)).Page;
        return Encoding.ASCII.GetString(PdfPageContentTestHelper.Content(
            reader, page, assertStreams: true, appendSeparatorAfterEveryStream: false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WatermarkStencilImageUsesItsStencilColor(bool sectionWatermark)
    {
        var watermark = new Watermark { Opacity = 1, Rotation = 0 };
        using var stream = new MemoryStream(ImageTestHelpers.OneBitGrayPng(8, 8));
        var image = watermark.SetImage(stream);
        image.Stencil = true;
        image.StencilColor = Color.FromRgb(255, 0, 0);

        var content = Content(DocumentReader.Parse(Build(sectionWatermark, watermark)));

        Assert.Contains("1 0 0 rg", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WatermarkImageOpacityCombinesWithWatermarkOpacity(bool sectionWatermark)
    {
        var watermark = new Watermark { Opacity = 0.5, Rotation = 0 };
        var image = watermark.SetImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Opacity = 0.4;

        var reader = DocumentReader.Parse(Build(sectionWatermark, watermark));
        var page = Assert.Single(PdfPageContentTestHelper.PageLeaves(reader, assertStructure: true));
        var states = reader.GetDictionary(page.Resources!, "ExtGState");

        Assert.NotNull(states);
        Assert.Contains(states!.Keys, key =>
            reader.AsDictionary(states[key]) is { } state
            && state.TryGetValue("ca", out var alphaValue)
            && reader.Resolve(alphaValue!) is NumberObject alpha
            && Math.Abs(alpha.DoubleValue - 0.2) < 0.000001);
    }
}
