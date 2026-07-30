using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

internal sealed class FixturePdf
{
    private readonly MemoryStream buffer = new();
    private readonly Dictionary<int, long> offsets = new();

    public long Position => buffer.Position;

    public FixturePdf Append(string ascii)
    {
        var bytes = Encoding.Latin1.GetBytes(ascii);
        buffer.Write(bytes, 0, bytes.Length);
        return this;
    }

    public FixturePdf Append(byte[] raw)
    {
        buffer.Write(raw, 0, raw.Length);
        return this;
    }

    public FixturePdf Object(int number, string body)
    {
        offsets[number] = buffer.Position;
        return Append(body);
    }

    public FixturePdf Mark(int number)
    {
        offsets[number] = buffer.Position;
        return this;
    }

    public long OffsetOf(int number) => offsets[number];

    public byte[] ToArray() => buffer.ToArray();

    public static string Entry20(long offset, int generation = 0, char type = 'n')
        => offset.ToString("D10", CultureInfo.InvariantCulture)
           + " " + generation.ToString("D5", CultureInfo.InvariantCulture)
           + " " + type + " \n";

    // ISO 32000-1 7.5.8.3: xref stream entry with W=[1 2 1] - 1-byte type, 2-byte field2, 1-byte field3.
    public static byte[] XrefStreamEntry(int type, int field2, int field3)
        => new[] { (byte)type, (byte)((field2 >> 8) & 0xFF), (byte)(field2 & 0xFF), (byte)field3 };

    public static byte[] Wrap(FixturePdf pdf, int count)
    {
        var xref = pdf.Position;
        pdf.Append("xref\n0 " + count + "\n");
        pdf.Append(Entry20(0, 65535, 'f'));
        for (var number = 1; number < count; number++)
        {
            pdf.Append(Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size " + count + " /Root 1 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }
}
