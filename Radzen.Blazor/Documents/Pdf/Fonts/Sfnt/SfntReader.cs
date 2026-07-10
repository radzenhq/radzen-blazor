#nullable enable
using System;
using System.IO;

namespace Radzen.Documents.Pdf.Fonts.Sfnt;

// Big-endian primitive reader over a byte[] with a movable cursor.
internal sealed class SfntReader(byte[] data, int position = 0)
{
    private readonly byte[] data = data ?? throw new ArgumentNullException(nameof(data));

    public int Position { get; set; } = position;

    public int Length => data.Length;

    private void Require(int count)
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
        Require(2);
        var value = (ushort)((data[Position] << 8) | data[Position + 1]);
        Position += 2;
        return value;
    }

    public short ReadInt16() => (short)ReadUInt16();

    public uint ReadUInt32()
    {
        Require(4);
        var value = ((uint)data[Position] << 24)
            | ((uint)data[Position + 1] << 16)
            | ((uint)data[Position + 2] << 8)
            | data[Position + 3];
        Position += 4;
        return value;
    }

    public int ReadInt32() => (int)ReadUInt32();

    public string ReadTag()
    {
        Require(4);
        var chars = new char[4];
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
