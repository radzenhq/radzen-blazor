using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class ListTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData(@"- foo
- bar
+ baz", @"<document>
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
    </list>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>baz</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"1. foo
2. bar
3) baz", @"<document>
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
    </list>
    <list type=""ordered"" start=""3"" tight=""true"">
        <item>
            <paragraph>
                <text>baz</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"Foo
- bar
- baz", @"<document>
    <paragraph>
        <text>Foo</text>
    </paragraph>
    <list type=""bullet"" tight=""true"">
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
    </list>
</document>")]
    public void Parse_List(string markdown, string expected)
    {
        var actual = ToXml(markdown);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(@"The number of windows in my house is
14.  The number of doors is 6.", @"<document>
    <paragraph>
        <text>The number of windows in my house is</text>
        <softbreak />
        <text>14.  The number of doors is 6.</text>
    </paragraph>
</document>")]
    [InlineData(@"The number of windows in my house is
1.  The number of doors is 6.", @"<document>
    <paragraph>
        <text>The number of windows in my house is</text>
    </paragraph>
    <list type=""ordered"" start=""1"" tight=""true"">
        <item>
            <paragraph>
                <text>The number of doors is 6.</text>
            </paragraph>
        </item>
    </list>
</document>")]
    public void Parse_OnlyNumberedListsThatStartWithOneCanInterruptParagraphs(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"- foo

- bar


- baz", @"<document>
    <list type=""bullet"" tight=""false"">
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
    </list>
</document>")]
    [InlineData(@"- foo
  - bar
    - baz


      bim
", @"<document>
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
                    <list type=""bullet"" tight=""false"">
                        <item>
                            <paragraph>
                                <text>baz</text>
                            </paragraph>
                            <paragraph>
                                <text>bim</text>
                            </paragraph>
                        </item>
                    </list>
                </item>
            </list>
        </item>
    </list>
</document>")]
    [InlineData(@"- a
- b

- c", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>b</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>c</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"* a
*

* c", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
        </item>
        <item />
        <item>
            <paragraph>
                <text>c</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"- a
- b

  c
- d", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>b</text>
            </paragraph>
            <paragraph>
                <text>c</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>d</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"- a
- b

  [ref]: /url
- d", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>b</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>d</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"- a
- ```
  b


  ```
- c", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
        </item>
        <item>
            <code_block>b


</code_block>
        </item>
        <item>
            <paragraph>
                <text>c</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"- a
  - b

    c
- d", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
            <list type=""bullet"" tight=""false"">
                <item>
                    <paragraph>
                        <text>b</text>
                    </paragraph>
                    <paragraph>
                        <text>c</text>
                    </paragraph>
                </item>
            </list>
        </item>
        <item>
            <paragraph>
                <text>d</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"* a
  > b
  >
* c", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
            <block_quote>
                <paragraph>
                    <text>b</text>
                </paragraph>
            </block_quote>
        </item>
        <item>
            <paragraph>
                <text>c</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"- a
  > b
  ```
  c
  ```
- d
", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
            <block_quote>
                <paragraph>
                    <text>b</text>
                </paragraph>
            </block_quote>
            <code_block>c
</code_block>
        </item>
        <item>
            <paragraph>
                <text>d</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"- a", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"- a
  - b", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
            <list type=""bullet"" tight=""true"">
                <item>
                    <paragraph>
                        <text>b</text>
                    </paragraph>
                </item>
            </list>
        </item>
    </list>
</document>")]
    [InlineData(@"1. ```
   foo
   ```

   bar", @"<document>
    <list type=""ordered"" start=""1"" tight=""false"">
        <item>
            <code_block>foo
</code_block>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"* foo
  * bar

  baz", @"<document>
    <list type=""bullet"" tight=""false"">
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
            <paragraph>
                <text>baz</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"- a
  - b
  - c

- d
  - e
  - f", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
            <list type=""bullet"" tight=""true"">
                <item>
                    <paragraph>
                        <text>b</text>
                    </paragraph>
                </item>
                <item>
                    <paragraph>
                        <text>c</text>
                    </paragraph>
                </item>
            </list>
        </item>
        <item>
            <paragraph>
                <text>d</text>
            </paragraph>
            <list type=""bullet"" tight=""true"">
                <item>
                    <paragraph>
                        <text>e</text>
                    </paragraph>
                </item>
                <item>
                    <paragraph>
                        <text>f</text>
                    </paragraph>
                </item>
            </list>
        </item>
    </list>
</document>")]
    public void Parse_TightAndLooseLists(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"- foo
- bar

<!-- -->

- baz
- bim", @"<document>
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
    </list>
    <html_block>&lt;!-- --&gt;</html_block>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>baz</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>bim</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"-   foo

    notcode

-   foo

<!-- -->

    code", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
            <paragraph>
                <text>notcode</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
    </list>
    <html_block>&lt;!-- --&gt;</html_block>
    <code_block>code
</code_block>
</document>")]
    public void Parse_List_Separators(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"- a
 - b
  - c
   - d
  - e
 - f
- g", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>b</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>c</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>d</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>e</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>f</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>g</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"1. a

  2. b

   3. c
", @"<document>
    <list type=""ordered"" start=""1"" tight=""false"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>b</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>c</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"- a
 - b
  - c
   - d
    - e", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>b</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>c</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>d</text>
                <softbreak />
                <text>- e</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"1. a

  2. b

    3. c", @"<document>
    <list type=""ordered"" start=""1"" tight=""false"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>b</text>
            </paragraph>
        </item>
    </list>
    <code_block>3. c
</code_block>
</document>")]
    public void Parse_List_Identation(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Fact]
    public void Parse_BasicNestedLists()
    {
        Assert.Equal(@"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>a</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>b</text>
            </paragraph>
        </item>
        <item>
            <paragraph>
                <text>c</text>
            </paragraph>
        </item>
    </list>
</document>", ToXml(@"
- a
 - b
  - c"));
    }
}
