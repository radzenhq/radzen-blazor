using Xunit;

namespace Radzen.Documents.Markdown.Tests;

public class HtmlSanitizerTests
{
    private static string Sanitize(string html, string[] tags = null, string[] attributes = null)
    {
        return new HtmlSanitizer(tags, attributes).Sanitize(html);
    }

    [Theory]
    [InlineData("<div class=\"a\">b</div>", "<div class=\"a\">b</div>")]
    [InlineData("<DIV CLASS=a>b</DIV>", "<div class=\"a\">b</div>")]
    [InlineData("<p>a<br>b<br/>c</p>", "<p>a<br>b<br>c</p>")]
    [InlineData("<span/>x", "<span></span>x")]
    [InlineData("<div title='\"><img src=x onerror=alert(1)>'>a</div>", "<div title=\"&quot;&gt;&lt;img src=x onerror=alert(1)&gt;\">a</div>")]
    [InlineData("<a href='x\" onclick=\"alert(1)'>y</a>", "<a href=\"x&quot; onclick=&quot;alert(1)\">y</a>")]
    [InlineData("<div class=\"a\" class=\"b\">x</div>", "<div class=\"a\">x</div>")]
    [InlineData("<details open>x</details>", "<details open=\"\">x</details>")]
    public void Serializes_Allowed_Tags_With_Encoded_Attributes(string html, string expected)
    {
        Assert.Equal(expected, Sanitize(html));
    }

    [Theory]
    [InlineData("<div>a &amp; b</div>", "<div>a &amp; b</div>")]
    [InlineData("<div>&lt;script&gt;alert(1)&lt;/script&gt;</div>", "<div>&lt;script&gt;alert(1)&lt;/script&gt;</div>")]
    [InlineData("<div>1 < 2 > 0</div>", "<div>1 &lt; 2 &gt; 0</div>")]
    [InlineData("<div>&quot;x&#39;</div>", "<div>&quot;x&#39;</div>")]
    [InlineData("<div><!-- <b>y</b></div>", "<div>&lt;!-- <b>y</b></div>")]
    [InlineData("<div><? <b>y</b></div>", "<div>&lt;? <b>y</b></div>")]
    [InlineData("<div><![CDATA[ <b>y</b></div>", "<div>&lt;![CDATA[ <b>y</b></div>")]
    public void Encodes_Text(string html, string expected)
    {
        Assert.Equal(expected, Sanitize(html));
    }

    [Theory]
    [InlineData("<div><script>alert(1)</script>x</div>", "<div>x</div>")]
    [InlineData("<div><style>a{}</style>x</div>", "<div>x</div>")]
    [InlineData("<div><SCRIPT>alert(1)</SCRIPT >x</div>", "<div>x</div>")]
    [InlineData("<div><script>alert(1)", "<div></div>")]
    [InlineData("<div><iframe src=\"https://example.com\"></iframe>x</div>", "<div>x</div>")]
    [InlineData("<div><svg onload=alert(1)></svg>x</div>", "<div>x</div>")]
    [InlineData("<div><!-- c --><![CDATA[d]]><?p?><!DOCTYPE html>x</div>", "<div>x</div>")]
    [InlineData("<div onclick=\"alert(1)\" onmouseover=alert(1)>x</div>", "<div>x</div>")]
    public void Drops_Disallowed_Markup(string html, string expected)
    {
        Assert.Equal(expected, Sanitize(html));
    }

    [Theory]
    [InlineData("<div><b>x</div>", "<div><b>x</b></div>")]
    [InlineData("<div>x", "<div>x</div>")]
    [InlineData("<a href=\"https://example.com\">x", "<a href=\"https://example.com\">x</a>")]
    [InlineData("</div></div>x", "x")]
    [InlineData("<div>x</span></div>", "<div>x</div>")]
    [InlineData("<div><span>x</div></span>", "<div><span>x</span></div>")]
    public void Balances_Elements_Within_The_Block(string html, string expected)
    {
        Assert.Equal(expected, Sanitize(html));
    }

    [Theory]
    [InlineData("<a href=\"&#106;avascript:alert(1)\">x</a>", "<a>x</a>")]
    [InlineData("<a href=\"data:image/png;base64,AA\">x</a>", "<a>x</a>")]
    [InlineData("<img src=\"data:image/png;base64,AA\">", "<img src=\"data:image/png;base64,AA\">")]
    [InlineData("<a href=\"/a?b=1&amp;c=2\">x</a>", "<a href=\"/a?b=1&amp;c=2\">x</a>")]
    public void Filters_Url_Attributes(string html, string expected)
    {
        Assert.Equal(expected, Sanitize(html));
    }

    [Fact]
    public void Allowed_Lists_Replace_The_Defaults()
    {
        Assert.Equal(
            "<form action=\"https://example.com\"><input name=\"q\" style=\"color:red\"></form>",
            Sanitize("<form action=\"https://example.com\"><input name=\"q\" style=\"color:red\" onfocus=\"x\"></form>", ["form", "input"], ["action", "name", "style"]));
    }

    [Fact]
    public void Allowed_Raw_Text_Element_Content_Cannot_Close_It()
    {
        Assert.Equal(
            "<noscript><p title=\"&lt;/noscript&gt;&lt;img src=x onerror=alert(1)&gt;\"></p></noscript>",
            Sanitize("<noscript><p title=\"</noscript><img src=x onerror=alert(1)>\"></p></noscript>", ["noscript", "p"], ["title"]));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("page", true)]
    [InlineData("/a/b:c", true)]
    [InlineData("?a:b", true)]
    [InlineData("#a:b", true)]
    [InlineData("//example.com", true)]
    [InlineData("HTTPS://example.com", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData(" \u0001javascript:alert(1)", false)]
    [InlineData("java\tscript:alert(1)", false)]
    [InlineData("java\nscript:alert(1)", false)]
    [InlineData("vbscript:x", false)]
    [InlineData("tel:123", false)]
    [InlineData("data:image/png;base64,AA", false)]
    [InlineData("mai\tlto:a@example.com", true)]
    [InlineData("abcdefghijk:x", false)]
    [InlineData("abcdefghijk/x:y", true)]
    [InlineData("\u00A0javascript:alert(1)", false)]
    public void IsSafeUrl_Allows_Http_Https_Mailto_And_Relative(string url, bool expected)
    {
        Assert.Equal(expected, HtmlSanitizer.IsSafeUrl(url, false));
    }

    [Theory]
    [InlineData("data:image/png;base64,AA", true)]
    [InlineData(" DATA:IMAGE/gif;base64,AA", true)]
    [InlineData("data:text/html,x", false)]
    [InlineData("data:,x", false)]
    [InlineData("data:\timage/png;base64,AA", true)]
    [InlineData("data:ima\nge/png;base64,AA", true)]
    [InlineData("data:imag", false)]
    public void IsSafeUrl_Allows_Data_Image_Only_For_Images(string url, bool expected)
    {
        Assert.Equal(expected, HtmlSanitizer.IsSafeUrl(url, true));
    }
}
