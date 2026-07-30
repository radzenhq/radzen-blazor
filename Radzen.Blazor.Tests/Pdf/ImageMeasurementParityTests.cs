#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ImageMeasurementParityTests
{
    private static readonly Regex Placement = new(
        @"([-\d.]+) 0 0 ([-\d.]+) ([-\d.]+) ([-\d.]+) cm", RegexOptions.Compiled);

    private sealed record Placed(double Width, double Height, double X, double Y);

    private static List<List<Placed>> Rendered(Document document)
    {
        var reader = BuildTestSupport.Read(document);
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

    private static List<List<Placed>> LaidOut(Document document)
    {
        var pages = new List<List<Placed>>();
        foreach (var page in DocumentLayouter.Layout(document).Pages)
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

    private static void AssertPaginationAgrees(Document document)
    {
        var laidOut = LaidOut(document);
        var rendered = Rendered(document);

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
    public void RegisteredDecoder_BridgesToNeutralProbe_AndPaginatesIdentically()
    {
        var decoder = new BridgeDecoder();

        Assert.Throws<NotSupportedException>(() => ImageProbes.None.PixelSize(BridgeDecoder.Payload()));

        var probes = ImageProbes.None.Add(
            data => decoder.TryDecode(data, ReaderLimits.Default, out var xobject) ? ImageDecoder.PixelSize(xobject) : null);

        Assert.Equal(((double)BridgeDecoder.PixelWidth, (double)BridgeDecoder.PixelHeight), probes.PixelSize(BridgeDecoder.Payload()));
        Assert.Equal(ImageFormat.Custom, probes.Format(BridgeDecoder.Payload()));

        ImageDecoder.Register(decoder);

        AssertPaginationAgrees(ImageFlow(BridgeDecoder.Payload(), 6, null));
    }

    [Fact]
    public void Layout_MeasuresWithTheSuppliedProbeSet_NotTheGlobalRegistry()
    {
        var document = ImageFlow(IsolatedFormat.Payload(), 1, null);

        Assert.Throws<NotSupportedException>(() => DocumentLayouter.Layout(document));

        var probes = ImageProbes.None.Add(
            data => data.Span.StartsWith(IsolatedFormat.Magic) ? (IsolatedFormat.PixelWidth, IsolatedFormat.PixelHeight) : null);
        var image = DocumentLayouter.Layout(document, probes).Pages[0].Body.Images[0];

        Assert.Equal(IsolatedFormat.PixelWidth * 72.0 / 96.0, image.Width, 3);
        Assert.Equal(IsolatedFormat.PixelHeight * 72.0 / 96.0, image.Height, 3);
    }

    private static class IsolatedFormat
    {
        public const double PixelWidth = 120;

        public const double PixelHeight = 60;

        public static readonly byte[] Magic = [0x52, 0x5A, 0x49, 0x53];

        public static byte[] Payload()
        {
            var data = new byte[Magic.Length + 1];
            Magic.CopyTo(data, 0);
            return data;
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
            var image = new Image(PdfTestResources.ReadAllBytes(resource));

            Assert.Equal(format, image.Info.Format);
            Assert.Equal(mediaType, ImageProbe.MediaType(image.Info.Format));
        }
    }

    [Fact]
    public void Inspect_ReportsJpeg2000Format()
    {
        var data = Jpeg2000Codestream(8, 4);

        Assert.Equal(ImageFormat.Jpeg2000, ImageProbe.Format(data));
        Assert.Equal("image/jp2", ImageProbe.MediaType(ImageProbe.Format(data)));
        Assert.Equal((8d, 4d), ImageProbe.PixelSize(data));
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

        public bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out ImageXObject? xobject)
        {
            if (!data.Span.StartsWith(Magic))
            {
                xobject = null;
                return false;
            }

            var stream = new StreamObject(new byte[PixelWidth * PixelHeight]);
            stream.Dictionary["Type"] = new NameObject("XObject");
            stream.Dictionary["Subtype"] = new NameObject("Image");
            stream.Dictionary["Width"] = new NumberObject(PixelWidth);
            stream.Dictionary["Height"] = new NumberObject(PixelHeight);
            stream.Dictionary["ColorSpace"] = new NameObject("DeviceGray");
            stream.Dictionary["BitsPerComponent"] = new NumberObject(8);
            xobject = new ImageXObject(stream, null);
            return true;
        }
    }
}
