using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class CodeTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData("`foo`",
@"<document>
    <paragraph>
        <code>foo</code>
    </paragraph>
</document>")]
    [InlineData("`` foo ` bar ``",
@"<document>
    <paragraph>
        <code>foo ` bar</code>
    </paragraph>
</document>")]
    [InlineData("` `` `",
@"<document>
    <paragraph>
        <code>``</code>
    </paragraph>
</document>")]
    public void Parse_BasicCode_ReturnsCodeNode(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData("`  ``  `",
@"<document>
    <paragraph>
        <code> `` </code>
    </paragraph>
</document>")]
    [InlineData("` a`",
@"<document>
    <paragraph>
        <code> a</code>
    </paragraph>
</document>")]
    [InlineData(@"` `
`  `",
@"<document>
    <paragraph>
        <code> </code>
        <softbreak />
        <code>  </code>
    </paragraph>
</document>")]
    public void Parse_CodeWithSpaces_PreservesSpaces(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }


    [Theory]
    [InlineData(@"``
foo
bar
baz
``",
@"<document>
    <paragraph>
        <code>foo bar baz</code>
    </paragraph>
</document>")]
    [InlineData(@"``
foo 
``",
@"<document>
    <paragraph>
        <code>foo </code>
    </paragraph>
</document>")]
    [InlineData(@"`foo   bar 
baz`", 
    @"<document>
    <paragraph>
        <code>foo   bar  baz</code>
    </paragraph>
</document>")]

    public void Parse_CodeWithLineBreaks_ConvertsToSpace(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData("`foo\\`bar`", @"<document>
    <paragraph>
        <code>foo\</code>
        <text>bar</text>
        <text>`</text>
    </paragraph>
</document>")]
    [InlineData("``foo`bar``", @"<document>
    <paragraph>
        <code>foo`bar</code>
    </paragraph>
</document>")]
    [InlineData("` foo `` bar `", @"<document>
    <paragraph>
        <code>foo `` bar</code>
    </paragraph>
</document>")]
    public void Parse_CodeWithBacktics(string mardown, string expected)
    {
        Assert.Equal(expected, ToXml(mardown));
    }

    [Theory]
    [InlineData("*foo`*`", @"<document>
    <paragraph>
        <text>*</text>
        <text>foo</text>
        <code>*</code>
    </paragraph>
</document>")]
    [InlineData("[not a `link](/foo`)", 
    @"<document>
    <paragraph>
        <text>[</text>
        <text>not a </text>
        <code>link](/foo</code>
        <text>)</text>
    </paragraph>
</document>")]
    [InlineData("`<https://foo.bar.`baz>`",@"<document>
    <paragraph>
        <code>&lt;https://foo.bar.</code>
        <text>baz&gt;</text>
        <text>`</text>
    </paragraph>
</document>")]
    public void Parse_CodePrecedence(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData("```foo``", @"<document>
    <paragraph>
        <text>```</text>
        <text>foo</text>
        <text>``</text>
    </paragraph>
</document>")]
    [InlineData("`foo", @"<document>
    <paragraph>
        <text>`</text>
        <text>foo</text>
    </paragraph>
</document>")]
    [InlineData("`foo``bar``", @"<document>
    <paragraph>
        <text>`</text>
        <text>foo</text>
        <code>bar</code>
    </paragraph>
</document>")]
    public void Parse_UnmatchingBacktics(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
} 