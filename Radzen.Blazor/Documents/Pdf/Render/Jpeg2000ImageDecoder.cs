using Radzen.Documents.Pdf.Objects;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Radzen.Documents.Pdf.Render;

internal sealed class Jpeg2000ImageDecoder : IImageDecoder
{
    private static readonly byte[] Jp2Signature = [0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A];
    private const uint Jp2HeaderTag = 0x6A703268;
    private const uint ImageHeaderTag = 0x69686472;
    private const uint CodestreamTag = 0x6A703263;

    public bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out ImageXObject? xobject)
    {
        if (!IsJpeg2000(data.Span))
        {
            xobject = null;
            return false;
        }

        xobject = DecodeJpeg2000(data, limits);
        return true;
    }

    private static bool IsJpeg2000(ReadOnlySpan<byte> data)
        => PdfBytes.Matches(data, Jp2Signature)
            || PdfBytes.Matches(data, [0xFF, 0x4F, 0xFF, 0x51]);

    // JPXDecode embeds verbatim with no /ColorSpace, so the JPX stream's own color space applies (ISO 32000-1 7.4.9).
    private static ImageXObject DecodeJpeg2000(ReadOnlyMemory<byte> data, ReaderLimits limits)
    {
        var (width, height, components) = PdfBytes.Matches(data.Span, Jp2Signature)
            ? ReadJp2Header(data)
            : ReadCodestreamSiz(data, 2);

        ValidateJpeg2000Dimensions(width, height, components, limits);

        var stream = new StreamObject(data);
        ImageXObjectShell.Apply(
            stream.Dictionary,
            new NumberObject(width),
            new NumberObject(height),
            colorSpace: null,
            new NumberObject(8),
            new NameObject("JPXDecode"));
        return new ImageXObject(stream, null);
    }

    private static (int Width, int Height, int Components) ReadJp2Header(ReadOnlyMemory<byte> data)
    {
        long codestream = -1;
        foreach (var box in Boxes(data, Jp2Signature.Length, data.Length))
        {
            if (box.Type == Jp2HeaderTag)
            {
                foreach (var child in Boxes(data, box.ContentStart, box.ContentEnd))
                {
                    if (child.Type == ImageHeaderTag)
                    {
                        return ReadIhdr(data, child);
                    }
                }
            }
            else if (box.Type == CodestreamTag && codestream < 0)
            {
                codestream = box.ContentStart;
            }
        }

        if (codestream >= 0)
        {
            return ReadCodestreamSiz(data, codestream + 2);
        }

        throw new InvalidDataException("JPEG2000 file has no ihdr or codestream.");
    }

    private static (int Width, int Height, int Components) ReadIhdr(ReadOnlyMemory<byte> data, Jp2Box box)
    {
        if (box.ContentEnd - box.ContentStart < 10)
        {
            throw new InvalidDataException("JPEG2000 ihdr box is truncated.");
        }

        var height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.Span[(int)box.ContentStart..]);
        var width = (int)BinaryPrimitives.ReadUInt32BigEndian(data.Span[((int)box.ContentStart + 4)..]);
        var components = BinaryPrimitives.ReadUInt16BigEndian(data.Span[((int)box.ContentStart + 8)..]);
        return (width, height, components);
    }

    private static IEnumerable<Jp2Box> Boxes(ReadOnlyMemory<byte> data, long start, long end)
    {
        var position = start;
        while (position + 8 <= end)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(data.Span[(int)position..]);
            var type = BinaryPrimitives.ReadUInt32BigEndian(data.Span[((int)position + 4)..]);
            var contentStart = position + 8;
            long contentEnd = length == 0 ? end : position + length;
            if (length == 1 || contentEnd < contentStart || contentEnd > end)
            {
                throw new InvalidDataException("JPEG2000 box length is invalid.");
            }

            yield return new Jp2Box(type, contentStart, contentEnd);
            position = contentEnd;
        }
    }

    private static (int Width, int Height, int Components) ReadCodestreamSiz(ReadOnlyMemory<byte> data, long sizPos)
    {
        if (sizPos + 40 > data.Length || data.Span[(int)sizPos] != 0xFF || data.Span[(int)sizPos + 1] != 0x51)
        {
            throw new InvalidDataException("JPEG2000 codestream is missing its SIZ marker.");
        }

        var body = (int)sizPos + 2;
        var xsiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data.Span[(body + 4)..]);
        var ysiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data.Span[(body + 8)..]);
        var xosiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data.Span[(body + 12)..]);
        var yosiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data.Span[(body + 16)..]);
        var components = BinaryPrimitives.ReadUInt16BigEndian(data.Span[(body + 36)..]);

        var width = xsiz - xosiz;
        var height = ysiz - yosiz;
        if (width is <= 0 or > int.MaxValue || height is <= 0 or > int.MaxValue)
        {
            throw new InvalidDataException("JPEG2000 codestream has invalid dimensions.");
        }

        return ((int)width, (int)height, components);
    }

    private static void ValidateJpeg2000Dimensions(int width, int height, int components, ReaderLimits limits)
    {
        if (components <= 0)
        {
            throw new InvalidDataException("JPEG2000 image has invalid dimensions.");
        }

        ImageDecoder.ValidateImageDimensions(width, height, limits, "JPEG2000");
    }

    private readonly record struct Jp2Box(uint Type, long ContentStart, long ContentEnd);
}
