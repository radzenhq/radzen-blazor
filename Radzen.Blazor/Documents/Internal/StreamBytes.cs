using System;
using System.IO;

namespace Radzen.Documents;

internal static class StreamBytes
{
    internal static byte[] ReadFully(Stream stream, long maxFileBytes)
    {
        if (stream.CanSeek)
        {
            var remaining = stream.Length - stream.Position;
            if (remaining < 0)
            {
                remaining = 0;
            }

            if (remaining > maxFileBytes || remaining > int.MaxValue)
            {
                throw new InvalidDataException("Maximum file size exceeded.");
            }

            var buffer = new byte[remaining];
            stream.ReadExactly(buffer, 0, buffer.Length);
            return buffer;
        }

        using var pooled = new PooledBufferStream();
        var chunk = new byte[81920];
        long total = 0;
        int read;
        while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            try
            {
                total = checked(total + read);
            }
            catch (OverflowException)
            {
                throw new InvalidDataException("Maximum file size exceeded.");
            }

            if (total > maxFileBytes || total > int.MaxValue)
            {
                throw new InvalidDataException("Maximum file size exceeded.");
            }

            pooled.Write(chunk, 0, read);
        }

        return pooled.ToArray();
    }
}
