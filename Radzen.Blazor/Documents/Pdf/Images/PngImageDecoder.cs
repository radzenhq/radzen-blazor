using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Radzen.Documents.Pdf;

internal sealed class PngImageDecoder : IImageDecoder
{
    public bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out DecodedImage? image)
    {
        if (!ImageHeaders.IsPng(data.Span))
        {
            image = null;
            return false;
        }

        image = DecodePng(data, limits);
        return true;
    }

    public bool TryReadPixelSize(ReadOnlyMemory<byte> data, ReaderLimits limits, out int width, out int height)
    {
        if (!ImageHeaders.IsPng(data.Span))
        {
            width = 0;
            height = 0;
            return false;
        }

        var header = ImageHeaders.ReadPngHeader(data.Span);
        width = header.Width;
        height = header.Height;
        return true;
    }

    private static ReadOnlyMemory<byte> JoinIdat(ReadOnlyMemory<byte> data, List<Range>? chunks)
    {
        if (chunks is null)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        if (chunks.Count == 1)
        {
            return data[chunks[0]];
        }

        var total = 0;
        foreach (var chunk in chunks)
        {
            total += chunk.End.Value - chunk.Start.Value;
        }

        var joined = new byte[total];
        var offset = 0;
        foreach (var chunk in chunks)
        {
            var span = data.Span[chunk];
            span.CopyTo(joined.AsSpan(offset));
            offset += span.Length;
        }

        return joined;
    }

    private static DecodedImage DecodePng(ReadOnlyMemory<byte> data, ReaderLimits limits)
    {
        var width = 0;
        var height = 0;
        var bitDepth = 0;
        var colorType = 0;
        byte[]? palette = null;
        byte[]? transparency = null;
        List<Range>? idat = null;

        var chunks = new PngChunkReader(data.Span);
        while (chunks.MoveNext())
        {
            var start = chunks.Start;
            var count = chunks.Count;

            switch (chunks.Type)
            {
                case "IHDR":
                    var header = ImageHeaders.ReadIhdr(data.Span, start, count);
                    width = header.Width;
                    height = header.Height;
                    bitDepth = header.BitDepth;
                    colorType = header.ColorType;
                    if (header.Interlace != 0)
                    {
                        throw new NotSupportedException("Adam7 interlaced PNG images are not supported.");
                    }

                    break;
                case "PLTE":
                    palette = data.Span[start..(start + count)].ToArray();
                    break;
                case "tRNS":
                    transparency = data.Span[start..(start + count)].ToArray();
                    break;
                case "IDAT":
                    (idat ??= []).Add(new Range(start, start + count));
                    break;
            }
        }

        limits.ValidateImageDimensions(width, height, "PNG");

        var channels = colorType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => throw new NotSupportedException($"Unsupported PNG color type {colorType}."),
        };

        ValidatePngBitDepth(colorType, bitDepth);

        var compressed = JoinIdat(data, idat);
        var raw = FlateFilter.Decode(compressed, limits.MaxDecodedStreamBytes);

        if (StreamDecoder.ExceedsExpansionRatio(raw.LongLength, compressed.Length, limits))
        {
            throw new InvalidDataException("PNG image data expansion ratio exceeds the maximum.");
        }

        var samples = PngPredictor.Decode(raw, channels, bitDepth, width);

        var bytesPerRow = (((long)width * channels * bitDepth) + 7) / 8;
        if (samples.Length < (long)height * bytesPerRow)
        {
            throw new InvalidDataException("PNG image data is truncated.");
        }

        return colorType switch
        {
            0 => BuildColorKeyedPng(width, height, bitDepth, ImageColorSpace.DeviceGray, samples, transparency, 1),
            2 => BuildColorKeyedPng(width, height, bitDepth, ImageColorSpace.DeviceRgb, samples, transparency, 3),
            3 => BuildPalettedPng(width, height, bitDepth, samples, palette, transparency),
            4 => BuildAlphaPng(width, height, ImageColorSpace.DeviceGray, samples, 1, bitDepth),
            6 => BuildAlphaPng(width, height, ImageColorSpace.DeviceRgb, samples, 3, bitDepth),
            _ => throw new NotSupportedException($"Unsupported PNG color type {colorType}."),
        };
    }

    // ISO/IEC 15948 Table 11.1: allowed bit depths per color type.
    private static void ValidatePngBitDepth(int colorType, int bitDepth)
    {
        var valid = colorType switch
        {
            0 => bitDepth is 1 or 2 or 4 or 8 or 16,
            2 => bitDepth is 8 or 16,
            3 => bitDepth is 1 or 2 or 4 or 8,
            4 => bitDepth is 8 or 16,
            6 => bitDepth is 8 or 16,
            _ => false,
        };

        if (!valid)
        {
            throw new InvalidDataException($"PNG bit depth {bitDepth} is not allowed for color type {colorType}.");
        }
    }

    // ISO 32000-1 8.9.6.4: a tRNS color key on grayscale/truecolor maps to /Mask.
    private static DecodedImage BuildColorKeyedPng(
        int width, int height, int bitDepth, ImageColorSpace colorSpace, byte[] samples, byte[]? transparency, int components)
    {
        var colorKey = ImmutableArray<int>.Empty;
        if (transparency is not null)
        {
            if (transparency.Length < 2 * components)
            {
                throw new InvalidDataException("PNG tRNS chunk is truncated for the color type.");
            }

            var builder = ImmutableArray.CreateBuilder<int>(2 * components);
            for (var c = 0; c < components; c++)
            {
                var value = BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(c * 2));
                builder.Add(value);
                builder.Add(value);
            }

            colorKey = builder.MoveToImmutable();
        }

        return new DecodedImage(samples, width, height, bitDepth, colorSpace) { ColorKeyMask = colorKey };
    }

    private static DecodedImage BuildPalettedPng(
        int width,
        int height,
        int bitDepth,
        byte[] indices,
        byte[]? palette,
        byte[]? transparency)
    {
        if (palette is null)
        {
            throw new InvalidDataException("Paletted PNG is missing its PLTE chunk.");
        }

        if (palette.Length == 0 || palette.Length % 3 != 0 || palette.Length / 3 > 256)
        {
            throw new InvalidDataException("PNG PLTE chunk must hold 1 to 256 RGB triples.");
        }

        if (transparency is null)
        {
            return new DecodedImage(indices, width, height, bitDepth, ImageColorSpace.Indexed) { Palette = palette };
        }

        var pixels = UnpackIndices(indices, width, height, bitDepth);
        var alpha = new byte[width * height];
        for (var i = 0; i < alpha.Length; i++)
        {
            var index = pixels[i];
            alpha[i] = index < transparency.Length ? transparency[index] : (byte)0xFF;
        }

        return new DecodedImage(indices, width, height, bitDepth, ImageColorSpace.Indexed)
        {
            Palette = palette,
            Alpha = Gray(width, height, alpha),
        };
    }

    private static byte[] UnpackIndices(byte[] indices, int width, int height, int bitDepth)
    {
        if (bitDepth == 8)
        {
            return indices;
        }

        var rowLength = ((width * bitDepth) + 7) / 8;
        var mask = (1 << bitDepth) - 1;
        var pixels = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            var rowStart = y * rowLength;
            for (var x = 0; x < width; x++)
            {
                var bit = x * bitDepth;
                var shift = 8 - bitDepth - (bit % 8);
                pixels[(y * width) + x] = (byte)((indices[rowStart + (bit / 8)] >> shift) & mask);
            }
        }

        return pixels;
    }

    private static DecodedImage BuildAlphaPng(
        int width, int height, ImageColorSpace colorSpace, byte[] samples, int colorChannels, int bitDepth)
    {
        var pixelCount = width * height;
        var bytesPerSample = bitDepth / 8;
        var stride = (colorChannels + 1) * bytesPerSample;
        var color = new byte[pixelCount * colorChannels];
        var alpha = new byte[pixelCount];

        for (var i = 0; i < pixelCount; i++)
        {
            for (var c = 0; c < colorChannels; c++)
            {
                color[(i * colorChannels) + c] = samples[(i * stride) + (c * bytesPerSample)];
            }

            alpha[i] = samples[(i * stride) + (colorChannels * bytesPerSample)];
        }

        return new DecodedImage(color, width, height, 8, colorSpace) { Alpha = Gray(width, height, alpha) };
    }

    private static DecodedImage Gray(int width, int height, byte[] samples)
        => new(samples, width, height, 8, ImageColorSpace.DeviceGray);
}
