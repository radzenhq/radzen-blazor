#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class WatermarkImageOptionsTests
{
    private static string Build(bool sectionWatermark, Watermark watermark)
    {
        var document = new Document();
        var section = document.Sections.Add();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Body");
        section.Blocks.Add(paragraph);
        if (sectionWatermark)
        {
            section.Watermark = watermark;
            return Encoding.Latin1.GetString(new DocumentRenderer().ToArray(document));
        }

        var pdf = new DocumentRenderer().Render(document);
        pdf.AddWatermark(watermark);
        return Emit(pdf);
    }

    private static double[] PageFillAlphas(string emission)
    {
        var states = Shaped(
            "page /Resources /ExtGState",
            @"/ExtGState << ((?:/\S+ << [^>]*>> )+)>>",
            Line(emission, "/Type /Page "));

        return [.. Regex.Matches(states.Groups[1].Value, @"/ca (-?[\d.]+)")
            .Select(match => double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))];
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WatermarkImageOpacityCombinesWithWatermarkOpacity(bool sectionWatermark)
    {
        var watermark = new Watermark { Opacity = 0.5, Rotation = 0 };
        var image = watermark.SetImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Opacity = 0.4;

        var alphas = PageFillAlphas(Build(sectionWatermark, watermark));

        Assert.True(
            alphas.Any(alpha => Math.Abs(alpha - 0.2) < 0.000001),
            $"No page ExtGState carries a /ca of 0.2. Fill alphas: {string.Join(", ", alphas)}");
    }

    [Fact]
    public void DocumentWatermarkMutationToInvalidOpacityIsRejectedEagerly()
    {
        var document = new PortableDocument();
        document.Pages.Add();
        var watermark = new Watermark { Text = "draft" };
        document.AddWatermark(watermark);

        Assert.Throws<ArgumentOutOfRangeException>(() => watermark.Opacity = 1.01);
    }

    [Fact]
    public void DocumentWatermarkMutationToValidOpacityStillSaves()
    {
        var document = new PortableDocument();
        document.Pages.Add();
        var watermark = new Watermark { Text = "draft" };
        document.AddWatermark(watermark);
        watermark.Opacity = 0.5;

        Assert.NotEmpty(document.ToArray());
    }
}
