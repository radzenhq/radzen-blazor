#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Fonts.Cff;

namespace Radzen.Blazor.Pdf.Tests;

// Assembles a minimal name-keyed CFF from hand-written DICT fragments so tests can
// pin parsing of operand encodings that no shipped fixture exercises (real-number
// nibble operands, non-default FontMatrix, malformed operand counts). Layout:
// header, Name INDEX ("TestFont"), Top DICT INDEX, empty String INDEX, empty
// Global Subr INDEX, CharStrings INDEX, Private DICT. All Top DICT offsets use the
// fixed 5-byte integer form so section positions resolve in a single pass.
internal static class CffFixtureBuilder
{
    public static byte[] Build(byte[] privateBody, byte[][] charStrings, byte[]? topDictExtra = null)
    {
        var extra = topDictExtra ?? [];
        return Build(privateBody, charStrings, (cs, priv) =>
        {
            var dict = new List<byte>(extra);
            Int5(dict, cs);
            dict.Add(17);
            Int5(dict, privateBody.Length);
            Int5(dict, priv);
            dict.Add(18);
            return [.. dict];
        });
    }

    // makeTopDict receives (charStringsOffset, privateOffset) and must return a dict
    // whose length does not depend on the offset values (use Int5 for offsets).
    public static byte[] Build(byte[] privateBody, byte[][] charStrings, Func<int, int, byte[]> makeTopDict)
    {
        var nameIndex = CffIndex.Write([Encoding.ASCII.GetBytes("TestFont")]);
        var csIndex = CffIndex.Write(charStrings);

        var topDictLen = makeTopDict(0, 0).Length;
        var topIndexLen = CffIndex.Write([new byte[topDictLen]]).Length;

        const int headerLen = 4;
        var csOffset = headerLen + nameIndex.Length + topIndexLen + 2 + 2;
        var privOffset = csOffset + csIndex.Length;

        var topDict = makeTopDict(csOffset, privOffset);
        if (topDict.Length != topDictLen)
        {
            throw new InvalidOperationException("Top DICT length changed between passes.");
        }

        var result = new List<byte> { 1, 0, headerLen, 1 };
        result.AddRange(nameIndex);
        result.AddRange(CffIndex.Write([topDict]));
        result.AddRange([0, 0]); // String INDEX (empty)
        result.AddRange([0, 0]); // Global Subr INDEX (empty)
        result.AddRange(csIndex);
        result.AddRange(privateBody);
        return [.. result];
    }

    public static void Int5(List<byte> dst, int value)
    {
        dst.Add(29);
        dst.Add((byte)(value >> 24));
        dst.Add((byte)(value >> 16));
        dst.Add((byte)(value >> 8));
        dst.Add((byte)value);
    }
}
