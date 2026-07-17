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

public class PngPalettedTrnsBitDepthTests
{
    [Fact]
    public void FourBitIndexedWithTrns_YieldsPerPixelAlpha()
    {
        byte[] scanlines = [0x00, 0x01, 0x20, 0x00, 0x30, 0x10];
        byte[] palette = [10, 10, 10, 20, 20, 20, 30, 30, 30, 40, 40, 40];
        byte[] trns = [0x00, 0xFF];
        var png = BuildPng(3, 2, 4, palette, trns, scanlines);

        var xobj = ImageDecoder.Decode(png);

        var alpha = FlateFilter.Decode(xobj.SoftMask!.Data.ToArray());
        Assert.Equal(new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0x00, 0xFF }, alpha);
    }

    [Fact]
    public void OneBitIndexedWithTrns_YieldsPerPixelAlpha()
    {
        byte[] scanlines = [0x00, 0x50, 0x00, 0xC0];
        byte[] palette = [0, 0, 0, 255, 255, 255];
        byte[] trns = [0x00];
        var png = BuildPng(4, 2, 1, palette, trns, scanlines);

        var xobj = ImageDecoder.Decode(png);

        var alpha = FlateFilter.Decode(xobj.SoftMask!.Data.ToArray());
        Assert.Equal(new byte[] { 0x00, 0xFF, 0x00, 0xFF, 0xFF, 0xFF, 0x00, 0x00 }, alpha);
    }

    [Fact]
    public void EightBitIndexedWithTrns_DecodesUnchanged()
    {
        byte[] scanlines = [0x00, 0x00, 0x01];
        byte[] palette = [10, 10, 10, 20, 20, 20];
        byte[] trns = [0x00, 0xFF];
        var png = BuildPng(2, 1, 8, palette, trns, scanlines);

        var xobj = ImageDecoder.Decode(png);

        var alpha = FlateFilter.Decode(xobj.SoftMask!.Data.ToArray());
        Assert.Equal(new byte[] { 0x00, 0xFF }, alpha);
    }

    [Fact]
    public void FourBitIndexedWithoutTrns_HasNoSoftMask()
    {
        byte[] scanlines = [0x00, 0x01, 0x20, 0x00, 0x30, 0x10];
        byte[] palette = [10, 10, 10, 20, 20, 20, 30, 30, 30, 40, 40, 40];
        var png = BuildPng(3, 2, 4, palette, null, scanlines);

        var xobj = ImageDecoder.Decode(png);

        Assert.Null(xobj.SoftMask);
    }

    private static byte[] BuildPng(int width, int height, int bitDepth, byte[] palette, byte[]? trns, byte[] rawScanlines)
    {
        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        byte[] ihdr =
        [
            (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
            (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height,
            (byte)bitDepth,
            0x03,
            0x00,
            0x00,
            0x00,
        ];
        WriteChunk(ms, "IHDR", ihdr);
        WriteChunk(ms, "PLTE", palette);
        if (trns is not null)
        {
            WriteChunk(ms, "tRNS", trns);
        }

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
