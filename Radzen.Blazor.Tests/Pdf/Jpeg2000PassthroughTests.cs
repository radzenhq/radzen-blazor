#nullable enable
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
namespace Radzen.Blazor.Pdf.Tests;

public class Jpeg2000PassthroughTests
{
    private static readonly byte[] SignatureBox =
        [0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A];

    [Fact]
    public void RawCodestream_JpxDecodePassthrough()
    {
        var bytes = Concat(RawCodestream(4, 3, components: 3), [0xDE, 0xAD, 0xFF, 0xD9]);

        var xobj = ImageDecoder.Decode(bytes);
        var dict = xobj.Image.Dictionary;

        Assert.Equal("XObject", ImageTestHelpers.Name(dict, "Type"));
        Assert.Equal("Image", ImageTestHelpers.Name(dict, "Subtype"));
        Assert.Equal(4, ImageTestHelpers.Int(dict, "Width"));
        Assert.Equal(3, ImageTestHelpers.Int(dict, "Height"));
        Assert.Equal("JPXDecode", ImageTestHelpers.Name(dict, "Filter"));
        Assert.Equal(8, ImageTestHelpers.Int(dict, "BitsPerComponent"));
        Assert.False(dict.ContainsKey("ColorSpace"));

        Assert.Equal(bytes, xobj.Image.Data);
        Assert.Null(xobj.SoftMask);
    }

    [Fact]
    public void Jp2File_JpxDecodePassthrough()
    {
        var bytes = Jp2File(width: 5, height: 7, components: 3);

        var xobj = ImageDecoder.Decode(bytes);
        var dict = xobj.Image.Dictionary;

        Assert.Equal("JPXDecode", ImageTestHelpers.Name(dict, "Filter"));
        Assert.Equal(5, ImageTestHelpers.Int(dict, "Width"));
        Assert.Equal(7, ImageTestHelpers.Int(dict, "Height"));
        Assert.False(dict.ContainsKey("ColorSpace"));
        Assert.Equal(bytes, xobj.Image.Data);
        Assert.Null(xobj.SoftMask);
    }

    [Fact]
    public void Jp2FileWithoutIhdr_ReadsSizFromCodestream()
    {
        var bytes = Jp2FileCodestreamOnly(6, 2, components: 1);

        var xobj = ImageDecoder.Decode(bytes);
        var dict = xobj.Image.Dictionary;

        Assert.Equal("JPXDecode", ImageTestHelpers.Name(dict, "Filter"));
        Assert.Equal(6, ImageTestHelpers.Int(dict, "Width"));
        Assert.Equal(2, ImageTestHelpers.Int(dict, "Height"));
    }

    [Fact]
    public void RawCodestream_OversizedDimensions_ThrowsFast()
    {
        var bytes = RawCodestream(70000, 70000, components: 3);
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(bytes));
    }

    [Fact]
    public void Jp2File_OversizedDimensions_ThrowsFast()
    {
        var bytes = Jp2File(70000, 70000, components: 3);
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(bytes));
    }

    [Fact]
    public void RawCodestream_ZeroDimension_Throws()
    {
        var bytes = RawCodestream(0, 8, components: 3);
        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(bytes));
    }

    [Theory]
    [InlineData("Images/rgb.png")]
    [InlineData("Images/rgb.jpg")]
    public void ExistingFormats_StillDecode(string resource)
    {
        var xobj = ImageDecoder.Decode(PdfTestResources.ReadAllBytes(resource));
        Assert.Equal(64, ImageTestHelpers.Int(xobj.Image.Dictionary, "Width"));
        Assert.Equal(64, ImageTestHelpers.Int(xobj.Image.Dictionary, "Height"));
        Assert.NotEqual("JPXDecode", ImageTestHelpers.Name(xobj.Image.Dictionary, "Filter"));
    }

    private static byte[] RawCodestream(int width, int height, int components)
    {
        var siz = Siz(width, height, components);
        return Concat([0xFF, 0x4F], siz);
    }

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
        WriteBox(ms, "jp2c", RawCodestream(width, height, components));
        return ms.ToArray();
    }

    private static byte[] Jp2FileCodestreamOnly(int width, int height, int components)
    {
        using var ms = new MemoryStream();
        ms.Write(SignatureBox);
        WriteBox(ms, "jp2c", RawCodestream(width, height, components));
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
