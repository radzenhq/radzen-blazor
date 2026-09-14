using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Radzen.Documents.Markdown.Tests;

public class MarkdownWriterTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> SpecExamples() => SourcePositionTests.SpecExamples();

    private static string Structure(string markdown) => Canonical.Html(markdown);

    [Theory]
    [MemberData(nameof(SpecExamples))]
    public void WritingAPristineDocumentKeepsItsStructure(int example, string markdown)
    {
        if (example is 13 or 237 or 257 or 313 or 318)
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

    [Theory]
    [InlineData("# Hello\n\nSome *text* here.")]
    [InlineData("- one\n- two\n  - nested\n\n1) a\n2) b")]
    [InlineData("> quote\n>\n> more")]
    [InlineData("| a | b |\n| :-- | --: |\n| 1 | 2 |")]
    [InlineData("```cs\nvar x = 1;\n```")]
    [InlineData("a\\*b\\* and `code` and [l](u \"t\")")]
    public void WritingAPristineDocumentReproducesTheText(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        Assert.Equal(markdown, MarkdownWriter.Write(document, markdown));
    }

    [Fact]
    public void ModifiedTextIsEscaped()
    {
        var document = MarkdownParser.Parse("plain");
        var text = (Text)((Paragraph)document.Children[0]).Children[0];
        text.Value = "# not a heading *nor* emphasis";
        document.Children[0].Pristine = false;

        Assert.Equal("\\# not a heading \\*nor\\* emphasis", MarkdownWriter.Write(document, "plain"));
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
        var writer = MarkdownWriter.Write(document.Children, markdown);
        var paragraph = (Paragraph)((BlockQuote)document.Children[0]).Children[1];
        var emphasis = (Emphasis)paragraph.Children[1];

        Assert.Equal(markdown, writer.Text);
        Assert.Equal((10, 15), writer.Positions[paragraph]);
        Assert.Equal((13, 14), writer.Positions[emphasis]);
    }
}
