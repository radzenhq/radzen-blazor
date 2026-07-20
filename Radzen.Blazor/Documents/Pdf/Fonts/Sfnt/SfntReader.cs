using System;
using System.IO;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Fonts.Sfnt;

internal struct SfntReader(byte[] data, int position = 0)
{
    private readonly byte[] data = data ?? throw new ArgumentNullException(nameof(data));

    public int Position { get; set; } = position;

    public readonly int Length => data.Length;

    public static bool TryReadTagValue(ReadOnlySpan<byte> data, out uint value)
    {
        if (data.Length < 4)
        {
            value = 0;
            return false;
        }

        value = PdfBytes.ReadUInt32BigEndian(data, 0, "Font data is too short to contain a valid header.");
        return true;
    }

    private readonly void Require(int count)
    {
        if (Position < 0 || Position + count > data.Length)
        {
            throw new InvalidDataException("Attempt to read past the end of the sfnt data.");
        }
    }

    public byte ReadByte()
    {
        Require(1);
        return data[Position++];
    }

    public ushort ReadUInt16()
    {
        var value = PdfBytes.ReadUInt16BigEndian(
            data, Position, "Attempt to read past the end of the sfnt data.");
        Position += 2;
        return value;
    }

    public short ReadInt16() => (short)ReadUInt16();

    public uint ReadUInt32()
    {
        var value = PdfBytes.ReadUInt32BigEndian(
            data, Position, "Attempt to read past the end of the sfnt data.");
        Position += 4;
        return value;
    }

    public int ReadInt32() => (int)ReadUInt32();

    public string ReadTag()
    {
        Require(4);
        Span<char> chars = stackalloc char[4];
        for (var i = 0; i < 4; i++)
        {
            chars[i] = (char)data[Position + i];
        }

        Position += 4;
        return new string(chars);
    }

    public ushort ReadUInt16At(int offset)
    {
        Position = offset;
        return ReadUInt16();
    }

    public short ReadInt16At(int offset)
    {
        Position = offset;
        return ReadInt16();
    }
}
