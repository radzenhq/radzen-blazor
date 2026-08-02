#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
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

    [Fact]
    public void AddingTheSameDecoderAgainDoesNotAddAnotherProbe()
    {
        var decoder = new CountingDecoder();
        var decoders = ImageDecoders.BuiltIn.Add(decoder).Add(decoder);

        Assert.Throws<NotSupportedException>(
            () => decoders.Decode(new byte[] { 0xEF, 0xFE, 0xFD }, ReaderLimits.Default));
        Assert.Equal(1, decoder.Probes);
        Assert.Equal(1, decoders.Probes.Count);
    }

    [Fact]
    public void AddingADecoderDoesNotMutateTheBuiltInSet()
    {
        var payload = new byte[] { 0x5A, 0x5A, 0xEE, 0x00 };
        var decoders = ImageDecoders.BuiltIn.Add(new StubDecoder([0x5A, 0x5A, 0xEE]));

        Assert.NotNull(decoders.Decode(payload, ReaderLimits.Default));
        Assert.Throws<NotSupportedException>(() => ImageDecoders.BuiltIn.Decode(payload, ReaderLimits.Default));
    }

    private sealed class StubDecoder(byte[] magic) : IImageDecoder
    {
        public bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out DecodedImage? image)
        {
            if (!data.Span.StartsWith(magic))
            {
                image = null;
                return false;
            }

            image = new DecodedImage(new byte[] { 0xFF }, 1, 1, 8, ImageColorSpace.DeviceGray);
            return true;
        }
    }

    private sealed class CountingDecoder : IImageDecoder
    {
        public int Probes;

        public bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out DecodedImage? image)
        {
            Interlocked.Increment(ref Probes);
            image = null;
            return false;
        }
    }
}
