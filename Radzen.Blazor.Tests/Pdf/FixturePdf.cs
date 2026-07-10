using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

// Test-side PDF assembler. It concatenates hand-written raw fragments and
// records the byte offset of each indirect object as it is appended, so the
// cross-reference offsets the tests feed to the reader are computed here (and
// stay visible/reviewable) rather than being lifted from DocumentWriter output.
// Uses only the BCL - deliberately independent of the library under test.
internal sealed class FixturePdf
{
    private readonly MemoryStream buffer = new();
    private readonly Dictionary<int, long> offsets = new();

    // Current write position - i.e. the offset the next appended byte will occupy.
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

    // Records the current position as the start of object `number`, then appends
    // `body` verbatim. `body` must be the full "N G obj\n...\nendobj\n" text.
    public FixturePdf Object(int number, string body)
    {
        offsets[number] = buffer.Position;
        return Append(body);
    }

    // Records the current position as the start of object `number` without
    // appending anything (for objects whose body is streamed via later Append calls).
    public FixturePdf Mark(int number)
    {
        offsets[number] = buffer.Position;
        return this;
    }

    public long OffsetOf(int number) => offsets[number];

    public byte[] ToArray() => buffer.ToArray();

    // A classic 20-byte cross-reference entry: "nnnnnnnnnn ggggg t \n".
    public static string Entry20(long offset, int generation = 0, char type = 'n')
        => offset.ToString("D10", CultureInfo.InvariantCulture)
           + " " + generation.ToString("D5", CultureInfo.InvariantCulture)
           + " " + type + " \n";

    // A cross-reference stream entry encoded with W = [1 2 1] (ISO 32000-1
    // 7.5.8.3): 1-byte type, 2-byte big-endian field2, 1-byte field3.
    // Type 1 => field2 is the byte offset, field3 the generation.
    // Type 2 => field2 is the object stream number, field3 the index within it.
    public static byte[] XrefStreamEntry(int type, int field2, int field3)
        => new[] { (byte)type, (byte)((field2 >> 8) & 0xFF), (byte)(field2 & 0xFF), (byte)field3 };
}
