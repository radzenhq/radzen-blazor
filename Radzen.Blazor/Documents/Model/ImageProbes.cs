using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Radzen.Documents;

internal sealed class ImageProbes
{
    public static readonly ImageProbes None = new([]);

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly byte[] Jp2Signature = [0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A];

    private const uint Jp2HeaderTag = 0x6A703268;
    private const uint ImageHeaderTag = 0x69686472;
    private const uint CodestreamTag = 0x6A703263;

    private readonly ImmutableArray<Func<ReadOnlyMemory<byte>, (double Width, double Height)?>> probes;

    private readonly ConditionalWeakTable<byte[], ImageInfo> cache = new();

    private ImageProbes(ImmutableArray<Func<ReadOnlyMemory<byte>, (double Width, double Height)?>> probes)
        => this.probes = probes;

    public int Count => probes.Length;

    public ImageProbes Add(Func<ReadOnlyMemory<byte>, (double Width, double Height)?> probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return new ImageProbes(probes.Add(probe));
    }

    public bool Contains(Func<ReadOnlyMemory<byte>, (double Width, double Height)?> probe)
    {
        foreach (var existing in probes)
        {
            if (existing.Equals(probe))
            {
                return true;
            }
        }

        return false;
    }

    public ImageInfo Inspect(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return cache.GetValue(data, Probe);
    }

    public ImageFormat Format(byte[] data) => Inspect(data).Format;

    public (double Width, double Height) PixelSize(byte[] data)
    {
        var info = Inspect(data);
        return (info.Width, info.Height);
    }

    public (double Width, double Height) Measure(Image image, double availableWidth)
    {
        ArgumentNullException.ThrowIfNull(image);

        var pixels = PixelSize(image.Data);
        return ImageProbe.Measure(image, pixels.Width, pixels.Height, availableWidth);
    }

    private ImageInfo Probe(byte[] data)
    {
        if (StartsWith(data, PngSignature))
        {
            return ReadPng(data);
        }

        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
        {
            return ReadJpeg(data);
        }

        if (StartsWith(data, Jp2Signature))
        {
            return ReadJp2(data);
        }

        if (StartsWith(data, [0xFF, 0x4F, 0xFF, 0x51]))
        {
            return ReadCodestreamSiz(data, 2, ImageFormat.Jpeg2000);
        }

        foreach (var probe in probes)
        {
            if (probe(data) is { } size)
            {
                return Validate((long)size.Width, (long)size.Height, ImageFormat.Custom);
            }
        }

        throw new NotSupportedException("Unrecognized image format; only PNG, JPEG and JPEG2000 are supported.");
    }

    // ISO/IEC 15948 5.3 (chunk layout) and 11.2.2 (IHDR carries the image dimensions).
    private static ImageInfo ReadPng(byte[] data)
    {
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
            if (type == "IHDR")
            {
                if (length < 13)
                {
                    throw new InvalidDataException("PNG IHDR chunk is truncated.");
                }

                var width = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)body));
                var height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)body + 4));
                return Validate(width, height, ImageFormat.Png);
            }

            if (type == "IEND")
            {
                break;
            }

            pos = body + length + 4;
        }

        throw new InvalidDataException("PNG IHDR chunk is truncated.");
    }

    // ITU-T T.81 B.2.2: the frame header (SOFn) holds Y then X after the sample precision.
    private static ImageInfo ReadJpeg(byte[] data)
    {
        var pos = 2;
        while (pos + 1 < data.Length)
        {
            if (data[pos] != 0xFF)
            {
                pos++;
                continue;
            }

            var marker = data[pos + 1];
            pos += 2;

            if (marker is 0x01 or (>= 0xD0 and <= 0xD9))
            {
                continue;
            }

            if (pos + 1 >= data.Length)
            {
                break;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos));
            if (segmentLength < 2 || pos + segmentLength > data.Length)
            {
                throw new InvalidDataException("JPEG segment length is invalid.");
            }

            if (IsStartOfFrame(marker))
            {
                if (marker is not (0xC0 or 0xC1 or 0xC2))
                {
                    throw new NotSupportedException(
                        $"JPEG start-of-frame marker 0xFF{marker:X2} is not supported.");
                }

                if (pos + 8 > data.Length)
                {
                    throw new InvalidDataException("JPEG start-of-frame segment is truncated.");
                }

                var height = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 3));
                var width = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 5));
                return Validate(width, height, ImageFormat.Jpeg);
            }

            pos += segmentLength;
        }

        throw new InvalidDataException("No JPEG start-of-frame marker was found.");
    }

    // ITU-T T.81 B.1.1.3 Table B.1: SOF0..SOF15, excluding the DHT/JPG/DAC markers interleaved in the range.
    private static bool IsStartOfFrame(byte marker) => marker switch
    {
        >= 0xC0 and <= 0xC3 => true,
        >= 0xC5 and <= 0xC7 => true,
        >= 0xC9 and <= 0xCB => true,
        >= 0xCD and <= 0xCF => true,
        _ => false,
    };

    private static ImageInfo ReadJp2(byte[] data)
    {
        long codestream = -1;
        long position = Jp2Signature.Length;
        while (TryReadBox(data, position, data.Length, out var type, out var contentStart, out var contentEnd))
        {
            if (type == Jp2HeaderTag)
            {
                var child = contentStart;
                while (TryReadBox(data, child, contentEnd, out var childType, out var childStart, out var childEnd))
                {
                    if (childType == ImageHeaderTag)
                    {
                        return ReadIhdr(data, childStart, childEnd);
                    }

                    child = childEnd;
                }
            }
            else if (type == CodestreamTag && codestream < 0)
            {
                codestream = contentStart;
            }

            position = contentEnd;
        }

        if (codestream >= 0)
        {
            return ReadCodestreamSiz(data, codestream + 2, ImageFormat.Jpeg2000);
        }

        throw new InvalidDataException("JPEG2000 file has no ihdr or codestream.");
    }

    // ISO/IEC 15444-1 I.5.3: the Image Header box holds HEIGHT then WIDTH as 32-bit big-endian values.
    private static ImageInfo ReadIhdr(byte[] data, long contentStart, long contentEnd)
    {
        if (contentEnd - contentStart < 10)
        {
            throw new InvalidDataException("JPEG2000 ihdr box is truncated.");
        }

        var height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)contentStart));
        var width = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)contentStart + 4));
        return Validate(width, height, ImageFormat.Jpeg2000);
    }

    // ISO/IEC 15444-1 I.4: box length and type precede the box content.
    private static bool TryReadBox(byte[] data, long position, long end, out uint type, out long contentStart, out long contentEnd)
    {
        type = 0;
        contentStart = 0;
        contentEnd = 0;
        if (position + 8 > end)
        {
            return false;
        }

        var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)position));
        type = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)position + 4));
        contentStart = position + 8;
        contentEnd = length == 0 ? end : position + length;
        if (length == 1 || contentEnd < contentStart || contentEnd > end)
        {
            throw new InvalidDataException("JPEG2000 box length is invalid.");
        }

        return true;
    }

    // ISO/IEC 15444-1 A.5.1: the SIZ marker segment gives the image area as Xsiz-XOsiz by Ysiz-YOsiz.
    private static ImageInfo ReadCodestreamSiz(byte[] data, long sizPos, ImageFormat format)
    {
        if (sizPos + 40 > data.Length || data[sizPos] != 0xFF || data[sizPos + 1] != 0x51)
        {
            throw new InvalidDataException("JPEG2000 codestream is missing its SIZ marker.");
        }

        var body = (int)sizPos + 2;
        var xsiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body + 4));
        var ysiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body + 8));
        var xosiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body + 12));
        var yosiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body + 16));

        var width = xsiz - xosiz;
        var height = ysiz - yosiz;
        if (width is <= 0 or > int.MaxValue || height is <= 0 or > int.MaxValue)
        {
            throw new InvalidDataException("JPEG2000 codestream has invalid dimensions.");
        }

        return Validate((int)width, (int)height, format);
    }

    private static ImageInfo Validate(long width, long height, ImageFormat format)
    {
        var limits = ResourceLimits.Default;
        var name = FormatName(format);
        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException($"{name} image has invalid dimensions.");
        }

        if (width * height > limits.MaxImagePixels)
        {
            throw new InvalidDataException($"{name} image dimensions exceed the maximum decodable size.");
        }

        return new ImageInfo(format, width, height);
    }

    private static string FormatName(ImageFormat format) => format switch
    {
        ImageFormat.Png => "PNG",
        ImageFormat.Jpeg => "JPEG",
        ImageFormat.Jpeg2000 => "JPEG2000",
        _ => "Custom-format",
    };

    private static bool StartsWith(byte[] data, ReadOnlySpan<byte> prefix)
        => data.Length >= prefix.Length && data.AsSpan(0, prefix.Length).SequenceEqual(prefix);
}
