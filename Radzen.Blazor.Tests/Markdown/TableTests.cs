using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class TableTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Fact]
    public void Parse_BasicTable()
    {
        Assert.Equal(
            @"<document>
    <table>
        <header>
            <cell>
                <text>foo</text>
            </cell>
            <cell>
                <text>bar</text>
            </cell>
        </header>
        <row>
            <cell>
                <text>baz</text>
            </cell>
            <cell>
                <text>bim</text>
            </cell>
        </row>
    </table>
</document>",
            ToXml(@"
foo|bar
--|--
baz|bim"));
    }

    [Theory]
    [InlineData(@"| foo | bar |
| --- | --- |
| baz | bim |", @"<document>
    <table>
        <header>
            <cell>
                <text>foo</text>
            </cell>
            <cell>
                <text>bar</text>
            </cell>
        </header>
        <row>
            <cell>
                <text>baz</text>
            </cell>
            <cell>
                <text>bim</text>
            </cell>
        </row>
    </table>
</document>")]
    [InlineData(@"| f\|oo  |
| ------ |
| b `\|` az |
| b **\|** im |", @"<document>
    <table>
        <header>
            <cell>
                <text>f|oo</text>
            </cell>
        </header>
        <row>
            <cell>
                <text>b </text>
                <code>|</code>
                <text> az</text>
            </cell>
        </row>
        <row>
            <cell>
                <text>b </text>
                <strong>
                    <text>|</text>
                </strong>
                <text> im</text>
            </cell>
        </row>
    </table>
</document>")]
    [InlineData(@"| abc | defghi |
:-: | -----------:
bar | baz", @"<document>
    <table>
        <header>
            <cell align=""center"">
                <text>abc</text>
            </cell>
            <cell align=""right"">
                <text>defghi</text>
            </cell>
        </header>
        <row>
            <cell align=""center"">
                <text>bar</text>
            </cell>
            <cell align=""right"">
                <text>baz</text>
            </cell>
        </row>
    </table>
</document>")]


    [InlineData(@"| abc | def |
| --- | --- |
  bar", @"<document>
    <table>
        <header>
            <cell>
                <text>abc</text>
            </cell>
            <cell>
                <text>def</text>
            </cell>
        </header>
        <row>
            <cell>
                <text>bar</text>
            </cell>
            <cell />
        </row>
    </table>
</document>")]
[InlineData(@"| abc | def |
| --- | --- |
  c:\\foo", @"<document>
    <table>
        <header>
            <cell>
                <text>abc</text>
            </cell>
            <cell>
                <text>def</text>
            </cell>
        </header>
        <row>
            <cell>
                <text>c:</text>
                <text>\</text>
                <text>foo</text>
            </cell>
            <cell />
        </row>
    </table>
</document>")]
    public void Parse_Table(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"| abc | def |
| --- | --- |
| bar | baz |

boo", @"<document>
    <table>
        <header>
            <cell>
                <text>abc</text>
            </cell>
            <cell>
                <text>def</text>
            </cell>
        </header>
        <row>
            <cell>
                <text>bar</text>
            </cell>
            <cell>
                <text>baz</text>
            </cell>
        </row>
    </table>
    <paragraph>
        <text>boo</text>
    </paragraph>
</document>")]
    [InlineData(@"| foo |
| --- |
# bar", @"<document>
    <table>
        <header>
            <cell>
                <text>foo</text>
            </cell>
        </header>
    </table>
    <heading level=""1"">
        <text>bar</text>
    </heading>
</document>")]
    [InlineData(@"| foo |
| --- |
- bar", @"<document>
    <table>
        <header>
            <cell>
                <text>foo</text>
            </cell>
        </header>
    </table>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"| foo |
| --- |
1. bar", @"<document>
    <table>
        <header>
            <cell>
                <text>foo</text>
            </cell>
        </header>
    </table>
    <list type=""ordered"" start=""1"" tight=""true"">
        <item>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"| abc | def |
| --- | --- |
| bar | baz |
> bar", @"<document>
    <table>
        <header>
            <cell>
                <text>abc</text>
            </cell>
            <cell>
                <text>def</text>
            </cell>
        </header>
        <row>
            <cell>
                <text>bar</text>
            </cell>
            <cell>
                <text>baz</text>
            </cell>
        </row>
    </table>
    <block_quote>
        <paragraph>
            <text>bar</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"| abc | def |
| --- | --- |
| bar | baz |
    bar", @"<document>
    <table>
        <header>
            <cell>
                <text>abc</text>
            </cell>
            <cell>
                <text>def</text>
            </cell>
        </header>
        <row>
            <cell>
                <text>bar</text>
            </cell>
            <cell>
                <text>baz</text>
            </cell>
        </row>
    </table>
    <code_block>bar
</code_block>
</document>")]
    [InlineData(@"| abc | def |
| --- | --- |
| bar | baz |
```
bar
```", @"<document>
    <table>
        <header>
            <cell>
                <text>abc</text>
            </cell>
            <cell>
                <text>def</text>
            </cell>
        </header>
        <row>
            <cell>
                <text>bar</text>
            </cell>
            <cell>
                <text>baz</text>
            </cell>
        </row>
    </table>
    <code_block>bar
</code_block>
</document>")]
    [InlineData(@"| abc | def |
| --- | --- |
| bar | baz |
<div>
</div>", @"<document>
    <table>
        <header>
            <cell>
                <text>abc</text>
            </cell>
            <cell>
                <text>def</text>
            </cell>
        </header>
        <row>
            <cell>
                <text>bar</text>
            </cell>
            <cell>
                <text>baz</text>
            </cell>
        </row>
    </table>
    <html_block>&lt;div&gt;
&lt;/div&gt;</html_block>
</document>")]
[InlineData(@"| abc | def |
| --- | --- |
| bar | baz |
---", @"<document>
    <table>
        <header>
            <cell>
                <text>abc</text>
            </cell>
            <cell>
                <text>def</text>
            </cell>
        </header>
        <row>
            <cell>
                <text>bar</text>
            </cell>
            <cell>
                <text>baz</text>
            </cell>
        </row>
    </table>
    <thematic_break />
</document>")]
    public void Parse_Table_AnyBlockOrEmptyLineBreaksTable(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"| abc | def |
| --- |", @"<document>
    <paragraph>
        <text>| abc | def |</text>
        <softbreak />
        <text>| --- |</text>
    </paragraph>
</document>")]
    public void Parse_Table_ChecksHeaderAndDelimiter(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"| abc |
| --- |
| bar | baz |", @"<document>
    <table>
        <header>
            <cell>
                <text>abc</text>
            </cell>
        </header>
        <row>
            <cell>
                <text>bar</text>
            </cell>
        </row>
    </table>
</document>")]
    public void Parse_Table_IgnoresExtraCells(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}