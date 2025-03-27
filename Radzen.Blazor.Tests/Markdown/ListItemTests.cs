using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class ListItemTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData(@"1.  A paragraph
    with two lines.

        indented code

    > A block quote.", @"<document>
    <list type=""ordered"" start=""1"" tight=""false"">
        <item>
            <paragraph>
                <text>A paragraph</text>
                <softbreak />
                <text>with two lines.</text>
            </paragraph>
            <code_block>indented code
</code_block>
            <block_quote>
                <paragraph>
                    <text>A block quote.</text>
                </paragraph>
            </block_quote>
        </item>
    </list>
</document>")]
    [InlineData(@"- one

 two", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>one</text>
            </paragraph>
        </item>
    </list>
    <paragraph>
        <text>two</text>
    </paragraph>
</document>")]
    [InlineData(@"- one

  two", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>one</text>
            </paragraph>
            <paragraph>
                <text>two</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@" -    one

     two", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>one</text>
            </paragraph>
        </item>
    </list>
    <code_block> two
</code_block>
</document>")]
    [InlineData(@" -    one

      two", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>one</text>
            </paragraph>
            <paragraph>
                <text>two</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"   > > 1.  one
>>
>>     two
", @"<document>
    <block_quote>
        <block_quote>
            <list type=""ordered"" start=""1"" tight=""false"">
                <item>
                    <paragraph>
                        <text>one</text>
                    </paragraph>
                    <paragraph>
                        <text>two</text>
                    </paragraph>
                </item>
            </list>
        </block_quote>
    </block_quote>
</document>")]
    [InlineData(@">>- one
>>
  >  > two
", @"<document>
    <block_quote>
        <block_quote>
            <list type=""bullet"" tight=""true"">
                <item>
                    <paragraph>
                        <text>one</text>
                    </paragraph>
                </item>
            </list>
            <paragraph>
                <text>two</text>
            </paragraph>
        </block_quote>
    </block_quote>
</document>")]
    [InlineData(@"-one

2.two
", @"<document>
    <paragraph>
        <text>-one</text>
    </paragraph>
    <paragraph>
        <text>2.two</text>
    </paragraph>
</document>")]
    [InlineData(@"- foo


  bar", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"1.  foo

    ```
    bar
    ```

    baz

    > bam", @"<document>
    <list type=""ordered"" start=""1"" tight=""false"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
            <code_block>bar
</code_block>
            <paragraph>
                <text>baz</text>
            </paragraph>
            <block_quote>
                <paragraph>
                    <text>bam</text>
                </paragraph>
            </block_quote>
        </item>
    </list>
</document>")]
    [InlineData(@"- Foo

      bar


      baz", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>Foo</text>
            </paragraph>
            <code_block>bar


baz
</code_block>
        </item>
    </list>
</document>")]
    [InlineData(@"123456789. ok", @"<document>
    <list type=""ordered"" start=""123456789"" tight=""true"">
        <item>
            <paragraph>
                <text>ok</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"1234567890. not ok", @"<document>
    <paragraph>
        <text>1234567890. not ok</text>
    </paragraph>
</document>")]
    [InlineData(@"0. ok", @"<document>
    <list type=""ordered"" start=""0"" tight=""true"">
        <item>
            <paragraph>
                <text>ok</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"003. ok", @"<document>
    <list type=""ordered"" start=""3"" tight=""true"">
        <item>
            <paragraph>
                <text>ok</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"-1. not ok", @"<document>
    <paragraph>
        <text>-1. not ok</text>
    </paragraph>
</document>")]
    [InlineData(@"- foo

      bar", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
            <code_block>bar
</code_block>
        </item>
    </list>
</document>")]
    [InlineData(@"  10.  foo

           bar", @"<document>
    <list type=""ordered"" start=""10"" tight=""false"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
            <code_block>bar
</code_block>
        </item>
    </list>
</document>")]
    [InlineData(@"1.     indented code

   paragraph

       more code", @"<document>
    <list type=""ordered"" start=""1"" tight=""false"">
        <item>
            <code_block>indented code
</code_block>
            <paragraph>
                <text>paragraph</text>
            </paragraph>
            <code_block>more code
</code_block>
        </item>
    </list>
</document>")]
    [InlineData(@"-     indented code

   paragraph

      more code", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <code_block>indented code
</code_block>
            <paragraph>
                <text>paragraph</text>
            </paragraph>
            <code_block>more code
</code_block>
        </item>
    </list>
</document>")]
    [InlineData(@"1.      indented code

   paragraph

       more code", @"<document>
    <list type=""ordered"" start=""1"" tight=""false"">
        <item>
            <code_block> indented code
</code_block>
            <paragraph>
                <text>paragraph</text>
            </paragraph>
            <code_block>more code
</code_block>
        </item>
    </list>
</document>")]
    [InlineData(@"-    foo

  bar", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
    </list>
    <paragraph>
        <text>bar</text>
    </paragraph>
</document>")]
    [InlineData(@"-  foo

   bar", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"-
  foo
-
  ```
  bar
  ```
-
      baz", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
        <item>
            <code_block>bar
</code_block>
        </item>
        <item>
            <code_block>baz
</code_block>
        </item>
    </list>
</document>")]
    [InlineData(@"1.
   foo", @"<document>
    <list type=""ordered"" start=""1"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"-   
  foo", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"-

  foo", @"<document>
    <list type=""bullet"" tight=""true"">
        <item />
    </list>
    <paragraph>
        <text>foo</text>
    </paragraph>
</document>")]
    [InlineData(@"- foo
-
- bar", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
        <item />
        <item>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
[InlineData(@"- foo
-   
- bar", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
        <item />
        <item>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
[InlineData(@"1. foo
2.
3. bar", @"<document>
    <list type=""ordered"" start=""1"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
        <item />
        <item>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"*", @"<document>
    <list type=""bullet"" tight=""true"">
        <item />
    </list>
</document>")]
    [InlineData(@"foo
*

foo
1.", @"<document>
    <paragraph>
        <text>foo</text>
        <softbreak />
        <text>*</text>
    </paragraph>
    <paragraph>
        <text>foo</text>
        <softbreak />
        <text>1.</text>
    </paragraph>
</document>")]
    [InlineData(@" 1.  A paragraph
     with two lines.

         indented code

     > A block quote.", @"<document>
    <list type=""ordered"" start=""1"" tight=""false"">
        <item>
            <paragraph>
                <text>A paragraph</text>
                <softbreak />
                <text>with two lines.</text>
            </paragraph>
            <code_block>indented code
</code_block>
            <block_quote>
                <paragraph>
                    <text>A block quote.</text>
                </paragraph>
            </block_quote>
        </item>
    </list>
</document>")]
    [InlineData(@"  1.  A paragraph
      with two lines.

          indented code

      > A block quote.", @"<document>
    <list type=""ordered"" start=""1"" tight=""false"">
        <item>
            <paragraph>
                <text>A paragraph</text>
                <softbreak />
                <text>with two lines.</text>
            </paragraph>
            <code_block>indented code
</code_block>
            <block_quote>
                <paragraph>
                    <text>A block quote.</text>
                </paragraph>
            </block_quote>
        </item>
    </list>
</document>")]
    [InlineData(@"   1.  A paragraph
       with two lines.

           indented code

       > A block quote.", @"<document>
    <list type=""ordered"" start=""1"" tight=""false"">
        <item>
            <paragraph>
                <text>A paragraph</text>
                <softbreak />
                <text>with two lines.</text>
            </paragraph>
            <code_block>indented code
</code_block>
            <block_quote>
                <paragraph>
                    <text>A block quote.</text>
                </paragraph>
            </block_quote>
        </item>
    </list>
</document>")]
    [InlineData(@"    1.  A paragraph
        with two lines.

            indented code

        > A block quote.", @"<document>
    <code_block>1.  A paragraph
    with two lines.

        indented code

    &gt; A block quote.
</code_block>
</document>")]
    public void Parse_ListItem(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"   1.  A paragraph
with two lines.

          indented code

      > A block quote.", @"<document>
    <list type=""ordered"" start=""1"" tight=""false"">
        <item>
            <paragraph>
                <text>A paragraph</text>
                <softbreak />
                <text>with two lines.</text>
            </paragraph>
            <paragraph>
                <text>indented code</text>
            </paragraph>
        </item>
    </list>
    <code_block>  &gt; A block quote.
</code_block>
</document>")]
    [InlineData(@"  1.  A paragraph
    with two lines.", @"<document>
    <list type=""ordered"" start=""1"" tight=""true"">
        <item>
            <paragraph>
                <text>A paragraph</text>
                <softbreak />
                <text>with two lines.</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"> 1. > Blockquote
continued here.", @"<document>
    <block_quote>
        <list type=""ordered"" start=""1"" tight=""true"">
            <item>
                <block_quote>
                    <paragraph>
                        <text>Blockquote</text>
                        <softbreak />
                        <text>continued here.</text>
                    </paragraph>
                </block_quote>
            </item>
        </list>
    </block_quote>
</document>")]
    [InlineData(@"> 1. > Blockquote
> continued here.", @"<document>
    <block_quote>
        <list type=""ordered"" start=""1"" tight=""true"">
            <item>
                <block_quote>
                    <paragraph>
                        <text>Blockquote</text>
                        <softbreak />
                        <text>continued here.</text>
                    </paragraph>
                </block_quote>
            </item>
        </list>
    </block_quote>
</document>")]
    public void Parse_ListItem_WithLazyContinuation(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"- foo
  - bar
    - baz
      - boo", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
            <list type=""bullet"" tight=""true"">
                <item>
                    <paragraph>
                        <text>bar</text>
                    </paragraph>
                    <list type=""bullet"" tight=""true"">
                        <item>
                            <paragraph>
                                <text>baz</text>
                            </paragraph>
                            <list type=""bullet"" tight=""true"">
                                <item>
                                    <paragraph>
                                        <text>boo</text>
                                    </paragraph>
                                </item>
                            </list>
                        </item>
                    </list>
                </item>
            </list>
        </item>
    </list>
</document>")]
    [InlineData(@"- foo
 - bar
  - baz
   - boo", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>baz</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>boo</text>
            </paragraph>
        </item>
    </list>
</document>")]
[InlineData(@"1. foo
 1. bar
  1. baz
   1. boo", @"<document>
    <list type=""ordered"" start=""1"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>baz</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>boo</text>
            </paragraph>
        </item>
    </list>
</document>")]
 [InlineData(@"
1. foo
   1. bar
      1. baz
         1. boo", @"<document>
    <list type=""ordered"" start=""1"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
            <list type=""ordered"" start=""1"" tight=""true"">
                <item>
                    <paragraph>
                        <text>bar</text>
                    </paragraph>
                    <list type=""ordered"" start=""1"" tight=""true"">
                        <item>
                            <paragraph>
                                <text>baz</text>
                            </paragraph>
                            <list type=""ordered"" start=""1"" tight=""true"">
                                <item>
                                    <paragraph>
                                        <text>boo</text>
                                    </paragraph>
                                </item>
                            </list>
                        </item>
                    </list>
                </item>
            </list>
        </item>
    </list>
</document>")]
    [InlineData(@"10) foo
    - bar", @"<document>
    <list type=""ordered"" start=""10"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
            <list type=""bullet"" tight=""true"">
                <item>
                    <paragraph>
                        <text>bar</text>
                    </paragraph>
                </item>
            </list>
        </item>
    </list>
</document>")]
    [InlineData(@"
10) foo
   - bar", @"<document>
    <list type=""ordered"" start=""10"" tight=""true"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
    </list>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"- - foo", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <list type=""bullet"" tight=""true"">
                <item>
                    <paragraph>
                        <text>foo</text>
                    </paragraph>
                </item>
            </list>
        </item>
    </list>
</document>")]
    [InlineData(@"1. - foo", @"<document>
    <list type=""ordered"" start=""1"" tight=""true"">
        <item>
            <list type=""bullet"" tight=""true"">
                <item>
                    <paragraph>
                        <text>foo</text>
                    </paragraph>
                </item>
            </list>
        </item>
    </list>
</document>")]
    [InlineData(@"1. 1. foo", @"<document>
    <list type=""ordered"" start=""1"" tight=""true"">
        <item>
            <list type=""ordered"" start=""1"" tight=""true"">
                <item>
                    <paragraph>
                        <text>foo</text>
                    </paragraph>
                </item>
            </list>
        </item>
    </list>
</document>")]
    [InlineData(@"- 1. foo", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <list type=""ordered"" start=""1"" tight=""true"">
                <item>
                    <paragraph>
                        <text>foo</text>
                    </paragraph>
                </item>
            </list>
        </item>
    </list>
</document>")]
    [InlineData(@"1. - 2. foo", @"<document>
    <list type=""ordered"" start=""1"" tight=""true"">
        <item>
            <list type=""bullet"" tight=""true"">
                <item>
                    <list type=""ordered"" start=""2"" tight=""true"">
                        <item>
                            <paragraph>
                                <text>foo</text>
                            </paragraph>
                        </item>
                    </list>
                </item>
            </list>
        </item>
    </list>
</document>")]
    [InlineData(@"- # Foo
- Bar
  ---
  baz", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <heading level=""1"">
                <text>Foo</text>
            </heading>
        </item>
        <item>
            <heading level=""2"">
                <text>Bar</text>
            </heading>
            <paragraph>
                <text>baz</text>
            </paragraph>
        </item>
    </list>
</document>")]
    public void Parse_ListItem_WithNesting(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}
