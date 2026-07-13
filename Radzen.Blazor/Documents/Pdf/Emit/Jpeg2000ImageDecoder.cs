using Radzen.Documents.Pdf.Objects;
using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class Jpeg2000ImageDecoder : IImageDecoder
{
    private static readonly byte[] Jp2Signature = [0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A];

    public bool TryDecode(byte[] data, ReaderLimits limits, [NotNullWhen(true)] out ImageXObject? xobject)
    {
        if (!IsJpeg2000(data))
        {
            xobject = null;
            return false;
        }

        xobject = DecodeJpeg2000(data, limits);
        return true;
    }

    // A JP2 file opens with the signature box; a bare codestream opens with SOC+SIZ.
    private static bool IsJpeg2000(byte[] data)
        => StartsWith(data, Jp2Signature)
            || (data.Length >= 4 && data[0] == 0xFF && data[1] == 0x4F && data[2] == 0xFF && data[3] == 0x51);

    // JPEG2000 embeds verbatim through the /JPXDecode filter, exactly like the /DCTDecode
    // JPEG path: only the header is parsed for geometry and no /ColorSpace is written, so
    // the JPX stream's own colour space applies (PDF 32000-1 7.4.9). BitsPerComponent is
    // informational for JPXDecode; a conforming producer writes 8.
    private static ImageXObject DecodeJpeg2000(byte[] data, ReaderLimits limits)
    {
        var (width, height, components) = StartsWith(data, Jp2Signature)
            ? ReadJp2Header(data)
            : ReadCodestreamSiz(data, 2);

        ValidateJpeg2000Dimensions(width, height, components, limits);

        var stream = new StreamObject(data);
        var dict = stream.Dictionary;
        dict["Type"] = new NameObject("XObject");
        dict["Subtype"] = new NameObject("Image");
        dict["Width"] = new NumberObject(width);
        dict["Height"] = new NumberObject(height);
        dict["BitsPerComponent"] = new NumberObject(8);
        dict["Filter"] = new NameObject("JPXDecode");
        return new ImageXObject(stream, null);
    }

    // Walk the top-level boxes: an ihdr (inside the jp2h superbox) gives dimensions
    // directly; otherwise fall back to the SIZ marker inside the jp2c codestream.
    private static (int Width, int Height, int Components) ReadJp2Header(byte[] data)
    {
        long codestream = -1;
        var pos = (long)Jp2Signature.Length;
        while (pos + 8 <= data.Length)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)pos));
            var type = Encoding.ASCII.GetString(data, (int)pos + 4, 4);
            var contentStart = pos + 8;
            long contentEnd = length == 0 ? data.Length : pos + length;
            if (length == 1 || contentEnd < contentStart || contentEnd > data.Length)
            {
                throw new InvalidDataException("JPEG2000 box length is invalid.");
            }

            if (type == "jp2h")
            {
                var ihdr = FindIhdr(data, contentStart, contentEnd);
                if (ihdr is { } dims)
                {
                    return dims;
                }
            }
            else if (type == "jp2c" && codestream < 0)
            {
                codestream = contentStart;
            }

            pos = contentEnd;
        }

        if (codestream >= 0)
        {
            return ReadCodestreamSiz(data, codestream + 2);
        }

        throw new InvalidDataException("JPEG2000 file has no ihdr or codestream.");
    }

    private static (int Width, int Height, int Components)? FindIhdr(byte[] data, long start, long end)
    {
        var pos = start;
        while (pos + 8 <= end)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)pos));
            var type = Encoding.ASCII.GetString(data, (int)pos + 4, 4);
            var contentStart = pos + 8;
            long contentEnd = length == 0 ? end : pos + length;
            if (length == 1 || contentEnd < contentStart || contentEnd > end)
            {
                throw new InvalidDataException("JPEG2000 box length is invalid.");
            }

            if (type == "ihdr")
            {
                if (contentEnd - contentStart < 10)
                {
                    throw new InvalidDataException("JPEG2000 ihdr box is truncated.");
                }

                var height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)contentStart));
                var width = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)contentStart + 4));
                var components = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan((int)contentStart + 8));
                return (width, height, components);
            }

            pos = contentEnd;
        }

        return null;
    }

    // sizPos points just past the SOC marker; the SIZ marker segment carries Xsiz/Ysiz,
    // the image origin XOsiz/YOsiz, and the component count Csiz.
    private static (int Width, int Height, int Components) ReadCodestreamSiz(byte[] data, long sizPos)
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
        var components = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(body + 36));

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

    private static bool StartsWith(byte[] data, byte[] prefix)
    {
        if (data.Length < prefix.Length)
        {
            return false;
        }

        for (var i = 0; i < prefix.Length; i++)
        {
            if (data[i] != prefix[i])
            {
                return false;
            }
        }

        return true;
    }
}
