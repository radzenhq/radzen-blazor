#nullable enable
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

public class ConditionalBreakTests
{
    private const double Tol = 0.5;

    private static double Width(FontCollection fonts, string text)
        => fonts.MeasureText(text, LineLayoutSupport.FontAt(12));

    private static string AllText(IReadOnlyList<LineBox> lines)
        => string.Concat(lines.SelectMany(l => l.Fragments).Select(f => f.Text));

    [Fact]
    public void SoftHyphen_WhenNotBroken_IsZeroWidthAndInvisible()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("aaa\u00ADbbb");

        var lines = LineBreaker.Break(paragraph, 400, fonts);

        Assert.Single(lines);
        Assert.Equal("aaabbb", AllText(lines));
        Assert.DoesNotContain('-', AllText(lines));
        Assert.Equal(Width(fonts, "aaabbb"), lines[0].Width, Tol);
    }

    [Fact]
    public void SoftHyphen_WhenBroken_RendersTrailingHyphen()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("aaaaaa\u00ADbbbbbb");
        var full = Width(fonts, "aaaaaabbbbbb");
        var left = Width(fonts, "aaaaaa");

        var lines = LineBreaker.Break(paragraph, full - 1, fonts);

        Assert.Equal(2, lines.Count);
        Assert.True(left <= full - 1, "the left half must fit on its own line");
        Assert.Contains(lines[0].Fragments, f => f.Text == "-");
        Assert.Contains(lines[1].Fragments, f => f.Text == "bbbbbb");
        Assert.DoesNotContain(lines[1].Fragments, f => f.Text == "-");
    }

    [Fact]
    public void Zwsp_ProvidesBreakOpportunity_WithNoHyphen()
    {
        var fonts = LineLayoutSupport.Fonts();
        const string left = "aaaaaa";
        const string right = "bbbbbb";
        var paragraph = LineLayoutSupport.SingleRun(left + "\u200B" + right);
        var full = Width(fonts, left + right);

        var lines = LineBreaker.Break(paragraph, full - 1, fonts);

        Assert.Equal(2, lines.Count);
        Assert.DoesNotContain(lines.SelectMany(l => l.Fragments), f => f.Text == "-");
        Assert.Equal(left + right, AllText(lines));
    }

    [Fact]
    public void Zwsp_WhenNotBroken_IsZeroWidth()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("aaa\u200Bbbb");

        var lines = LineBreaker.Break(paragraph, 400, fonts);

        Assert.Single(lines);
        Assert.Equal("aaabbb", AllText(lines));
        Assert.Equal(Width(fonts, "aaabbb"), lines[0].Width, Tol);
    }

    [Fact]
    public void PlainHyphen_IsNotSplit_StaysOneFragment()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("table-heavy body");

        var lines = LineBreaker.Break(paragraph, 400, fonts);

        Assert.Single(lines);
        Assert.Contains(lines[0].Fragments, f => f.Text == "table-heavy");
    }

    [Fact]
    public void OversizedWord_BreaksAfterHyphen()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("hyphenated-compound-token");
        var max = Width(fonts, "hyphenated-compound-token") - Width(fonts, "token");

        var lines = LineBreaker.Break(paragraph, max, fonts);

        Assert.True(lines.Count > 1);
        foreach (var line in lines)
        {
            Assert.True(line.Width <= max + 1e-6, "no line exceeds the measure");
        }

        Assert.Contains(lines, l => l.Fragments.Count > 0 && l.Fragments[^1].Text.EndsWith('-'));
        Assert.Equal("hyphenated-compound-token", AllText(lines));
    }
}
