using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Documents.Pdf.Objects;

internal static class PdfBytes
{
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

    // Finds the last startxref keyword and returns the cross-reference offset it points at.
    internal static long FindStartXref(byte[] data)
    {
        const string pattern = "startxref";
        for (var i = data.Length - pattern.Length; i >= 0; i--)
        {
            if (!Matches(data, i, pattern))
            {
                continue;
            }

            var index = i + pattern.Length;
            while (index < data.Length && Lexer.IsWhitespace(data[index]))
            {
                index++;
            }

            var start = index;
            if (index < data.Length && (data[index] == (byte)'+' || data[index] == (byte)'-'))
            {
                index++;
            }

            while (index < data.Length && data[index] >= (byte)'0' && data[index] <= (byte)'9')
            {
                index++;
            }

            if (index == start)
            {
                throw new DocumentParseException("Expected integer after startxref.", start);
            }

            return long.Parse(Encoding.Latin1.GetString(data, start, index - start), CultureInfo.InvariantCulture);
        }

        throw new DocumentParseException("Missing startxref.", -1);
    }

    // Minimum number of bytes needed to hold value in a cross-reference stream field.
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

    internal static void WriteBigEndian(Stream stream, long value, int width)
    {
        for (var i = width - 1; i >= 0; i--)
        {
            stream.WriteByte((byte)(value >> (8 * i)));
        }
    }

    internal static void WriteBigEndian(byte[] data, ref int pos, long value, int width)
    {
        for (var i = width - 1; i >= 0; i--)
        {
            data[pos + i] = (byte)value;
            value >>= 8;
        }

        pos += width;
    }

    // Writes one in-use classic cross-reference entry: a 10-digit offset, generation 0,
    // and the "n" flag with the mandatory trailing space and newline.
    internal static void WriteXrefEntry(Stream stream, long offset)
    {
        Span<char> padded = stackalloc char[20];
        offset.TryFormat(padded, out var written, "D10", CultureInfo.InvariantCulture);
        WriteAscii(stream, padded[..written]);
        WriteAscii(stream, " 00000 n \n");
    }

    internal static void WriteAscii(Stream stream, string text) => WriteAscii(stream, text.AsSpan());

    internal static void WriteAscii(Stream stream, ReadOnlySpan<char> text)
    {
        Span<byte> buffer = stackalloc byte[256];
        while (!text.IsEmpty)
        {
            var chunk = Math.Min(text.Length, buffer.Length);
            for (var i = 0; i < chunk; i++)
            {
                buffer[i] = (byte)text[i];
            }

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

// One cross-reference stream row: the type byte plus its two /W-sized fields.
internal readonly record struct XrefRow(byte Type, long Field2, long Field3);

internal static class XrefStreamPacker
{
    // Packs rows into a Flate-encoded /Type /XRef stream with /W widths derived from the
    // rows themselves. The caller supplies the one key that distinguishes the two shapes:
    // /Size for a contiguous full-save table, /Index for incremental subsections. It is
    // stamped after /Type so the emitted key order matches each caller's original.
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

        var xref = new StreamObject(FlateFilter.Encode(data));
        xref.Dictionary["Type"] = new NameObject("XRef");
        xref.Dictionary[key] = value;
        xref.Dictionary["W"] = new ArrayObject { new NumberObject(1), new NumberObject(w1), new NumberObject(w2) };
        xref.Dictionary["Filter"] = new NameObject("FlateDecode");

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
