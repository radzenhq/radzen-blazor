#nullable enable
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ImageProbeParityTests
{
    private static readonly byte[] SignatureBox =
        [0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A];

    public static TheoryData<string> ImageResources =>
    [
        "Images/rgb.png",
        "Images/gray.png",
        "Images/palette.png",
        "Images/palette-trns.png",
        "Images/alpha.png",
        "Images/rgb.jpg",
        "Images/gray.jpg",
        "Images/cmyk.jpg",
    ];

    private static byte[] Bytes(string resource) => PdfTestResources.ReadAllBytes(resource);

    [Theory]
    [MemberData(nameof(ImageResources))]
    public void PixelSize_MatchesDecoder(string resource)
    {
        var data = Bytes(resource);
        var expected = ImageTestHelpers.PixelSize(ImageTestHelpers.Decode(data));

        Assert.Equal(expected, ImageProbes.None.PixelSize(data));
    }

    [Theory]
    [InlineData(4, 3, 3)]
    [InlineData(64, 64, 1)]
    [InlineData(1, 1, 4)]
    public void RawJpeg2000Codestream_PixelSizeMatchesDecoder(int width, int height, int components)
    {
        var data = Concat([0xFF, 0x4F], Siz(width, height, components));

        Assert.Equal(ImageTestHelpers.PixelSize(ImageTestHelpers.Decode(data)), ImageProbes.None.PixelSize(data));
    }

    [Theory]
    [InlineData(5, 7, 3)]
    [InlineData(64, 32, 1)]
    public void Jp2FileWithIhdr_PixelSizeMatchesDecoder(int width, int height, int components)
    {
        var data = Jp2File(width, height, components);

        Assert.Equal(ImageTestHelpers.PixelSize(ImageTestHelpers.Decode(data)), ImageProbes.None.PixelSize(data));
    }

    [Fact]
    public void Jp2FileWithoutIhdr_PixelSizeMatchesDecoder()
    {
        var data = Jp2FileCodestreamOnly(6, 2, components: 1);

        Assert.Equal(ImageTestHelpers.PixelSize(ImageTestHelpers.Decode(data)), ImageProbes.None.PixelSize(data));
    }

    [Fact]
    public void UnrecognizedFormat_Throws()
        => Assert.Throws<NotSupportedException>(() => ImageProbes.None.PixelSize([0x00, 0x01, 0x02, 0x03]));

    [Fact]
    public void TruncatedPng_Throws()
        => Assert.Throws<InvalidDataException>(
            () => ImageProbes.None.PixelSize([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));

    [Fact]
    public void JpegWithoutStartOfFrame_Throws()
        => Assert.Throws<InvalidDataException>(() => ImageProbes.None.PixelSize([0xFF, 0xD8, 0xFF, 0xD9]));

    private static byte[] Siz(int width, int height, int components)
    {
        var lsiz = 38 + (3 * components);
        var siz = new byte[2 + lsiz];
        siz[0] = 0xFF;
        siz[1] = 0x51;
        BinaryPrimitives.WriteUInt16BigEndian(siz.AsSpan(2), (ushort)lsiz);
        BinaryPrimitives.WriteUInt32BigEndian(siz.AsSpan(6), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(siz.AsSpan(10), (uint)height);
        BinaryPrimitives.WriteUInt32BigEndian(siz.AsSpan(22), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(siz.AsSpan(26), (uint)height);
        BinaryPrimitives.WriteUInt16BigEndian(siz.AsSpan(38), (ushort)components);
        for (var c = 0; c < components; c++)
        {
            var o = 40 + (3 * c);
            siz[o] = 0x07;
            siz[o + 1] = 0x01;
            siz[o + 2] = 0x01;
        }

        return siz;
    }

    private static byte[] Jp2File(int width, int height, int components)
    {
        using var ms = new MemoryStream();
        ms.Write(SignatureBox);
        WriteBox(ms, "jp2h", Ihdr(width, height, components));
        WriteBox(ms, "jp2c", Concat([0xFF, 0x4F], Siz(width, height, components)));
        return ms.ToArray();
    }

    private static byte[] Jp2FileCodestreamOnly(int width, int height, int components)
    {
        using var ms = new MemoryStream();
        ms.Write(SignatureBox);
        WriteBox(ms, "jp2c", Concat([0xFF, 0x4F], Siz(width, height, components)));
        return ms.ToArray();
    }

    private static byte[] Ihdr(int width, int height, int components)
    {
        var content = new byte[14];
        BinaryPrimitives.WriteUInt32BigEndian(content.AsSpan(0), (uint)height);
        BinaryPrimitives.WriteUInt32BigEndian(content.AsSpan(4), (uint)width);
        BinaryPrimitives.WriteUInt16BigEndian(content.AsSpan(8), (ushort)components);
        content[10] = 0x07;
        content[11] = 0x00;
        using var ms = new MemoryStream();
        WriteBox(ms, "ihdr", content);
        return ms.ToArray();
    }

    private static void WriteBox(Stream stream, string type, byte[] content)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)(8 + content.Length));
        stream.Write(length);
        stream.Write(Encoding.ASCII.GetBytes(type));
        stream.Write(content);
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, result, 0, a.Length);
        Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
        return result;
    }
}
