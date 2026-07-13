#nullable enable
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// Soft hyphen (U+00AD) as a zero-width conditional break, ZWSP (U+200B) as a zero-width
// break opportunity, and '-'/en/em-dash breaks in the emergency (oversized-word) path.
// Assertions run on the internal LineBreaker fragments with deterministic Liberation Sans
// metrics, matching the style of the tab-stop and emergency-break contract tests.
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
        Assert.Equal("aaabbb", AllText(lines));                 // the soft hyphen is dropped
        Assert.DoesNotContain('-', AllText(lines));             // no visible hyphen when not broken
        Assert.Equal(Width(fonts, "aaabbb"), lines[0].Width, Tol);
    }

    [Fact]
    public void SoftHyphen_WhenBroken_RendersTrailingHyphen()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("aaaaaa\u00ADbbbbbb");
        var full = Width(fonts, "aaaaaabbbbbb");
        var left = Width(fonts, "aaaaaa");

        // Wide enough for "aaaaaa" alone, too narrow for the whole word: the break is taken.
        var lines = LineBreaker.Break(paragraph, full - 1, fonts);

        Assert.Equal(2, lines.Count);
        Assert.True(left <= full - 1, "the left half must fit on its own line");
        Assert.Contains(lines[0].Fragments, f => f.Text == "-");   // conditional hyphen appears
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
        Assert.DoesNotContain(lines.SelectMany(l => l.Fragments), f => f.Text == "-"); // no hyphen for ZWSP
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
        // Byte-safety: a plain hyphen is not a tokenizer break, so a hyphenated word that
        // fits stays a single fragment and existing layouts are unchanged.
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("table-heavy body");

        var lines = LineBreaker.Break(paragraph, 400, fonts);

        Assert.Single(lines);
        Assert.Contains(lines[0].Fragments, f => f.Text == "table-heavy");
    }

    [Fact]
    public void OversizedWord_BreaksAfterHyphen()
    {
        // A single word wider than the whole measure breaks at the '-' rather than mid-glyph.
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("hyphenated-compound-token");
        var max = Width(fonts, "hyphenated-compound-token") - Width(fonts, "token");

        var lines = LineBreaker.Break(paragraph, max, fonts);

        Assert.True(lines.Count > 1);
        foreach (var line in lines)
        {
            Assert.True(line.Width <= max + 1e-6, "no line exceeds the measure");
        }

        // At least one line ends right after a hyphen (a preferred break was taken).
        Assert.Contains(lines, l => l.Fragments.Count > 0 && l.Fragments[^1].Text.EndsWith('-'));
        // The word is reconstructed exactly (no dropped or added characters).
        Assert.Equal("hyphenated-compound-token", AllText(lines));
    }
}
