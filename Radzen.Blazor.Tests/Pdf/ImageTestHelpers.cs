#nullable enable
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Blazor.Pdf.Tests;

internal static class ImageTestHelpers
{
    public static string Name(DictionaryObject dict, string key) => ((NameObject)dict[key]).Value;

    public static int Int(DictionaryObject dict, string key) => ((NumberObject)dict[key]).IntValue;

    // Minimal 1-bit greyscale PNG (colour type 0, bit depth 1): one filter byte plus
    // ceil(width/8) packed sample bytes per row, zlib-deflated into a single IDAT.
    public static byte[] OneBitGrayPng(int width, int height)
    {
        var rowBytes = ((width * 1) + 7) / 8;
        var raw = new byte[height * (rowBytes + 1)];
        for (var y = 0; y < height; y++)
        {
            var rowStart = y * (rowBytes + 1);
            raw[rowStart] = 0;
            for (var b = 0; b < rowBytes; b++)
            {
                raw[rowStart + 1 + b] = (byte)((y % 2 == 0) ? 0xAA : 0x55);
            }
        }

        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        byte[] ihdr =
        [
            (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
            (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height,
            0x01, 0x00, 0x00, 0x00, 0x00,
        ];
        WriteChunk(ms, "IHDR", ihdr);
        WriteChunk(ms, "IDAT", Deflate(raw));
        WriteChunk(ms, "IEND", []);
        return ms.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        stream.Write(length);
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
        var crc = 0xFFFFFFFFu;
        crc = Crc32Update(crc, type);
        crc = Crc32Update(crc, data);
        return crc ^ 0xFFFFFFFFu;
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
