#nullable enable
using System;
using System.IO;

namespace Radzen.Documents.Pdf.Fonts.Cff;

// CFF INDEX (spec section 5): Card16 count, Card8 offSize, (count+1) 1-based offsets,
// then packed object data. An empty INDEX is just a Card16 count of 0.
internal sealed class CffIndex(byte[] data, int[] offsets, int endOffset)
{
    public int Count => offsets.Length - 1;

    public int EndOffset => endOffset;

    public byte[] GetBytes(int index)
    {
        var start = offsets[index];
        var length = offsets[index + 1] - start;
        var result = new byte[length];
        Array.Copy(data, start, result, 0, length);
        return result;
    }

    public static CffIndex Read(byte[] data, int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        var count = ReadCard16(data, offset);
        if (count == 0)
        {
            return new CffIndex(data, [offset + 2], offset + 2);
        }

        var offSize = ReadByte(data, offset + 2);
        if (offSize is < 1 or > 4)
        {
            throw new InvalidDataException("Invalid CFF INDEX offset size.");
        }

        var offsetArrayStart = offset + 3;

        // Offsets are 1-based relative to the byte before the object data block.
        var dataBase = offsetArrayStart + ((count + 1) * offSize) - 1;
        var offsets = new int[count + 1];
        for (var i = 0; i <= count; i++)
        {
            offsets[i] = dataBase + ReadOffset(data, offsetArrayStart + (i * offSize), offSize);
        }

        var endOffset = offsets[count];
        if (endOffset < 0 || endOffset > data.Length)
        {
            throw new InvalidDataException("CFF INDEX extends past the end of the data.");
        }

        return new CffIndex(data, offsets, endOffset);
    }

    private static int ReadCard16(byte[] data, int offset)
    {
        Require(data, offset, 2);
        return (data[offset] << 8) | data[offset + 1];
    }

    private static byte ReadByte(byte[] data, int offset)
    {
        Require(data, offset, 1);
        return data[offset];
    }

    private static int ReadOffset(byte[] data, int offset, int size)
    {
        Require(data, offset, size);
        var value = 0;
        for (var i = 0; i < size; i++)
        {
            value = (value << 8) | data[offset + i];
        }

        return value;
    }

    private static void Require(byte[] data, int offset, int count)
    {
        if (offset < 0 || offset + count > data.Length)
        {
            throw new InvalidDataException("Attempt to read past the end of the CFF data.");
        }
    }
}
