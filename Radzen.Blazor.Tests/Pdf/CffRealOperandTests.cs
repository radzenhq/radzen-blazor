#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Cff;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// CFF real-number operands, spec 5176 section 4: b0 = 30, packed BCD nibbles (0-9 digits, a '.', b 'E', c 'E-', e '-', f end).
public class CffRealOperandTests
{
    private static readonly byte[] Real658Point5 = [30, 0x65, 0x8A, 0x5F];

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
        var dict = CffDict.Parse([30, 0xE2, 0xA5, 0xFF, 20]);

        Assert.True(dict.TryGetValue(20, out var operands));
        Assert.Equal(-2.5, Assert.Single(operands!), 9);
    }

    [Fact]
    public void Parse_RealOperand_DecodesPositiveExponent()
    {
        var dict = CffDict.Parse([30, 0x1B, 0x3F, 20]);

        Assert.True(dict.TryGetValue(20, out var operands));
        Assert.Equal(1000.0, Assert.Single(operands!), 9);
    }

    [Fact]
    public void Parse_RealOperand_DecodesNegativeExponent()
    {
        var dict = CffDict.Parse([30, 0x25, 0xC2, 0xFF, 20]);

        Assert.True(dict.TryGetValue(20, out var operands));
        Assert.Equal(0.25, Assert.Single(operands!), 9);
    }

    [Fact]
    public void Parse_RealOperand_MixesWithIntegers()
    {
        var dict = CffDict.Parse([181, .. Real5Point5, 6]);

        Assert.True(dict.TryGetValue(6, out var operands));
        Assert.Equal(2, operands!.Length);
        Assert.Equal(42.0, operands[0], 9);
        Assert.Equal(5.5, operands[1], 9);
    }

    private static byte[] RealWidthPrivateDict() => [.. Real658Point5, 20, .. Real5Point5, 21];

    private static byte[][] ThreeCharStrings() => [[0x0E], [0x0E], [239, 0x0E]];

    [Fact]
    public void Parse_RealDefaultWidthX_UsedForWidthlessGlyph()
    {
        var font = CffFont.Parse(CffFixtureBuilder.Build(RealWidthPrivateDict(), ThreeCharStrings()));

        Assert.Equal(659, font.GetAdvanceWidth(1));
    }

    [Fact]
    public void Parse_RealNominalWidthX_AddedToWidthOperand()
    {
        var font = CffFont.Parse(CffFixtureBuilder.Build(RealWidthPrivateDict(), ThreeCharStrings()));

        Assert.Equal(106, font.GetAdvanceWidth(2));
    }

    [Fact]
    public void Parse_PrivateOperatorWithSingleOperand_DoesNotThrowIndexOutOfRange()
    {
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
