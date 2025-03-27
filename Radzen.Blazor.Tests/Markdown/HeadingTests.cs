using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class HeadingTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Fact]
    public void Parse_BasicAtxHeading()
    {
        Assert.Equal(@"<document>
    <heading level=""1"">
        <text>foo</text>
    </heading>
</document>", ToXml("# foo"));
    }

    [Theory]
    [InlineData(@"# foo
## foo
### foo
#### foo
##### foo
###### foo", @"<document>
    <heading level=""1"">
        <text>foo</text>
    </heading>
    <heading level=""2"">
        <text>foo</text>
    </heading>
    <heading level=""3"">
        <text>foo</text>
    </heading>
    <heading level=""4"">
        <text>foo</text>
    </heading>
    <heading level=""5"">
        <text>foo</text>
    </heading>
    <heading level=""6"">
        <text>foo</text>
    </heading>
</document>")]
    [InlineData(@"####### foo", @"<document>
    <paragraph>
        <text>####### foo</text>
    </paragraph>
</document>")]
    [InlineData(@"#5 bolt

#hashtag", @"<document>
    <paragraph>
        <text>#5 bolt</text>
    </paragraph>
    <paragraph>
        <text>#hashtag</text>
    </paragraph>
</document>")]
    [InlineData(@"\## foo", @"<document>
    <paragraph>
        <text>#</text>
        <text># foo</text>
    </paragraph>
</document>")]
    [InlineData(@"# foo *bar* \*baz\*", @"<document>
    <heading level=""1"">
        <text>foo </text>
        <emph>
            <text>bar</text>
        </emph>
        <text> </text>
        <text>*</text>
        <text>baz</text>
        <text>*</text>
    </heading>
</document>")]
    [InlineData(@"#                  foo                     ", @"<document>
    <heading level=""1"">
        <text>foo</text>
    </heading>
</document>")]
    [InlineData(@" ### foo
  ## foo
   # foo", @"<document>
    <heading level=""3"">
        <text>foo</text>
    </heading>
    <heading level=""2"">
        <text>foo</text>
    </heading>
    <heading level=""1"">
        <text>foo</text>
    </heading>
</document>")]
    [InlineData(@"    # foo", @"<document>
    <code_block># foo
</code_block>
</document>")]
    [InlineData(@"foo
    # bar", @"<document>
    <paragraph>
        <text>foo</text>
        <softbreak />
        <text># bar</text>
    </paragraph>
</document>")]
    [InlineData(@"## foo ##
  ###   bar    ###", @"<document>
    <heading level=""2"">
        <text>foo</text>
    </heading>
    <heading level=""3"">
        <text>bar</text>
    </heading>
</document>")]
    [InlineData(@"# foo ##################################
##### foo ##", @"<document>
    <heading level=""1"">
        <text>foo</text>
    </heading>
    <heading level=""5"">
        <text>foo</text>
    </heading>
</document>")]
    [InlineData(@"### foo ###     ", @"<document>
    <heading level=""3"">
        <text>foo</text>
    </heading>
</document>")]
    [InlineData(@"### foo ### b", @"<document>
    <heading level=""3"">
        <text>foo ### b</text>
    </heading>
</document>")]
    [InlineData(@"# foo#", @"<document>
    <heading level=""1"">
        <text>foo#</text>
    </heading>
</document>")]
    [InlineData(@"### foo \###
## foo #\##
# foo \#", @"<document>
    <heading level=""3"">
        <text>foo </text>
        <text>#</text>
        <text>##</text>
    </heading>
    <heading level=""2"">
        <text>foo #</text>
        <text>#</text>
        <text>#</text>
    </heading>
    <heading level=""1"">
        <text>foo </text>
        <text>#</text>
    </heading>
</document>")]
    [InlineData(@"****
## foo
****", @"<document>
    <thematic_break />
    <heading level=""2"">
        <text>foo</text>
    </heading>
    <thematic_break />
</document>")]
    [InlineData(@"Foo bar
# baz
Bar foo", @"<document>
    <paragraph>
        <text>Foo bar</text>
    </paragraph>
    <heading level=""1"">
        <text>baz</text>
    </heading>
    <paragraph>
        <text>Bar foo</text>
    </paragraph>
</document>")]
    [InlineData(@"## 
#
### ###", @"<document>
    <heading level=""2"" />
    <heading level=""1"" />
    <heading level=""3"" />
</document>")]
    public void Parse_AtxHeading(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"Foo *bar*
=========

Foo *baz*
---------", @"<document>
    <heading level=""1"">
        <text>Foo </text>
        <emph>
            <text>bar</text>
        </emph>
    </heading>
    <heading level=""2"">
        <text>Foo </text>
        <emph>
            <text>baz</text>
        </emph>
    </heading>
</document>")]
    [InlineData(@"Foo *bar
baz*
====", @"<document>
    <heading level=""1"">
        <text>Foo </text>
        <emph>
            <text>bar</text>
            <softbreak />
            <text>baz</text>
        </emph>
    </heading>
</document>")]
    [InlineData(@"  Foo *bar
baz*
====", @"<document>
    <heading level=""1"">
        <text>Foo </text>
        <emph>
            <text>bar</text>
            <softbreak />
            <text>baz</text>
        </emph>
    </heading>
</document>")]
    [InlineData(@"Foo
-------------------------

Foo
=", @"<document>
    <heading level=""2"">
        <text>Foo</text>
    </heading>
    <heading level=""1"">
        <text>Foo</text>
    </heading>
</document>")]
    [InlineData(@"   Foo
---

  Foo
-----

  Foo
  ===", @"<document>
    <heading level=""2"">
        <text>Foo</text>
    </heading>
    <heading level=""2"">
        <text>Foo</text>
    </heading>
    <heading level=""1"">
        <text>Foo</text>
    </heading>
</document>")]
    [InlineData(@"    Foo
    ---

    Foo
---", @"<document>
    <code_block>Foo
---

Foo
</code_block>
    <thematic_break />
</document>")]
    [InlineData(@"Foo
   ----      ", @"<document>
    <heading level=""2"">
        <text>Foo</text>
    </heading>
</document>")]
    [InlineData(@"Foo
    ---", @"<document>
    <paragraph>
        <text>Foo</text>
        <softbreak />
        <text>---</text>
    </paragraph>
</document>")]
    [InlineData(@"Foo
= =

Foo
--- -", @"<document>
    <paragraph>
        <text>Foo</text>
        <softbreak />
        <text>= =</text>
    </paragraph>
    <paragraph>
        <text>Foo</text>
    </paragraph>
    <thematic_break />
</document>")]
    [InlineData(@"Foo  
-----", @"<document>
    <heading level=""2"">
        <text>Foo</text>
    </heading>
</document>")]
    [InlineData(@"Foo\
----", @"<document>
    <heading level=""2"">
        <text>Foo\</text>
    </heading>
</document>")]
    [InlineData(@"`Foo
----
`

<a title=""a lot
---
of dashes""/>", @"<document>
    <heading level=""2"">
        <text>`</text>
        <text>Foo</text>
    </heading>
    <paragraph>
        <text>`</text>
    </paragraph>
    <heading level=""2"">
        <text>&lt;a title=""a lot</text>
    </heading>
    <paragraph>
        <text>of dashes""/&gt;</text>
    </paragraph>
</document>")]
    [InlineData(@"> Foo
---", @"<document>
    <block_quote>
        <paragraph>
            <text>Foo</text>
        </paragraph>
    </block_quote>
    <thematic_break />
</document>")]
    [InlineData(@"> foo
bar
===", @"<document>
    <block_quote>
        <paragraph>
            <text>foo</text>
            <softbreak />
            <text>bar</text>
            <softbreak />
            <text>===</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"- Foo
---", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>Foo</text>
            </paragraph>
        </item>
    </list>
    <thematic_break />
</document>")]
    [InlineData(@"Foo
Bar
---", @"<document>
    <heading level=""2"">
        <text>Foo</text>
        <softbreak />
        <text>Bar</text>
    </heading>
</document>")]
    [InlineData(@"---
Foo
---
Bar
---
Baz", @"<document>
    <thematic_break />
    <heading level=""2"">
        <text>Foo</text>
    </heading>
    <heading level=""2"">
        <text>Bar</text>
    </heading>
    <paragraph>
        <text>Baz</text>
    </paragraph>
</document>")]
    [InlineData(@"
====", @"<document>
    <paragraph>
        <text>====</text>
    </paragraph>
</document>")]
    [InlineData(@"---
---", @"<document>
    <thematic_break />
    <thematic_break />
</document>")]
    [InlineData(@"- foo
-----", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
    </list>
    <thematic_break />
</document>")]
    [InlineData(@"    foo
---", @"<document>
    <code_block>foo
</code_block>
    <thematic_break />
</document>")]
    [InlineData(@"> foo
-----", @"<document>
    <block_quote>
        <paragraph>
            <text>foo</text>
        </paragraph>
    </block_quote>
    <thematic_break />
</document>")]
    [InlineData(@"\> foo
------", @"<document>
    <heading level=""2"">
        <text>&gt; foo</text>
    </heading>
</document>")]
    [InlineData(@"Foo

bar
---
baz", @"<document>
    <paragraph>
        <text>Foo</text>
    </paragraph>
    <heading level=""2"">
        <text>bar</text>
    </heading>
    <paragraph>
        <text>baz</text>
    </paragraph>
</document>")]
    [InlineData(@"Foo
bar

---

baz", @"<document>
    <paragraph>
        <text>Foo</text>
        <softbreak />
        <text>bar</text>
    </paragraph>
    <thematic_break />
    <paragraph>
        <text>baz</text>
    </paragraph>
</document>")]
    [InlineData(@"Foo
bar
* * *
baz", @"<document>
    <paragraph>
        <text>Foo</text>
        <softbreak />
        <text>bar</text>
    </paragraph>
    <thematic_break />
    <paragraph>
        <text>baz</text>
    </paragraph>
</document>")]
    [InlineData(@"Foo
bar
\---
baz", @"<document>
    <paragraph>
        <text>Foo</text>
        <softbreak />
        <text>bar</text>
        <softbreak />
        <text>-</text>
        <text>--</text>
        <softbreak />
        <text>baz</text>
    </paragraph>
</document>")]
    public void Parse_SetExtHeading(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}
