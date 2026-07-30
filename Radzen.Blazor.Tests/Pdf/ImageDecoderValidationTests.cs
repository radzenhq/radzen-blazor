#nullable enable
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
namespace Radzen.Blazor.Pdf.Tests;

public class ImageDecoderValidationTests
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Theory]
    [InlineData(6, 4)]
    [InlineData(4, 1)]
    [InlineData(2, 4)]
    [InlineData(3, 16)]
    public void IllegalBitDepthForColorType_Throws(int colorType, int bitDepth)
    {
        var channels = colorType switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, _ => 4 };
        var rowBytes = ((channels * bitDepth) + 7) / 8;
        var scanline = new byte[1 + rowBytes];
        byte[]? palette = colorType == 3 ? [0, 0, 0] : null;
        var png = FullPng(1, 1, bitDepth, colorType, palette, trns: null, scanline);
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(png));
    }

    [Fact]
    public void IdatInflatesPastLimit_Throws()
    {
        var png = FullPng(1, 1, 8, colorType: 0, palette: null, trns: null, new byte[4096]);
        var limits = new ReaderLimits { MaxDecodedStreamBytes = 1024 };
        Assert.Throws<DocumentParseException>(() => ImageDecoder.Decode(png, limits));
    }

    [Fact]
    public void EmptyPalette_Throws()
    {
        var png = FullPng(1, 1, 8, colorType: 3, palette: [], trns: null, [0x00, 0x00]);
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(png));
    }

    [Fact]
    public void PaletteNotMultipleOfThree_Throws()
    {
        var png = FullPng(1, 1, 8, colorType: 3, palette: [10, 20, 30, 40], trns: null, [0x00, 0x00]);
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(png));
    }

    [Fact]
    public void TruecolorTrns_BecomesColorKeyMask()
    {
        byte[] scanline = [0x00, 0x11, 0x22, 0x33];
        byte[] trns = [0x00, 0x10, 0x00, 0x20, 0x00, 0x30];
        var png = FullPng(1, 1, 8, colorType: 2, palette: null, trns, scanline);

        var xobj = ImageTestHelpers.Xobject(ImageDecoder.Decode(png));

        Assert.Null(xobj.SoftMask);
        var mask = Assert.IsType<ArrayObject>(xobj.Image.Dictionary["Mask"]);
        int[] expected = [16, 16, 32, 32, 48, 48];
        Assert.Equal(expected.Length, mask.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], Assert.IsType<NumberObject>(mask[i]).IntValue);
        }
    }

    [Fact]
    public void GrayscaleTrns_BecomesColorKeyMask()
    {
        byte[] scanline = [0x00, 0x80];
        byte[] trns = [0x00, 0x80];
        var png = FullPng(1, 1, 8, colorType: 0, palette: null, trns, scanline);

        var xobj = ImageTestHelpers.Xobject(ImageDecoder.Decode(png));

        var mask = Assert.IsType<ArrayObject>(xobj.Image.Dictionary["Mask"]);
        Assert.Equal(2, mask.Count);
        Assert.Equal(128, Assert.IsType<NumberObject>(mask[0]).IntValue);
        Assert.Equal(128, Assert.IsType<NumberObject>(mask[1]).IntValue);
    }

    [Fact]
    public void TruncatedTrns_Throws()
    {
        byte[] scanline = [0x00, 0x11, 0x22, 0x33];
        byte[] trns = [0x00, 0x10, 0x00];
        var png = FullPng(1, 1, 8, colorType: 2, palette: null, trns, scanline);
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(png));
    }

    [Fact]
    public void GrayscaleWithoutTrns_HasNoMask()
    {
        byte[] scanline = [0x00, 0x80];
        var png = FullPng(1, 1, 8, colorType: 0, palette: null, trns: null, scanline);

        var xobj = ImageTestHelpers.Xobject(ImageDecoder.Decode(png));

        Assert.False(xobj.Image.Dictionary.TryGetValue("Mask", out _));
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(8, 0)]
    public void JpegZeroDimension_Throws(int width, int height)
    {
        var jpeg = Jpeg(0xC0, width, height, components: 1, adobe: false);
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(jpeg));
    }

    [Fact]
    public void JpegOverPixelLimit_Throws()
    {
        var jpeg = Jpeg(0xC0, 1000, 1000, components: 1, adobe: false);
        var limits = new ReaderLimits { MaxImagePixels = 100 };
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(jpeg, limits));
    }

    [Fact]
    public void NonAdobeCmyk_HasNoInvertedDecode()
    {
        var jpeg = Jpeg(0xC0, 8, 8, components: 4, adobe: false);
        var xobj = ImageTestHelpers.Xobject(ImageDecoder.Decode(jpeg));
        Assert.Equal("DeviceCMYK", ImageTestHelpers.Name(xobj.Image.Dictionary, "ColorSpace"));
        Assert.False(xobj.Image.Dictionary.TryGetValue("Decode", out _));
    }

    [Fact]
    public void AdobeCmyk_HasInvertedDecode()
    {
        var jpeg = Jpeg(0xC0, 8, 8, components: 4, adobe: true);
        var xobj = ImageTestHelpers.Xobject(ImageDecoder.Decode(jpeg));
        Assert.IsType<ArrayObject>(xobj.Image.Dictionary["Decode"]);
    }

    [Theory]
    [InlineData(0xC3)]
    [InlineData(0xC9)]
    [InlineData(0xCB)]
    public void UndecodableSofMarker_Throws(int marker)
    {
        var jpeg = Jpeg((byte)marker, 8, 8, components: 1, adobe: false);
        Assert.Throws<NotSupportedException>(() => ImageDecoder.Decode(jpeg));
    }

    // ISO 32000-1 8.9.5.1: /BitsPerComponent allows only 1/2/4/8/16, so 12-bit JPEG (SOF1/SOF2) must fail loud.
    [Theory]
    [InlineData(0xC1)]
    [InlineData(0xC2)]
    public void TwelveBitJpeg_Throws(int marker)
    {
        var jpeg = Jpeg((byte)marker, 8, 8, components: 1, adobe: false, precision: 12);
        Assert.Throws<NotSupportedException>(() => ImageDecoder.Decode(jpeg));
    }

    [Theory]
    [InlineData(0xC0)]
    [InlineData(0xC1)]
    [InlineData(0xC2)]
    public void EightBitJpeg_EmitsLegalBitsPerComponent(int marker)
    {
        var jpeg = Jpeg((byte)marker, 8, 8, components: 1, adobe: false);
        var dict = ImageTestHelpers.Xobject(ImageDecoder.Decode(jpeg)).Image.Dictionary;
        Assert.Equal(8, ImageTestHelpers.Int(dict, "BitsPerComponent"));
    }

    private static byte[] FullPng(int width, int height, int bitDepth, int colorType, byte[]? palette, byte[]? trns, byte[] rawScanlines)
    {
        using var ms = new MemoryStream();
        ms.Write(Signature);
        WriteChunk(ms, "IHDR", Ihdr(width, height, (byte)bitDepth, (byte)colorType));
        if (palette is not null)
        {
            WriteChunk(ms, "PLTE", palette);
        }

        if (trns is not null)
        {
            WriteChunk(ms, "tRNS", trns);
        }

        WriteChunk(ms, "IDAT", Deflate(rawScanlines));
        WriteChunk(ms, "IEND", []);
        return ms.ToArray();
    }

    private static byte[] Ihdr(int width, int height, byte bitDepth, byte colorType)
    {
        var body = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(0), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(4), (uint)height);
        body[8] = bitDepth;
        body[9] = colorType;
        return body;
    }

    private static void WriteChunk(Stream stream, string type, byte[] body)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)body.Length);
        stream.Write(length);
        stream.Write(Encoding.ASCII.GetBytes(type));
        stream.Write(body);
        stream.Write(stackalloc byte[4]);
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

    private static byte[] Jpeg(byte sofMarker, int width, int height, int components, bool adobe, byte precision = 8)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0xFF);
        ms.WriteByte(0xD8);

        if (adobe)
        {
            WriteSegment(ms, 0xEE, [(byte)'A', (byte)'d', (byte)'o', (byte)'b', (byte)'e', 0, 0, 0, 0, 0, 0, 0]);
        }

        var sof = new byte[6 + (3 * components)];
        sof[0] = precision;
        sof[1] = (byte)(height >> 8);
        sof[2] = (byte)height;
        sof[3] = (byte)(width >> 8);
        sof[4] = (byte)width;
        sof[5] = (byte)components;
        WriteSegment(ms, sofMarker, sof);
        return ms.ToArray();
    }

    private static void WriteSegment(Stream stream, byte marker, byte[] body)
    {
        stream.WriteByte(0xFF);
        stream.WriteByte(marker);
        var length = body.Length + 2;
        stream.WriteByte((byte)(length >> 8));
        stream.WriteByte((byte)length);
        stream.Write(body);
    }
}
