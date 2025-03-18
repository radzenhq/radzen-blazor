using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class FencedCodeBlockTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Fact]
    public void Parse_BasicFencedCodeBlock()
    {
        Assert.Equal(@"<document>
    <code_block>foo
</code_block>
</document>", ToXml(@"```
foo
```"));
    }

    [Theory]
    [InlineData(@"```
<
 >
```", @"<document>
    <code_block>&lt;
 &gt;
</code_block>
</document>")]
    [InlineData(@"~~~
<
 >
~~~", @"<document>
    <code_block>&lt;
 &gt;
</code_block>
</document>")]
    [InlineData(@"``
foo
``", @"<document>
    <paragraph>
        <code>foo</code>
    </paragraph>
</document>")]
    [InlineData(@"```
aaa
~~~
```", @"<document>
    <code_block>aaa
~~~
</code_block>
</document>")]
    [InlineData(@"~~~
aaa
```
~~~", @"<document>
    <code_block>aaa
```
</code_block>
</document>")]
    [InlineData(@"````
aaa
```
``````", @"<document>
    <code_block>aaa
```
</code_block>
</document>")]
    [InlineData(@"~~~~
aaa
~~~
~~~~", @"<document>
    <code_block>aaa
~~~
</code_block>
</document>")]
    [InlineData(@"```", @"<document>
    <code_block></code_block>
</document>")]
    [InlineData(@"`````

```
aaa", @"<document>
    <code_block>
```
aaa
</code_block>
</document>")]
    [InlineData(@"> ```
> aaa

bbb", @"<document>
    <block_quote>
        <code_block>aaa
</code_block>
    </block_quote>
    <paragraph>
        <text>bbb</text>
    </paragraph>
</document>")]
    [InlineData(@"```

  
```", @"<document>
    <code_block>
  
</code_block>
</document>")]
    [InlineData(@"```
```", @"<document>
    <code_block></code_block>
</document>")]
    [InlineData(@" ```
 aaa
aaa
```", @"<document>
    <code_block>aaa
aaa
</code_block>
</document>")]
    [InlineData(@"  ```
aaa
  aaa
aaa
  ```", @"<document>
    <code_block>aaa
aaa
aaa
</code_block>
</document>")]
    [InlineData(@"   ```
   aaa
    aaa
  aaa
   ```", @"<document>
    <code_block>aaa
 aaa
aaa
</code_block>
</document>")]
    [InlineData(@"    ```
    aaa
    ```", @"<document>
    <code_block>```
aaa
```
</code_block>
</document>")]
    [InlineData(@"```
aaa
  ```", @"<document>
    <code_block>aaa
</code_block>
</document>")]
    [InlineData(@"   ```
aaa
  ````", @"<document>
    <code_block>aaa
</code_block>
</document>")]
    [InlineData(@"```
aaa
    ```", @"<document>
    <code_block>aaa
    ```
</code_block>
</document>")]
    [InlineData(@"``` ```
aaa", @"<document>
    <paragraph>
        <code> </code>
        <softbreak />
        <text>aaa</text>
    </paragraph>
</document>")]
    [InlineData(@"~~~~~~
aaa
~~~ ~~", @"<document>
    <code_block>aaa
~~~ ~~
</code_block>
</document>")]
    [InlineData(@"foo
```
bar
```
baz", @"<document>
    <paragraph>
        <text>foo</text>
    </paragraph>
    <code_block>bar
</code_block>
    <paragraph>
        <text>baz</text>
    </paragraph>
</document>")]
    [InlineData(@"foo
---
~~~
bar
~~~
# baz", @"<document>
    <heading level=""2"">
        <text>foo</text>
    </heading>
    <code_block>bar
</code_block>
    <heading level=""1"">
        <text>baz</text>
    </heading>
</document>")]
    [InlineData(@"```ruby
def foo(x)
  return 3
end
```", @"<document>
    <code_block info=""ruby"">def foo(x)
  return 3
end
</code_block>
</document>")]
    [InlineData(@"~~~~    ruby startline=3 $%@#$
def foo(x)
  return 3
end
~~~~~~~", @"<document>
    <code_block info=""ruby startline=3 $%@#$"">def foo(x)
  return 3
end
</code_block>
</document>")]
    [InlineData(@"````;
````", @"<document>
    <code_block info="";""></code_block>
</document>")]
    [InlineData(@"``` aa ```
foo", @"<document>
    <paragraph>
        <code>aa</code>
        <softbreak />
        <text>foo</text>
    </paragraph>
</document>")]
    [InlineData(@"~~~ aa ``` ~~~
foo
~~~", @"<document>
    <code_block info=""aa ``` ~~~"">foo
</code_block>
</document>")]
    [InlineData(@"```
``` aaa
```", @"<document>
    <code_block>``` aaa
</code_block>
</document>")]
    public void Parse_FencedCodeBlock(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}