#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Content;
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

    private static Document RedactableDocument(string streamData)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, $"4 0 obj\n<< /Length {streamData.Length} >>\nstream\n{streamData}\nendstream\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        using var input = new MemoryStream(pdf.ToArray());
        return Document.LoadFromStream(input);
    }

    private static Document Load(byte[] content)
    {
        var document = new Document();
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
    public void Tokenize_NumericOperands_DoNotAllocateAStringPerNumber()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < 2000; i++)
        {
            builder.Append(i).Append(".25 ").Append(-i).Append(" 0.5 ");
        }

        var data = Ascii(builder.ToString());
        ContentTokenizer.Tokenize(data);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var tokens = ContentTokenizer.Tokenize(data);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(6000, tokens.Count);
        Assert.True(allocated < 780_000, $"Tokenizing 6000 numeric operands allocated {allocated} bytes.");
    }

    [Fact]
    public void RedactText_SharesOneTokenizationPerContentArray()
    {
        var content = new StringBuilder("BT /F0 10 Tf ");
        for (var i = 0; i < 400; i++)
        {
            content.Append($"1 0 0 1 72 {700 - (i % 60)} Tm (Line{i} of filler text) Tj ");
        }

        content.Append("ET ");
        for (var i = 0; i < 4000; i++)
        {
            content.Append($"{i % 100}.5 {i % 77}.25 m {i % 90}.125 {i % 61}.75 l S ");
        }

        var stream = content.ToString();
        RedactableDocument(stream).Pages[0].RedactText("Line7 of");

        var document = RedactableDocument(stream);
        var before = GC.GetAllocatedBytesForCurrentThread();
        document.Pages[0].RedactText("Line7 of");
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated < 23_000_000, $"RedactText allocated {allocated} bytes, suggesting the token cache is no longer shared.");
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
