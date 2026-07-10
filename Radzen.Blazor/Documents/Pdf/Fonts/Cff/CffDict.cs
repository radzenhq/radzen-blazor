#nullable enable
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Fonts.Cff;

// CFF DICT (spec section 4): operands precede an operator. Two-byte operators use the
// 12 escape and are keyed here as 1200 + b1. Real operands (b0 == 30) are skipped since
// every operator this parser consumes carries integer operands.
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
                i = SkipReal(data, i + 1);
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

    private static int SkipReal(byte[] data, int i)
    {
        while (i < data.Length)
        {
            var b = data[i++];
            if ((b >> 4) == 0x0f || (b & 0x0f) == 0x0f)
            {
                break;
            }
        }

        return i;
    }
}
