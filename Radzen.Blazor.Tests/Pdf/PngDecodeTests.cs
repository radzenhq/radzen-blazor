#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
namespace Radzen.Blazor.Pdf.Tests;

public class PngDecodeTests
{
    [Fact]
    public void Rgb_TrueColor_ToDeviceRgbFlateXObject()
    {
        var xobj = ImageTestHelpers.Xobject(ImageTestHelpers.Decode(PdfTestResources.ReadAllBytes("Images/rgb.png")));
        var dict = xobj.Image.Dictionary;

        Assert.Equal("XObject", ImageTestHelpers.Name(dict, "Type"));
        Assert.Equal("Image", ImageTestHelpers.Name(dict, "Subtype"));
        Assert.Equal(64, ImageTestHelpers.Int(dict, "Width"));
        Assert.Equal(64, ImageTestHelpers.Int(dict, "Height"));
        Assert.Equal("DeviceRGB", ImageTestHelpers.Name(dict, "ColorSpace"));
        Assert.Equal(8, ImageTestHelpers.Int(dict, "BitsPerComponent"));
        Assert.Equal("FlateDecode", ImageTestHelpers.Name(dict, "Filter"));

        var samples = FlateFilter.Decode(xobj.Image.Data.ToArray());
        Assert.Equal(64 * 64 * 3, samples.Length);

        Assert.Null(xobj.SoftMask);
    }

    [Fact]
    public void Gray_ToDeviceGray()
    {
        var xobj = ImageTestHelpers.Xobject(ImageTestHelpers.Decode(PdfTestResources.ReadAllBytes("Images/gray.png")));
        var dict = xobj.Image.Dictionary;

        Assert.Equal("Image", ImageTestHelpers.Name(dict, "Subtype"));
        Assert.Equal("DeviceGray", ImageTestHelpers.Name(dict, "ColorSpace"));
        Assert.Equal(8, ImageTestHelpers.Int(dict, "BitsPerComponent"));
        Assert.Equal("FlateDecode", ImageTestHelpers.Name(dict, "Filter"));
        Assert.Equal(64, ImageTestHelpers.Int(dict, "Width"));
        Assert.Equal(64, ImageTestHelpers.Int(dict, "Height"));

        var samples = FlateFilter.Decode(xobj.Image.Data.ToArray());
        Assert.Equal(64 * 64, samples.Length);

        Assert.Null(xobj.SoftMask);
    }

    [Fact]
    public void Palette_ToIndexedDeviceRgb()
    {
        var xobj = ImageTestHelpers.Xobject(ImageTestHelpers.Decode(PdfTestResources.ReadAllBytes("Images/palette.png")));
        var dict = xobj.Image.Dictionary;

        Assert.Equal(8, ImageTestHelpers.Int(dict, "BitsPerComponent"));
        Assert.Equal("FlateDecode", ImageTestHelpers.Name(dict, "Filter"));

        var cs = Assert.IsType<ArrayObject>(dict["ColorSpace"]);
        Assert.Equal(4, cs.Count);
        Assert.Equal("Indexed", Assert.IsType<NameObject>(cs[0]).Value);
        Assert.Equal("DeviceRGB", Assert.IsType<NameObject>(cs[1]).Value);
        Assert.Equal(255, Assert.IsType<NumberObject>(cs[2]).IntValue);

        var palette = Assert.IsType<StringObject>(cs[3]);
        Assert.Equal(256 * 3, palette.Value.Length);

        var indices = FlateFilter.Decode(xobj.Image.Data.ToArray());
        Assert.Equal(64 * 64, indices.Length);

        Assert.Null(xobj.SoftMask);
    }

    [Fact]
    public void Rgba_ProducesDeviceGraySoftMask()
    {
        var xobj = ImageTestHelpers.Xobject(ImageTestHelpers.Decode(PdfTestResources.ReadAllBytes("Images/alpha.png")));
        var dict = xobj.Image.Dictionary;

        Assert.Equal("DeviceRGB", ImageTestHelpers.Name(dict, "ColorSpace"));
        Assert.Equal(8, ImageTestHelpers.Int(dict, "BitsPerComponent"));
        var colour = FlateFilter.Decode(xobj.Image.Data.ToArray());
        Assert.Equal(64 * 64 * 3, colour.Length);

        var mask = xobj.SoftMask;
        Assert.NotNull(mask);
        var maskDict = mask!.Dictionary;
        Assert.Equal("XObject", ImageTestHelpers.Name(maskDict, "Type"));
        Assert.Equal("Image", ImageTestHelpers.Name(maskDict, "Subtype"));
        Assert.Equal(64, ImageTestHelpers.Int(maskDict, "Width"));
        Assert.Equal(64, ImageTestHelpers.Int(maskDict, "Height"));
        Assert.Equal("DeviceGray", ImageTestHelpers.Name(maskDict, "ColorSpace"));
        Assert.Equal(8, ImageTestHelpers.Int(maskDict, "BitsPerComponent"));
        Assert.Equal("FlateDecode", ImageTestHelpers.Name(maskDict, "Filter"));

        var alpha = FlateFilter.Decode(mask.Data.ToArray());
        Assert.Equal(64 * 64, alpha.Length);
    }

    [Fact]
    public void PaletteWithTrns_ProducesSoftMaskFromTrns()
    {
        var xobj = ImageTestHelpers.Xobject(ImageTestHelpers.Decode(PdfTestResources.ReadAllBytes("Images/palette-trns.png")));

        var cs = Assert.IsType<ArrayObject>(xobj.Image.Dictionary["ColorSpace"]);
        Assert.Equal("Indexed", Assert.IsType<NameObject>(cs[0]).Value);

        var mask = xobj.SoftMask;
        Assert.NotNull(mask);
        Assert.Equal("DeviceGray", ImageTestHelpers.Name(mask!.Dictionary, "ColorSpace"));
        Assert.Equal(8, ImageTestHelpers.Int(mask.Dictionary, "BitsPerComponent"));
        Assert.Equal(64, ImageTestHelpers.Int(mask.Dictionary, "Width"));

        var alpha = FlateFilter.Decode(mask.Data.ToArray());
        Assert.Equal(64 * 64, alpha.Length);
    }

    [Fact]
    public void Adam7Interlaced_Throws()
    {
        var png = BuildInterlacedPngHeader();
        Assert.Throws<NotSupportedException>(() => ImageTestHelpers.Decode(png));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(17)]
    public void SplitIdat_DecodesSameSamplesAsSingleChunk(int chunkSize)
    {
        var idat = RgbIdat();
        var expected = ImageTestHelpers.Xobject(ImageTestHelpers.Decode(BuildRgbPng([idat])));
        var actual = ImageTestHelpers.Xobject(ImageTestHelpers.Decode(BuildRgbPng(SplitIntoChunks(idat, chunkSize))));

        Assert.Equal(
            FlateFilter.Decode(expected.Image.Data.ToArray()),
            FlateFilter.Decode(actual.Image.Data.ToArray()));
    }

    [Fact]
    public void NoIdat_Throws()
    {
        Assert.Throws<InvalidDataException>(() => ImageTestHelpers.Decode(BuildRgbPng([])));
    }

    private static byte[] RgbIdat()
    {
        var raw = new byte[4 * (1 + (4 * 3))];
        for (int i = 0; i < raw.Length; i++)
        {
            raw[i] = (byte)(i * 7);
        }

        for (int y = 0; y < 4; y++)
        {
            raw[y * 13] = 0;
        }

        return Deflate(raw);
    }

    private static byte[][] SplitIntoChunks(byte[] data, int chunkSize)
    {
        var chunks = new List<byte[]>();
        for (int offset = 0; offset < data.Length; offset += chunkSize)
        {
            chunks.Add(data[offset..Math.Min(offset + chunkSize, data.Length)]);
        }

        return [.. chunks];
    }

    private static byte[] BuildRgbPng(byte[][] idatChunks)
    {
        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        byte[] ihdr = [0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x08, 0x02, 0x00, 0x00, 0x00];
        WriteChunk(ms, "IHDR", ihdr);

        foreach (var chunk in idatChunks)
        {
            WriteChunk(ms, "IDAT", chunk);
        }

        WriteChunk(ms, "IEND", []);
        return ms.ToArray();
    }

    private static byte[] BuildInterlacedPngHeader()
    {
        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        byte[] ihdr =
        [
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01,
            0x08,
            0x02,
            0x00,
            0x00,
            0x01,
        ];
        WriteChunk(ms, "IHDR", ihdr);

        byte[] idat = Deflate([0x00, 0xFF, 0x00, 0x00]);
        WriteChunk(ms, "IDAT", idat);
        WriteChunk(ms, "IEND", []);
        return ms.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        Span<byte> len = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(len, (uint)data.Length);
        stream.Write(len);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = Crc32(typeBytes, data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }

    private static byte[] Deflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        crc = Crc32Update(crc, type);
        crc = Crc32Update(crc, data);
        return crc ^ 0xFFFFFFFF;
    }

    private static uint Crc32Update(uint crc, byte[] bytes)
    {
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }

        return crc;
    }
}
