using System.Runtime.InteropServices;
using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class LinkTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData("[link](/uri \"title\")",
@"<document>
    <paragraph>
        <link destination=""/uri"" title=""title"">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    [InlineData("[link](/uri)", @"<document>
    <paragraph>
        <link destination=""/uri"" title="""">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    [InlineData("[](./target.md)", @"<document>
    <paragraph>
        <link destination=""./target.md"" title="""" />
    </paragraph>
</document>")]
    [InlineData("[link]()", @"<document>
    <paragraph>
        <link destination="""" title="""">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    [InlineData("[link](<>)", @"<document>
    <paragraph>
        <link destination="""" title="""">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    [InlineData("[]()", @"<document>
    <paragraph>
        <link destination="""" title="""" />
    </paragraph>
</document>")]
    public void Parse_BasicLinks_ReturnsLinkNode(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData("[link](/my uri)", @"<document>
    <paragraph>
        <text>[</text>
        <text>link</text>
        <text>](/my uri)</text>
    </paragraph>
</document>")]
    [InlineData("[link](</my uri>)", @"<document>
    <paragraph>
        <link destination=""/my uri"" title="""">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    public void Parse_LinkDestinationWithSpaces(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[link](foo
bar)", @"<document>
    <paragraph>
        <text>[</text>
        <text>link</text>
        <text>](foo</text>
        <softbreak />
        <text>bar)</text>
    </paragraph>
</document>")]

    public void Parse_LinkDestinationWithNewLines(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[a](<b)c>)", @"<document>
    <paragraph>
        <link destination=""b)c"" title="""">
            <text>a</text>
        </link>
    </paragraph>
</document>")]
    public void Parse_LinkDestinationWithCloseParenthesis(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[link](<foo\>)", @"<document>
    <paragraph>
        <text>[</text>
        <text>link</text>
        <text>](&lt;foo&gt;)</text>
    </paragraph>
</document>")]
    [InlineData(@"[a](<b)c
[a](<b)c>
[a](<b>c)", @"<document>
    <paragraph>
        <text>[</text>
        <text>a</text>
        <text>](&lt;b)c</text>
        <softbreak />
        <text>[</text>
        <text>a</text>
        <text>](&lt;b)c&gt;</text>
        <softbreak />
        <text>[</text>
        <text>a</text>
        <text>](</text>
        <html_inline>&lt;b&gt;</html_inline>
        <text>c)</text>
    </paragraph>
</document>")]
    public void Parse_LinkDestinationUnclosedPointyBracket(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[link](\(foo\))", @"<document>
    <paragraph>
        <link destination=""(foo)"" title="""">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[link](foo\)\:)", @"<document>
    <paragraph>
        <link destination=""foo):"" title="""">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[link](foo\bar)", @"<document>
    <paragraph>
        <link destination=""foo\bar"" title="""">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    public void Parse_Escapes(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[link](foo(and(bar)))", @"<document>
    <paragraph>
        <link destination=""foo(and(bar))"" title="""">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[link](foo(and(bar))", @"<document>
    <paragraph>
        <text>[</text>
        <text>link</text>
        <text>](foo(and(bar))</text>
    </paragraph>
</document>")]
    [InlineData(@"[link](foo\(and\(bar\))", @"<document>
    <paragraph>
        <link destination=""foo(and(bar)"" title="""">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[link](<foo(and(bar)>)", @"<document>
    <paragraph>
        <link destination=""foo(and(bar)"" title="""">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    public void Parse_BallancedParenthesisInLinkDestination(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[link](#fragment)
[link](https://example.com#fragment)
[link](https://example.com?foo=3#frag)", @"<document>
    <paragraph>
        <link destination=""#fragment"" title="""">
            <text>link</text>
        </link>
        <softbreak />
        <link destination=""https://example.com#fragment"" title="""">
            <text>link</text>
        </link>
        <softbreak />
        <link destination=""https://example.com?foo=3#frag"" title="""">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    public void Parse_FragmentAndQueryStringInLinkDestination(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[link](""title"")", @"<document>
    <paragraph>
        <link destination=""&quot;title&quot;"" title="""">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    public void Parse_QuotesInDestination(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[link](/url ""title"")
[link](/url 'title')
[link](/url (title))", @"<document>
    <paragraph>
        <link destination=""/url"" title=""title"">
            <text>link</text>
        </link>
        <softbreak />
        <link destination=""/url"" title=""title"">
            <text>link</text>
        </link>
        <softbreak />
        <link destination=""/url"" title=""title"">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[link](/url ""title ""and"" title"")", @"<document>
    <paragraph>
        <text>[</text>
        <text>link</text>
        <text>](/url ""title ""and"" title"")</text>
    </paragraph>
</document>")]
    [InlineData(@"[link](/url 'title ""and"" title')", @"<document>
    <paragraph>
        <link destination=""/url"" title=""title &quot;and&quot; title"">
            <text>link</text>
        </link>
    </paragraph>
</document>")] 
    public void Parse_Title(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[link](   /url
  ""title""  )", @"<document>
    <paragraph>
        <link destination=""/url"" title=""title"">
            <text>link</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[link] (/uri)", @"<document>
    <paragraph>
        <text>[</text>
        <text>link</text>
        <text>] (/uri)</text>
    </paragraph>
</document>")]
    public void Parse_SpacesInLink(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[link [foo [bar]]](/uri)", @"<document>
    <paragraph>
        <link destination=""/uri"" title="""">
            <text>link </text>
            <text>[</text>
            <text>foo </text>
            <text>[</text>
            <text>bar</text>
            <text>]</text>
            <text>]</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[link] bar](/uri)", @"<document>
    <paragraph>
        <text>[</text>
        <text>link</text>
        <text>] bar](/uri)</text>
    </paragraph>
</document>")]
    [InlineData(@"[link [bar](/uri)", @"<document>
    <paragraph>
        <text>[</text>
        <text>link </text>
        <link destination=""/uri"" title="""">
            <text>bar</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[link \[bar](/uri)", @"<document>
    <paragraph>
        <link destination=""/uri"" title="""">
            <text>link </text>
            <text>[</text>
            <text>bar</text>
        </link>
    </paragraph>
</document>")]
    public void Parse_BracketsInText(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[link *foo **bar** `#`*](/uri)", @"<document>
    <paragraph>
        <link destination=""/uri"" title="""">
            <text>link </text>
            <emph>
                <text>foo </text>
                <strong>
                    <text>bar</text>
                </strong>
                <text> </text>
                <code>#</code>
            </emph>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[![alt](img)](url)", @"<document>
    <paragraph>
        <link destination=""url"" title="""">
            <image destination=""img"" title="""">
                <text>alt</text>
            </image>
        </link>
    </paragraph>
</document>")]

    public void Parse_LinkTextIsInlineContent(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[foo [bar](/uri)](/uri)", @"<document>
    <paragraph>
        <text>[</text>
        <text>foo </text>
        <link destination=""/uri"" title="""">
            <text>bar</text>
        </link>
        <text>](/uri)</text>
    </paragraph>
</document>")]
    [InlineData(@"[foo *[bar [baz](/uri)](/uri)*](/uri)", @"<document>
    <paragraph>
        <text>[</text>
        <text>foo </text>
        <emph>
            <text>[</text>
            <text>bar </text>
            <link destination=""/uri"" title="""">
                <text>baz</text>
            </link>
            <text>](/uri)</text>
        </emph>
        <text>](/uri)</text>
    </paragraph>
</document>")]
    public void Parse_NestedLinks(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"*[foo*](/uri)", @"<document>
    <paragraph>
        <text>*</text>
        <link destination=""/uri"" title="""">
            <text>foo</text>
            <text>*</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[foo *bar](baz*)", @"<document>
    <paragraph>
        <link destination=""baz*"" title="""">
            <text>foo </text>
            <text>*</text>
            <text>bar</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"*foo [bar* baz]", @"<document>
    <paragraph>
        <emph>
            <text>foo </text>
            <text>[</text>
            <text>bar</text>
        </emph>
        <text> baz</text>
        <text>]</text>
    </paragraph>
</document>")]
    [InlineData(@"[foo`](/uri)`", @"<document>
    <paragraph>
        <text>[</text>
        <text>foo</text>
        <code>](/uri)</code>
    </paragraph>
</document>")]
    public void Parse_Precedence(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"<http://foo.bar.baz>", @"<document>
    <paragraph>
        <link destination=""http://foo.bar.baz"" title="""">
            <text>http://foo.bar.baz</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"<https://foo.bar.baz/test?q=hello&id=22&boolean>", @"<document>
    <paragraph>
        <link destination=""https://foo.bar.baz/test?q=hello&amp;id=22&amp;boolean"" title="""">
            <text>https://foo.bar.baz/test?q=hello&amp;id=22&amp;boolean</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"<irc://foo.bar:2233/baz>", @"<document>
    <paragraph>
        <link destination=""irc://foo.bar:2233/baz"" title="""">
            <text>irc://foo.bar:2233/baz</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"<MAILTO:FOO@BAR.BAZ>", @"<document>
    <paragraph>
        <link destination=""MAILTO:FOO@BAR.BAZ"" title="""">
            <text>MAILTO:FOO@BAR.BAZ</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"<https://foo.bar/baz bim>", @"<document>
    <paragraph>
        <text>&lt;https://foo.bar/baz bim&gt;</text>
    </paragraph>
</document>")]
    [InlineData(@"<https://example.com/\[\>", @"<document>
    <paragraph>
        <link destination=""https://example.com/\[\"" title="""">
            <text>https://example.com/\[\</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"<foo@bar.example.com>", @"<document>
    <paragraph>
        <link destination=""mailto:foo@bar.example.com"" title="""">
            <text>foo@bar.example.com</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"<foo+special@Bar.baz-bar0.com>", @"<document>
    <paragraph>
        <link destination=""mailto:foo+special@Bar.baz-bar0.com"" title="""">
            <text>foo+special@Bar.baz-bar0.com</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"<foo\+@bar.example.com>", @"<document>
    <paragraph>
        <text>&lt;foo+@bar.example.com&gt;</text>
    </paragraph>
</document>")]
    [InlineData(@"<>", @"<document>
    <paragraph>
        <text>&lt;&gt;</text>
    </paragraph>
</document>")]
    [InlineData(@"< https://foo.bar >", @"<document>
    <paragraph>
        <text>&lt; https://foo.bar &gt;</text>
    </paragraph>
</document>")]
    [InlineData(@"<m:abc>", @"<document>
    <paragraph>
        <text>&lt;m:abc&gt;</text>
    </paragraph>
</document>")]
    [InlineData(@"<foo.bar.baz>", @"<document>
    <paragraph>
        <text>&lt;foo.bar.baz&gt;</text>
    </paragraph>
</document>")]
    [InlineData(@"https://example.com", @"<document>
    <paragraph>
        <text>https://example.com</text>
    </paragraph>
</document>")]
    [InlineData(@"foo@bar.example.com", @"<document>
    <paragraph>
        <text>foo@bar.example.com</text>
    </paragraph>
</document>")]
    public void Parse_AutoLink(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }

    [Theory]
    [InlineData(@"[foo]: /url ""title""

[foo]", @"<document>
    <paragraph>
        <link destination=""/url"" title=""title"">
            <text>foo</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"   [foo]: 
      /url  
           'the title'  

[foo]", @"<document>
    <paragraph>
        <link destination=""/url"" title=""the title"">
            <text>foo</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[Foo*bar\]]:my_(url) 'title (with parens)'

[Foo*bar\]]", @"<document>
    <paragraph>
        <link destination=""my_(url)"" title=""title (with parens)"">
            <text>Foo</text>
            <text>*</text>
            <text>bar</text>
            <text>]</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[Foo bar]:
<my url>
'title'

[Foo bar]", @"<document>
    <paragraph>
        <link destination=""my url"" title=""title"">
            <text>Foo bar</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[foo]: /url '
title
line1
line2
'

[foo]", @"<document>
    <paragraph>
        <link destination=""/url"" title=""&#xA;title&#xA;line1&#xA;line2&#xA;"">
            <text>foo</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[foo]: /url 'title

with blank line'

[foo]", @"<document>
    <paragraph>
        <text>[</text>
        <text>foo</text>
        <text>]: /url 'title</text>
    </paragraph>
    <paragraph>
        <text>with blank line'</text>
    </paragraph>
    <paragraph>
        <text>[</text>
        <text>foo</text>
        <text>]</text>
    </paragraph>
</document>")]
    [InlineData(@"[foo]:
/url

[foo]", @"<document>
    <paragraph>
        <link destination=""/url"" title="""">
            <text>foo</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[foo]:

[foo]", @"<document>
    <paragraph>
        <text>[</text>
        <text>foo</text>
        <text>]:</text>
    </paragraph>
    <paragraph>
        <text>[</text>
        <text>foo</text>
        <text>]</text>
    </paragraph>
</document>")]
    [InlineData(@"[foo]: <>

[foo]", @"<document>
    <paragraph>
        <link destination="""" title="""">
            <text>foo</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[foo]: <bar>(baz)

[foo]", @"<document>
    <paragraph>
        <text>[</text>
        <text>foo</text>
        <text>]: </text>
        <html_inline>&lt;bar&gt;</html_inline>
        <text>(baz)</text>
    </paragraph>
    <paragraph>
        <text>[</text>
        <text>foo</text>
        <text>]</text>
    </paragraph>
</document>")]
    [InlineData(@"[foo]: /url\bar\*baz ""foo\""bar\baz""

[foo]
", @"<document>
    <paragraph>
        <link destination=""/url\bar*baz"" title=""foo\&quot;bar\baz"">
            <text>foo</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[foo]

[foo]: url", @"<document>
    <paragraph>
        <link destination=""url"" title="""">
            <text>foo</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[foo]

[foo]: first
[foo]: second", @"<document>
    <paragraph>
        <link destination=""first"" title="""">
            <text>foo</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[FOO]: /url

[Foo]", @"<document>
    <paragraph>
        <link destination=""/url"" title="""">
            <text>Foo</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[ΑΓΩ]: /φου

[αγω]", @"<document>
    <paragraph>
        <link destination=""/φου"" title="""">
            <text>αγω</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[foo]: /url", @"<document />")]
    [InlineData(@"[
foo
]: /url
bar", @"<document>
    <paragraph>
        <text>bar</text>
    </paragraph>
</document>")]
    [InlineData(@"[foo]: /url ""title"" ok", @"<document>
    <paragraph>
        <text>[</text>
        <text>foo</text>
        <text>]: /url ""title"" ok</text>
    </paragraph>
</document>")]
    [InlineData(@"[foo]: /url
""title"" ok", @"<document>
    <paragraph>
        <text>""title"" ok</text>
    </paragraph>
</document>")]
    [InlineData(@"    [foo]: /url ""title""

[foo]", @"<document>
    <code_block>[foo]: /url ""title""
</code_block>
    <paragraph>
        <text>[</text>
        <text>foo</text>
        <text>]</text>
    </paragraph>
</document>")]
    [InlineData(@"```
[foo]: /url
```

[foo]", @"<document>
    <code_block>[foo]: /url
</code_block>
    <paragraph>
        <text>[</text>
        <text>foo</text>
        <text>]</text>
    </paragraph>
</document>")]
    [InlineData(@"Foo
[bar]: /baz

[bar]", @"<document>
    <paragraph>
        <text>Foo</text>
        <softbreak />
        <text>[</text>
        <text>bar</text>
        <text>]: /baz</text>
    </paragraph>
    <paragraph>
        <text>[</text>
        <text>bar</text>
        <text>]</text>
    </paragraph>
</document>")]
    [InlineData(@"# [Foo]
[foo]: /url
> bar", @"<document>
    <heading level=""1"">
        <link destination=""/url"" title="""">
            <text>Foo</text>
        </link>
    </heading>
    <block_quote>
        <paragraph>
            <text>bar</text>
        </paragraph>
    </block_quote>
</document>")]
    [InlineData(@"[foo]: /url
bar
===
[foo]", @"<document>
    <heading level=""1"">
        <text>bar</text>
    </heading>
    <paragraph>
        <link destination=""/url"" title="""">
            <text>foo</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[foo]: /url
===
[foo]", @"<document>
    <paragraph>
        <text>===</text>
        <softbreak />
        <link destination=""/url"" title="""">
            <text>foo</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[foo]: /foo-url ""foo""
[bar]: /bar-url
  ""bar""
[baz]: /baz-url

[foo],
[bar],
[baz]", @"<document>
    <paragraph>
        <link destination=""/foo-url"" title=""foo"">
            <text>foo</text>
        </link>
        <text>,</text>
        <softbreak />
        <link destination=""/bar-url"" title=""bar"">
            <text>bar</text>
        </link>
        <text>,</text>
        <softbreak />
        <link destination=""/baz-url"" title="""">
            <text>baz</text>
        </link>
    </paragraph>
</document>")]
    [InlineData(@"[foo]

> [foo]: /url
", @"<document>
    <paragraph>
        <link destination=""/url"" title="""">
            <text>foo</text>
        </link>
    </paragraph>
    <block_quote />
</document>")]
    public void Parse_LinkReference(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}