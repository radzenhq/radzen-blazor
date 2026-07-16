#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Token = Radzen.Documents.Pdf.Objects.Token;
using TokenKind = Radzen.Documents.Pdf.Objects.TokenKind;

namespace Radzen.Blazor.Pdf.Tests;

// Lexing dominates document open: a large PDF yields tens of millions of tokens, so the
// per-token cost must not be a heap allocation. Tokens carry no identity, numbers and
// keywords can be decoded straight from the source buffer, and unescaped names are the
// overwhelmingly common case. These pin the cost model, not the grammar (see LexerTests).
public class LexerAllocationTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Measure(Action action)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static long LexAllBytes(string text, int count)
    {
        var data = Encoding.Latin1.GetBytes(text);

        Drain(data);

        return Measure(() =>
        {
            for (var i = 0; i < count; i++)
            {
                Drain(data);
            }
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Drain(byte[] data)
    {
        var lexer = new Lexer(data, 0);
        var total = 0;
        while (true)
        {
            var token = lexer.Next();
            if (token.Kind == TokenKind.EndOfData)
            {
                return total;
            }

            total++;
        }
    }

    [Fact]
    public void IntegersAndDelimiters_DoNotAllocate()
    {
        var builder = new StringBuilder("[");
        for (var i = 0; i < 500; i++)
        {
            builder.Append(i).Append(' ');
        }

        builder.Append(']');

        var bytes = LexAllBytes(builder.ToString(), 20);

        Assert.True(bytes < 4096, $"Lexing 502 integer/delimiter tokens 20 times allocated {bytes} bytes.");
    }

    [Fact]
    public void Reals_DoNotAllocate()
    {
        var builder = new StringBuilder("[");
        for (var i = 0; i < 500; i++)
        {
            builder.Append(i).Append(".25 ");
        }

        builder.Append(']');

        var bytes = LexAllBytes(builder.ToString(), 20);

        Assert.True(bytes < 4096, $"Lexing 500 real tokens 20 times allocated {bytes} bytes.");
    }

    [Fact]
    public void IndirectReferences_DoNotAllocate()
    {
        var builder = new StringBuilder("[");
        for (var i = 1; i <= 500; i++)
        {
            builder.Append(i).Append(" 0 R ");
        }

        builder.Append(']');

        var bytes = LexAllBytes(builder.ToString(), 20);

        Assert.True(bytes < 4096, $"Lexing 500 'n 0 R' triples 20 times allocated {bytes} bytes.");
    }

    [Fact]
    public void StandardNames_DoNotAllocate()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < 500; i++)
        {
            builder.Append("/Type /Page /Length /Filter /Font ");
        }

        var bytes = LexAllBytes(builder.ToString(), 20);

        // 50,000 name tokens, all of them standard: under a byte each, against ~200 when
        // every name built a List, a copied array and a fresh string.
        Assert.True(bytes < 50_000, $"Lexing 2500 standard name tokens 20 times allocated {bytes} bytes.");
    }

    [Fact]
    public void UnescapedName_AllocatesOnlyTheString()
    {
        var data = Encoding.Latin1.GetBytes("/NotAStandardPdfNameAtAll");

        Drain(data);

        var bytes = Measure(() => Drain(data));

        // The Lexer itself plus one 25-char string, and nothing else: no List, no copy.
        Assert.True(bytes <= 128, $"Lexing a single unescaped name allocated {bytes} bytes.");
    }

    [Fact]
    public void EscapedName_StillDecodes()
    {
        var token = new Lexer(Encoding.Latin1.GetBytes("/A#20B#23C"), 0).Next();

        Assert.Equal(TokenKind.Name, token.Kind);
        Assert.Equal("A B#C", token.Text);
    }

    [Fact]
    public void Token_IsAValueType()
    {
        Assert.True(typeof(Token).IsValueType, "Token is a heap-allocated class; one instance per token dominates parse cost.");
    }
}
