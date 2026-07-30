using Radzen.Documents.Pdf.Objects;
using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class JpegImageDecoder : IImageDecoder
{
    public bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out ImageXObject? xobject)
    {
        if (data.Length < 2 || data.Span[0] != 0xFF || data.Span[1] != 0xD8)
        {
            xobject = null;
            return false;
        }

        xobject = DecodeJpeg(data, limits);
        return true;
    }

    private static ImageXObject DecodeJpeg(ReadOnlyMemory<byte> data, ReaderLimits limits)
    {
        var (width, height, precision, components, adobe) = ReadJpegFrame(data.Span);

        ImageDecoder.ValidateImageDimensions(width, height, limits, "JPEG");

        // ISO 32000-1 8.9.5.1: /BitsPerComponent admits only 1/2/4/8/16.
        if (precision != 8)
        {
            throw new NotSupportedException(
                $"JPEG sample precision {precision} is not supported; PDF images require 8-bit DCTDecode samples.");
        }

        var colorSpace = components switch
        {
            1 => "DeviceGray",
            3 => "DeviceRGB",
            4 => "DeviceCMYK",
            _ => throw new NotSupportedException($"Unsupported JPEG component count {components}."),
        };

        var stream = new StreamObject(data);
        var dict = stream.Dictionary;
        ImageXObjectShell.Apply(
            dict,
            new NumberObject(width),
            new NumberObject(height),
            new NameObject(colorSpace),
            new NumberObject(precision),
            new NameObject("DCTDecode"));

        if (components == 4 && adobe)
        {
            dict["Decode"] = new ArrayObject
            {
                new NumberObject(1), new NumberObject(0),
                new NumberObject(1), new NumberObject(0),
                new NumberObject(1), new NumberObject(0),
                new NumberObject(1), new NumberObject(0),
            };
        }

        return new ImageXObject(stream, null);
    }

    private static (int Width, int Height, int Precision, int Components, bool Adobe) ReadJpegFrame(ReadOnlySpan<byte> data)
    {
        var adobe = false;
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

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(data[pos..]);
            if (segmentLength < 2 || pos + segmentLength > data.Length)
            {
                throw new InvalidDataException("JPEG segment length is invalid.");
            }

            if (marker == 0xEE && IsAdobeApp14(data, pos, segmentLength))
            {
                adobe = true;
            }

            if (IsStartOfFrame(marker))
            {
                if (marker is not (0xC0 or 0xC1 or 0xC2))
                {
                    throw new NotSupportedException(
                        $"JPEG start-of-frame marker 0xFF{marker:X2} is not supported by DCTDecode.");
                }

                if (pos + 8 > data.Length)
                {
                    throw new InvalidDataException("JPEG start-of-frame segment is truncated.");
                }

                var precision = data[pos + 2];
                var height = BinaryPrimitives.ReadUInt16BigEndian(data[(pos + 3)..]);
                var width = BinaryPrimitives.ReadUInt16BigEndian(data[(pos + 5)..]);
                var components = data[pos + 7];
                return (width, height, precision, components, adobe);
            }

            pos += segmentLength;
        }

        throw new InvalidDataException("No JPEG start-of-frame marker was found.");
    }

    private static bool IsAdobeApp14(ReadOnlySpan<byte> data, int pos, int segmentLength)
        => segmentLength >= 8
            && data[pos + 2] == (byte)'A'
            && data[pos + 3] == (byte)'d'
            && data[pos + 4] == (byte)'o'
            && data[pos + 5] == (byte)'b'
            && data[pos + 6] == (byte)'e';

    private static bool IsStartOfFrame(byte marker) => marker switch
    {
        >= 0xC0 and <= 0xC3 => true,
        >= 0xC5 and <= 0xC7 => true,
        >= 0xC9 and <= 0xCB => true,
        >= 0xCD and <= 0xCF => true,
        _ => false,
    };
}
