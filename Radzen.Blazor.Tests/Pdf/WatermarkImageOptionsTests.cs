#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class WatermarkImageOptionsTests
{
    private static byte[] Build(bool sectionWatermark, Watermark watermark)
    {
        var document = new Radzen.Documents.Document();
        var section = document.Sections.Add();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Body");
        section.Blocks.Add(paragraph);
        if (sectionWatermark)
        {
            section.Watermark = watermark;
            return new DocumentRenderer().ToArray(document);
        }

        var pdf = new DocumentRenderer().Render(document);
        pdf.AddWatermark(watermark);
        return pdf.ToArray();
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

    [Fact]
    public void DocumentWatermarkMutationToInvalidOpacityIsRejectedEagerly()
    {
        var document = new Document();
        document.Pages.Add();
        var watermark = new Watermark { Text = "draft" };
        document.AddWatermark(watermark);

        Assert.Throws<ArgumentOutOfRangeException>(() => watermark.Opacity = 1.01);
    }

    [Fact]
    public void DocumentWatermarkMutationToValidOpacityStillSaves()
    {
        var document = new Document();
        document.Pages.Add();
        var watermark = new Watermark { Text = "draft" };
        document.AddWatermark(watermark);
        watermark.Opacity = 0.5;

        Assert.NotEmpty(document.ToArray());
    }
}
