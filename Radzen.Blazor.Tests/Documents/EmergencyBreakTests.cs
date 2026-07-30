#nullable enable
using System.Linq;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;

public class EmergencyBreakTests
{
    private const string Token = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static double Measure(FontCollection fonts, string text)
        => fonts.MeasureText(text, LineLayoutSupport.FontAt(12));

    [Fact]
    public void RightAlignedOversizedToken_HasNonNegativeOffsets_AndFitsMeasure()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Token, alignment: HorizontalAlignment.Right);
        var max = Measure(fonts, Token) / 4.0;

        var lines = LineBreaker.Break(paragraph, max, fonts);

        Assert.True(lines.Count > 1);
        foreach (var line in lines)
        {
            Assert.True(line.Width <= max + 1e-6);
            Assert.All(line.Fragments, f => Assert.True(f.XOffset >= 0));
        }
    }

    [Fact]
    public void CenterAlignedOversizedToken_HasNonNegativeOffsets_AndFitsMeasure()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Token, alignment: HorizontalAlignment.Center);
        var max = Measure(fonts, Token) / 4.0;

        var lines = LineBreaker.Break(paragraph, max, fonts);

        Assert.True(lines.Count > 1);
        foreach (var line in lines)
        {
            Assert.True(line.Width <= max + 1e-6);
            Assert.All(line.Fragments, f => Assert.True(f.XOffset >= 0));
        }
    }

    [Fact]
    public void SurrogatePairs_AreNeverSplitAcrossLines()
    {
        var fonts = LineLayoutSupport.Fonts();
        var token = string.Concat(Enumerable.Repeat("\U0001F600", 20));
        var paragraph = LineLayoutSupport.SingleRun(token);
        var max = fonts.MeasureText(token, LineLayoutSupport.FontAt(12)) / 4.0;

        var lines = LineBreaker.Break(paragraph, max, fonts);

        Assert.True(lines.Count > 1);
        foreach (var line in lines)
        {
            var text = line.Fragments[0].Text;
            Assert.False(char.IsLowSurrogate(text[0]));
            Assert.False(char.IsHighSurrogate(text[^1]));
        }

        Assert.Equal(token, string.Concat(lines.Select(l => l.Fragments[0].Text)));
    }

    [Fact]
    public void TokenExactlyAtMeasure_IsNotSplit()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(Token);
        var width = Measure(fonts, Token);

        var lines = LineBreaker.Break(paragraph, width + 0.5, fonts);

        Assert.Single(lines);
        Assert.Single(lines[0].Fragments);
        Assert.Equal(Token, lines[0].Fragments[0].Text);
        Assert.Equal(width, lines[0].Width, 6);
    }
}
