#nullable enable
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

public class PngAlphaBitDepthTests
{
    [Fact]
    public void SixteenBitGrayAlpha_DownsamplesHighBytes()
    {
        // 2x1 gray+alpha, big-endian: p0 gray=0xAABB a=0xCCDD, p1 gray=0x1122 a=0x3344.
        byte[] scanline = [0x00, 0xAA, 0xBB, 0xCC, 0xDD, 0x11, 0x22, 0x33, 0x44];
        var png = BuildPng(2, 1, 16, colorType: 4, scanline);

        var xobj = ImageDecoder.Decode(png);

        Assert.Equal("DeviceGray", ImageTestHelpers.Name(xobj.Image.Dictionary, "ColorSpace"));
        Assert.Equal(8, ImageTestHelpers.Int(xobj.Image.Dictionary, "BitsPerComponent"));

        var color = FlateFilter.Decode(xobj.Image.Data);
        Assert.Equal(new byte[] { 0xAA, 0x11 }, color);

        var alpha = FlateFilter.Decode(xobj.SoftMask!.Data);
        Assert.Equal(new byte[] { 0xCC, 0x33 }, alpha);
    }

    [Fact]
    public void SixteenBitRgba_DownsamplesHighBytes()
    {
        // 2x1 RGBA, big-endian high bytes: p0 = 10 20 30 / a 40, p1 = 50 60 70 / a 80.
        byte[] scanline =
        [
            0x00,
            0x10, 0x01, 0x20, 0x02, 0x30, 0x03, 0x40, 0x04,
            0x50, 0x05, 0x60, 0x06, 0x70, 0x07, 0x80, 0x08,
        ];
        var png = BuildPng(2, 1, 16, colorType: 6, scanline);

        var xobj = ImageDecoder.Decode(png);

        Assert.Equal("DeviceRGB", ImageTestHelpers.Name(xobj.Image.Dictionary, "ColorSpace"));
        Assert.Equal(8, ImageTestHelpers.Int(xobj.Image.Dictionary, "BitsPerComponent"));

        var color = FlateFilter.Decode(xobj.Image.Data);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30, 0x50, 0x60, 0x70 }, color);

        var alpha = FlateFilter.Decode(xobj.SoftMask!.Data);
        Assert.Equal(new byte[] { 0x40, 0x80 }, alpha);
    }

    [Fact]
    public void EightBitRgba_DecodesUnchanged()
    {
        // 2x1 RGBA 8-bit: p0 = 10 20 30 a40, p1 = 50 60 70 a80.
        byte[] scanline = [0x00, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80];
        var png = BuildPng(2, 1, 8, colorType: 6, scanline);

        var xobj = ImageDecoder.Decode(png);

        Assert.Equal(8, ImageTestHelpers.Int(xobj.Image.Dictionary, "BitsPerComponent"));

        var color = FlateFilter.Decode(xobj.Image.Data);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30, 0x50, 0x60, 0x70 }, color);

        var alpha = FlateFilter.Decode(xobj.SoftMask!.Data);
        Assert.Equal(new byte[] { 0x40, 0x80 }, alpha);
    }

    private static byte[] BuildPng(int width, int height, int bitDepth, int colorType, byte[] rawScanlines)
    {
        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        byte[] ihdr =
        [
            (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
            (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height,
            (byte)bitDepth,
            (byte)colorType,
            0x00,
            0x00,
            0x00,
        ];
        WriteChunk(ms, "IHDR", ihdr);
        WriteChunk(ms, "IDAT", Deflate(rawScanlines));
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
