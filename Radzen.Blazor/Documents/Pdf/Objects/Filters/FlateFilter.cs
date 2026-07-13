using System;
using System.IO;
using System.IO.Compression;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class FlateFilter
{
    public static byte[] Decode(byte[] data) => Decode(data, ReaderLimits.Default.MaxDecodedStreamBytes);

    // maxOutput bounds the decompressed size so a compression bomb aborts with a
    // recoverable DocumentParseException instead of exhausting memory. A fixed-size
    // read loop is used rather than CopyTo so the cap is checked before each grow.
    public static byte[] Decode(byte[] data, long maxOutput)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0)
        {
            throw new InvalidDataException("Empty stream is not a valid zlib stream.");
        }

        using var input = new MemoryStream(data);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new PooledBufferStream((int)Math.Min((long)data.Length * 4, 1 << 20));
        var buffer = new byte[64 * 1024];
        int read;
        while ((read = zlib.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (output.Length + read > maxOutput)
            {
                throw new DocumentParseException("Decoded stream exceeds the maximum allowed size.", -1);
            }

            output.Write(buffer, 0, read);
        }

        return output.ToArray();
    }

    public static byte[] Encode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Encode(data.AsSpan());
    }

    public static byte[] Encode(ReadOnlySpan<byte> data)
    {
        // ZLibStream emits nothing for a zero-length write, so produce a canonical
        // empty zlib stream (header, empty stored block, Adler-32 of no data).
        if (data.Length == 0)
        {
            return [0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01];
        }

        using var output = new PooledBufferStream(data.Length / 2 + 64);
        using (var zlib = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(data);
        }

        return output.ToArray();
    }

    public static StreamObject EncodeStream(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return EncodeStream(data.AsSpan());
    }

    public static StreamObject EncodeStream(ReadOnlySpan<byte> data)
    {
        var stream = new StreamObject(Encode(data));
        stream.Dictionary["Filter"] = new NameObject("FlateDecode");
        return stream;
    }
}

internal sealed class FlateStreamFilter : IStreamFilter
{
    public string Name => "FlateDecode";

    public byte[] Decode(byte[] data, DictionaryObject? parms, long maxOutput)
        => StreamPredictor.Apply(FlateFilter.Decode(data, maxOutput), parms);
}
