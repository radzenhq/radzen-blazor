#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

internal static class SfntChecksumValidator
{
    internal readonly record struct Entry(uint Checksum, uint Offset, uint Length);

    public static IReadOnlyDictionary<string, Entry> ReadDirectory(byte[] data)
    {
        var numTables = ReadUInt16(data, 4);
        var tables = new Dictionary<string, Entry>(StringComparer.Ordinal);
        for (var i = 0; i < numTables; i++)
        {
            var rec = 12 + i * 16;
            var tag = Encoding.ASCII.GetString(data, rec, 4);
            tables[tag] = new Entry(ReadUInt32(data, rec + 4), ReadUInt32(data, rec + 8), ReadUInt32(data, rec + 12));
        }

        return tables;
    }

    public static uint[] ReadLocaOffsets(byte[] data)
    {
        var dir = ReadDirectory(data);
        var head = dir["head"];
        var maxp = dir["maxp"];
        var loca = dir["loca"];

        var numGlyphs = ReadUInt16(data, (int)maxp.Offset + 4);
        var longFormat = ReadInt16(data, (int)head.Offset + 50) != 0;

        var offsets = new uint[numGlyphs + 1];
        for (var i = 0; i <= numGlyphs; i++)
        {
            offsets[i] = longFormat
                ? ReadUInt32(data, (int)loca.Offset + i * 4)
                : (uint)ReadUInt16(data, (int)loca.Offset + i * 2) * 2;
        }

        return offsets;
    }

    public static short IndexToLocFormat(byte[] data)
        => ReadInt16(data, (int)ReadDirectory(data)["head"].Offset + 50);

    public static void AssertTableChecksums(byte[] data)
    {
        var dir = ReadDirectory(data);
        foreach (var (tag, entry) in dir)
        {
            var buf = new byte[entry.Length];
            Array.Copy(data, (int)entry.Offset, buf, 0, (int)entry.Length);
            if (tag == "head")
            {
                buf[8] = buf[9] = buf[10] = buf[11] = 0;
            }

            var calc = TableChecksum(buf, 0, buf.Length);
            if (calc != entry.Checksum)
            {
                throw new Xunit.Sdk.XunitException($"Checksum mismatch for '{tag}': stored 0x{entry.Checksum:X8} calc 0x{calc:X8}.");
            }
        }
    }

    public static void AssertHeadAdjustment(byte[] data)
    {
        var dir = ReadDirectory(data);
        var head = dir["head"];
        var stored = ReadUInt32(data, (int)head.Offset + 8);

        var whole = (byte[])data.Clone();
        var adj = (int)head.Offset + 8;
        whole[adj] = whole[adj + 1] = whole[adj + 2] = whole[adj + 3] = 0;

        var expected = unchecked(0xB1B0AFBAu - TableChecksum(whole, 0, whole.Length));
        if (stored != expected)
        {
            throw new Xunit.Sdk.XunitException($"head.checkSumAdjustment 0x{stored:X8} != expected 0x{expected:X8}.");
        }
    }

    private static uint TableChecksum(byte[] data, int offset, int length)
    {
        uint sum = 0;
        var i = offset;
        var end = offset + length;
        while (i + 4 <= end)
        {
            sum = unchecked(sum + ReadUInt32(data, i));
            i += 4;
        }

        if (i < end)
        {
            uint tail = 0;
            for (var shift = 24; i < end; i++, shift -= 8)
            {
                tail |= (uint)data[i] << shift;
            }

            sum = unchecked(sum + tail);
        }

        return sum;
    }

    private static ushort ReadUInt16(byte[] d, int o) => (ushort)((d[o] << 8) | d[o + 1]);

    private static short ReadInt16(byte[] d, int o) => (short)((d[o] << 8) | d[o + 1]);

    private static uint ReadUInt32(byte[] d, int o)
        => ((uint)d[o] << 24) | ((uint)d[o + 1] << 16) | ((uint)d[o + 2] << 8) | d[o + 3];
}
