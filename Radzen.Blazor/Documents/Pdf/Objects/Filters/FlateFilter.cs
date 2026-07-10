using System;
using System.IO;
using System.IO.Compression;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class FlateFilter
{
    public static byte[] Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0)
        {
            throw new InvalidDataException("Empty stream is not a valid zlib stream.");
        }

        using var input = new MemoryStream(data);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    public static byte[] Encode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        // ZLibStream emits nothing for a zero-length write, so produce a canonical
        // empty zlib stream (header, empty stored block, Adler-32 of no data).
        if (data.Length == 0)
        {
            return [0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01];
        }

        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    public static StreamObject EncodeStream(byte[] data)
    {
        var stream = new StreamObject(Encode(data));
        stream.Dictionary["Filter"] = new NameObject("FlateDecode");
        return stream;
    }
}
