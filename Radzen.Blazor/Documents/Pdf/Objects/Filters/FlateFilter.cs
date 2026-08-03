using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Radzen.Documents.Internal;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class FlateFilter
{
    public static byte[] Decode(byte[] data, long maxOutput)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Decode(data.AsMemory(), maxOutput);
    }

    public static byte[] Decode(ReadOnlyMemory<byte> data) => Decode(data, ReaderLimits.Default.MaxDecodedStreamBytes);

    public static byte[] Decode(ReadOnlyMemory<byte> data, long maxOutput)
    {
        if (data.Length == 0)
        {
            throw new InvalidDataException("Empty stream is not a valid zlib stream.");
        }

        using var input = AsStream(data);
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

    private static MemoryStream AsStream(ReadOnlyMemory<byte> data)
        => MemoryMarshal.TryGetArray(data, out var segment)
            ? new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false)
            : new MemoryStream(data.ToArray(), writable: false);

    public static byte[] Encode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Encode(data.AsSpan());
    }

    public static byte[] Encode(ReadOnlySpan<byte> data)
    {
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

    public static StreamObject EncodeStream(byte[] data, Action<DictionaryObject> configureBeforeFilter)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(configureBeforeFilter);

        return EncodeStreamCore(data, configureBeforeFilter);
    }

    public static StreamObject EncodeStream(ReadOnlySpan<byte> data)
        => EncodeStreamCore(data, null);

    private static StreamObject EncodeStreamCore(
        ReadOnlySpan<byte> data, Action<DictionaryObject>? configureBeforeFilter)
    {
        var stream = new StreamObject(Encode(data));
        configureBeforeFilter?.Invoke(stream.Dictionary);
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
