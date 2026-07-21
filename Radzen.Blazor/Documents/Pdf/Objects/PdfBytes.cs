using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Documents.Pdf.Objects;

internal static class PdfBytes
{
    internal static bool Matches(ReadOnlySpan<byte> data, ReadOnlySpan<byte> prefix)
        => data.StartsWith(prefix);

    internal static bool Matches(byte[] data, int index, string pattern)
    {
        if (index < 0 || index + pattern.Length > data.Length)
        {
            return false;
        }

        for (var i = 0; i < pattern.Length; i++)
        {
            if (data[index + i] != (byte)pattern[i])
            {
                return false;
            }
        }

        return true;
    }

    internal static long FindStartXref(byte[] data)
    {
        const string pattern = "startxref";
        for (var i = data.Length - pattern.Length; i >= 0; i--)
        {
            if (!Matches(data, i, pattern))
            {
                continue;
            }

            var index = Lexer.SkipWhitespace(data, i + pattern.Length);
            return ReadInteger(data, ref index, "Expected integer after startxref.", "The startxref offset is out of range.");
        }

        throw new DocumentParseException("Missing startxref.", -1);
    }

    internal static long ReadInteger(byte[] data, ref int index, string emptyError, string overflowError)
    {
        var start = index;
        var negative = false;
        if (index < data.Length && (data[index] == (byte)'+' || data[index] == (byte)'-'))
        {
            negative = data[index] == (byte)'-';
            index++;
        }

        var digits = index;
        long value = 0;
        while (index < data.Length && data[index] >= (byte)'0' && data[index] <= (byte)'9')
        {
            if (value > (long.MaxValue - (data[index] - '0')) / 10)
            {
                throw new DocumentParseException(overflowError, start);
            }

            value = (value * 10) + (data[index] - '0');
            index++;
        }

        if (index == digits)
        {
            throw new DocumentParseException(emptyError, start);
        }

        return negative ? -value : value;
    }

    internal static int FieldWidth(long value)
    {
        var width = 1;
        while (value > 0xFF)
        {
            value >>= 8;
            width++;
        }

        return width;
    }

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

    internal static long ReadBigEndian(ReadOnlySpan<byte> data, ref int position, int width, string errorMessage)
    {
        Require(data, position, width, errorMessage);
        long value = 0;
        for (var i = 0; i < width; i++)
        {
            value = (value << 8) | data[position++];
        }

        return value;
    }

    internal static void WriteBigEndian(Stream stream, long value, int width)
    {
        Span<byte> bytes = stackalloc byte[8];
        WriteBigEndian(bytes[..width], value);
        stream.Write(bytes[..width]);
    }

    internal static void WriteBigEndian(byte[] data, ref int pos, long value, int width)
    {
        WriteBigEndian(data.AsSpan(pos, width), value);
        pos += width;
    }

    internal static void WriteBigEndian(Span<byte> destination, long value)
    {
        for (var i = destination.Length - 1; i >= 0; i--)
        {
            destination[i] = (byte)value;
            value >>= 8;
        }
    }

    private static void Require(ReadOnlySpan<byte> data, int offset, int count, string errorMessage)
    {
        if (offset < 0 || count < 0 || offset > data.Length - count)
        {
            throw new InvalidDataException(errorMessage);
        }
    }

    internal static void WriteXrefEntry(Stream stream, long offset, int generation = 0)
    {
        Span<char> field = stackalloc char[20];
        offset.TryFormat(field, out var written, "D10", CultureInfo.InvariantCulture);
        WriteAscii(stream, field[..written]);
        WriteAscii(stream, " ");
        generation.TryFormat(field, out written, "D5", CultureInfo.InvariantCulture);
        WriteAscii(stream, field[..written]);
        WriteAscii(stream, " n \n");
    }

    internal static void WriteAscii(Stream stream, string text) => WriteAscii(stream, text.AsSpan());

    internal static void WriteAscii(Stream stream, ReadOnlySpan<char> text)
    {
        Span<byte> buffer = stackalloc byte[256];
        while (!text.IsEmpty)
        {
            var chunk = Math.Min(text.Length, buffer.Length);
            Latin1ByteEncoder.Encode(text[..chunk], buffer[..chunk]);
            stream.Write(buffer[..chunk]);
            text = text[chunk..];
        }
    }

    internal static void WriteInteger(Stream stream, long value)
    {
        Span<char> digits = stackalloc char[20];
        value.TryFormat(digits, out var written, provider: CultureInfo.InvariantCulture);
        WriteAscii(stream, digits[..written]);
    }
}

internal readonly record struct XrefRow(byte Type, long Field2, long Field3);

internal static class XrefStreamPacker
{
    internal static StreamObject Pack(IReadOnlyList<XrefRow> rows, string key, DocumentObject value, DictionaryObject trailer)
    {
        var w1 = 1;
        var w2 = 1;
        foreach (var row in rows)
        {
            w1 = Math.Max(w1, PdfBytes.FieldWidth(row.Field2));
            w2 = Math.Max(w2, PdfBytes.FieldWidth(row.Field3));
        }

        var data = new byte[rows.Count * (1 + w1 + w2)];
        var pos = 0;
        foreach (var row in rows)
        {
            data[pos++] = row.Type;
            PdfBytes.WriteBigEndian(data, ref pos, row.Field2, w1);
            PdfBytes.WriteBigEndian(data, ref pos, row.Field3, w2);
        }

        var xref = FlateFilter.EncodeStream(data, dictionary =>
        {
            dictionary["Type"] = new NameObject("XRef");
            dictionary[key] = value;
            dictionary["W"] = new ArrayObject { new NumberObject(1), new NumberObject(w1), new NumberObject(w2) };
        });

        foreach (var pair in trailer)
        {
            if (!xref.Dictionary.ContainsKey(pair.Key))
            {
                xref.Dictionary[pair.Key] = pair.Value;
            }
        }

        return xref;
    }
}
