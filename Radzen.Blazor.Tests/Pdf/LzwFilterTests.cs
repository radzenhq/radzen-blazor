#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class LzwFilterTests
{
    // ISO 32000-1 worked example: the encoded stream 80 0B 60 50 22 0C 0C 85 01
    // decodes to the byte sequence 45 45 45 45 45 65 45 45 45 66 (decimal).
    // Verified by hand-simulating the LZW state machine and with a python3 reference.
    [Fact]
    public void Decode_Iso32000_WorkedExample()
    {
        var input = new byte[] { 0x80, 0x0B, 0x60, 0x50, 0x22, 0x0C, 0x0C, 0x85, 0x01 };
        var expected = new byte[] { 45, 45, 45, 45, 45, 65, 45, 45, 45, 66 };
        Assert.Equal(expected, LzwFilter.Decode(input, early: 1));
    }

    [Fact]
    public void Decode_Empty_ReturnsEmpty()
    {
        Assert.Empty(LzwFilter.Decode(Array.Empty<byte>(), early: 1));
    }

    // Round-trip with the default early-change (1). The encoder helper below is part of
    // the test contract; it mirrors the decoder's early-change rule so streams it produces
    // must decode back to the original.
    [Fact]
    public void Decode_RoundTrip_EarlyChange1()
    {
        var data = MakeDeterministic(2000);
        var encoded = LzwEncode(data, early: 1);
        Assert.Equal(data, LzwFilter.Decode(encoded, early: 1));
    }

    // Round-trip with early-change disabled (0). The bit layout differs from early=1.
    [Fact]
    public void Decode_RoundTrip_EarlyChange0()
    {
        var data = MakeDeterministic(2000);
        var encoded = LzwEncode(data, early: 0);
        Assert.Equal(data, LzwFilter.Decode(encoded, early: 0));
    }

    // early=1 and early=0 pick different code-width boundaries, so the encoded streams
    // for the same input differ once the table grows past the first boundary.
    [Fact]
    public void EarlyChange_ChangesEncoding()
    {
        var data = MakeDeterministic(2000);
        var e1 = LzwEncode(data, early: 1);
        var e0 = LzwEncode(data, early: 0);
        Assert.NotEqual(e1, e0);
    }

    // Code-width growth: a 2000-byte input pushes the table past 511 and 1023 entries,
    // exercising 10- and 11-bit codes. The stream is produced by the inline encoder.
    [Fact]
    public void Decode_CodeWidthGrowth_Past11Bits()
    {
        var data = MakeDeterministic(4000);
        var encoded = LzwEncode(data, early: 1);
        Assert.Equal(data, LzwFilter.Decode(encoded, early: 1));
    }

    static byte[] MakeDeterministic(int n)
    {
        var data = new byte[n];
        for (int i = 0; i < n; i++)
        {
            data[i] = (byte)((i * 7 + 3) & 0xFF);
        }
        return data;
    }

    // Reference LZW encoder (MSB-first bit packing, 9..12 bit codes, clear=256, eod=257).
    // Width increases when nextCode + early - 1 == 2^width, the mirror of the decoder rule
    // (decoder increases when nextCode + early == 2^width, one entry behind the encoder).
    static byte[] LzwEncode(byte[] data, int early)
    {
        const int Clear = 256, Eod = 257;
        var table = new Dictionary<string, int>();
        void Reset()
        {
            table.Clear();
            for (int i = 0; i < 256; i++) table[((char)i).ToString()] = i;
        }
        Reset();
        int next = 258, width = 9;
        long bitBuf = 0; int bitCnt = 0;
        var outBytes = new List<byte>();

        void Emit(int code)
        {
            bitBuf = (bitBuf << width) | (uint)code;
            bitCnt += width;
            while (bitCnt >= 8)
            {
                bitCnt -= 8;
                outBytes.Add((byte)((bitBuf >> bitCnt) & 0xFF));
            }
            bitBuf &= (1L << bitCnt) - 1;
        }

        Emit(Clear);
        string w = "";
        foreach (var b in data)
        {
            string c = ((char)b).ToString();
            string wc = w + c;
            if (table.ContainsKey(wc))
            {
                w = wc;
            }
            else
            {
                Emit(table[w]);
                table[wc] = next++;
                if (next + early - 1 == (1 << width) && width < 12) width++;
                w = c;
            }
        }
        if (w.Length > 0) Emit(table[w]);
        Emit(Eod);
        if (bitCnt > 0)
        {
            outBytes.Add((byte)((bitBuf << (8 - bitCnt)) & 0xFF));
        }
        return outBytes.ToArray();
    }
}
