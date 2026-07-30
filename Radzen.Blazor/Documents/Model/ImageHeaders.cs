using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Radzen.Documents;

internal readonly record struct PngHeader(int Width, int Height, int BitDepth, int ColorType, int Interlace);

internal readonly record struct JpegFrame(int Width, int Height, int Precision, int Components);

internal readonly record struct Jpeg2000Size(int Width, int Height, int Components);

// ISO/IEC 15948 5.3: a chunk is a four-byte length, a four-byte type, the data and a four-byte CRC.
internal ref struct PngChunkReader
{
    private readonly ReadOnlySpan<byte> data;

    private long position;

    public PngChunkReader(ReadOnlySpan<byte> data)
    {
        this.data = data;
        position = ImageHeaders.PngSignature.Length;
        Type = string.Empty;
    }

    public string Type { get; private set; }

    public int Start { get; private set; }

    public int Count { get; private set; }

    public bool MoveNext()
    {
        if (position + 8 > data.Length)
        {
            return false;
        }

        var length = BinaryPrimitives.ReadUInt32BigEndian(data[(int)position..]);
        var body = position + 8;
        if (length > data.Length - body)
        {
            throw new InvalidDataException("PNG chunk length exceeds the available data.");
        }

        Type = Encoding.ASCII.GetString(data.Slice((int)position + 4, 4));
        Start = (int)body;
        Count = (int)length;
        position = body + Count + 4;
        return Type != "IEND";
    }
}

internal static class ImageHeaders
{
    public static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static ReadOnlySpan<byte> Jp2Signature => [0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A];

    public static ReadOnlySpan<byte> Jpeg2000CodestreamSignature => [0xFF, 0x4F, 0xFF, 0x51];

    private const uint Jp2HeaderTag = 0x6A703268;

    private const uint ImageHeaderTag = 0x69686472;

    private const uint CodestreamTag = 0x6A703263;

    public static bool IsPng(ReadOnlySpan<byte> data) => data.StartsWith(PngSignature);

    public static bool IsJpeg(ReadOnlySpan<byte> data) => data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8;

    public static bool IsJpeg2000(ReadOnlySpan<byte> data)
        => data.StartsWith(Jp2Signature) || data.StartsWith(Jpeg2000CodestreamSignature);

    public static PngHeader ReadPngHeader(ReadOnlySpan<byte> data)
    {
        var chunks = new PngChunkReader(data);
        while (chunks.MoveNext())
        {
            if (chunks.Type == "IHDR")
            {
                return ReadIhdr(data, chunks.Start, chunks.Count);
            }
        }

        throw new InvalidDataException("PNG IHDR chunk is truncated.");
    }

    // ISO/IEC 15948 11.2.2: IHDR carries width, height, bit depth, colour type, compression, filter and interlace method.
    public static PngHeader ReadIhdr(ReadOnlySpan<byte> data, int start, int count)
    {
        if (count < 13)
        {
            throw new InvalidDataException("PNG IHDR chunk is truncated.");
        }

        return new PngHeader(
            (int)BinaryPrimitives.ReadUInt32BigEndian(data[start..]),
            (int)BinaryPrimitives.ReadUInt32BigEndian(data[(start + 4)..]),
            data[start + 8],
            data[start + 9],
            data[start + 12]);
    }

    // ITU-T T.81 B.1.1.3 Table B.1: SOF0..SOF15, excluding the DHT/JPG/DAC markers interleaved in the range.
    public static bool IsStartOfFrame(byte marker) => marker switch
    {
        >= 0xC0 and <= 0xC3 => true,
        >= 0xC5 and <= 0xC7 => true,
        >= 0xC9 and <= 0xCB => true,
        >= 0xCD and <= 0xCF => true,
        _ => false,
    };

    // ITU-T T.81 B.1.1.2: markers are 0xFF-prefixed; TEM and RSTn/SOI/EOI carry no segment.
    public static int FindJpegFrame(ReadOnlySpan<byte> data, out byte frameMarker, out bool adobe)
    {
        frameMarker = 0;
        adobe = false;
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
                frameMarker = marker;
                return pos;
            }

            pos += segmentLength;
        }

        throw new InvalidDataException("No JPEG start-of-frame marker was found.");
    }

    // ITU-T T.81 B.2.2: the frame header holds P, Y, X and Nf after the segment length.
    public static JpegFrame ReadJpegFrame(ReadOnlySpan<byte> data, int position)
    {
        if (position + 8 > data.Length)
        {
            throw new InvalidDataException("JPEG start-of-frame segment is truncated.");
        }

        return new JpegFrame(
            BinaryPrimitives.ReadUInt16BigEndian(data[(position + 5)..]),
            BinaryPrimitives.ReadUInt16BigEndian(data[(position + 3)..]),
            data[position + 2],
            data[position + 7]);
    }

    private static bool IsAdobeApp14(ReadOnlySpan<byte> data, int pos, int segmentLength)
        => segmentLength >= 8
            && data[pos + 2] == (byte)'A'
            && data[pos + 3] == (byte)'d'
            && data[pos + 4] == (byte)'o'
            && data[pos + 5] == (byte)'b'
            && data[pos + 6] == (byte)'e';

    public static Jpeg2000Size ReadJpeg2000Size(ReadOnlySpan<byte> data)
        => data.StartsWith(Jp2Signature) ? ReadJp2(data) : ReadCodestreamSiz(data, 2);

    private static Jpeg2000Size ReadJp2(ReadOnlySpan<byte> data)
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
                        return ReadImageHeaderBox(data, childStart, childEnd);
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
            return ReadCodestreamSiz(data, codestream + 2);
        }

        throw new InvalidDataException("JPEG2000 file has no ihdr or codestream.");
    }

    // ISO/IEC 15444-1 I.5.3: the Image Header box holds HEIGHT then WIDTH then NC as big-endian values.
    private static Jpeg2000Size ReadImageHeaderBox(ReadOnlySpan<byte> data, long contentStart, long contentEnd)
    {
        if (contentEnd - contentStart < 10)
        {
            throw new InvalidDataException("JPEG2000 ihdr box is truncated.");
        }

        var height = (int)BinaryPrimitives.ReadUInt32BigEndian(data[(int)contentStart..]);
        var width = (int)BinaryPrimitives.ReadUInt32BigEndian(data[((int)contentStart + 4)..]);
        var components = BinaryPrimitives.ReadUInt16BigEndian(data[((int)contentStart + 8)..]);
        return new Jpeg2000Size(width, height, components);
    }

    // ISO/IEC 15444-1 I.4: box length and type precede the box content.
    private static bool TryReadBox(
        ReadOnlySpan<byte> data, long position, long end, out uint type, out long contentStart, out long contentEnd)
    {
        type = 0;
        contentStart = 0;
        contentEnd = 0;
        if (position + 8 > end)
        {
            return false;
        }

        var length = BinaryPrimitives.ReadUInt32BigEndian(data[(int)position..]);
        type = BinaryPrimitives.ReadUInt32BigEndian(data[((int)position + 4)..]);
        contentStart = position + 8;
        contentEnd = length == 0 ? end : position + length;
        if (length == 1 || contentEnd < contentStart || contentEnd > end)
        {
            throw new InvalidDataException("JPEG2000 box length is invalid.");
        }

        return true;
    }

    // ISO/IEC 15444-1 A.5.1: the SIZ marker segment gives the image area as Xsiz-XOsiz by Ysiz-YOsiz.
    private static Jpeg2000Size ReadCodestreamSiz(ReadOnlySpan<byte> data, long sizPos)
    {
        if (sizPos + 40 > data.Length || data[(int)sizPos] != 0xFF || data[(int)sizPos + 1] != 0x51)
        {
            throw new InvalidDataException("JPEG2000 codestream is missing its SIZ marker.");
        }

        var body = (int)sizPos + 2;
        var xsiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data[(body + 4)..]);
        var ysiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data[(body + 8)..]);
        var xosiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data[(body + 12)..]);
        var yosiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data[(body + 16)..]);
        var components = BinaryPrimitives.ReadUInt16BigEndian(data[(body + 36)..]);

        var width = xsiz - xosiz;
        var height = ysiz - yosiz;
        if (width is <= 0 or > int.MaxValue || height is <= 0 or > int.MaxValue)
        {
            throw new InvalidDataException("JPEG2000 codestream has invalid dimensions.");
        }

        return new Jpeg2000Size((int)width, (int)height, components);
    }
}
