#nullable enable
using System;
using System.IO.Compression;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class PngDecodeTests
{
    [Fact]
    public void Rgb_TrueColor_ToDeviceRgbFlateXObject()
    {
        var xobj = ImageDecoder.Decode(PdfTestResources.ReadAllBytes("Images/rgb.png"));
        var dict = xobj.Image.Dictionary;

        Assert.Equal("XObject", ImageTestHelpers.Name(dict, "Type"));
        Assert.Equal("Image", ImageTestHelpers.Name(dict, "Subtype"));
        Assert.Equal(64, ImageTestHelpers.Int(dict, "Width"));
        Assert.Equal(64, ImageTestHelpers.Int(dict, "Height"));
        Assert.Equal("DeviceRGB", ImageTestHelpers.Name(dict, "ColorSpace"));
        Assert.Equal(8, ImageTestHelpers.Int(dict, "BitsPerComponent"));
        Assert.Equal("FlateDecode", ImageTestHelpers.Name(dict, "Filter"));

        var samples = FlateFilter.Decode(xobj.Image.Data);
        Assert.Equal(64 * 64 * 3, samples.Length);

        Assert.Null(xobj.SoftMask);
    }

    [Fact]
    public void Gray_ToDeviceGray()
    {
        var xobj = ImageDecoder.Decode(PdfTestResources.ReadAllBytes("Images/gray.png"));
        var dict = xobj.Image.Dictionary;

        Assert.Equal("Image", ImageTestHelpers.Name(dict, "Subtype"));
        Assert.Equal("DeviceGray", ImageTestHelpers.Name(dict, "ColorSpace"));
        Assert.Equal(8, ImageTestHelpers.Int(dict, "BitsPerComponent"));
        Assert.Equal("FlateDecode", ImageTestHelpers.Name(dict, "Filter"));
        Assert.Equal(64, ImageTestHelpers.Int(dict, "Width"));
        Assert.Equal(64, ImageTestHelpers.Int(dict, "Height"));

        var samples = FlateFilter.Decode(xobj.Image.Data);
        Assert.Equal(64 * 64, samples.Length);

        Assert.Null(xobj.SoftMask);
    }

    [Fact]
    public void Palette_ToIndexedDeviceRgb()
    {
        var xobj = ImageDecoder.Decode(PdfTestResources.ReadAllBytes("Images/palette.png"));
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

        var indices = FlateFilter.Decode(xobj.Image.Data);
        Assert.Equal(64 * 64, indices.Length);

        Assert.Null(xobj.SoftMask);
    }

    [Fact]
    public void Rgba_ProducesDeviceGraySoftMask()
    {
        var xobj = ImageDecoder.Decode(PdfTestResources.ReadAllBytes("Images/alpha.png"));
        var dict = xobj.Image.Dictionary;

        Assert.Equal("DeviceRGB", ImageTestHelpers.Name(dict, "ColorSpace"));
        Assert.Equal(8, ImageTestHelpers.Int(dict, "BitsPerComponent"));
        var colour = FlateFilter.Decode(xobj.Image.Data);
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

        var alpha = FlateFilter.Decode(mask.Data);
        Assert.Equal(64 * 64, alpha.Length);
    }

    [Fact]
    public void PaletteWithTrns_ProducesSoftMaskFromTrns()
    {
        var xobj = ImageDecoder.Decode(PdfTestResources.ReadAllBytes("Images/palette-trns.png"));

        var cs = Assert.IsType<ArrayObject>(xobj.Image.Dictionary["ColorSpace"]);
        Assert.Equal("Indexed", Assert.IsType<NameObject>(cs[0]).Value);

        var mask = xobj.SoftMask;
        Assert.NotNull(mask);
        Assert.Equal("DeviceGray", ImageTestHelpers.Name(mask!.Dictionary, "ColorSpace"));
        Assert.Equal(8, ImageTestHelpers.Int(mask.Dictionary, "BitsPerComponent"));
        Assert.Equal(64, ImageTestHelpers.Int(mask.Dictionary, "Width"));

        var alpha = FlateFilter.Decode(mask.Data);
        Assert.Equal(64 * 64, alpha.Length);
    }

    [Fact]
    public void Adam7Interlaced_Throws()
    {
        var png = BuildInterlacedPngHeader();
        Assert.Throws<NotSupportedException>(() => ImageDecoder.Decode(png));
    }

    private static byte[] BuildInterlacedPngHeader()
    {
        using var ms = new System.IO.MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR: 1x1, 8-bit, truecolor, no compression/filter method flags, interlace = 1 (Adam7).
        byte[] ihdr =
        [
            0x00, 0x00, 0x00, 0x01, // width 1
            0x00, 0x00, 0x00, 0x01, // height 1
            0x08,                   // bit depth
            0x02,                   // colour type 2 (truecolor)
            0x00,                   // compression
            0x00,                   // filter
            0x01,                   // interlace = Adam7
        ];
        WriteChunk(ms, "IHDR", ihdr);

        // A well-formed IDAT so decoders that scan past IHDR still reach the interlace rejection.
        byte[] idat = Deflate([0x00, 0xFF, 0x00, 0x00]);
        WriteChunk(ms, "IDAT", idat);
        WriteChunk(ms, "IEND", []);
        return ms.ToArray();
    }

    private static void WriteChunk(System.IO.Stream stream, string type, byte[] data)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        Span<byte> len = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(len, (uint)data.Length);
        stream.Write(len);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = Crc32(typeBytes, data);
        Span<byte> crcBytes = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }

    private static byte[] Deflate(byte[] data)
    {
        using var output = new System.IO.MemoryStream();
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
