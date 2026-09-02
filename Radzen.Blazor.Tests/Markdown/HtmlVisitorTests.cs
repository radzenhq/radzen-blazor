using Radzen.Blazor.Documents.Markdown;
using Xunit;

namespace Radzen.Documents.Markdown.Tests;

public class HtmlVisitorTests
{
    [Theory]
    [InlineData("# Hi", "<h1>Hi</h1>")]
    [InlineData("plain *em* **strong** ~~del~~ `code`",
        "<p>plain <em>em</em> <strong>strong</strong> <del>del</del> <code>code</code></p>")]
    [InlineData("[link](https://a.b \"t\")", "<p><a href=\"https://a.b\">link</a></p>")]
    [InlineData("![alt](img.png)", "<p><img src=\"img.png\" alt=\"alt\"></p>")]
    [InlineData("> quote", "<blockquote><p>quote</p></blockquote>")]
    [InlineData("- a\n- b", "<ul><li>a</li><li>b</li></ul>")]
    [InlineData("3. a", "<ol start=\"3\"><li>a</li></ol>")]
    [InlineData("- [x] done", "<ul><li><input type=\"checkbox\" checked> done</li></ul>")]
    [InlineData("```csharp\nvar x = 1;\n```", "<pre><code class=\"language-csharp\">var x = 1;\n</code></pre>")]
    [InlineData("---", "<hr>")]
    [InlineData("a  \nb", "<p>a<br>b</p>")]
    [InlineData("a\nb", "<p>a b</p>")]
    [InlineData("<script>x</script>", "<p>&lt;script&gt;x&lt;/script&gt;</p>")]
    [InlineData("a < b & c", "<p>a &lt; b &amp; c</p>")]
    [InlineData("| a | b |\n| --- | ---: |\n| 1 | 2 |",
        "<table><thead><tr><th>a</th><th style=\"text-align:right\">b</th></tr></thead><tbody><tr><td>1</td><td style=\"text-align:right\">2</td></tr></tbody></table>")]
    public void ToHtml_renders(string markdown, string expected)
    {
        Assert.Equal(expected, HtmlVisitor.ToHtml(markdown));
    }

    [Fact]
    public void ToHtml_separates_blocks_without_text_between_them()
    {
        Assert.Equal("<h1>A</h1><p>b</p>", HtmlVisitor.ToHtml("# A\n\nb"));
    }

    [Fact]
    public void ToHtml_wraps_loose_list_items_in_paragraphs()
    {
        Assert.Equal(
            "<ul><li><p>a</p></li><li><p>b</p></li></ul>",
            HtmlVisitor.ToHtml("- a\n\n- b"));
    }

    [Theory]
    [InlineData("[x](javascript:alert(1))", "<p><a href=\"\">x</a></p>")]
    [InlineData("[x](JaVaScRiPt:alert(1))", "<p><a href=\"\">x</a></p>")]
    [InlineData("![a](javascript:alert(1))", "<p><img src=\"\" alt=\"a\"></p>")]
    public void ToHtml_blanks_dangerous_urls(string markdown, string expected)
    {
        Assert.Equal(expected, HtmlVisitor.ToHtml(markdown));
    }
}
