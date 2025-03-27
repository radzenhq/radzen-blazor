using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class HardLineBreakTests
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
        <linebreak />
        <text>baz</text>
    </paragraph>
</document>")]
    [InlineData("foo  \r\nbaz", @"<document>
    <paragraph>
        <text>foo</text>
        <linebreak />
        <text>baz</text>
    </paragraph>
</document>")]
    [InlineData(@"foo\
baz", @"<document>
    <paragraph>
        <text>foo</text>
        <linebreak />
        <text>baz</text>
    </paragraph>
</document>")]
    [InlineData(@"foo       
baz", @"<document>
    <paragraph>
        <text>foo</text>
        <linebreak />
        <text>baz</text>
    </paragraph>
</document>")]
    [InlineData(@"foo  
     bar", @"<document>
    <paragraph>
        <text>foo</text>
        <linebreak />
        <text>bar</text>
    </paragraph>
</document>")]
    [InlineData(@"foo\
     bar", @"<document>
    <paragraph>
        <text>foo</text>
        <linebreak />
        <text>bar</text>
    </paragraph>
</document>")]
    [InlineData(@"*foo  
bar*", @"<document>
    <paragraph>
        <emph>
            <text>foo</text>
            <linebreak />
            <text>bar</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData(@"*foo\
bar*", @"<document>
    <paragraph>
        <emph>
            <text>foo</text>
            <linebreak />
            <text>bar</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData(@"`code\
span`", @"<document>
    <paragraph>
        <code>code\ span</code>
    </paragraph>
</document>")]
    [InlineData(@"`code  
span`", @"<document>
    <paragraph>
        <code>code   span</code>
    </paragraph>
</document>")]
    [InlineData(@"<a href=""foo  
bar"">", @"<document>
    <paragraph>
        <html_inline>&lt;a href=""foo  
bar""&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"<a href=""foo\
bar"">", @"<document>
    <paragraph>
        <html_inline>&lt;a href=""foo\
bar""&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"foo\", @"<document>
    <paragraph>
        <text>foo\</text>
    </paragraph>
</document>")]
    [InlineData(@"foo  ", @"<document>
    <paragraph>
        <text>foo</text>
    </paragraph>
</document>")]
    [InlineData(@"### foo\", @"<document>
    <heading level=""3"">
        <text>foo\</text>
    </heading>
</document>")]
    [InlineData(@"### foo  ", @"<document>
    <heading level=""3"">
        <text>foo</text>
    </heading>
</document>")]
    public void Parse_HardLineBreak(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}
