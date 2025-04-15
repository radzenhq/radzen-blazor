using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class EmphasisTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);
        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData(@"**foo** bar
baz",@"<document>
    <paragraph>
        <strong>
            <text>foo</text>
        </strong>
        <text> bar</text>
        <softbreak />
        <text>baz</text>
    </paragraph>
</document>")]
    [InlineData("*foo bar*",
@"<document>
    <paragraph>
        <emph>
            <text>foo bar</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData("a * foo bar*",
@"<document>
    <paragraph>
        <text>a </text>
        <text>*</text>
        <text> foo bar</text>
        <text>*</text>
    </paragraph>
</document>")]
    [InlineData("a*\"foo\"*",
@"<document>
    <paragraph>
        <text>a</text>
        <text>*</text>
        <text>""foo""</text>
        <text>*</text>
    </paragraph>
</document>")]
    [InlineData("* a *",
@"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>a </text>
                <text>*</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData("foo*bar*",
@"<document>
    <paragraph>
        <text>foo</text>
        <emph>
            <text>bar</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData("5*6*78",
@"<document>
    <paragraph>
        <text>5</text>
        <emph>
            <text>6</text>
        </emph>
        <text>78</text>
    </paragraph>
</document>")]
    [InlineData("_foo bar_",
@"<document>
    <paragraph>
        <emph>
            <text>foo bar</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData("_ foo bar_",
@"<document>
    <paragraph>
        <text>_</text>
        <text> foo bar</text>
        <text>_</text>
    </paragraph>
</document>")]
    [InlineData("a_\"foo\"_",
@"<document>
    <paragraph>
        <text>a</text>
        <text>_</text>
        <text>""foo""</text>
        <text>_</text>
    </paragraph>
</document>")]
    [InlineData("foo_bar_",
@"<document>
    <paragraph>
        <text>foo</text>
        <text>_</text>
        <text>bar</text>
        <text>_</text>
    </paragraph>
</document>")]
    [InlineData("5_6_78",
@"<document>
    <paragraph>
        <text>5</text>
        <text>_</text>
        <text>6</text>
        <text>_</text>
        <text>78</text>
    </paragraph>
</document>")]
    [InlineData("пристаням_стремятся_",
@"<document>
    <paragraph>
        <text>пристаням</text>
        <text>_</text>
        <text>стремятся</text>
        <text>_</text>
    </paragraph>
</document>")]
    [InlineData("aa_\"bb\"_cc",
@"<document>
    <paragraph>
        <text>aa</text>
        <text>_</text>
        <text>""bb""</text>
        <text>_</text>
        <text>cc</text>
    </paragraph>
</document>")]
    [InlineData("foo-_(bar)_",
@"<document>
    <paragraph>
        <text>foo-</text>
        <emph>
            <text>(bar)</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData("_foo*",
@"<document>
    <paragraph>
        <text>_</text>
        <text>foo</text>
        <text>*</text>
    </paragraph>
</document>")]
    [InlineData("*foo bar *",
@"<document>
    <paragraph>
        <text>*</text>
        <text>foo bar </text>
        <text>*</text>
    </paragraph>
</document>")]
    [InlineData("*foo bar\nbaz*",
@"<document>
    <paragraph>
        <emph>
            <text>foo bar</text>
            <softbreak />
            <text>baz</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData("*(*foo)",
@"<document>
    <paragraph>
        <text>*</text>
        <text>(</text>
        <text>*</text>
        <text>foo)</text>
    </paragraph>
</document>")]
    [InlineData("*(*foo*)*",
@"<document>
    <paragraph>
        <emph>
            <text>(</text>
            <emph>
                <text>foo</text>
            </emph>
            <text>)</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData("*foo*bar",
@"<document>
    <paragraph>
        <emph>
            <text>foo</text>
        </emph>
        <text>bar</text>
    </paragraph>
</document>")]
    [InlineData("_foo bar _",
@"<document>
    <paragraph>
        <text>_</text>
        <text>foo bar </text>
        <text>_</text>
    </paragraph>
</document>")]
    [InlineData("_(_foo_)_",
@"<document>
    <paragraph>
        <emph>
            <text>(</text>
            <emph>
                <text>foo</text>
            </emph>
            <text>)</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData("_foo_bar",
@"<document>
    <paragraph>
        <text>_</text>
        <text>foo</text>
        <text>_</text>
        <text>bar</text>
    </paragraph>
</document>")]
    [InlineData("_пристаням_стремятся",
@"<document>
    <paragraph>
        <text>_</text>
        <text>пристаням</text>
        <text>_</text>
        <text>стремятся</text>
    </paragraph>
</document>")]
    [InlineData("_foo_bar_baz_",
@"<document>
    <paragraph>
        <emph>
            <text>foo</text>
            <text>_</text>
            <text>bar</text>
            <text>_</text>
            <text>baz</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData("_(bar)_.",
@"<document>
    <paragraph>
        <emph>
            <text>(bar)</text>
        </emph>
        <text>.</text>
    </paragraph>
</document>")]
    public void Parse_EmphasisRules_AdheresToCommonMarkSpec(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
} 