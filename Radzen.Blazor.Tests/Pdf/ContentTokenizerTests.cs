#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The single shared content-stream tokenizer feeds both ContentInterpreter and
// TextExtractor. These assert its token stream directly and that both consumers
// produce consistent output for a stream exercising text, TJ arrays, hex strings,
// paths, marked-content dictionaries and inline images.
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

        // No /W, /H, ID or EI leak out of the inline image as operators.
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

    [Fact]
    public void Interpreter_MaterializesPathAndTextAndSkipsInlineImage()
    {
        var content = Load(StreamWithEverything()).Pages[0].Content;

        Assert.IsType<PathContent>(content[0]);
        Assert.Equal("Hello", Assert.IsType<TextContent>(content[1]).Text);
        Assert.Equal("World", Assert.IsType<TextContent>(content[2]).Text);
        Assert.Equal("Hi", Assert.IsType<TextContent>(content[3]).Text);
        Assert.Equal(4, content.Count);
    }

    [Fact]
    public void Extractor_ReadsTextInReadingOrderAndSkipsInlineImage()
    {
        var extracted = Load(StreamWithEverything()).Pages[0].ExtractText();

        Assert.Equal("Hello\nWorld\nHi", extracted);
    }
}
