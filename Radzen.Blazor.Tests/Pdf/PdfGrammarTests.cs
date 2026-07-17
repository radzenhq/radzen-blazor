#nullable enable
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using StreamToken = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using StreamTokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Blazor.Pdf.Tests;

// The file-object grammar (Objects/Lexer) and the content-stream grammar (Content/
// ContentTokenizer) are two readers of ONE syntax. What a number, a string, a hex string
// and a name are is defined once; only recovery differs, because a corrupt file object must
// be rejected while a content stream must keep rendering. These tests pin that split: every
// Agree_* case asserts the two readers accept the same text and produce the same value, and
// every Recovery_* case asserts the only permitted difference.
public class PdfGrammarTests
{
    private static byte[] B(string text) => Encoding.Latin1.GetBytes(text);

    private static Token Object(string text) => new Lexer(B(text), 0).Next();

    private static StreamToken[] Content(string text) => [.. ContentTokenizer.Tokenize(B(text))];

    private static StreamToken Single(string text) => Assert.Single(Content(text));

    // ISO 32000-1 7.3.3 has no exponent notation, so "1e3" is malformed input, not 1000.
    // It used to parse as 1000 in a content stream while a file object rejected it, which let
    // "1e3 0 0 1e3 0 0 cm" execute as a scale-1000 matrix built from an operand the grammar
    // does not define.
    [Theory]
    [InlineData("1e3")]
    [InlineData("1E3")]
    [InlineData("1e-3")]
    [InlineData("1.5e10")]
    public void Agree_ExponentNotationIsNotANumber(string text)
    {
        Assert.Throws<DocumentParseException>(() => Object(text));
        Assert.Empty(Content(text).Where(token => token.Kind == StreamTokenKind.Number));
    }

    [Fact]
    public void ExponentOperandDoesNotReachTheOperator()
    {
        var tokens = Content("1e3 0 0 1e3 0 0 cm");

        Assert.Equal(4, tokens.Count(token => token.Kind == StreamTokenKind.Number));
        Assert.All(tokens.Where(token => token.Kind == StreamTokenKind.Number), token => Assert.Equal(0, token.Number));
    }

    [Theory]
    [InlineData("--5")]
    [InlineData("1.0.5")]
    [InlineData("5x")]
    [InlineData("1-2")]
    [InlineData(".")]
    [InlineData("+")]
    public void Agree_MalformedNumbersAreRejected(string text)
    {
        Assert.Throws<DocumentParseException>(() => Object(text));
        Assert.Empty(Content(text).Where(token => token.Kind == StreamTokenKind.Number));
    }

    // The awkward-but-legal forms of ISO 32000-1 7.3.3.
    [Theory]
    [InlineData("4.", 4d)]
    [InlineData("-.002", -0.002d)]
    [InlineData("+7", 7d)]
    [InlineData(".5", 0.5d)]
    [InlineData("-0", 0d)]
    [InlineData("34.5", 34.5d)]
    [InlineData("-3.62", -3.62d)]
    [InlineData("+123.6", 123.6d)]
    [InlineData("4", 4d)]
    [InlineData("0.002", 0.002d)]
    public void Agree_LegalNumbers(string text, double expected)
    {
        var token = Object(text);

        Assert.Equal(expected, token.RealValue);
        Assert.Equal(expected, Single(text).Number);
    }

    [Fact]
    public void Object_IntegerAndRealStayDistinct()
    {
        Assert.Equal(TokenKind.Integer, Object("+7").Kind);
        Assert.Equal(TokenKind.Real, Object("4.").Kind);
    }

    [Theory]
    [InlineData("<4869>", "Hi")]
    [InlineData("<48 69>", "Hi")]
    [InlineData("<486>", "H`")]
    [InlineData("<>", "")]
    public void Agree_HexStrings(string text, string expected)
    {
        Assert.Equal(expected, Encoding.Latin1.GetString(Object(text).Bytes));
        Assert.Equal(expected, Encoding.Latin1.GetString(Single(text).Bytes!));
    }

    [Theory]
    [InlineData(@"(Hello)", "Hello")]
    [InlineData(@"(a(b)c)", "a(b)c")]
    [InlineData(@"(a\053b)", "a+b")]
    [InlineData(@"(a\nb)", "a\nb")]
    [InlineData(@"(a\)b)", "a)b")]
    [InlineData("(a\rb)", "a\nb")]
    [InlineData("(a\r\nb)", "a\nb")]
    [InlineData("(a\\\nb)", "ab")]
    public void Agree_LiteralStrings(string text, string expected)
    {
        Assert.Equal(expected, Encoding.Latin1.GetString(Object(text).Bytes));
        Assert.Equal(expected, Encoding.Latin1.GetString(Single(text).Bytes!));
    }

    [Theory]
    [InlineData("/Type", "Type")]
    [InlineData("/A#20B", "A B")]
    [InlineData("/A#23C", "A#C")]
    [InlineData("/A#4", "A#4")]
    public void Agree_Names(string text, string expected)
    {
        Assert.Equal(expected, Object(text).Text);
        Assert.Equal(expected, Single(text).Text);
    }

    // Recovery is the ONE permitted difference: the file object is rejected, the content
    // stream keeps what it decoded and renders on.
    [Fact]
    public void Recovery_UnterminatedHexString()
    {
        Assert.Throws<DocumentParseException>(() => Object("<4869"));
        Assert.Equal("Hi", Encoding.Latin1.GetString(Single("<4869").Bytes!));
    }

    [Fact]
    public void Recovery_HexStringWithNonHexDigit()
    {
        Assert.Throws<DocumentParseException>(() => Object("<48ZZ69>"));
        Assert.Equal("Hi", Encoding.Latin1.GetString(Single("<48ZZ69>").Bytes!));
    }

    [Fact]
    public void Recovery_UnterminatedLiteralString()
    {
        Assert.Throws<DocumentParseException>(() => Object("(abc"));
        Assert.Equal("abc", Encoding.Latin1.GetString(Single("(abc").Bytes!));
    }

    [Fact]
    public void Recovery_IntegerTooLargeForLong()
    {
        Assert.Throws<DocumentParseException>(() => Object("99999999999999999999"));
        Assert.Equal(1e20, Single("99999999999999999999").Number);
    }
}
