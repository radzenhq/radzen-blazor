#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Content;
using Radzen.Documents;
namespace Radzen.Blazor.Pdf.Tests;

public class ContentTokenizerTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static byte[] StreamWithEverything()
    {
        var bytes = new List<byte>();
        bytes.AddRange(Ascii(
            "q 1 0 0 1 0 0 cm\n" +
            "1 0 0 RG 2 w 10 10 m 90 10 l S\n" +
            "/Span << /MCID 0 >> BDC\n" +
            "BT /F0 12 Tf 72 700 Td (Hello) Tj ET\n" +
            "BT /F0 12 Tf 72 680 Td [(Wor) -30 (ld)] TJ ET\n" +
            "BT /F0 12 Tf 72 660 Td <4869> Tj ET\n" +
            "EMC\n" +
            "BI /W 2 /H 2 /CS /RGB /BPC 8 ID "));
        bytes.AddRange([0x00, 0x01, 0x45, 0x49, 0xFF, 0x0A, 0xDE, 0xAD, 0x28, 0x42]);
        bytes.AddRange(Ascii(" EI\nQ\n"));
        return [.. bytes];
    }

    private static PortableDocument Load(byte[] content)
    {
        var document = new PortableDocument();
        document.Pages.Add().SetContent(content);
        return InterpreterTestSupport.Load(document.ToArray());
    }

    [Fact]
    public void Tokenize_EmitsOperatorsInOrderAndSkipsInlineImage()
    {
        var tokens = ContentTokenizer.Tokenize(StreamWithEverything());

        var operators = tokens
            .Where(t => t.Kind == ContentTokenizer.TokenKind.Operator)
            .Select(t => t.Text)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "q", "cm", "RG", "w", "m", "l", "S", "BDC",
                "BT", "Tf", "Td", "Tj", "ET",
                "BT", "Tf", "Td", "TJ", "ET",
                "BT", "Tf", "Td", "Tj", "ET",
                "EMC", "Q",
            },
            operators);
    }

    [Fact]
    public void Tokenize_MarkedContentDictionary_EmitsDictDelimiters()
    {
        var tokens = ContentTokenizer.Tokenize(StreamWithEverything());

        Assert.Contains(tokens, t => t.Kind == ContentTokenizer.TokenKind.DictStart);
        Assert.Contains(tokens, t => t.Kind == ContentTokenizer.TokenKind.DictEnd);
    }

    [Fact]
    public void Tokenize_TjArray_EmitsBracketedStringsAndNumbers()
    {
        var tokens = ContentTokenizer.Tokenize(Ascii("[(Wor) -30 (ld)] TJ"));

        Assert.Equal(ContentTokenizer.TokenKind.ArrayStart, tokens[0].Kind);
        Assert.Equal(ContentTokenizer.TokenKind.String, tokens[1].Kind);
        Assert.Equal("Wor", Encoding.ASCII.GetString(tokens[1].Bytes!));
        Assert.Equal(ContentTokenizer.TokenKind.Number, tokens[2].Kind);
        Assert.Equal(-30, tokens[2].Number);
        Assert.Equal(ContentTokenizer.TokenKind.String, tokens[3].Kind);
        Assert.Equal("ld", Encoding.ASCII.GetString(tokens[3].Bytes!));
        Assert.Equal(ContentTokenizer.TokenKind.ArrayEnd, tokens[4].Kind);
    }

    [Fact]
    public void Tokenize_HexString_DecodesBytes()
    {
        var tokens = ContentTokenizer.Tokenize(Ascii("<4869> Tj"));

        Assert.Equal(ContentTokenizer.TokenKind.String, tokens[0].Kind);
        Assert.Equal("Hi", Encoding.ASCII.GetString(tokens[0].Bytes!));
    }

    // ISO 32000-1 7.3.4.2: CR, LF or CRLF inside a literal string decodes to a single LF
    [Theory]
    [InlineData("(a\rb)", "a\nb")]
    [InlineData("(a\r\nb)", "a\nb")]
    [InlineData("(a\nb)", "a\nb")]
    [InlineData("(a\r\r\nb)", "a\n\nb")]
    public void Tokenize_LiteralStringEndOfLine_NormalizesToLineFeed(string literal, string expected)
    {
        var tokens = ContentTokenizer.Tokenize(Encoding.Latin1.GetBytes(literal));

        Assert.Equal(expected, Encoding.Latin1.GetString(tokens[0].Bytes!));
    }

    [Theory]
    [InlineData("(a\rb)")]
    [InlineData("(a\r\nb)")]
    [InlineData("(a\r\r\nb)")]
    [InlineData("(a\\rb)")]
    [InlineData("(a\\\r\nb)")]
    public void Tokenize_LiteralString_DecodesLikeTheObjectLexer(string literal)
    {
        var data = Encoding.Latin1.GetBytes(literal);

        var content = ContentTokenizer.Tokenize(data)[0].Bytes!;
        var lexed = new Radzen.Documents.Pdf.Objects.Lexer(data, 0).Next().Bytes!;

        Assert.Equal(lexed, content);
    }

    [Theory]
    [InlineData("4.", 4.0)]
    [InlineData("3. Tj", 3.0)]
    [InlineData("-.002", -0.002)]
    [InlineData("+7", 7.0)]
    [InlineData(".5", 0.5)]
    [InlineData("-5", -5.0)]
    [InlineData("007", 7.0)]
    public void Tokenize_NumericOperand_AcceptsEveryPermittedForm(string source, double expected)
    {
        var tokens = ContentTokenizer.Tokenize(Ascii(source));

        Assert.Equal(ContentTokenizer.TokenKind.Number, tokens[0].Kind);
        Assert.Equal(expected, tokens[0].Number);
    }

    [Theory]
    [InlineData("--5")]
    [InlineData("1.2.3")]
    [InlineData("4.-5")]
    [InlineData("-.")]
    [InlineData(".")]
    // ISO 32000-1 7.3.3 has no exponent notation
    [InlineData("1e-5")]
    [InlineData("6.02E23")]
    public void Tokenize_MalformedNumber_EmitsNoNumberToken(string source)
    {
        var tokens = ContentTokenizer.Tokenize(Ascii(source));

        Assert.DoesNotContain(tokens, t => t.Kind == ContentTokenizer.TokenKind.Number);
    }

    [Fact]
    public void Cache_SameArray_TokenizesOnce()
    {
        var cache = new ContentTokenizer.Cache();
        var data = StreamWithEverything();

        Assert.Same(ContentTokenizer.Tokenize(data, cache), ContentTokenizer.Tokenize(data, cache));
    }

    [Fact]
    public void Cache_EqualButDistinctArray_Retokenizes()
    {
        var cache = new ContentTokenizer.Cache();
        var first = Ascii("(a) Tj");
        var second = Ascii("(a) Tj");

        Assert.NotSame(ContentTokenizer.Tokenize(first, cache), ContentTokenizer.Tokenize(second, cache));
    }

    [Fact]
    public void Cache_MovedToNewArray_DoesNotServeStaleTokens()
    {
        var cache = new ContentTokenizer.Cache();
        var original = Ascii("(before) Tj");
        var edited = Ascii("(after) Tj (extra) Tj");

        ContentTokenizer.Tokenize(original, cache);
        var tokens = ContentTokenizer.Tokenize(edited, cache);

        Assert.Equal(2, tokens.Count(t => t.Kind == ContentTokenizer.TokenKind.Operator));
        Assert.Equal("after", Encoding.Latin1.GetString(tokens[0].Bytes!));
    }

    [Fact]
    public void Cache_NullCache_TokenizesEveryCall()
    {
        var data = StreamWithEverything();

        Assert.NotSame(ContentTokenizer.Tokenize(data, null), ContentTokenizer.Tokenize(data, null));
    }

    [Fact]
    public void Interpreter_MaterializesPathAndTextAndInlineImage()
    {
        var content = Load(StreamWithEverything()).Pages[0].Content;

        Assert.IsType<PathContent>(content[0]);
        Assert.Equal("Hello", Assert.IsType<TextContent>(content[1]).Text);
        Assert.Equal("World", Assert.IsType<TextContent>(content[2]).Text);
        Assert.Equal("Hi", Assert.IsType<TextContent>(content[3]).Text);
        Assert.IsType<InlineImageContent>(content[4]);
        Assert.Equal(5, content.Count);
    }

    [Fact]
    public void Extractor_ReadsTextInReadingOrderAndSkipsInlineImage()
    {
        var extracted = Load(StreamWithEverything()).Pages[0].ExtractText();

        Assert.Equal("Hello\nWorld\nHi", extracted);
    }

    private static byte[] InlineImageStream(string dictionary, IEnumerable<byte> payload, string trailer = "\nEI\nQ\n")
    {
        var stream = new List<byte>();
        stream.AddRange(Ascii("q\nBI " + dictionary + " ID "));
        stream.AddRange(payload);
        stream.AddRange(Ascii(trailer));
        return stream.ToArray();
    }

    private static string[] Operators(byte[] stream) => ContentTokenizer
        .Tokenize(stream)
        .Where(t => t.Kind == ContentTokenizer.TokenKind.Operator)
        .Select(t => t.Text!)
        .ToArray();

    [Fact]
    public void Tokenize_FilteredInlineImageWithoutLength_DoesNotTerminateOnPayloadBytesSpellingEI()
    {
        var payload = new List<byte> { 0x78, 0x9c, 0x01, 0x20 };
        payload.AddRange(Ascii(" EI "));
        payload.AddRange(new byte[] { 0x00, 0x01, 0x02, 0x03 });

        Assert.Equal(new[] { "q", "Q" }, Operators(InlineImageStream("/W 4 /H 4 /BPC 8 /CS /G /F /Fl", payload)));
    }

    [Fact]
    public void Tokenize_Ascii85InlineImageWithoutLength_EndsAtEndOfDataMarker()
    {
        var payload = Ascii("87cURD] EI ]i<~>");

        Assert.Equal(new[] { "q", "Q" }, Operators(InlineImageStream("/W 4 /H 4 /BPC 8 /CS /G /F /A85", payload)));
    }

    [Fact]
    public void Tokenize_AsciiHexInlineImageWithoutLength_EndsAtEndOfDataMarker()
    {
        Assert.Equal(new[] { "q", "Q" }, Operators(InlineImageStream("/W 4 /H 4 /BPC 8 /CS /G /F /AHx", Ascii("48656C6C6F>"))));
    }

    [Fact]
    public void Tokenize_RunLengthInlineImageWithoutLength_EndsAtEndOfDataMarker()
    {
        var payload = new List<byte> { 3 };
        payload.AddRange(Ascii(" EI "));
        payload.Add(128);

        Assert.Equal(new[] { "q", "Q" }, Operators(InlineImageStream("/W 4 /H 4 /BPC 8 /CS /G /F /RL", payload)));
    }

    [Fact]
    public void Tokenize_InlineImageFilterArray_MeasuresPayloadWithFirstFilter()
    {
        var payload = Ascii("87cURD] EI ]i<~>");

        Assert.Equal(new[] { "q", "Q" }, Operators(InlineImageStream("/W 4 /H 4 /BPC 8 /CS /G /F [/A85 /Fl]", payload)));
    }

    [Fact]
    public void Tokenize_FilteredInlineImageWithNoPlausibleTerminator_TakesFirstCandidate()
    {
        var payload = new List<byte> { 0x78, 0x9c };
        payload.AddRange(Ascii(" EI "));
        payload.AddRange(new byte[] { 0x00, 0x01, 0x02, 0x03 });

        var tokens = ContentTokenizer.Tokenize(InlineImageStream("/W 4 /H 4 /BPC 8 /CS /G /F /Fl", payload, string.Empty));

        Assert.Equal(new[] { "q", "\x01\x02\x03" }, tokens.Where(t => t.Kind == ContentTokenizer.TokenKind.Operator).Select(t => t.Text).ToArray());
        Assert.Single(tokens, t => t.Kind == ContentTokenizer.TokenKind.InlineImage);
    }

    [Fact]
    public void Tokenize_FilteredInlineImageWithLength_UsesDeclaredLength()
    {
        var payload = new List<byte>();
        payload.AddRange(Ascii(" EI "));
        payload.AddRange(new byte[] { 0x00, 0x01, 0x02, 0x03 });

        Assert.Equal(new[] { "q", "Q" }, Operators(InlineImageStream("/W 4 /H 4 /BPC 8 /CS /G /F /Fl /L 8", payload)));
    }
}
