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

    private static void AssertPaginationAgrees(Document document)
    {
        var laidOut = LaidOut(document, ImageProbes.None);
        var rendered = Rendered(document, null);

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
}
