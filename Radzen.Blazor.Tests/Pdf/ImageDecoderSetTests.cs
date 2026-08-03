#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ImageDecoderSetTests
{
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF, 0xE0];
    private static readonly byte[] Jpeg2000CodestreamMagic = [0xFF, 0x4F, 0xFF, 0x51];

    private static IImageDecoder Decoder(string format) => format switch
    {
        "png" => new PngImageDecoder(),
        "jpeg" => new JpegImageDecoder(),
        _ => new Jpeg2000ImageDecoder(),
    };

    private static byte[] Magic(string format) => format switch
    {
        "png" => PngMagic,
        "jpeg" => JpegMagic,
        _ => Jpeg2000CodestreamMagic,
    };

    [Theory]
    [InlineData("png", "jpeg")]
    [InlineData("png", "jpeg2000")]
    [InlineData("jpeg", "png")]
    [InlineData("jpeg2000", "png")]
    public void Decoder_YieldsOnForeignMagic(string decoder, string magic)
    {
        Assert.False(Decoder(decoder).TryDecode(Magic(magic), ReaderLimits.Default, out var image));
        Assert.Null(image);
    }

    [Fact]
    public void PngDecoder_ClaimsThenDecodesOwnMagic()
        => Assert.Throws<InvalidDataException>(
            () => new PngImageDecoder().TryDecode(PngMagic, ReaderLimits.Default, out _));

    [Fact]
    public void Jpeg2000Decoder_ClaimsThenDecodesOwnMagic()
        => Assert.Throws<InvalidDataException>(
            () => new Jpeg2000ImageDecoder().TryDecode(Jpeg2000CodestreamMagic, ReaderLimits.Default, out _));

    [Fact]
    public void Decode_UnrecognizedFormat_Throws()
        => Assert.Throws<NotSupportedException>(
            () => ImageDecoders.BuiltIn.Decode(new byte[] { 0x00, 0x01, 0x02, 0x03 }, ReaderLimits.Default));
}
