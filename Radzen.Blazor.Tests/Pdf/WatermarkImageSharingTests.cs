#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

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

    private static List<int> WatermarkImageRefs(byte[] pdf)
    {
        var reader = DocumentReader.Parse(pdf);
        var refs = new List<int>();
        foreach (var (page, resources) in PdfPageContentTestHelper.PageLeaves(reader, assertStructure: false))
        {
            Assert.NotNull(resources);
            var xobjects = Assert.IsType<DictionaryObject>(reader.Resolve(resources!["XObject"]!));
            var entry = Assert.Single(xobjects.Keys);
            refs.Add(Assert.IsType<ReferenceObject>(xobjects[entry]).ObjectNumber);
        }

        return refs;
    }

    private static int ImageStreamCount(byte[] pdf)
    {
        var reader = DocumentReader.Parse(pdf);
        var count = 0;
        for (var number = 1; number <= reader.ObjectCount; number++)
        {
            if (reader.Resolve(new ReferenceObject(number, 0)) is StreamObject stream
                && stream.Dictionary.TryGetValue("Subtype", out var subtype)
                && subtype is NameObject { Value: "Image" })
            {
                count++;
            }
        }

        return count;
    }

    [Fact]
    public void ImageWatermark_EmitsOneImageStreamSharedByEveryPage()
    {
        var pdf = Watermarked(10).ToArray();

        Assert.Equal(1, ImageStreamCount(pdf));
        var refs = WatermarkImageRefs(pdf);
        Assert.Equal(10, refs.Count);
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
        var pdf = Watermarked(10, configure: image => image.Interpolate = true).ToArray();

        Assert.Equal(1, ImageStreamCount(pdf));
        var reader = DocumentReader.Parse(pdf);
        var (_, resources) = PdfPageContentTestHelper.PageLeaves(reader, assertStructure: false)[0];
        var xobjects = Assert.IsType<DictionaryObject>(reader.Resolve(resources!["XObject"]!));
        var image = Assert.IsType<StreamObject>(reader.Resolve(xobjects[Assert.Single(xobjects.Keys)]!));
        Assert.True(((BooleanObject)image.Dictionary["Interpolate"]!).Value);
    }

    [Fact]
    public void ImageWatermarkOptionsChangedBetweenSaves_ReDecodesRatherThanServingStale()
    {
        var document = new PortableDocument();
        document.Pages.Add();
        var watermark = new Watermark();
        var image = watermark.SetImage(new MemoryStream(PdfTestResources.ReadAllBytes("Images/rgb.png")));
        document.AddWatermark(watermark);

        Assert.DoesNotContain("Interpolate", ImageDictionaryKeys(document.ToArray()));

        image.Interpolate = true;

        Assert.Contains("Interpolate", ImageDictionaryKeys(document.ToArray()));
    }

    private static IEnumerable<string> ImageDictionaryKeys(byte[] pdf)
    {
        var reader = DocumentReader.Parse(pdf);
        var (_, resources) = PdfPageContentTestHelper.PageLeaves(reader, assertStructure: false)[0];
        var xobjects = Assert.IsType<DictionaryObject>(reader.Resolve(resources!["XObject"]!));
        var image = Assert.IsType<StreamObject>(reader.Resolve(xobjects[Assert.Single(xobjects.Keys)]!));
        return image.Dictionary.Keys;
    }

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

        foreach (var pdf in new[] { first.ToArray(), second.ToArray() })
        {
            var reader = DocumentReader.Parse(pdf);
            var (_, resources) = PdfPageContentTestHelper.PageLeaves(reader, assertStructure: false)[0];
            var xobjects = Assert.IsType<DictionaryObject>(reader.Resolve(resources!["XObject"]!));
            var image = Assert.IsType<StreamObject>(reader.Resolve(xobjects[Assert.Single(xobjects.Keys)]!));
            var mask = Assert.IsType<StreamObject>(reader.Resolve(image.Dictionary["SMask"]!));
            Assert.Equal("Image", ((NameObject)mask.Dictionary["Subtype"]!).Value);
        }

        Assert.Equal(2, ImageStreamCount(first.ToArray()));
        Assert.Equal(2, ImageStreamCount(second.ToArray()));
    }
}
