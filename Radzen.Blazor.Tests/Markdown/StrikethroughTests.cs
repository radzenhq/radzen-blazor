using Xunit;

namespace Radzen.Documents.Markdown.Tests;

public class StrikethroughTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);
        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData("~~struck~~", @"<document>
    <paragraph>
        <strikethrough>
            <text>struck</text>
        </strikethrough>
    </paragraph>
</document>")]
    [InlineData("a ~~b **c** d~~ e", @"<document>
    <paragraph>
        <text>a </text>
        <strikethrough>
            <text>b </text>
            <strong>
                <text>c</text>
            </strong>
            <text> d</text>
        </strikethrough>
        <text> e</text>
    </paragraph>
</document>")]
    // single tilde is NOT strikethrough (GFM requires exactly ~~)
    [InlineData("~one~", @"<document>
    <paragraph>
        <text>~</text>
        <text>one</text>
        <text>~</text>
    </paragraph>
</document>")]
    // flanking: closing run preceded by space stays literal
    [InlineData("~~a ~~b", @"<document>
    <paragraph>
        <text>~~</text>
        <text>a </text>
        <text>~~</text>
        <text>b</text>
    </paragraph>
</document>")]
    public void Strikethrough_parses(string markdown, string expected)
    {
        Assert.Equal(expected.Replace("\r\n", "\n"), ToXml(markdown).Replace("\r\n", "\n"));
    }
}
