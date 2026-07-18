using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class PngImageDecoder : IImageDecoder
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public bool TryDecode(byte[] data, ReaderLimits limits, [NotNullWhen(true)] out ImageXObject? xobject)
    {
        if (!IsPng(data))
        {
            xobject = null;
            return false;
        }

        xobject = DecodePng(data, limits);
        return true;
    }

    private static bool IsPng(byte[] data)
    {
        if (data.Length < PngSignature.Length)
        {
            return false;
        }

        for (var i = 0; i < PngSignature.Length; i++)
        {
            if (data[i] != PngSignature[i])
            {
                return false;
            }
        }

        return true;
    }

    private static ReadOnlyMemory<byte> JoinIdat(byte[] data, List<Range>? chunks)
    {
        if (chunks is null)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        if (chunks.Count == 1)
        {
            return data.AsMemory(chunks[0]);
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
            var span = data.AsSpan(chunk);
            span.CopyTo(joined.AsSpan(offset));
            offset += span.Length;
        }

        return joined;
    }

    private static ImageXObject DecodePng(byte[] data, ReaderLimits limits)
    {
        var width = 0;
        var height = 0;
        var bitDepth = 0;
        var colorType = 0;
        byte[]? palette = null;
        byte[]? transparency = null;
        List<Range>? idat = null;

        long pos = PngSignature.Length;
        while (pos + 8 <= data.Length)
        {
            uint length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)pos));
            long body = pos + 8;
            if (length > data.Length - body)
            {
                throw new InvalidDataException("PNG chunk length exceeds the available data.");
            }

            var type = Encoding.ASCII.GetString(data, (int)pos + 4, 4);
            var start = (int)body;
            var count = (int)length;

            switch (type)
            {
                case "IHDR":
                    if (count < 13)
                    {
                        throw new InvalidDataException("PNG IHDR chunk is truncated.");
                    }

                    width = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(start));
                    height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(start + 4));
                    bitDepth = data[start + 8];
                    colorType = data[start + 9];
                    if (data[start + 12] != 0)
                    {
                        throw new NotSupportedException("Adam7 interlaced PNG images are not supported.");
                    }

                    break;
                case "PLTE":
                    palette = data[start..(start + count)];
                    break;
                case "tRNS":
                    transparency = data[start..(start + count)];
                    break;
                case "IDAT":
                    (idat ??= []).Add(new Range(start, start + count));
                    break;
                case "IEND":
                    pos = data.Length;
                    continue;
            }

            pos = body + count + 4;
        }

        ImageDecoder.ValidateImageDimensions(width, height, limits, "PNG");

        var channels = colorType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => throw new NotSupportedException($"Unsupported PNG colour type {colorType}."),
        };

        ValidatePngBitDepth(colorType, bitDepth);

        var compressed = JoinIdat(data, idat);
        var raw = FlateFilter.Decode(compressed, limits.MaxDecodedStreamBytes);

        if (raw.LongLength > limits.ExpansionRatioFloorBytes
            && compressed.Length > 0
            && raw.LongLength / compressed.Length > limits.MaxDecodeExpansionRatio)
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
            0 => BuildColorKeyedPng(width, height, bitDepth, new NameObject("DeviceGray"), samples, transparency, 1),
            2 => BuildColorKeyedPng(width, height, bitDepth, new NameObject("DeviceRGB"), samples, transparency, 3),
            3 => BuildPalettedPng(width, height, bitDepth, samples, palette, transparency),
            4 => BuildAlphaPng(width, height, new NameObject("DeviceGray"), samples, 1, bitDepth),
            6 => BuildAlphaPng(width, height, new NameObject("DeviceRGB"), samples, 3, bitDepth),
            _ => throw new NotSupportedException($"Unsupported PNG colour type {colorType}."),
        };
    }

    // ISO/IEC 15948 Table 11.1: allowed bit depths per colour type.
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
            throw new InvalidDataException($"PNG bit depth {bitDepth} is not allowed for colour type {colorType}.");
        }
    }

    // ISO 32000-1 8.9.6.4: a tRNS colour key on grayscale/truecolor maps to /Mask.
    private static ImageXObject BuildColorKeyedPng(
        int width, int height, int bitDepth, NameObject colorSpace, byte[] samples, byte[]? transparency, int components)
    {
        var image = BuildImage(width, height, bitDepth, colorSpace, samples);

        if (transparency is not null)
        {
            if (transparency.Length < 2 * components)
            {
                throw new InvalidDataException("PNG tRNS chunk is truncated for the colour type.");
            }

            var mask = new ArrayObject();
            for (var c = 0; c < components; c++)
            {
                var value = BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(c * 2));
                mask.Add(new NumberObject(value));
                mask.Add(new NumberObject(value));
            }

            image.Dictionary["Mask"] = mask;
        }

        return new ImageXObject(image, null);
    }

    private static ImageXObject BuildPalettedPng(
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

        var hival = (palette.Length / 3) - 1;
        var colorSpace = new ArrayObject
        {
            new NameObject("Indexed"),
            new NameObject("DeviceRGB"),
            new NumberObject(hival),
            new StringObject(BytesToString(palette)),
        };

        var image = BuildImage(width, height, bitDepth, colorSpace, indices);

        if (transparency is null)
        {
            return new ImageXObject(image, null);
        }

        var pixels = UnpackIndices(indices, width, height, bitDepth);
        var alpha = new byte[width * height];
        for (var i = 0; i < alpha.Length; i++)
        {
            var index = pixels[i];
            alpha[i] = index < transparency.Length ? transparency[index] : (byte)0xFF;
        }

        var mask = BuildImage(width, height, 8, new NameObject("DeviceGray"), alpha);
        return new ImageXObject(image, mask);
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

    private static ImageXObject BuildAlphaPng(int width, int height, NameObject colorSpace, byte[] samples, int colorChannels, int bitDepth)
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

        var image = BuildImage(width, height, 8, colorSpace, color);
        var mask = BuildImage(width, height, 8, new NameObject("DeviceGray"), alpha);
        return new ImageXObject(image, mask);
    }

    private static StreamObject BuildImage(int width, int height, int bitsPerComponent, DocumentObject colorSpace, byte[] samples)
        => ImageXObjectShell.FlateImage(samples, width, height, bitsPerComponent, colorSpace);

    private static string BytesToString(byte[] bytes)
    {
        var chars = new char[bytes.Length];
        for (var i = 0; i < bytes.Length; i++)
        {
            chars[i] = (char)bytes[i];
        }

        return new string(chars);
    }
}
