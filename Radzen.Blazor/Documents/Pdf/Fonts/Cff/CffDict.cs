using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Fonts.Cff;

// CFF DICT (spec section 4): operands precede an operator. Two-byte operators use the
// 12 escape and are keyed here as 1200 + b1. Real operands (b0 == 30) are decoded from
// packed BCD nibbles.
internal static class CffDict
{
    private const int TwoByteOperator = 12;

    public static Dictionary<int, double[]> Parse(byte[] data)
    {
        var result = new Dictionary<int, double[]>();
        List<double> operands = [];
        var i = 0;
        while (i < data.Length)
        {
            int b0 = data[i];
            if (b0 <= 21)
            {
                var op = b0;
                i++;
                if (b0 == TwoByteOperator)
                {
                    op = 1200 + data[i];
                    i++;
                }

                result[op] = [.. operands];
                operands.Clear();
            }
            else if (b0 == 28)
            {
                operands.Add((short)((data[i + 1] << 8) | data[i + 2]));
                i += 3;
            }
            else if (b0 == 29)
            {
                operands.Add((data[i + 1] << 24) | (data[i + 2] << 16) | (data[i + 3] << 8) | data[i + 4]);
                i += 5;
            }
            else if (b0 == 30)
            {
                i = ParseReal(data, i + 1, out var real);
                operands.Add(real);
            }
            else if (b0 <= 246)
            {
                operands.Add(b0 - 139);
                i++;
            }
            else if (b0 <= 250)
            {
                operands.Add(((b0 - 247) * 256) + data[i + 1] + 108);
                i += 2;
            }
            else if (b0 <= 254)
            {
                operands.Add((-(b0 - 251) * 256) - data[i + 1] - 108);
                i += 2;
            }
            else
            {
                i++;
            }
        }

        return result;
    }

    public static void WriteInteger(List<byte> dst, int value)
    {
        if (value is >= -107 and <= 107)
        {
            dst.Add((byte)(value + 139));
        }
        else if (value is >= 108 and <= 1131)
        {
            var w = value - 108;
            dst.Add((byte)((w >> 8) + 247));
            dst.Add((byte)(w & 0xff));
        }
        else if (value is >= -1131 and <= -108)
        {
            var w = -value - 108;
            dst.Add((byte)((w >> 8) + 251));
            dst.Add((byte)(w & 0xff));
        }
        else if (value is >= -32768 and <= 32767)
        {
            dst.Add(28);
            dst.Add((byte)(value >> 8));
            dst.Add((byte)value);
        }
        else
        {
            WriteInteger32(dst, value);
        }
    }

    // Forces the 5-byte 32-bit integer form so an operand's encoded width is stable
    // regardless of the value; used for placeholder offsets resolved in a later pass.
    public static void WriteOffset(List<byte> dst, int value) => WriteInteger32(dst, value);

    public static void WriteNumber(List<byte> dst, double value)
    {
        if (value == System.Math.Floor(value) && value is >= int.MinValue and <= int.MaxValue)
        {
            WriteInteger(dst, (int)value);
        }
        else
        {
            WriteReal(dst, value);
        }
    }

    public static void WriteOperator(List<byte> dst, int op)
    {
        if (op >= 1200)
        {
            dst.Add(TwoByteOperator);
            dst.Add((byte)(op - 1200));
        }
        else
        {
            dst.Add((byte)op);
        }
    }

    private static void WriteInteger32(List<byte> dst, int value)
    {
        dst.Add(29);
        dst.Add((byte)(value >> 24));
        dst.Add((byte)(value >> 16));
        dst.Add((byte)(value >> 8));
        dst.Add((byte)value);
    }

    private static void WriteReal(List<byte> dst, double value)
    {
        var s = value.ToString("R", CultureInfo.InvariantCulture);
        var nibbles = new List<byte>();
        for (var k = 0; k < s.Length; k++)
        {
            var c = s[k];
            if (c is >= '0' and <= '9')
            {
                nibbles.Add((byte)(c - '0'));
            }
            else if (c == '.')
            {
                nibbles.Add(0xa);
            }
            else if (c == '-')
            {
                nibbles.Add(0xe);
            }
            else if (c is 'E' or 'e')
            {
                if (k + 1 < s.Length && s[k + 1] == '-')
                {
                    nibbles.Add(0xc);
                    k++;
                }
                else
                {
                    nibbles.Add(0xb);
                    if (k + 1 < s.Length && s[k + 1] == '+')
                    {
                        k++;
                    }
                }
            }
        }

        nibbles.Add(0xf);
        if (nibbles.Count % 2 == 1)
        {
            nibbles.Add(0xf);
        }

        dst.Add(30);
        for (var k = 0; k < nibbles.Count; k += 2)
        {
            dst.Add((byte)((nibbles[k] << 4) | nibbles[k + 1]));
        }
    }

    private static int ParseReal(byte[] data, int i, out double value)
    {
        var text = new System.Text.StringBuilder();
        var done = false;
        while (i < data.Length && !done)
        {
            var b = data[i++];
            for (var shift = 4; shift >= 0 && !done; shift -= 4)
            {
                var nibble = (b >> shift) & 0x0f;
                switch (nibble)
                {
                    case <= 9:
                        text.Append((char)('0' + nibble));
                        break;
                    case 0xa:
                        text.Append('.');
                        break;
                    case 0xb:
                        text.Append('E');
                        break;
                    case 0xc:
                        text.Append("E-");
                        break;
                    case 0xe:
                        text.Append('-');
                        break;
                    case 0xf:
                        done = true;
                        break;
                    default:
                        break;
                }
            }
        }

        value = double.TryParse(text.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
        return i;
    }
}
