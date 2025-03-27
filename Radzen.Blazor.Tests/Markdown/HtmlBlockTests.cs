using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class HtmlBlockTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData(@"<table><tr><td>
<pre>
**Hello**,

_world_.
</pre>
</td></tr></table>", @"<document>
    <html_block>&lt;table&gt;&lt;tr&gt;&lt;td&gt;
&lt;pre&gt;
**Hello**,</html_block>
    <paragraph>
        <emph>
            <text>world</text>
        </emph>
        <text>.</text>
        <softbreak />
        <html_inline>&lt;/pre&gt;</html_inline>
    </paragraph>
    <html_block>&lt;/td&gt;&lt;/tr&gt;&lt;/table&gt;</html_block>
</document>")]
    [InlineData(@"<table>
  <tr>
    <td>
           hi
    </td>
  </tr>
</table>

okay.", @"<document>
    <html_block>&lt;table&gt;
  &lt;tr&gt;
    &lt;td&gt;
           hi
    &lt;/td&gt;
  &lt;/tr&gt;
&lt;/table&gt;</html_block>
    <paragraph>
        <text>okay.</text>
    </paragraph>
</document>")]
    [InlineData(@" <div>
  *hello*
         <foo><a>", @"<document>
    <html_block> &lt;div&gt;
  *hello*
         &lt;foo&gt;&lt;a&gt;</html_block>
</document>")]
    [InlineData(@"</div>
*foo*", @"<document>
    <html_block>&lt;/div&gt;
*foo*</html_block>
</document>")]
    [InlineData(@"<DIV CLASS=""foo"">

*Markdown*

</DIV>", @"<document>
    <html_block>&lt;DIV CLASS=""foo""&gt;</html_block>
    <paragraph>
        <emph>
            <text>Markdown</text>
        </emph>
    </paragraph>
    <html_block>&lt;/DIV&gt;</html_block>
</document>")]
    [InlineData(@"<div id=""foo""
  class=""bar"">
</div>", @"<document>
    <html_block>&lt;div id=""foo""
  class=""bar""&gt;
&lt;/div&gt;</html_block>
</document>")]
    [InlineData(@"<div id=""foo"" class=""bar
  baz"">
</div>", @"<document>
    <html_block>&lt;div id=""foo"" class=""bar
  baz""&gt;
&lt;/div&gt;</html_block>
</document>")]
    [InlineData(@"<div>
*foo*

*bar*", @"<document>
    <html_block>&lt;div&gt;
*foo*</html_block>
    <paragraph>
        <emph>
            <text>bar</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData(@"<div id=""foo""
*hi*", @"<document>
    <html_block>&lt;div id=""foo""
*hi*</html_block>
</document>")]
    [InlineData(@"<div class
foo", @"<document>
    <html_block>&lt;div class
foo</html_block>
</document>")]
    [InlineData(@"<div *???-&&&-<---
*foo*", @"<document>
    <html_block>&lt;div *???-&amp;&amp;&amp;-&lt;---
*foo*</html_block>
</document>")]
    [InlineData(@"<div><a href=""bar"">*foo*</a></div>", @"<document>
    <html_block>&lt;div&gt;&lt;a href=""bar""&gt;*foo*&lt;/a&gt;&lt;/div&gt;</html_block>
</document>")]
    [InlineData(@"<table><tr><td>
foo
</td></tr></table>", @"<document>
    <html_block>&lt;table&gt;&lt;tr&gt;&lt;td&gt;
foo
&lt;/td&gt;&lt;/tr&gt;&lt;/table&gt;</html_block>
</document>")]
    [InlineData(@"<div></div>
``` c
int x = 33;
```", @"<document>
    <html_block>&lt;div&gt;&lt;/div&gt;
``` c
int x = 33;
```</html_block>
</document>")]
    [InlineData(@"<a href=""foo"">
*bar*
</a>", @"<document>
    <html_block>&lt;a href=""foo""&gt;
*bar*
&lt;/a&gt;</html_block>
</document>")]
    [InlineData(@"<Warning>
*bar*
</Warning>", @"<document>
    <html_block>&lt;Warning&gt;
*bar*
&lt;/Warning&gt;</html_block>
</document>")]
    [InlineData(@"<i class=""foo"">
*bar*
</i>", @"<document>
    <html_block>&lt;i class=""foo""&gt;
*bar*
&lt;/i&gt;</html_block>
</document>")]
    [InlineData(@"</ins>
*bar*", @"<document>
    <html_block>&lt;/ins&gt;
*bar*</html_block>
</document>")]
    [InlineData(@"<del>
*foo*
</del>", @"<document>
    <html_block>&lt;del&gt;
*foo*
&lt;/del&gt;</html_block>
</document>")]
    [InlineData(@"<del>

*foo*

</del>", @"<document>
    <html_block>&lt;del&gt;</html_block>
    <paragraph>
        <emph>
            <text>foo</text>
        </emph>
    </paragraph>
    <html_block>&lt;/del&gt;</html_block>
</document>")]
    [InlineData(@"<del>*foo*</del>", @"<document>
    <paragraph>
        <html_inline>&lt;del&gt;</html_inline>
        <emph>
            <text>foo</text>
        </emph>
        <html_inline>&lt;/del&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"<pre language=""haskell""><code>
import Text.HTML.TagSoup

main :: IO ()
main = print $ parseTags tags
</code></pre>
okay", @"<document>
    <html_block>&lt;pre language=""haskell""&gt;&lt;code&gt;
import Text.HTML.TagSoup

main :: IO ()
main = print $ parseTags tags
&lt;/code&gt;&lt;/pre&gt;</html_block>
    <paragraph>
        <text>okay</text>
    </paragraph>
</document>")]
    [InlineData(@"<script type=""text/javascript"">
// JavaScript example

document.getElementById(""demo"").innerHTML = ""Hello JavaScript!"";
</script>
okay", @"<document>
    <html_block>&lt;script type=""text/javascript""&gt;
// JavaScript example

document.getElementById(""demo"").innerHTML = ""Hello JavaScript!"";
&lt;/script&gt;</html_block>
    <paragraph>
        <text>okay</text>
    </paragraph>
</document>")]
    [InlineData(@"<textarea>

*foo*

_bar_

</textarea>", @"<document>
    <html_block>&lt;textarea&gt;

*foo*

_bar_

&lt;/textarea&gt;</html_block>
</document>")]
    [InlineData(@"<style
  type=""text/css"">
h1 {color:red;}

p {color:blue;}
</style>
okay",@"<document>
    <html_block>&lt;style
  type=""text/css""&gt;
h1 {color:red;}

p {color:blue;}
&lt;/style&gt;</html_block>
    <paragraph>
        <text>okay</text>
    </paragraph>
</document>")]
    [InlineData(@"<style
  type=""text/css"">

foo", @"<document>
    <html_block>&lt;style
  type=""text/css""&gt;

foo</html_block>
</document>")]
    [InlineData(@"> <div>
> foo

bar", @"<document>
    <block_quote>
        <html_block>&lt;div&gt;
foo</html_block>
    </block_quote>
    <paragraph>
        <text>bar</text>
    </paragraph>
</document>")]
    [InlineData(@"- <div>
- foo", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <html_block>&lt;div&gt;</html_block>
        </item>
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(@"<style>p{color:red;}</style>
*foo*", @"<document>
    <html_block>&lt;style&gt;p{color:red;}&lt;/style&gt;</html_block>
    <paragraph>
        <emph>
            <text>foo</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData(@"<!-- foo -->*bar*
*baz*", @"<document>
    <html_block>&lt;!-- foo --&gt;*bar*</html_block>
    <paragraph>
        <emph>
            <text>baz</text>
        </emph>
    </paragraph>
</document>")]
    [InlineData(@"<script>
foo
</script>1. *bar*", @"<document>
    <html_block>&lt;script&gt;
foo
&lt;/script&gt;1. *bar*</html_block>
</document>")]
    [InlineData(@"<!-- Foo

bar
   baz -->
okay", @"<document>
    <html_block>&lt;!-- Foo

bar
   baz --&gt;</html_block>
    <paragraph>
        <text>okay</text>
    </paragraph>
</document>")]
    [InlineData(@"<?php

  echo '>';

?>
okay", @"<document>
    <html_block>&lt;?php

  echo '&gt;';

?&gt;</html_block>
    <paragraph>
        <text>okay</text>
    </paragraph>
</document>")]
    [InlineData(@"<!DOCTYPE html>", @"<document>
    <html_block>&lt;!DOCTYPE html&gt;</html_block>
</document>")]
    [InlineData(@"<![CDATA[
function matchwo(a,b)
{
  if (a < b && a < 0) then {
    return 1;

  } else {

    return 0;
  }
}
]]>
okay", @"<document>
    <html_block>&lt;![CDATA[
function matchwo(a,b)
{
  if (a &lt; b &amp;&amp; a &lt; 0) then {
    return 1;

  } else {

    return 0;
  }
}
]]&gt;</html_block>
    <paragraph>
        <text>okay</text>
    </paragraph>
</document>")]
    [InlineData(@"  <!-- foo -->

    <!-- foo -->", @"<document>
    <html_block>  &lt;!-- foo --&gt;</html_block>
    <code_block>&lt;!-- foo --&gt;
</code_block>
</document>")]
    [InlineData(@"  <div>

    <div>", @"<document>
    <html_block>  &lt;div&gt;</html_block>
    <code_block>&lt;div&gt;
</code_block>
</document>")]
    [InlineData(@"Foo
<div>
bar
</div>", @"<document>
    <paragraph>
        <text>Foo</text>
    </paragraph>
    <html_block>&lt;div&gt;
bar
&lt;/div&gt;</html_block>
</document>")]
    [InlineData(@"<div>
bar
</div>
*foo*", @"<document>
    <html_block>&lt;div&gt;
bar
&lt;/div&gt;
*foo*</html_block>
</document>")]
    [InlineData(@"Foo
<a href=""bar"">
baz", @"<document>
    <paragraph>
        <text>Foo</text>
        <softbreak />
        <html_inline>&lt;a href=""bar""&gt;</html_inline>
        <softbreak />
        <text>baz</text>
    </paragraph>
</document>")]
    [InlineData(@"<div>

*Emphasized* text.

</div>", @"<document>
    <html_block>&lt;div&gt;</html_block>
    <paragraph>
        <emph>
            <text>Emphasized</text>
        </emph>
        <text> text.</text>
    </paragraph>
    <html_block>&lt;/div&gt;</html_block>
</document>")]
    [InlineData(@"<div>
*Emphasized* text.
</div>", @"<document>
    <html_block>&lt;div&gt;
*Emphasized* text.
&lt;/div&gt;</html_block>
</document>")]
    [InlineData(@"<table>

<tr>

<td>
Hi
</td>

</tr>

</table>", @"<document>
    <html_block>&lt;table&gt;</html_block>
    <html_block>&lt;tr&gt;</html_block>
    <html_block>&lt;td&gt;
Hi
&lt;/td&gt;</html_block>
    <html_block>&lt;/tr&gt;</html_block>
    <html_block>&lt;/table&gt;</html_block>
</document>")]
    [InlineData(@"<table>

  <tr>

    <td>
      Hi
    </td>

  </tr>

</table>", @"<document>
    <html_block>&lt;table&gt;</html_block>
    <html_block>  &lt;tr&gt;</html_block>
    <code_block>&lt;td&gt;
  Hi
&lt;/td&gt;
</code_block>
    <html_block>  &lt;/tr&gt;</html_block>
    <html_block>&lt;/table&gt;</html_block>
</document>")]
    public void Parse_HtmlBlock(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}