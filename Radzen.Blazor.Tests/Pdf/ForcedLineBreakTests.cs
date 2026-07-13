#nullable enable
using System;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// P3(c): LineBreaker treats '\n' as a forced line break, "\r\n" as a single forced
// break, '\t' as breakable whitespace and NBSP as non-breaking. Control characters
// never leak into fragment text.
public class ForcedLineBreakTests
{
    private const double Wide = 100000;

    [Fact]
    public void Newline_ForcesLineBreak()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("Alpha\nBeta");

        var lines = LineBreaker.Break(paragraph, Wide, fonts);

        Assert.Equal(2, lines.Count);
        Assert.Equal("Alpha", string.Concat(lines[0].Fragments.Select(f => f.Text)));
        Assert.Equal("Beta", string.Concat(lines[1].Fragments.Select(f => f.Text)));
    }

    [Fact]
    public void CarriageReturnLineFeed_IsSingleBreak()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("Alpha\r\nBeta");

        var lines = LineBreaker.Break(paragraph, Wide, fonts);

        Assert.Equal(2, lines.Count);
        foreach (var fragment in lines.SelectMany(l => l.Fragments))
        {
            Assert.DoesNotContain('\r', fragment.Text);
            Assert.DoesNotContain('\n', fragment.Text);
        }
    }

    [Fact]
    public void Tab_IsWhitespace_SeparatesWords()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("Alpha\tBeta");

        var lines = LineBreaker.Break(paragraph, Wide, fonts);

        Assert.Single(lines);
        var texts = lines[0].Fragments.Select(f => f.Text).ToList();
        Assert.Equal(["Alpha", "Beta"], texts);
    }

    [Fact]
    public void Tab_IsBreakOpportunity()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("Alpha\tBeta", size: 12);
        var alpha = LineLayoutSupport.WordWidth(fonts, "Alpha", 12);

        var lines = LineBreaker.Break(paragraph, alpha + 1.0, fonts);

        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public void NonBreakingSpace_DoesNotBreak()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("Alpha\u00A0Beta", size: 12);
        var alpha = LineLayoutSupport.WordWidth(fonts, "Alpha", 12);

        var lines = LineBreaker.Break(paragraph, alpha + 1.0, fonts);

        Assert.Single(lines);
    }

    [Fact]
    public void Newline_ProducesTwoBaselines_InEmittedContent()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Blocks.AddParagraph("Line one\nLine two");

        var content = CascadeTestSupport.FirstPageContent(builder);
        var baselines = CascadeTestSupport.TdPositions(content)
            .Select(p => Math.Round(p.Y, 2))
            .Distinct()
            .ToList();

        Assert.Equal(2, baselines.Count);
    }
}
