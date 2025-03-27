using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class ParagraphTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }
    [Fact]
    public void Parse_BasicParagraph()
    {
        Assert.Equal(@"<document>
    <paragraph>
        <text>foo</text>
    </paragraph>
</document>", ToXml(@"foo"));
    }

    [Theory]
    [InlineData(@"aaa

bbb", @"<document>
    <paragraph>
        <text>aaa</text>
    </paragraph>
    <paragraph>
        <text>bbb</text>
    </paragraph>
</document>")]
    [InlineData(@"aaa
bbb

ccc
ddd", @"<document>
    <paragraph>
        <text>aaa</text>
        <softbreak />
        <text>bbb</text>
    </paragraph>
    <paragraph>
        <text>ccc</text>
        <softbreak />
        <text>ddd</text>
    </paragraph>
</document>")]
    [InlineData(@"aaa


bbb
", @"<document>
    <paragraph>
        <text>aaa</text>
    </paragraph>
    <paragraph>
        <text>bbb</text>
    </paragraph>
</document>")]
    [InlineData(@"  aaa
 bbb", @"<document>
    <paragraph>
        <text>aaa</text>
        <softbreak />
        <text>bbb</text>
    </paragraph>
</document>")]
    [InlineData(@"aaa
             bbb
                                       ccc", @"<document>
    <paragraph>
        <text>aaa</text>
        <softbreak />
        <text>bbb</text>
        <softbreak />
        <text>ccc</text>
    </paragraph>
</document>")]
    [InlineData(@"   aaa
bbb", @"<document>
    <paragraph>
        <text>aaa</text>
        <softbreak />
        <text>bbb</text>
    </paragraph>
</document>")]
    [InlineData(@"    aaa
bbb", @"<document>
    <code_block>aaa
</code_block>
    <paragraph>
        <text>bbb</text>
    </paragraph>
</document>")]
    [InlineData(@"aaa     
bbb     ", @"<document>
    <paragraph>
        <text>aaa</text>
        <linebreak />
        <text>bbb</text>
    </paragraph>
</document>")]
    public void Parse_Paragraph(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}
