using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class ThematicBreakTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData(@"***
---
___", @"<document>
    <thematic_break />
    <thematic_break />
    <thematic_break />
</document>")]
    [InlineData(@"--
**
__", @"<document>
    <paragraph>
        <text>--</text>
        <softbreak />
        <text>**</text>
        <softbreak />
        <text>__</text>
    </paragraph>
</document>")]
    [InlineData(@" ***
  ***
   ***", @"<document>
    <thematic_break />
    <thematic_break />
    <thematic_break />
</document>")]
    [InlineData(@"    ***", @"<document>
    <code_block>***
</code_block>
</document>")]
    [InlineData(@"Foo
    ***", @"<document>
    <paragraph>
        <text>Foo</text>
        <softbreak />
        <text>***</text>
    </paragraph>
</document>")]
    [InlineData(@"_____________________________________", @"<document>
    <thematic_break />
</document>")]
    [InlineData(@"- - -", @"<document>
    <thematic_break />
</document>")]
    [InlineData(@" **  * ** * ** * **", @"<document>
    <thematic_break />
</document>")]
    [InlineData(@"-     -      -      -", @"<document>
    <thematic_break />
</document>")]
    [InlineData(@"- - - -    ", @"<document>
    <thematic_break />
</document>")]
    [InlineData(@"_ _ _ _ a

a------

---a---", @"<document>
    <paragraph>
        <text>_</text>
        <text> </text>
        <text>_</text>
        <text> </text>
        <text>_</text>
        <text> </text>
        <text>_</text>
        <text> a</text>
    </paragraph>
    <paragraph>
        <text>a------</text>
    </paragraph>
    <paragraph>
        <text>---a---</text>
    </paragraph>
</document>")]
    [InlineData(@" *-*", @"<document>
    <paragraph>
        <emph>
            <text>-</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData(@"- foo
***
- bar", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
    </list>
    <thematic_break />
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"Foo
***
bar", @"<document>
    <paragraph>
        <text>Foo</text>
    </paragraph>
    <thematic_break />
    <paragraph>
        <text>bar</text>
    </paragraph>
</document>")]
    [InlineData(@"* Foo
* * *
* Bar", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>Foo</text>
            </paragraph>
        </item>
    </list>
    <thematic_break />
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>Bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"- Foo
- * * *", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>Foo</text>
            </paragraph>
        </item>
        <item>
            <thematic_break />
        </item>
    </list>
</document>")]
    public void Parse_ThematicBreak(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}