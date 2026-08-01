#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Render;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class ImageMeasurementParityTests
{
    private static readonly Regex Placement = new(
        @"([-\d.]+) 0 0 ([-\d.]+) ([-\d.]+) ([-\d.]+) cm", RegexOptions.Compiled);

    private sealed record Placed(double Width, double Height, double X, double Y);

    private static List<List<Placed>> Rendered(Document document, DocumentRenderer? renderer)
    {
        var reader = BuildTestSupport.Read(document, renderer);
        var pages = new List<List<Placed>>();
        var leaves = BuildTestSupport.PageLeaves(reader);
        for (var i = 0; i < leaves.Count; i++)
        {
            var content = Encoding.ASCII.GetString(ContentTestHelpers.PageContent(reader, i));
            var placed = new List<Placed>();
            foreach (Match match in Placement.Matches(content))
            {
                placed.Add(new Placed(
                    Number(match.Groups[1].Value),
                    Number(match.Groups[2].Value),
                    Number(match.Groups[3].Value),
                    Number(match.Groups[4].Value)));
            }

            pages.Add(placed);
        }

        return pages;
    }

    private static double Number(string value)
        => double.Parse(value, CultureInfo.InvariantCulture);

    private static List<List<Placed>> LaidOut(Document document, ImageProbes probes)
    {
        var pages = new List<List<Placed>>();
        foreach (var page in DocumentLayouter.Layout(document, probes).Pages)
        {
            var left = page.ContentBox.X;
            var top = page.Size.Height.Point - page.ContentBox.Y;
            var placed = new List<Placed>();
            foreach (var image in page.Body.Images)
            {
                placed.Add(new Placed(
                    image.Width,
                    image.Height,
                    left + image.X,
                    top - image.Y - image.Height));
            }

            pages.Add(placed);
        }

        return pages;
    }

    private static void AssertPaginationAgrees(Document document, DocumentRenderer? renderer = null)
    {
        var laidOut = LaidOut(document, (renderer?.ImageDecoders ?? ImageDecoders.Default).Probes);
        var rendered = Rendered(document, renderer);

        Assert.Equal(laidOut.Count, rendered.Count);
        for (var i = 0; i < laidOut.Count; i++)
        {
            Assert.Equal(laidOut[i].Count, rendered[i].Count);
            for (var j = 0; j < laidOut[i].Count; j++)
            {
                var expected = laidOut[i][j];
                var actual = rendered[i][j];
                Assert.Equal(expected.Width, actual.Width, 3);
                Assert.Equal(expected.Height, actual.Height, 3);
                Assert.Equal(expected.X, actual.X, 3);
                Assert.Equal(expected.Y, actual.Y, 3);
            }
        }
    }

    private static Document ImageFlow(byte[] data, int count, double? height)
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(20));
        for (var i = 0; i < count; i++)
        {
            var image = new Image(data);
            if (height is { } value)
            {
                image.Height = Unit.FromPoint(value);
            }

            section.Blocks.Add(image);
        }

        return document;
    }

    [Theory]
    [InlineData("Images/rgb.png")]
    [InlineData("Images/alpha.png")]
    [InlineData("Images/rgb.jpg")]
    public void StandaloneLayoutAndRender_PaginateImagesIdentically(string resource)
    {
        var data = PdfTestResources.ReadAllBytes(resource);

        AssertPaginationAgrees(ImageFlow(data, 8, 70));
    }

    [Fact]
    public void StandaloneLayoutAndRender_PaginateNaturallySizedImagesIdentically()
    {
        var data = PdfTestResources.ReadAllBytes("Images/rgb.png");

        AssertPaginationAgrees(ImageFlow(data, 12, null));
    }

    [Fact]
    public void ScopedDecoder_BridgesToItsProbeSet_AndPaginatesIdentically()
    {
        Assert.Throws<NotSupportedException>(() => ImageProbes.None.PixelSize(BridgeDecoder.Payload()));

        var renderer = new DocumentRenderer { ImageDecoders = ImageDecoders.BuiltIn.Add(new BridgeDecoder()) };
        var probes = renderer.ImageDecoders.Probes;

        Assert.Equal(((double)BridgeDecoder.PixelWidth, (double)BridgeDecoder.PixelHeight), probes.PixelSize(BridgeDecoder.Payload()));
        Assert.Equal(ImageFormat.Custom, probes.Format(BridgeDecoder.Payload()));

        AssertPaginationAgrees(ImageFlow(BridgeDecoder.Payload(), 6, null), renderer);
    }

    private const double IsolatedPointWidth = IsolatedDecoder.PixelWidth * 72.0 / 96.0;

    private const double IsolatedPointHeight = IsolatedDecoder.PixelHeight * 72.0 / 96.0;

    private static DocumentRenderer IsolatedRenderer()
    {
        Assert.Throws<NotSupportedException>(
            () => ImageDecoders.Default.Probes.PixelSize(IsolatedDecoder.Payload()));

        return new DocumentRenderer { ImageDecoders = ImageDecoders.BuiltIn.Add(new IsolatedDecoder()) };
    }

    private static LaidOutPage LaidOutWithRendererProbes(Document document, DocumentRenderer renderer, int page = 0)
    {
        Assert.Throws<NotSupportedException>(() => DocumentLayouter.Layout(document));

        return DocumentLayouter.Layout(document, renderer.ImageDecoders.Probes).Pages[page];
    }

    private static Paragraph PlainText(string text)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(text);
        return paragraph;
    }

    private static Section IsolatedSection(Document document, double width, double height)
    {
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(width), Unit.FromPoint(height));
        section.Margins.SetAll(Unit.FromPoint(0));
        section.HeaderDistance = Unit.FromPoint(0);
        section.FooterDistance = Unit.FromPoint(0);
        return section;
    }

    [Fact]
    public void Layout_MeasuresBlockImagesWithTheRendererProbeSet_NotTheGlobalRegistry()
    {
        var renderer = IsolatedRenderer();
        var document = ImageFlow(IsolatedDecoder.Payload(), 1, null);

        var image = LaidOutWithRendererProbes(document, renderer).Body.Images[0];

        Assert.Equal(IsolatedPointWidth, image.Width, 3);
        Assert.Equal(IsolatedPointHeight, image.Height, 3);
    }

    [Fact]
    public void Layout_MeasuresWatermarkImagesWithTheRendererProbeSet_NotTheGlobalRegistry()
    {
        var renderer = IsolatedRenderer();
        var document = new Document();
        var section = IsolatedSection(document, 300, 300);
        section.Blocks.Add(PlainText("body"));
        section.Watermark = new Watermark { Image = new Image(IsolatedDecoder.Payload()) };

        var watermark = LaidOutWithRendererProbes(document, renderer).Watermark;

        Assert.NotNull(watermark);
        var image = Assert.NotNull(watermark!.Image);
        Assert.Equal(IsolatedPointWidth, image.Width, 3);
        Assert.Equal(IsolatedPointHeight, image.Height, 3);
        Assert.NotEmpty(renderer.ToArray(document));
    }

    [Fact]
    public void Layout_MeasuresInlineImagesWithTheRendererProbeSet_NotTheGlobalRegistry()
    {
        var renderer = IsolatedRenderer();
        var document = new Document();
        var section = IsolatedSection(document, 300, 300);
        var paragraph = new Paragraph();
        paragraph.Inlines.AddImage(new MemoryStream(IsolatedDecoder.Payload()));
        section.Blocks.Add(paragraph);

        var line = Assert.Single(LaidOutWithRendererProbes(document, renderer).Body.Lines);
        var fragment = Assert.Single(line.Line.Fragments);
        var inline = Assert.NotNull(fragment.Paint.InlineImage);

        Assert.Equal(IsolatedPointWidth, inline.Width, 3);
        Assert.Equal(IsolatedPointHeight, inline.Height, 3);
        Assert.Equal(IsolatedPointWidth, fragment.Advance, 3);
        Assert.NotEmpty(renderer.ToArray(document));
    }

    [Fact]
    public void Layout_MeasuresTheKeepWithNextLookaheadWithTheRendererProbeSet_NotTheGlobalRegistry()
    {
        var renderer = IsolatedRenderer();
        var lineHeight = PaginationSupport.BuiltInLineHeight();
        var document = new Document();
        var section = IsolatedSection(document, 300, lineHeight + IsolatedPointHeight + 1);
        section.Blocks.Add(PlainText("filler"));
        var heading = PlainText("heading");
        heading.KeepWithNext = true;
        section.Blocks.Add(heading);
        section.Blocks.Add(new Image(IsolatedDecoder.Payload()));

        Assert.Throws<NotSupportedException>(() => DocumentLayouter.Layout(document));
        var pages = DocumentLayouter.Layout(document, renderer.ImageDecoders.Probes).Pages;

        Assert.Equal(2, pages.Length);
        Assert.Empty(pages[0].Body.Images);
        Assert.Single(pages[1].Body.Lines);
        Assert.Single(pages[1].Body.Images);
    }

    private sealed class IsolatedDecoder : IImageDecoder
    {
        public const int PixelWidth = 120;

        public const int PixelHeight = 60;

        private static readonly byte[] Magic = [0x52, 0x5A, 0x49, 0x53];

        public static byte[] Payload()
        {
            var data = new byte[Magic.Length + 1];
            Magic.CopyTo(data, 0);
            return data;
        }

        public bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out DecodedImage? image)
        {
            if (!data.Span.StartsWith(Magic))
            {
                image = null;
                return false;
            }

            image = new DecodedImage(
                new byte[PixelWidth * PixelHeight], PixelWidth, PixelHeight, 8, ImageColorSpace.DeviceGray);
            return true;
        }
    }

    [Fact]
    public void Inspect_ReportsFormatAndMediaType()
    {
        var expected = new (string Resource, ImageFormat Format, string MediaType)[]
        {
            ("Images/rgb.png", ImageFormat.Png, "image/png"),
            ("Images/alpha.png", ImageFormat.Png, "image/png"),
            ("Images/palette.png", ImageFormat.Png, "image/png"),
            ("Images/rgb.jpg", ImageFormat.Jpeg, "image/jpeg"),
            ("Images/cmyk.jpg", ImageFormat.Jpeg, "image/jpeg"),
        };

        foreach (var (resource, format, mediaType) in expected)
        {
            var info = ImageProbes.None.Inspect(PdfTestResources.ReadAllBytes(resource));

            Assert.Equal(format, info.Format);
            Assert.Equal(mediaType, ImageMetrics.MediaType(info.Format));
        }
    }

    [Fact]
    public void Inspect_ReportsJpeg2000Format()
    {
        var data = Jpeg2000Codestream(8, 4);

        Assert.Equal(ImageFormat.Jpeg2000, ImageProbes.None.Format(data));
        Assert.Equal("image/jp2", ImageMetrics.MediaType(ImageProbes.None.Format(data)));
        Assert.Equal((8d, 4d), ImageProbes.None.PixelSize(data));
    }

    // ISO/IEC 15444-1 A.5.1: SOC followed by the SIZ marker segment carrying the image area.
    private static byte[] Jpeg2000Codestream(int width, int height)
    {
        const int Components = 1;
        var lsiz = 38 + (3 * Components);
        var data = new byte[4 + lsiz];
        data[0] = 0xFF;
        data[1] = 0x4F;
        data[2] = 0xFF;
        data[3] = 0x51;
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(4), (ushort)lsiz);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(12), (uint)height);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(24), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(28), (uint)height);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(40), (ushort)Components);
        data[42] = 0x07;
        data[43] = 0x01;
        data[44] = 0x01;
        return data;
    }

    private sealed class BridgeDecoder : IImageDecoder
    {
        public const int PixelWidth = 240;

        public const int PixelHeight = 90;

        private static readonly byte[] Magic = [0x52, 0x5A, 0x42, 0x52];

        public static byte[] Payload()
        {
            var data = new byte[Magic.Length + 1];
            Magic.CopyTo(data, 0);
            return data;
        }

        public bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out DecodedImage? image)
        {
            if (!data.Span.StartsWith(Magic))
            {
                image = null;
                return false;
            }

            image = new DecodedImage(
                new byte[PixelWidth * PixelHeight], PixelWidth, PixelHeight, 8, ImageColorSpace.DeviceGray);
            return true;
        }
    }
}
