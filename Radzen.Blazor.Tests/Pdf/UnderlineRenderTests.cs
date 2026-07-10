#nullable enable
using System;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// P3(d): Font.Underline draws a horizontal line under the run - below the baseline,
// above baseline minus the font size, spanning roughly the run's advance width.
public class UnderlineRenderTests
{
    private const string Text = "Underlined words";
    private const double Size = 12;

    private static DocumentBuilder Author(bool underline)
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var paragraph = section.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add(Text);
        run.Font.Size = Size;
        run.Font.Underline = underline;
        return builder;
    }

    [Fact]
    public void Underline_DrawsLineUnderRun()
    {
        var builder = Author(underline: true);
        var content = CascadeTestSupport.FirstPageContent(builder);

        var positions = CascadeTestSupport.TdPositions(content);
        Assert.NotEmpty(positions);
        var (textX, baseline) = positions[0];

        var width = new FontCollection().MeasureText(Text, new Font { Size = Size });
        var spans = CascadeTestSupport.HorizontalSpans(content);

        Assert.Contains(spans, s =>
            s.Y < baseline
            && s.Y > baseline - Size
            && Math.Abs(s.X - textX) <= 2.0
            && s.Width >= width * 0.7
            && s.Width <= width * 1.3);
    }

    [Fact]
    public void NoUnderline_DrawsNoLines()
    {
        var builder = Author(underline: false);
        var content = CascadeTestSupport.FirstPageContent(builder);

        Assert.Empty(CascadeTestSupport.HorizontalSpans(content));
    }

    [Fact]
    public void Underline_AppliesOnlyToUnderlinedRun()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var paragraph = section.Blocks.AddParagraph();
        var plain = paragraph.Inlines.Add("Plain ");
        plain.Font.Size = Size;
        var marked = paragraph.Inlines.Add("Marked");
        marked.Font.Size = Size;
        marked.Font.Underline = true;

        var content = CascadeTestSupport.FirstPageContent(builder);
        var spans = CascadeTestSupport.HorizontalSpans(content);
        Assert.Single(spans);

        var fonts = new FontCollection();
        var markedWidth = fonts.MeasureText("Marked", new Font { Size = Size });
        Assert.True(spans[0].Width <= markedWidth * 1.3, "underline covers only the underlined run");
    }
}
