#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Opt-in keys stamped on the image XObject dictionary: /Interpolate, the /ImageMask
// stencil transform, and /Mask colour-key masking. Each test builds a document through
// the public DocumentBuilder API, round-trips the bytes through DocumentReader and asserts
// the exact ISO 32000-1 construct on the reloaded XObject.
public class ImageXObjectOptionTests
{
    private static Image AddImage(DocumentBuilder builder, string resource)
    {
        var section = builder.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open(resource));
        image.Width = Unit.FromPoint(48);
        image.Height = Unit.FromPoint(48);
        return image;
    }

    private static DictionaryObject SingleImageDictionary(DocumentBuilder builder)
    {
        var reader = BuildTestSupport.Read(builder);
        var images = BuildTestSupport.ImageXObjects(reader);
        return Assert.Single(images).Dictionary;
    }

    [Fact]
    public void Interpolate_WhenSet_EmitsInterpolateTrueOnXObject()
    {
        var builder = new DocumentBuilder();
        AddImage(builder, "Images/gray.png").Interpolate = true;

        var dict = SingleImageDictionary(builder);

        Assert.True(dict.ContainsKey("Interpolate"), "image XObject is missing /Interpolate");
        Assert.True(Assert.IsType<BooleanObject>(dict["Interpolate"]).Value);
        // The colour image keeps its colour space; the flag is purely additive.
        Assert.Equal("DeviceGray", ((NameObject)dict["ColorSpace"]).Value);
    }

    [Fact]
    public void Interpolate_WhenUnset_OmitsInterpolateKey()
    {
        var builder = new DocumentBuilder();
        AddImage(builder, "Images/gray.png");

        var dict = SingleImageDictionary(builder);

        Assert.False(dict.ContainsKey("Interpolate"), "default image must not carry /Interpolate");
    }

    [Fact]
    public void DefaultImage_IsByteIdentical_AcrossBuilds()
    {
        static byte[] Build()
        {
            var builder = new DocumentBuilder();
            AddImage(builder, "Images/rgb.png");
            return builder.ToArray();
        }

        // An image that opts into none of the new XObject keys must take the unchanged
        // emission path, so two identical documents produce byte-for-byte identical output.
        Assert.Equal(Build(), Build());
    }

    [Fact]
    public void ColorKeyMask_WhenSet_EmitsMaskRangeArray()
    {
        var builder = new DocumentBuilder();
        AddImage(builder, "Images/rgb.png").ColorKeyMask = [0, 0, 0, 0, 0, 0];

        var dict = SingleImageDictionary(builder);

        var mask = Assert.IsType<ArrayObject>(dict["Mask"]);
        Assert.Equal(6, mask.Count);
        foreach (var entry in mask)
        {
            Assert.Equal(0, ((NumberObject)entry).IntValue);
        }

        Assert.Equal("DeviceRGB", ((NameObject)dict["ColorSpace"]).Value);
    }

    [Fact]
    public void ColorKeyMask_PreservesRangeValues()
    {
        var builder = new DocumentBuilder();
        AddImage(builder, "Images/rgb.png").ColorKeyMask = [10, 20, 30, 40, 50, 60];

        var mask = Assert.IsType<ArrayObject>(SingleImageDictionary(builder)["Mask"]);
        var values = new int[mask.Count];
        for (var i = 0; i < mask.Count; i++)
        {
            values[i] = ((NumberObject)mask[i]).IntValue;
        }

        Assert.Equal([10, 20, 30, 40, 50, 60], values);
    }

    [Fact]
    public void ColorKeyMask_WhenUnset_OmitsMaskKey()
    {
        var builder = new DocumentBuilder();
        AddImage(builder, "Images/rgb.png");

        Assert.False(SingleImageDictionary(builder).ContainsKey("Mask"), "default image must not carry /Mask");
    }

    [Fact]
    public void ColorKeyMask_WrongComponentCount_Throws()
    {
        var builder = new DocumentBuilder();
        AddImage(builder, "Images/rgb.png").ColorKeyMask = [0, 0];

        Assert.Throws<ArgumentException>(() => builder.ToArray());
    }

    private static Image AddImage(DocumentBuilder builder, byte[] png)
    {
        var section = builder.Sections.Add();
        using var stream = new MemoryStream(png);
        var image = section.Blocks.AddImage(stream);
        image.Width = Unit.FromPoint(48);
        image.Height = Unit.FromPoint(48);
        return image;
    }

    [Fact]
    public void Stencil_WhenSet_EmitsImageMaskWithoutColorSpace()
    {
        var builder = new DocumentBuilder();
        AddImage(builder, OneBitGrayPng(8, 8)).Stencil = true;

        var reader = BuildTestSupport.Read(builder);
        var dict = Assert.Single(BuildTestSupport.ImageXObjects(reader)).Dictionary;

        Assert.Equal("XObject", ((NameObject)dict["Type"]).Value);
        Assert.Equal("Image", ((NameObject)dict["Subtype"]).Value);
        Assert.True(Assert.IsType<BooleanObject>(dict["ImageMask"]).Value);
        Assert.False(dict.ContainsKey("ColorSpace"), "an image mask must not carry /ColorSpace");
        Assert.Equal(1, ((NumberObject)dict["BitsPerComponent"]).IntValue);
        Assert.Equal(8, ((NumberObject)dict["Width"]).IntValue);
        Assert.Equal(8, ((NumberObject)dict["Height"]).IntValue);

        var content = Encoding.Latin1.GetString(
            BuildTestSupport.Content(reader, BuildTestSupport.PageLeaves(reader)[0].Page));
        Assert.Contains(" Do", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Stencil_WhenUnset_KeepsColorSpaceAndOmitsImageMask()
    {
        var builder = new DocumentBuilder();
        AddImage(builder, OneBitGrayPng(8, 8));

        var dict = SingleImageDictionary(builder);

        Assert.Equal("DeviceGray", ((NameObject)dict["ColorSpace"]).Value);
        Assert.False(dict.ContainsKey("ImageMask"), "default image must not carry /ImageMask");
    }

    [Fact]
    public void Stencil_OnNonOneBitImage_Throws()
    {
        var builder = new DocumentBuilder();
        AddImage(builder, "Images/gray.png").Stencil = true;

        Assert.Throws<InvalidOperationException>(() => builder.ToArray());
    }

    // Minimal 1-bit greyscale PNG (colour type 0, bit depth 1): one filter byte plus
    // ceil(width/8) packed sample bytes per row, zlib-deflated into a single IDAT.
    private static byte[] OneBitGrayPng(int width, int height)
    {
        var rowBytes = ((width * 1) + 7) / 8;
        var raw = new byte[height * (rowBytes + 1)];
        for (var y = 0; y < height; y++)
        {
            var rowStart = y * (rowBytes + 1);
            raw[rowStart] = 0; // filter: none
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
            0x01, // bit depth
            0x00, // colour type 0 (greyscale)
            0x00, // compression
            0x00, // filter
            0x00, // interlace
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
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        stream.Write(length);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = Crc32(typeBytes, data);
        Span<byte> crcBytes = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
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
