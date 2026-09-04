using Xunit;

namespace Radzen.Documents.Markdown.Tests;

public class InlineSpanTests
{
    [Fact]
    public void Finds_strong_span()
    {
        var spans = InlineParser.ScanSpans("a **bold** b");
        var span = Assert.Single(spans);
        Assert.Equal((2, 10, 2, '*'), (span.Start, span.End, span.DelimiterLength, span.Char));
    }

    [Fact]
    public void Finds_underscore_emphasis()
    {
        var spans = InlineParser.ScanSpans("_it_");
        var span = Assert.Single(spans);
        Assert.Equal((0, 4, 1, '_'), (span.Start, span.End, span.DelimiterLength, span.Char));
    }

    [Fact]
    public void Finds_nested_strong_and_emphasis()
    {
        var spans = InlineParser.ScanSpans("***a***");
        Assert.Contains(spans, s => s is { Start: 1, End: 6, DelimiterLength: 2 }); // **a** inner
        Assert.Contains(spans, s => s is { Start: 0, End: 7, DelimiterLength: 1 }); // *…* outer
    }

    [Fact]
    public void Finds_code_span_and_ignores_emphasis_inside()
    {
        var spans = InlineParser.ScanSpans("x `a *b*` y");
        var span = Assert.Single(spans);
        Assert.Equal((2, 9, 1, '`'), (span.Start, span.End, span.DelimiterLength, span.Char));
    }

    [Fact]
    public void Finds_strikethrough()
    {
        var spans = InlineParser.ScanSpans("~~gone~~");
        var span = Assert.Single(spans);
        Assert.Equal((0, 8, 2, '~'), (span.Start, span.End, span.DelimiterLength, span.Char));
    }

    [Fact]
    public void Flanking_rejects_space_before_closer()
    {
        Assert.Empty(InlineParser.ScanSpans("**hello **world"));
    }
}
