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
    public void SegmentsCoverTheRenderedTextInDocumentOrder(int example, string markdown)
    {
        var rendered = HtmlVisitor.Render(markdown);
        var text = TextOf(rendered.Html);
        var offset = 0;
        var position = 0;

        foreach (var segment in rendered.Segments)
        {
            var linear = segment.End - segment.Start == segment.Length;
            Assert.True(segment.Start >= position && segment.Length > 0 && (linear || segment.Start == segment.End || segment.End == segment.Start + 1),
                $"example {example}: segment [{segment.Start},{segment.End}) length {segment.Length} after position {position} in {Show(markdown)}");
            position = segment.End;
            offset += segment.Length;
        }

        Assert.True(offset == text.Length, $"example {example}: segments cover {offset} of {text.Length} characters of {Show(text)} in {Show(markdown)}");
    }

    private static string Show(string text) => "\"" + text.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";

    private static string Dump(string markdown) => string.Join(" ", HtmlVisitor.Render(markdown).Segments.Select(s => $"{s.Start}-{s.End}:{s.Length}"));

    [Theory]
    [InlineData("**foo** bar", "0-3:3 3-7:4")]
    [InlineData("a\nb", "0-1:1 1-2:1 2-3:1")]
    [InlineData("`x`", "0-1:1")]
    [InlineData("`` a`b ``", "0-3:3")]
    [InlineData("- [x] done", "0-0:1 0-4:4")]
    [InlineData("```\nab\n```", "0-0:1 1-3:2 4-4:1")]
    [InlineData("a\\*b", "0-1:1 1-2:1 2-3:1")]
    [InlineData("> q\n> r", "0-1:1 1-2:1 2-3:1")]
    [InlineData("a :smile: b", "0-2:2 2-4:2 4-6:2")]
    [InlineData("`a\nb`", "0-3:3")]
    [InlineData("x  \ny", "0-1:1 2-3:1")]
    [InlineData("![i](j) x", "0-0:1 1-3:2")]
    [InlineData("a ![i](j)", "0-2:2 3-3:1")]
    public void SegmentsFollowTheDocumentPositions(string markdown, string expected)
    {
        Assert.Equal(expected, Dump(markdown));
    }
}
