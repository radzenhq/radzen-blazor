using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Radzen.Documents.Markdown.Tests;

public class HtmlSegmentTests
{
    public static IEnumerable<object[]> SpecExamples() => SourcePositionTests.SpecExamples();

    private static string TextOf(string html) => Regex.Replace(html, "<[^>]*>", string.Empty)
        .Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&amp;", "&");

    [Theory]
    [MemberData(nameof(SpecExamples))]
    public void SegmentsCoverTheRenderedTextAndLinearOnesMatchTheSource(int example, string markdown)
    {
        var rendered = HtmlVisitor.Render(markdown);
        var text = TextOf(rendered.Html);
        var offset = 0;

        foreach (var segment in rendered.Segments)
        {
            Assert.True(0 <= segment.SourceStart && segment.SourceStart <= segment.SourceEnd && segment.SourceEnd <= markdown.Length && segment.Length > 0,
                $"example {example}: segment [{segment.SourceStart},{segment.SourceEnd}) length {segment.Length} in {Show(markdown)}");

            if (segment.SourceEnd - segment.SourceStart == segment.Length)
            {
                var expected = markdown[segment.SourceStart..segment.SourceEnd];
                var actual = text.Substring(offset, segment.Length);
                Assert.True(expected == actual || expected is "\n" or "\r" && actual is " " or "\n",
                    $"example {example}: segment [{segment.SourceStart},{segment.SourceEnd}) source {Show(expected)} renders {Show(actual)} in {Show(markdown)}");
            }

            offset += segment.Length;
        }

        Assert.True(offset == text.Length, $"example {example}: segments cover {offset} of {text.Length} characters of {Show(text)} in {Show(markdown)}");
    }

    private static string Show(string text) => "\"" + text.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";

    private static string Dump(string markdown) => string.Join(" ", HtmlVisitor.Render(markdown).Segments.Select(s => $"{s.SourceStart}-{s.SourceEnd}:{s.Length}"));

    [Theory]
    [InlineData("**foo** bar", "2-5:3 7-11:4")]
    [InlineData("a\nb", "0-1:1 1-2:1 2-3:1")]
    [InlineData("`x`", "1-2:1")]
    [InlineData("`` a`b ``", "3-6:3")]
    [InlineData("- [x] done", "6-6:1 6-10:4")]
    [InlineData("```\nab\n```", "0-0:1 4-6:2 10-10:1")]
    [InlineData("a\\*b", "0-1:1 1-3:1 3-4:1")]
    [InlineData("> q\n> r", "2-3:1 3-4:1 6-7:1")]
    [InlineData("a :smile: b", "0-2:2 2-9:2 9-11:2")]
    [InlineData("`a\nb`", "0-5:3")]
    [InlineData("x  \ny", "0-1:1 4-5:1")]
    public void SegmentsPointAtTheirSource(string markdown, string expected)
    {
        Assert.Equal(expected, Dump(markdown));
    }
}
