#nullable enable
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// Hardening for ImageDecoder against hostile PNG/JPEG headers: oversized or
// wrapped IHDR dimensions, a PLTE chunk length that runs off the buffer, and a
// truncated JPEG frame must all be rejected quickly with InvalidDataException
// (the decoder's own failure type) rather than OOM or an unbounded slice/read.
// Positive controls prove the ordinary fixtures still decode unchanged.
public class ImageDecoderHardeningTests
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // 30000 x 20000 RGBA = 600M pixels, an ~1.8GB decode from a 65-byte header.
    [Fact]
    public void Png_OversizedDimensions_ThrowsFast()
    {
        var png = PngHeaderOnly(30000, 20000, bitDepth: 8, colorType: 6);
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(png));
    }

    // 60000 x 40000 overflows a 32-bit pixel count; the checked long product rejects it.
    [Fact]
    public void Png_DimensionsThatWrapInt32_ThrowsFast()
    {
        var png = PngHeaderOnly(60000, 40000, bitDepth: 8, colorType: 6);
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(png));
    }

    [Fact]
    public void Png_ZeroDimension_Throws()
    {
        var png = PngHeaderOnly(0, 16, bitDepth: 8, colorType: 2);
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(png));
    }

    // A PLTE claiming 0x80000000 bytes must be rejected, not sliced out of range.
    [Fact]
    public void Png_PaletteLengthPastBuffer_ThrowsFast()
    {
        using var ms = new MemoryStream();
        ms.Write(Signature);
        WriteChunk(ms, "IHDR", Ihdr(1, 1, bitDepth: 8, colorType: 3));
        WriteRawChunk(ms, "PLTE", declaredLength: 0x80000000u, body: [0x00, 0x00, 0x00]);
        WriteChunk(ms, "IEND", []);

        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(ms.ToArray()));
    }

    // 7-byte JPEG: a start-of-frame marker with no room for its frame header.
    [Fact]
    public void Jpeg_TruncatedStartOfFrame_ThrowsFast()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x0B, 0x08];
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(jpeg));
    }

    [Theory]
    [InlineData("Images/rgb.png")]
    [InlineData("Images/gray.png")]
    [InlineData("Images/palette.png")]
    [InlineData("Images/alpha.png")]
    public void ValidPng_StillDecodes(string resource)
    {
        var xobj = ImageDecoder.Decode(PdfTestResources.ReadAllBytes(resource));
        Assert.Equal(64, ImageTestHelpers.Int(xobj.Image.Dictionary, "Width"));
        Assert.Equal(64, ImageTestHelpers.Int(xobj.Image.Dictionary, "Height"));
    }

    [Fact]
    public void ValidJpeg_StillDecodes()
    {
        var xobj = ImageDecoder.Decode(PdfTestResources.ReadAllBytes("Images/rgb.jpg"));
        Assert.Equal(64, ImageTestHelpers.Int(xobj.Image.Dictionary, "Width"));
        Assert.Equal("DCTDecode", ImageTestHelpers.Name(xobj.Image.Dictionary, "Filter"));
    }

    private static byte[] PngHeaderOnly(int width, int height, byte bitDepth, byte colorType)
    {
        using var ms = new MemoryStream();
        ms.Write(Signature);
        WriteChunk(ms, "IHDR", Ihdr(width, height, bitDepth, colorType));
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
        => WriteRawChunk(stream, type, (uint)body.Length, body);

    private static void WriteRawChunk(Stream stream, string type, uint declaredLength, byte[] body)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, declaredLength);
        stream.Write(length);
        stream.Write(Encoding.ASCII.GetBytes(type));
        stream.Write(body);
        stream.Write(stackalloc byte[4]); // CRC placeholder; the decoder does not verify it.
    }
}
