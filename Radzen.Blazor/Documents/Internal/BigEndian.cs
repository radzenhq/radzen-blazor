using System;
using System.Buffers.Binary;
using System.IO;

namespace Radzen.Documents;

internal static class BigEndian
{
    internal static ushort ReadUInt16BigEndian(ReadOnlySpan<byte> data, int offset, string errorMessage)
    {
        Require(data, offset, 2, errorMessage);
        return BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
    }

    internal static short ReadInt16BigEndian(ReadOnlySpan<byte> data, int offset, string errorMessage)
        => unchecked((short)ReadUInt16BigEndian(data, offset, errorMessage));

    internal static uint ReadUInt32BigEndian(ReadOnlySpan<byte> data, int offset, string errorMessage)
    {
        Require(data, offset, 4, errorMessage);
        return BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
    }

    private static void Require(ReadOnlySpan<byte> data, int offset, int count, string errorMessage)
    {
        if (offset < 0 || count < 0 || offset > data.Length - count)
        {
            throw new InvalidDataException(errorMessage);
        }
    }
}
