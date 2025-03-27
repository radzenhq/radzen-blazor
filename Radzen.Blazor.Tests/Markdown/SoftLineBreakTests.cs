using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class SoftLineBreakTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData(@"foo
baz", @"<document>
    <paragraph>
        <text>foo</text>
        <softbreak />
        <text>baz</text>
    </paragraph>
</document>")]
    public void Parse_SoftLineBreak(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"foo 
 baz", @"<document>
    <paragraph>
        <text>foo</text>
        <softbreak />
        <text>baz</text>
    </paragraph>
</document>")]
    public void Parse_SoftLine_RemovesSpacesAtEndAndStartOfLine(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}
