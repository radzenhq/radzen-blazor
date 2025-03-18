using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class StrongTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);
        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData("**foo bar**",
@"<document>
    <paragraph>
        <strong>
            <text>foo bar</text>
        </strong>
    </paragraph>
</document>")]
    [InlineData("** foo bar**",
@"<document>
    <paragraph>
        <text>**</text>
        <text> foo bar</text>
        <text>**</text>
    </paragraph>
</document>")]
    [InlineData("a**\"foo\"**",
@"<document>
    <paragraph>
        <text>a</text>
        <text>**</text>
        <text>""foo""</text>
        <text>**</text>
    </paragraph>
</document>")]
    [InlineData("foo**bar**",
@"<document>
    <paragraph>
        <text>foo</text>
        <strong>
            <text>bar</text>
        </strong>
    </paragraph>
</document>")]
    [InlineData("__foo bar__",
@"<document>
    <paragraph>
        <strong>
            <text>foo bar</text>
        </strong>
    </paragraph>
</document>")]
    [InlineData("__ foo bar__",
@"<document>
    <paragraph>
        <text>__</text>
        <text> foo bar</text>
        <text>__</text>
    </paragraph>
</document>")]
    [InlineData("__\nfoo bar__",
@"<document>
    <paragraph>
        <text>__</text>
        <softbreak />
        <text>foo bar</text>
        <text>__</text>
    </paragraph>
</document>")]
    [InlineData("a__\"foo\"__",
@"<document>
    <paragraph>
        <text>a</text>
        <text>__</text>
        <text>""foo""</text>
        <text>__</text>
    </paragraph>
</document>")]
    [InlineData("foo__bar__",
@"<document>
    <paragraph>
        <text>foo</text>
        <text>__</text>
        <text>bar</text>
        <text>__</text>
    </paragraph>
</document>")]
    [InlineData("5__6__78",
@"<document>
    <paragraph>
        <text>5</text>
        <text>__</text>
        <text>6</text>
        <text>__</text>
        <text>78</text>
    </paragraph>
</document>")]
    [InlineData("пристаням__стремятся__",
@"<document>
    <paragraph>
        <text>пристаням</text>
        <text>__</text>
        <text>стремятся</text>
        <text>__</text>
    </paragraph>
</document>")]
    [InlineData("__foo, __bar__, baz__",
@"<document>
    <paragraph>
        <strong>
            <text>foo, </text>
            <strong>
                <text>bar</text>
            </strong>
            <text>, baz</text>
        </strong>
    </paragraph>
</document>")]
    [InlineData("foo-__(bar)__",
@"<document>
    <paragraph>
        <text>foo-</text>
        <strong>
            <text>(bar)</text>
        </strong>
    </paragraph>
</document>")]
    public void Parse_StrongEmphasisRules_AdheresToCommonMarkSpec(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}
