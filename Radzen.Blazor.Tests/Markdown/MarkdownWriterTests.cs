using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Radzen.Documents.Markdown.Tests;

public class MarkdownWriterTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> SpecExamples() => SourcePositionTests.SpecExamples();

    private static string Structure(string markdown) => Canonical.Html(markdown);

    private static readonly int[] ExamplesTheParserDeviatesFrom = [13, 237, 257, 313, 318];

    [Theory]
    [MemberData(nameof(SpecExamples))]
    public void WritingAPristineDocumentKeepsItsStructure(int example, string markdown)
    {
        if (ExamplesTheParserDeviatesFrom.Contains(example))
        {
            return;
        }

        var document = MarkdownParser.Parse(markdown);
        var written = MarkdownWriter.Write(document, markdown);

        var expected = Structure(markdown);
        var actual = Structure(written);

        if (expected != actual)
        {
            output.WriteLine("example " + example);
            output.WriteLine("input:   " + markdown.Replace("\n", "\\n"));
            output.WriteLine("written: " + written.Replace("\n", "\\n"));
        }

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ModifiedEmphasisKeepsItsOriginalMarkerAndExpelsWhitespace()
    {
        var document = MarkdownParser.Parse("a _b_ c");
        var emphasis = (Emphasis)((Paragraph)document.Children[0]).Children[1];
        var text = (Text)emphasis.Children[0];
        text.Value = "b x ";
        document.Children[0].Pristine = false;

        Assert.Equal("a _b x_  c", MarkdownWriter.Write(document, "a _b_ c"));
    }

    [Fact]
    public void PositionsPointAtTheWrittenContent()
    {
        var markdown = "> # H\n>\n> a *b*";
        var document = MarkdownParser.Parse(markdown);
        document.Children[0].Pristine = false;
        var writer = MarkdownWriter.Preserve(document, markdown);
        var paragraph = (Paragraph)((BlockQuote)document.Children[0]).Children[1];
        var emphasis = (Emphasis)paragraph.Children[1];

        Assert.Equal(markdown, writer.Text);
        Assert.Equal((10, 15), writer.Positions[paragraph]);
        Assert.Equal((13, 14), writer.Positions[emphasis]);
    }
}

public class MarkdownWriterEscapingTests
{
    private static IEnumerable<Inline> Lines(string value) => value.Split('\n').SelectMany((line, index) => index == 0 ? [new Text(line)] : new Inline[] { new SoftLineBreak(), new Text(line) });

    private static Leaf FirstLeaf(Block block) => block is Leaf leaf ? leaf : FirstLeaf(((BlockContainer)block).Children[0]);

    private static string Write(string source, params Inline[] inlines)
    {
        var document = MarkdownParser.Parse(source);
        FirstLeaf(document.Children[0]).ReplaceInlines(inlines);
        document.Children[0].Pristine = false;
        return MarkdownWriter.Write(document, source);
    }

    [Theory]
    [InlineData("> a\n> b\nc >", "\\> a\n\\> b\nc >")]
    [InlineData("a\\\nb", "a\\\\\nb")]
    [InlineData("&amp", "\\&amp")]
    [InlineData("&#9;", "\\&#9;")]
    [InlineData("a\\+b", "a\\\\+b")]
    [InlineData("```js\n```", "\\`\\`\\`js\n\\`\\`\\`")]
    [InlineData("[a]: b", "\\[a]: b")]
    [InlineData("*a*", "\\*a\\*")]
    [InlineData("_a_", "\\_a\\_")]
    [InlineData("# a", "\\# a")]
    [InlineData("a\n=", "a\n\\=")]
    [InlineData("a\n-", "a\n\\-")]
    [InlineData("<a\nb>", "\\<a\nb>")]
    [InlineData("a `b`\n`c` d", "a \\`b\\`\n\\`c\\` d")]
    [InlineData("![a][b]", "!\\[a]\\[b]")]
    [InlineData("![](a.jpg)", "!\\[]\\(a.jpg)")]
    [InlineData("[a][b]", "\\[a]\\[b]")]
    [InlineData("[](a.jpg)", "\\[]\\(a.jpg)")]
    [InlineData("+ a\n+ b", "\\+ a\n\\+ b")]
    [InlineData("+a", "+a")]
    [InlineData("- a\n- b", "\\- a\n\\- b")]
    [InlineData("-a", "-a")]
    [InlineData("--a", "\\--a")]
    [InlineData("1. a\n2. b", "1\\. a\n2\\. b")]
    [InlineData("1) a\n2) b", "1\\) a\n2\\) b")]
    [InlineData("1.2.3. asd", "1.2.3. asd")]
    [InlineData("snake_case", "snake_case")]
    [InlineData("~~a~~", "\\~\\~a\\~\\~")]
    [InlineData("| a | b |\n| - | - |", "\\| a | b |\n\\| - | - |")]
    [InlineData(":-: | a", "\\:-: | a")]
    public void EscapesWhatWouldOtherwiseBeAConstruct(string value, string expected)
    {
        Assert.Equal(expected, Write("plain", Lines(value).ToArray()));
    }

    [Theory]
    [InlineData("> x", "> \\> a\n> \\> b")]
    [InlineData("- x", "- \\> a\n  \\> b")]
    public void EscapesAtEveryLineStartOfAContainer(string source, string expected)
    {
        Assert.Equal(expected, Write(source, Lines("> a\n> b").ToArray()));
    }

    [Fact]
    public void EscapesABangThatWouldTurnALinkIntoAnImage()
    {
        var link = new Link { Destination = "b" };
        link.Add(new Text("a"));

        Assert.Equal("\\![a](b)", Write("plain", new Text("!"), link));
    }

    [Fact]
    public void EscapesABackslashBeforeAnAutolink()
    {
        var link = new Link { Destination = "https://a.b", Autolink = true };
        link.Add(new Text("https://a.b"));

        Assert.Equal("a\\\\<https://a.b>", Write("plain", new Text("a\\"), link));
    }

    [Fact]
    public void DoesNotEscapeInsideAnAutolink()
    {
        var link = new Link { Destination = "mailto:a.b-c_d@a.b", Autolink = true };
        link.Add(new Text("a.b-c_d@a.b"));

        Assert.Equal("<a.b-c_d@a.b>", Write("plain", link));
    }

    [Theory]
    [InlineData("b <c", null, "[x](<b \\<c>)")]
    [InlineData("b >c", null, "[x](<b \\>c>)")]
    [InlineData("b \\+c", null, "[x](<b \\\\+c>)")]
    [InlineData("b\nc", null, "[x](b%0Ac)")]
    [InlineData("b(c", null, "[x](b\\(c)")]
    [InlineData("b)c", null, "[x](b\\)c)")]
    [InlineData("b\\.c", null, "[x](b\\\\.c)")]
    [InlineData("\f", null, "[x](<\f>)")]
    [InlineData("", "b\"c", "[x](<> \"b\\\"c\")")]
    [InlineData("", "b\\-c", "[x](<> \"b\\\\-c\")")]
    [InlineData("a b![c](d*e_f[g_h`i", null, "[x](<a b![c](d*e_f[g_h`i>)")]
    [InlineData("a![b](c*d_e[f_g`h<i</j", null, "[x](a![b]\\(c*d_e[f_g`h<i</j)")]
    [InlineData("#", "a![b](c*d_e[f_g`h<i</j", "[x](# \"a![b](c*d_e[f_g`h<i</j\")")]
    [InlineData("y", "a\n* b\n* c", "[x](y \"a\n\\* b\n\\* c\")")]
    [InlineData("y", "a\n*b", "[x](y \"a\n*b\")")]
    [InlineData("y", "a\n**b", "[x](y \"a\n\\**b\")")]
    public void EscapesLinkDestinationsAndTitles(string destination, string? title, string expected)
    {
        var link = new Link { Destination = destination, Title = title };
        link.Add(new Text("x"));

        Assert.Equal(expected, Write("plain", link));
    }

    [Theory]
    [InlineData("## x", "# a", "## # a")]
    [InlineData("## x", "1) a", "## 1) a")]
    [InlineData("## x", "+ a", "## + a")]
    [InlineData("## x", "- a", "## - a")]
    [InlineData("## x", "= a", "## = a")]
    [InlineData("## x", "> a", "## > a")]
    [InlineData("# x", "a #", "# a \\#")]
    [InlineData("# x", "a ##", "# a #\\#")]
    [InlineData("# x", "a # b", "# a # b")]
    public void EscapesOnlyAClosingSequenceInAHeading(string source, string value, string expected)
    {
        Assert.Equal(expected, Write(source, new Text(value)));
    }

    [Theory]
    [InlineData("  a", "a")]
    [InlineData("a  \nb", "a\nb")]
    [InlineData("a\n  b", "a\nb")]
    public void DropsWhitespaceAtLineEdgesInsteadOfEncodingIt(string value, string expected)
    {
        Assert.Equal(expected, Write("plain", Lines(value).ToArray()));
    }

    [Fact]
    public void EscapesInlineHtmlThatWouldStartAnHtmlBlockAfterALineBreak()
    {
        Assert.Equal("a\\\n\\<div>b</div>", Write("plain", new Text("a"), new LineBreak { Backslash = true }, new HtmlInline { Value = "<div>" }, new Text("b"), new HtmlInline { Value = "</div>" }));
    }

    [Fact]
    public void IndentsContinuationLinesOfInlineHtml()
    {
        Assert.Equal("a<b\n    >c", Write("plain", new Text("a"), new HtmlInline { Value = "<b\n>" }, new Text("c")));
    }
}
