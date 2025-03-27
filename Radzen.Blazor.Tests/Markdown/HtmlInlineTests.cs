
using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class HtmlInlineTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData(@"<a><bab><c2c>", @"<document>
    <paragraph>
        <html_inline>&lt;a&gt;</html_inline>
        <html_inline>&lt;bab&gt;</html_inline>
        <html_inline>&lt;c2c&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"<a/><b2/>", @"<document>
    <paragraph>
        <html_inline>&lt;a/&gt;</html_inline>
        <html_inline>&lt;b2/&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"<a  /><b2
data=""foo"" >", @"<document>
    <paragraph>
        <html_inline>&lt;a  /&gt;</html_inline>
        <html_inline>&lt;b2
data=""foo"" &gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"<a foo=""bar"" bam = 'baz <em>""</em>'
_boolean zoop:33=zoop:33 />", @"<document>
    <paragraph>
        <html_inline>&lt;a foo=""bar"" bam = 'baz &lt;em&gt;""&lt;/em&gt;'
_boolean zoop:33=zoop:33 /&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"Foo <responsive-image src=""foo.jpg"" />", @"<document>
    <paragraph>
        <text>Foo </text>
        <html_inline>&lt;responsive-image src=""foo.jpg"" /&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData("<33> <__>", @"<document>
    <paragraph>
        <text>&lt;33&gt; &lt;</text>
        <text>__</text>
        <text>&gt;</text>
    </paragraph>
</document>")]
    [InlineData(@"<a h*#ref=""hi"">", @"<document>
    <paragraph>
        <text>&lt;a h</text>
        <text>*</text>
        <text>#ref=""hi""&gt;</text>
    </paragraph>
</document>")]
    [InlineData(@"<a href='bar'title=title>", @"<document>
    <paragraph>
        <text>&lt;a href='bar'title=title&gt;</text>
    </paragraph>
</document>")]
    [InlineData(@"</a></foo >", @"<document>
    <paragraph>
        <html_inline>&lt;/a&gt;</html_inline>
        <html_inline>&lt;/foo &gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"</a href=""foo"">", @"<document>
    <paragraph>
        <text>&lt;/a href=""foo""&gt;</text>
    </paragraph>
</document>")]
    [InlineData(@"foo <!-- this is a --
comment - with hyphens -->", @"<document>
    <paragraph>
        <text>foo </text>
        <html_inline>&lt;!-- this is a --
comment - with hyphens --&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"foo <!--> foo -->

foo <!---> foo -->
", @"<document>
    <paragraph>
        <text>foo </text>
        <html_inline>&lt;!--&gt;</html_inline>
        <text> foo --&gt;</text>
    </paragraph>
    <paragraph>
        <text>foo </text>
        <html_inline>&lt;!---&gt;</html_inline>
        <text> foo --&gt;</text>
    </paragraph>
</document>")]
    [InlineData(@"foo <?php echo $a; ?>", @"<document>
    <paragraph>
        <text>foo </text>
        <html_inline>&lt;?php echo $a; ?&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"foo <!ELEMENT br EMPTY>", @"<document>
    <paragraph>
        <text>foo </text>
        <html_inline>&lt;!ELEMENT br EMPTY&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"foo <![CDATA[>&<]]>", @"<document>
    <paragraph>
        <text>foo </text>
        <html_inline>&lt;![CDATA[&gt;&amp;&lt;]]&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"foo <a href=""&ouml;"">", @"<document>
    <paragraph>
        <text>foo </text>
        <html_inline>&lt;a href=""&amp;ouml;""&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"foo <a href=""\*"">", @"<document>
    <paragraph>
        <text>foo </text>
        <html_inline>&lt;a href=""\*""&gt;</html_inline>
    </paragraph>
</document>")]
    [InlineData(@"foo <a href=""\"""">", @"<document>
    <paragraph>
        <text>foo &lt;a href=""</text>
        <text>""</text>
        <text>""&gt;</text>
    </paragraph>
</document>")]
    public void Parse_Html(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}
