using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class BlockQuoteTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Fact]
    public void Parse_BasicBlockQuote()
    {

        Assert.Equal(@"<document>
    <block_quote>
        <paragraph>
            <text>foo</text>
        </paragraph>
    </block_quote>
</document>", ToXml(@"> foo"));
    }

    [Theory]
    [InlineData(@"> # Foo
> bar
> baz", @"<document>
    <block_quote>
        <heading level=""1"">
            <text>Foo</text>
        </heading>
        <paragraph>
            <text>bar</text>
            <softbreak />
            <text>baz</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"># Foo
>bar
> baz", @"<document>
    <block_quote>
        <heading level=""1"">
            <text>Foo</text>
        </heading>
        <paragraph>
            <text>bar</text>
            <softbreak />
            <text>baz</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"   > # Foo
   > bar
 > baz", @"<document>
    <block_quote>
        <heading level=""1"">
            <text>Foo</text>
        </heading>
        <paragraph>
            <text>bar</text>
            <softbreak />
            <text>baz</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"    > # Foo
    > bar
    > baz", @"<document>
    <code_block>&gt; # Foo
&gt; bar
&gt; baz
</code_block>
</document>")]
    [InlineData(@"> # Foo
> bar
baz", @"<document>
    <block_quote>
        <heading level=""1"">
            <text>Foo</text>
        </heading>
        <paragraph>
            <text>bar</text>
            <softbreak />
            <text>baz</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"> bar
baz
> foo", @"<document>
    <block_quote>
        <paragraph>
            <text>bar</text>
            <softbreak />
            <text>baz</text>
            <softbreak />
            <text>foo</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"> foo
---", @"<document>
    <block_quote>
        <paragraph>
            <text>foo</text>
        </paragraph>
    </block_quote>
    <thematic_break />
</document>")]
    [InlineData(@"> - foo
- bar", @"<document>
    <block_quote>
        <list type=""bullet"" tight=""true"">
            <item>
                <paragraph>
                    <text>foo</text>
                </paragraph>
            </item>
        </list>
    </block_quote>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@">     foo
    bar", @"<document>
    <block_quote>
        <code_block>foo
</code_block>
    </block_quote>
    <code_block>bar
</code_block>
</document>")]
    [InlineData(@"> ```
foo
```", @"<document>
    <block_quote>
        <code_block></code_block>
    </block_quote>
    <paragraph>
        <text>foo</text>
    </paragraph>
    <code_block></code_block>
</document>")]
    [InlineData(@"> foo
    - bar", @"<document>
    <block_quote>
        <paragraph>
            <text>foo</text>
            <softbreak />
            <text>- bar</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@">", @"<document>
    <block_quote />
</document>")]
    [InlineData(@">
>  
> ", @"<document>
    <block_quote />
</document>")]
    [InlineData(@">
> foo
>  ", @"<document>
    <block_quote>
        <paragraph>
            <text>foo</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"> foo

> bar", @"<document>
    <block_quote>
        <paragraph>
            <text>foo</text>
        </paragraph>
    </block_quote>
    <block_quote>
        <paragraph>
            <text>bar</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"> foo
>
> bar", @"<document>
    <block_quote>
        <paragraph>
            <text>foo</text>
        </paragraph>
        <paragraph>
            <text>bar</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"foo
> bar", @"<document>
    <paragraph>
        <text>foo</text>
    </paragraph>
    <block_quote>
        <paragraph>
            <text>bar</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"> aaa
***
> bbb", @"<document>
    <block_quote>
        <paragraph>
            <text>aaa</text>
        </paragraph>
    </block_quote>
    <thematic_break />
    <block_quote>
        <paragraph>
            <text>bbb</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"> bar
baz", @"<document>
    <block_quote>
        <paragraph>
            <text>bar</text>
            <softbreak />
            <text>baz</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"> bar

baz", @"<document>
    <block_quote>
        <paragraph>
            <text>bar</text>
        </paragraph>
    </block_quote>
    <paragraph>
        <text>baz</text>
    </paragraph>
</document>")]
    [InlineData(@"> bar
>
baz", @"<document>
    <block_quote>
        <paragraph>
            <text>bar</text>
        </paragraph>
    </block_quote>
    <paragraph>
        <text>baz</text>
    </paragraph>
</document>")]
    [InlineData(@"> > > foo
bar", @"<document>
    <block_quote>
        <block_quote>
            <block_quote>
                <paragraph>
                    <text>foo</text>
                    <softbreak />
                    <text>bar</text>
                </paragraph>
            </block_quote>
        </block_quote>
    </block_quote>
</document>")]
    [InlineData(@">>> foo
> bar
>>baz", @"<document>
    <block_quote>
        <block_quote>
            <block_quote>
                <paragraph>
                    <text>foo</text>
                    <softbreak />
                    <text>bar</text>
                    <softbreak />
                    <text>baz</text>
                </paragraph>
            </block_quote>
        </block_quote>
    </block_quote>
</document>")]
    [InlineData(@">     code

>    not code", @"<document>
    <block_quote>
        <code_block>code
</code_block>
    </block_quote>
    <block_quote>
        <paragraph>
            <text>not code</text>
        </paragraph>
    </block_quote>
</document>")]
    public void Parse_BlockQuote(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}
