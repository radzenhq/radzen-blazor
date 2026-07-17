using System;
using System.Collections.Generic;
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

    public static byte[] Write(IReadOnlyList<byte[]> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var count = entries.Count;
        if (count == 0)
        {
            return [0, 0];
        }

        var dataSize = 0;
        foreach (var entry in entries)
        {
            dataSize += entry.Length;
        }

        var lastOffset = dataSize + 1;
        var offSize = lastOffset <= 0xFF ? 1 : lastOffset <= 0xFFFF ? 2 : lastOffset <= 0xFFFFFF ? 3 : 4;

        var result = new byte[2 + 1 + ((count + 1) * offSize) + dataSize];
        var pos = 0;
        result[pos++] = (byte)(count >> 8);
        result[pos++] = (byte)count;
        result[pos++] = (byte)offSize;

        var offset = 1;
        WriteOffsetValue(result, ref pos, offset, offSize);
        foreach (var entry in entries)
        {
            offset += entry.Length;
            WriteOffsetValue(result, ref pos, offset, offSize);
        }

        foreach (var entry in entries)
        {
            Array.Copy(entry, 0, result, pos, entry.Length);
            pos += entry.Length;
        }

        return result;
    }

    private static void WriteOffsetValue(byte[] dst, ref int pos, int value, int size)
    {
        for (var i = size - 1; i >= 0; i--)
        {
            dst[pos++] = (byte)(value >> (8 * i));
        }
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

        // GetBytes sizes an allocation from the gap between adjacent offsets, so every offset
        // is checked here rather than there: each is attacker-chosen and independent, and
        // validating only offsets[count] leaves the other count of them free to name any
        // address. Checking once at Read keeps GetBytes (per glyph, per subr call) branch-free.
        var previous = (long)dataBase;
        for (var i = 0; i <= count; i++)
        {
            var raw = ReadOffset(data, offsetArrayStart + (i * offSize), offSize);

            // 1-based, so raw 0 would address the byte before the data block. Widened to long
            // because a 4-byte offset spans the full uint range and would otherwise wrap.
            if (raw < 1)
            {
                throw new InvalidDataException("CFF INDEX offset is out of range.");
            }

            var absolute = dataBase + raw;
            if (absolute < previous || absolute > data.Length)
            {
                throw new InvalidDataException("CFF INDEX offset extends past the end of the data.");
            }

            previous = absolute;
            offsets[i] = (int)absolute;
        }

        return new CffIndex(data, offsets, offsets[count]);
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

    private static long ReadOffset(byte[] data, int offset, int size)
    {
        Require(data, offset, size);
        var value = 0L;
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
