#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class WatermarkImageSharingTests
{
    private static PortableDocument Watermarked(int pages, string png = "Images/rgb.png", Action<Image>? configure = null)
    {
        var document = new PortableDocument();
        for (var i = 0; i < pages; i++)
        {
            document.Pages.Add();
        }

        var watermark = new Watermark();
        var image = watermark.SetImage(new MemoryStream(PdfTestResources.ReadAllBytes(png)));
        configure?.Invoke(image);
        document.AddWatermark(watermark);
        return document;
    }

    private static string[] PageImageRefs(string emission)
    {
        var refs = Regex.Matches(emission, @"/Type /Page [^\n]*?/XObject << /\S+ (\d+) 0 R >>")
            .Select(match => match.Groups[1].Value)
            .ToArray();

        Assert.True(
            refs.Length > 0,
            $"No page carries a single-entry /XObject resource dictionary.\nEmission:\n{Excerpt(emission)}");
        return refs;
    }

    private static int ImageStreamCount(string emission)
        => Regex.Matches(emission, "/Subtype /Image").Count;

    [Fact]
    public void ImageWatermark_EmitsOneImageStreamSharedByEveryPage()
    {
        var emission = Emit(Watermarked(10));

        Assert.Equal(1, ImageStreamCount(emission));
        var refs = PageImageRefs(emission);
        Assert.Equal(10, refs.Length);
        Assert.All(refs, number => Assert.Equal(refs[0], number));
    }

    [Fact]
    public void ImageWatermark_PayloadIsEmittedOnceNotPerPage()
    {
        var text = new PortableDocument();
        for (var i = 0; i < 10; i++)
        {
            text.Pages.Add();
        }

        text.AddWatermark(new Watermark { Text = "DRAFT" });
        var textOnly = text.ToArray().Length;
        var withImage = Watermarked(10).ToArray().Length;

        var payload = PdfTestResources.ReadAllBytes("Images/rgb.png").Length;
        Assert.True(withImage - textOnly < 3 * payload,
            $"text-only {textOnly} bytes, image watermark {withImage} bytes, payload {payload} bytes");
    }

    [Fact]
    public void ImageWatermarkWithXObjectOptions_StillSharesOneImageStream()
    {
        var emission = Emit(Watermarked(10, configure: image => image.Interpolate = true));

        Assert.Equal(1, ImageStreamCount(emission));

        var number = PageImageRefs(emission)[0];
        Carries($"watermark image XObject {number} 0 R", "/Interpolate true", IndirectObject(emission, number));
    }

    [Fact]
    public void ImageWatermarkOptionsChangedBetweenSaves_ReDecodesRatherThanServingStale()
    {
        var document = new PortableDocument();
        document.Pages.Add();
        var watermark = new Watermark();
        var image = watermark.SetImage(new MemoryStream(PdfTestResources.ReadAllBytes("Images/rgb.png")));
        document.AddWatermark(watermark);

        Lacks("watermark image XObject", "/Interpolate", WatermarkImage(Emit(document)));

        image.Interpolate = true;

        Carries("watermark image XObject", "/Interpolate", WatermarkImage(Emit(document)));
    }

    private static string WatermarkImage(string emission)
        => IndirectObject(emission, PageImageRefs(emission)[0]);

    [Fact]
    public void SoftMaskedWatermarkSharedAcrossDocuments_ResolvesItsMaskInEverySave()
    {
        var watermark = new Watermark();
        watermark.SetImage(new MemoryStream(PdfTestResources.ReadAllBytes("Images/alpha.png")));

        var first = new PortableDocument();
        first.Pages.Add();
        first.Pages.Add();
        first.AddWatermark(watermark);

        var second = new PortableDocument();
        second.Pages.Add();
        second.AddWatermark(watermark);

        foreach (var emission in new[] { Emit(first), Emit(second) })
        {
            var image = WatermarkImage(emission);
            var mask = Shaped("watermark image XObject", @"/SMask (\d+) 0 R", image);

            Carries(
                $"watermark soft mask {mask.Groups[1].Value} 0 R",
                "/Subtype /Image",
                IndirectObject(emission, mask.Groups[1].Value));

            Assert.Equal(2, ImageStreamCount(emission));
        }
    }
}
