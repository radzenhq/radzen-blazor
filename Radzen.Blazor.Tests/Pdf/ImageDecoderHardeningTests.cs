#nullable enable
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
namespace Radzen.Blazor.Pdf.Tests;

public class ImageDecoderHardeningTests
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void Png_OversizedDimensions_ThrowsFast()
    {
        var png = PngHeaderOnly(30000, 20000, bitDepth: 8, colorType: 6);
        Assert.Throws<InvalidDataException>(() => ImageTestHelpers.Decode(png));
    }

    [Fact]
    public void Png_DimensionsThatWrapInt32_ThrowsFast()
    {
        var png = PngHeaderOnly(60000, 40000, bitDepth: 8, colorType: 6);
        Assert.Throws<InvalidDataException>(() => ImageTestHelpers.Decode(png));
    }

    [Fact]
    public void Png_ZeroDimension_Throws()
    {
        var png = PngHeaderOnly(0, 16, bitDepth: 8, colorType: 2);
        Assert.Throws<InvalidDataException>(() => ImageTestHelpers.Decode(png));
    }

    [Fact]
    public void Png_PaletteLengthPastBuffer_ThrowsFast()
    {
        using var ms = new MemoryStream();
        ms.Write(Signature);
        WriteChunk(ms, "IHDR", Ihdr(1, 1, bitDepth: 8, colorType: 3));
        WriteRawChunk(ms, "PLTE", declaredLength: 0x80000000u, body: [0x00, 0x00, 0x00]);
        WriteChunk(ms, "IEND", []);

        Assert.Throws<InvalidDataException>(() => ImageTestHelpers.Decode(ms.ToArray()));
    }

    [Fact]
    public void Jpeg_TruncatedStartOfFrame_ThrowsFast()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x0B, 0x08];
        Assert.Throws<InvalidDataException>(() => ImageTestHelpers.Decode(jpeg));
    }

    [Theory]
    [InlineData("Images/rgb.png")]
    [InlineData("Images/gray.png")]
    [InlineData("Images/palette.png")]
    [InlineData("Images/alpha.png")]
    public void ValidPng_StillDecodes(string resource)
    {
        var xobj = ImageTestHelpers.Xobject(ImageTestHelpers.Decode(PdfTestResources.ReadAllBytes(resource)));
        Assert.Equal(64, ImageTestHelpers.Int(xobj.Image.Dictionary, "Width"));
        Assert.Equal(64, ImageTestHelpers.Int(xobj.Image.Dictionary, "Height"));
    }

    [Fact]
    public void ValidJpeg_StillDecodes()
    {
        var xobj = ImageTestHelpers.Xobject(ImageTestHelpers.Decode(PdfTestResources.ReadAllBytes("Images/rgb.jpg")));
        Assert.Equal(64, ImageTestHelpers.Int(xobj.Image.Dictionary, "Width"));
        Assert.Equal("DCTDecode", ImageTestHelpers.Name(xobj.Image.Dictionary, "Filter"));
    }

    [Fact]
    public void Png_TruncatedIdat_Throws()
    {
        using var ms = new MemoryStream();
        ms.Write(Signature);
        WriteChunk(ms, "IHDR", Ihdr(4, 4, 8, 6));
        WriteChunk(ms, "IDAT", FlateFilter.Encode(new byte[17]));
        WriteChunk(ms, "IEND", []);

        Assert.Throws<InvalidDataException>(() => ImageTestHelpers.Decode(ms.ToArray()));
    }

    [Fact]
    public void ImageDecoder_HonorsTightenedMaxImagePixels()
    {
        var png = PdfTestResources.ReadAllBytes("Images/rgb.png");
        var tight = new ReaderLimits { MaxImagePixels = 100 };
        Assert.Throws<InvalidDataException>(() => ImageTestHelpers.Decode(png, tight));
        Assert.NotNull(ImageTestHelpers.Decode(png));
    }

    [Fact]
    public void TightenedMaxImagePixels_ReachesMeasurementAndDecodingAlike()
    {
        var png = PdfTestResources.ReadAllBytes("Images/rgb.png");
        var tight = new ReaderLimits { MaxImagePixels = 100 };
        var decoders = ImageDecoders.BuiltIn.WithLimits(tight);

        var measured = Assert.Throws<InvalidDataException>(() => decoders.Probes.PixelSize(png));
        var decoded = Assert.Throws<InvalidDataException>(() => ImageTestHelpers.Decode(png, tight));
        Assert.Equal(decoded.Message, measured.Message);

        var document = new Document();
        document.Sections.Add().Blocks.AddImage(new MemoryStream(png));

        Assert.Throws<InvalidDataException>(
            () => new DocumentRenderer { ImageDecoders = decoders }.Render(document));
        Assert.NotNull(new DocumentRenderer().Render(document));
    }

    [Fact]
    public void LoadFromStream_CarriesMaxImagePixelsIntoLoadedImageDecoding()
    {
        var source = new Document();
        source.Sections.Add().Blocks.AddParagraph("body");
        var bytes = new DocumentRenderer().ToArray(source);
        var watermark = new Watermark { Text = "DRAFT" };
        watermark.SetImage(PdfTestResources.Open("Images/rgb.jpg"));

        using var tightStream = new MemoryStream(bytes);
        var tight = PortableDocument.LoadFromStream(tightStream, new ReaderLimits { MaxImagePixels = 4 });
        tight.AddWatermark(watermark);
        Assert.Throws<InvalidDataException>(() => tight.ToArray());

        using var defaultStream = new MemoryStream(bytes);
        var loaded = PortableDocument.LoadFromStream(defaultStream);
        loaded.AddWatermark(watermark);
        Assert.NotEmpty(loaded.ToArray());
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
        stream.Write(stackalloc byte[4]);
    }
}
