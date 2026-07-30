using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Token = Radzen.Documents.Pdf.Objects.Token;
using TokenKind = Radzen.Documents.Pdf.Objects.TokenKind;
using Radzen.Documents;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

// ISO 32000-1 section 7.2: token grammar for the Lexer.
public class LexerTests
{
    private static List<Token> LexAll(string text)
    {
        var lexer = new Lexer(Encoding.Latin1.GetBytes(text), 0);
        var tokens = new List<Token>();
        while (true)
        {
            var token = lexer.Next();
            if (token.Kind == TokenKind.EndOfData)
            {
                break;
            }

            tokens.Add(token);
        }

        return tokens;
    }

    private static Token First(string text) => new Lexer(Encoding.Latin1.GetBytes(text), 0).Next();

    private static string StringValue(Token token) => Encoding.Latin1.GetString(token.Bytes);

    [Fact]
    public void Integer_Positive()
    {
        var token = First("123");
        Assert.Equal(TokenKind.Integer, token.Kind);
        Assert.Equal(123L, token.IntValue);
    }

    [Fact]
    public void Integer_ExplicitPlus()
    {
        var token = First("+45");
        Assert.Equal(TokenKind.Integer, token.Kind);
        Assert.Equal(45L, token.IntValue);
    }

    [Fact]
    public void Integer_Negative()
    {
        var token = First("-7");
        Assert.Equal(TokenKind.Integer, token.Kind);
        Assert.Equal(-7L, token.IntValue);
    }

    [Fact]
    public void Real_Simple()
    {
        var token = First("3.5");
        Assert.Equal(TokenKind.Real, token.Kind);
        Assert.Equal(3.5, token.RealValue, 10);
    }

    [Fact]
    public void Real_LeadingDot()
    {
        var token = First(".5");
        Assert.Equal(TokenKind.Real, token.Kind);
        Assert.Equal(0.5, token.RealValue, 10);
    }

    [Fact]
    public void Real_TrailingDot()
    {
        var token = First("17.");
        Assert.Equal(TokenKind.Real, token.Kind);
        Assert.Equal(17.0, token.RealValue, 10);
    }

    [Fact]
    public void Real_NegativeFraction()
    {
        var token = First("-0.002");
        Assert.Equal(TokenKind.Real, token.Kind);
        Assert.Equal(-0.002, token.RealValue, 10);
    }

    [Fact]
    public void Name_Simple()
    {
        var token = First("/Type");
        Assert.Equal(TokenKind.Name, token.Kind);
        Assert.Equal("Type", token.Text);
    }

    [Fact]
    public void Name_HexEscapeUnescaped()
    {
        var token = First("/A#20B");
        Assert.Equal(TokenKind.Name, token.Kind);
        Assert.Equal("A B", token.Text);
    }

    [Fact]
    public void Name_HexEscapeNumberSign()
    {
        var token = First("/paired#23");
        Assert.Equal("paired#", token.Text);
    }

    [Fact]
    public void Name_Empty()
    {
        var token = First("/ 1");
        Assert.Equal(TokenKind.Name, token.Kind);
        Assert.Equal("", token.Text);
    }

    [Fact]
    public void StringLiteral_Plain()
    {
        Assert.Equal("Hello", StringValue(First("(Hello)")));
    }

    [Fact]
    public void StringLiteral_Empty()
    {
        var token = First("()");
        Assert.Equal(TokenKind.StringLiteral, token.Kind);
        Assert.Equal("", StringValue(token));
    }

    [Fact]
    public void StringLiteral_NamedEscapes()
    {
        Assert.Equal("\n\r\t\b\f", StringValue(First("(\\n\\r\\t\\b\\f)")));
    }

    [Fact]
    public void StringLiteral_EscapedParensAndBackslash()
    {
        Assert.Equal("a(b)c\\d", StringValue(First("(a\\(b\\)c\\\\d)")));
    }

    [Fact]
    public void StringLiteral_NestedBalancedParens()
    {
        Assert.Equal("a(b)c", StringValue(First("(a(b)c)")));
    }

    [Fact]
    public void StringLiteral_OctalThreeDigits()
    {
        Assert.Equal("A", StringValue(First("(\\101)")));
    }

    [Fact]
    public void StringLiteral_OctalOneDigit()
    {
        Assert.Equal("", StringValue(First("(\\5)")));
    }

    [Fact]
    public void StringLiteral_OctalTwoDigits()
    {
        Assert.Equal("+", StringValue(First("(\\53)")));
    }

    [Fact]
    public void StringLiteral_LineContinuation()
    {
        Assert.Equal("foobar", StringValue(First("(foo\\\nbar)")));
    }

    [Fact]
    public void HexString_Even()
    {
        Assert.Equal("Hello", StringValue(First("<48656C6C6F>")));
    }

    [Fact]
    public void HexString_OddDigitImpliesTrailingZero()
    {
        Assert.Equal("HelloP", StringValue(First("<48656C6C6F5>")));
    }

    [Fact]
    public void HexString_EmbeddedWhitespaceIgnored()
    {
        Assert.Equal("Hello", StringValue(First("<48 65\n6C\t6C6F>")));
    }

    [Fact]
    public void HexString_Empty()
    {
        var token = First("<>");
        Assert.Equal(TokenKind.HexString, token.Kind);
        Assert.Equal("", StringValue(token));
    }

    [Fact]
    public void Delimiters_ArrayAndDict()
    {
        var tokens = LexAll("[]<<>>");
        Assert.Equal(TokenKind.ArrayOpen, tokens[0].Kind);
        Assert.Equal(TokenKind.ArrayClose, tokens[1].Kind);
        Assert.Equal(TokenKind.DictOpen, tokens[2].Kind);
        Assert.Equal(TokenKind.DictClose, tokens[3].Kind);
        Assert.Equal(4, tokens.Count);
    }

    [Fact]
    public void Keywords_Recognized()
    {
        var tokens = LexAll("obj endobj stream endstream R true false null xref trailer startxref");
        Assert.Equal(11, tokens.Count);
        foreach (var token in tokens)
        {
            Assert.Equal(TokenKind.Keyword, token.Kind);
        }

        Assert.Equal("obj", tokens[0].Text);
        Assert.Equal("endobj", tokens[1].Text);
        Assert.Equal("R", tokens[4].Text);
        Assert.Equal("true", tokens[5].Text);
        Assert.Equal("null", tokens[7].Text);
        Assert.Equal("startxref", tokens[10].Text);
    }

    [Fact]
    public void Comment_SkippedToEndOfLine()
    {
        var token = First("% a comment here\n/Name");
        Assert.Equal(TokenKind.Name, token.Kind);
        Assert.Equal("Name", token.Text);
    }

    [Fact]
    public void Whitespace_CarriageReturnSeparatesTokens()
    {
        var tokens = LexAll("1\r2");
        Assert.Equal(1L, tokens[0].IntValue);
        Assert.Equal(2L, tokens[1].IntValue);
        Assert.Equal(2, tokens.Count);
    }

    [Fact]
    public void Whitespace_CrLfHandled()
    {
        var tokens = LexAll("1\r\n2\n3");
        Assert.Equal(3, tokens.Count);
        Assert.Equal(1L, tokens[0].IntValue);
        Assert.Equal(2L, tokens[1].IntValue);
        Assert.Equal(3L, tokens[2].IntValue);
    }

    [Fact]
    public void Position_StartOffsetHonored()
    {
        var bytes = Encoding.Latin1.GetBytes("XXX/Name");
        var token = new Lexer(bytes, 3).Next();
        Assert.Equal(TokenKind.Name, token.Kind);
        Assert.Equal("Name", token.Text);
    }

    [Fact]
    public void EndOfData_ReturnedAtEnd()
    {
        var lexer = new Lexer(Encoding.Latin1.GetBytes("1"), 0);
        Assert.Equal(TokenKind.Integer, lexer.Next().Kind);
        Assert.Equal(TokenKind.EndOfData, lexer.Next().Kind);
        Assert.Equal(TokenKind.EndOfData, lexer.Next().Kind);
    }

    [Fact]
    public void ReferenceTriplet_LexesAsThreeTokens()
    {
        var tokens = LexAll("3 0 R");
        Assert.Equal(3, tokens.Count);
        Assert.Equal(3L, tokens[0].IntValue);
        Assert.Equal(0L, tokens[1].IntValue);
        Assert.Equal(TokenKind.Keyword, tokens[2].Kind);
        Assert.Equal("R", tokens[2].Text);
    }

    private static byte[] Digits(string text, Lexer.Recovery recovery, Lexer.HexEnd end)
    {
        var position = 0;
        return Lexer.ReadHexDigits(Encoding.Latin1.GetBytes(text), ref position, recovery, end);
    }

    [Fact]
    public void HexDigits_StrictDigit_OptionalEnd_RejectsBadDigitButAcceptsMissingEod()
    {
        Assert.Equal(new byte[] { 0x48, 0x65 }, Digits("4865", Lexer.Recovery.Strict, Lexer.HexEnd.Optional));
        Assert.Throws<DocumentParseException>(() => Digits("48ZZ>", Lexer.Recovery.Strict, Lexer.HexEnd.Optional));
    }

    [Fact]
    public void HexDigits_LenientDigit_RequiredEnd_SkipsBadDigitButRejectsMissingEod()
    {
        Assert.Equal(new byte[] { 0x48, 0x65 }, Digits("48Z65>", Lexer.Recovery.Lenient, Lexer.HexEnd.Required));
        Assert.Throws<DocumentParseException>(() => Digits("4865", Lexer.Recovery.Lenient, Lexer.HexEnd.Required));
    }

    [Fact]
    public void HexDigits_OddTrailingDigit_ZeroPads()
    {
        Assert.Equal(new byte[] { 0x40 }, Digits("4>", Lexer.Recovery.Strict, Lexer.HexEnd.Required));
        Assert.Equal(new byte[] { 0x40 }, Digits("4", Lexer.Recovery.Strict, Lexer.HexEnd.Optional));
    }

    [Fact]
    public void HexDigits_SkipsTheSameWhitespaceAsTheLexer()
    {
        foreach (var ws in new byte[] { 0, 9, 10, 12, 13, 32 })
        {
            Assert.True(Lexer.IsWhitespace(ws));
            var position = 0;
            var data = new byte[] { (byte)'4', ws, (byte)'8', (byte)'>' };
            Assert.Equal(new byte[] { 0x48 }, Lexer.ReadHexDigits(data, ref position, Lexer.Recovery.Strict, Lexer.HexEnd.Required));
        }
    }

    [Fact]
    public void ReadHexString_UnterminatedOffset_PointsAtTheOpeningDelimiter()
    {
        var position = 3;
        var data = Encoding.Latin1.GetBytes("   <4865");
        var e = Assert.Throws<DocumentParseException>(() =>
        {
            var p = position;
            Lexer.ReadHexString(data, ref p, Lexer.Recovery.Strict);
        });

        Assert.Equal(3L, e.Offset);
    }
}
