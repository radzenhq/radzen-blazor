#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Cff;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// G5(a): CFF real-number operands (spec 5176 section 4, b0 = 30, packed BCD
// nibbles: 0-9 digits, a '.', b 'E', c 'E-', e '-', f end). CffDict.Parse must
// decode them onto the operand list; legitimate OTFs encode Private DICT
// defaultWidthX/nominalWidthX as reals, and skipping them leaves an empty operand
// array that CffFont.ReadPrivate then indexes (dw[0]) out of range. Operand
// indexing must be guarded by length checks so a short operand list surfaces as a
// parse error, never IndexOutOfRangeException.
public class CffRealOperandTests
{
    // "658.5": nibbles 6 5 8 a 5 f.
    private static readonly byte[] Real658Point5 = [30, 0x65, 0x8A, 0x5F];

    // "5.5": nibbles 5 a 5 f.
    private static readonly byte[] Real5Point5 = [30, 0x5A, 0x5F];

    [Fact]
    public void Parse_RealOperand_DecodesFraction()
    {
        var dict = CffDict.Parse([.. Real658Point5, 20]);

        Assert.True(dict.TryGetValue(20, out var operands));
        Assert.Equal(658.5, Assert.Single(operands!), 9);
    }

    [Fact]
    public void Parse_RealOperand_DecodesNegative()
    {
        // "-2.5": nibbles e 2 a 5 f, padded with a second f.
        var dict = CffDict.Parse([30, 0xE2, 0xA5, 0xFF, 20]);

        Assert.True(dict.TryGetValue(20, out var operands));
        Assert.Equal(-2.5, Assert.Single(operands!), 9);
    }

    [Fact]
    public void Parse_RealOperand_DecodesPositiveExponent()
    {
        // "1E3": nibbles 1 b 3 f.
        var dict = CffDict.Parse([30, 0x1B, 0x3F, 20]);

        Assert.True(dict.TryGetValue(20, out var operands));
        Assert.Equal(1000.0, Assert.Single(operands!), 9);
    }

    [Fact]
    public void Parse_RealOperand_DecodesNegativeExponent()
    {
        // "25E-2": nibbles 2 5 c 2 f, padded with a second f.
        var dict = CffDict.Parse([30, 0x25, 0xC2, 0xFF, 20]);

        Assert.True(dict.TryGetValue(20, out var operands));
        Assert.Equal(0.25, Assert.Single(operands!), 9);
    }

    [Fact]
    public void Parse_RealOperand_MixesWithIntegers()
    {
        // Integer 42 (42 + 139 = 181) followed by real 5.5, operator 6.
        var dict = CffDict.Parse([181, .. Real5Point5, 6]);

        Assert.True(dict.TryGetValue(6, out var operands));
        Assert.Equal(2, operands!.Length);
        Assert.Equal(42.0, operands[0], 9);
        Assert.Equal(5.5, operands[1], 9);
    }

    // Private DICT: defaultWidthX 658.5 (real), nominalWidthX 5.5 (real).
    private static byte[] RealWidthPrivateDict() => [.. Real658Point5, 20, .. Real5Point5, 21];

    // gid 0/1: bare endchar (no width operand -> defaultWidthX).
    // gid 2: "100 endchar" (width operand -> nominalWidthX + 100).
    private static byte[][] ThreeCharStrings() => [[0x0E], [0x0E], [239, 0x0E]];

    [Fact]
    public void Parse_RealDefaultWidthX_UsedForWidthlessGlyph()
    {
        var font = CffFont.Parse(CffFixtureBuilder.Build(RealWidthPrivateDict(), ThreeCharStrings()));

        // 658.5 rounded away from zero.
        Assert.Equal(659, font.GetAdvanceWidth(1));
    }

    [Fact]
    public void Parse_RealNominalWidthX_AddedToWidthOperand()
    {
        var font = CffFont.Parse(CffFixtureBuilder.Build(RealWidthPrivateDict(), ThreeCharStrings()));

        // nominalWidthX 5.5 + operand 100 = 105.5, rounded away from zero.
        Assert.Equal(106, font.GetAdvanceWidth(2));
    }

    [Fact]
    public void Parse_PrivateOperatorWithSingleOperand_DoesNotThrowIndexOutOfRange()
    {
        // Top DICT whose Private operator (18) carries one operand instead of
        // (size, offset). ReadPrivate must length-check before indexing op[1].
        var data = CffFixtureBuilder.Build([0x8B, 20], ThreeCharStrings(), (cs, priv) =>
        {
            var dict = new List<byte>();
            CffFixtureBuilder.Int5(dict, cs);
            dict.Add(17);
            CffFixtureBuilder.Int5(dict, priv);
            dict.Add(18);
            return [.. dict];
        });

        var exception = Record.Exception(() => CffFont.Parse(data).GetAdvanceWidth(1));

        Assert.False(exception is IndexOutOfRangeException,
            $"Malformed operand count must not surface as IndexOutOfRangeException but threw {exception}");
    }
}
