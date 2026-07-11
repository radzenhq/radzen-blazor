#nullable enable
using System;
using System.Reflection;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Q1(b): public Font.Strikethrough draws a horizontal line through the run at
// roughly the x-height midline (~0.3 * fontSize above the baseline), one line per
// maximal run of consecutive strikethrough fragments. Set via reflection so the
// suite compiles before the property exists and fails pointing at the gap.
public class StrikethroughRenderTests
{
    private const string Text = "Struck words";
    private const double Size = 12;

    private static void SetStrikethrough(Font font)
    {
        var property = typeof(Font).GetProperty("Strikethrough", BindingFlags.Public | BindingFlags.Instance);
        Assert.True(property is not null, "Font.Strikethrough public property is not implemented");
        property!.SetValue(font, true);
    }

    private static DocumentBuilder Author(bool strikethrough)
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var paragraph = section.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add(Text);
        run.Font.Size = Size;
        if (strikethrough)
        {
            SetStrikethrough(run.Font);
        }

        return builder;
    }

    [Fact]
    public void Strikethrough_DrawsLineAtMidHeight()
    {
        var builder = Author(strikethrough: true);
        var content = CascadeTestSupport.FirstPageContent(builder);

        var positions = CascadeTestSupport.TdPositions(content);
        Assert.NotEmpty(positions);
        var (textX, baseline) = positions[0];

        var width = new FontCollection().MeasureText(Text, new Font { Size = Size });
        var spans = CascadeTestSupport.HorizontalSpans(content);

        Assert.Contains(spans, s =>
            s.Y > baseline + Size * 0.1
            && s.Y < baseline + Size * 0.55
            && Math.Abs(s.X - textX) <= 2.0
            && s.Width >= width * 0.7
            && s.Width <= width * 1.3);
    }

    [Fact]
    public void NoStrikethrough_DrawsNoLines()
    {
        var builder = Author(strikethrough: false);
        var content = CascadeTestSupport.FirstPageContent(builder);

        Assert.Empty(CascadeTestSupport.HorizontalSpans(content));
    }

    [Fact]
    public void Strikethrough_AppliesOnlyToMarkedRun()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var paragraph = section.Blocks.AddParagraph();
        var plain = paragraph.Inlines.Add("Plain ");
        plain.Font.Size = Size;
        var marked = paragraph.Inlines.Add("Marked");
        marked.Font.Size = Size;
        SetStrikethrough(marked.Font);

        var content = CascadeTestSupport.FirstPageContent(builder);
        var baseline = CascadeTestSupport.TdPositions(content)[0].Y;
        var spans = CascadeTestSupport.HorizontalSpans(content);
        Assert.Single(spans);
        Assert.True(spans[0].Y > baseline, "strikethrough sits above the baseline");

        var fonts = new FontCollection();
        var markedWidth = fonts.MeasureText("Marked", new Font { Size = Size });
        Assert.True(spans[0].Width <= markedWidth * 1.3, "strike covers only the marked run");
    }

    [Fact]
    public void ConsecutiveStrikethroughFragments_MergeIntoSingleLine()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var paragraph = section.Blocks.AddParagraph();
        var first = paragraph.Inlines.Add("Struck ");
        first.Font.Size = Size;
        SetStrikethrough(first.Font);
        var second = paragraph.Inlines.Add("through");
        second.Font.Size = Size;
        SetStrikethrough(second.Font);

        var content = CascadeTestSupport.FirstPageContent(builder);
        var spans = CascadeTestSupport.HorizontalSpans(content);
        Assert.Single(spans);

        var combined = new FontCollection().MeasureText("Struck through", new Font { Size = Size });
        Assert.True(spans[0].Width >= combined * 0.7, "one line spans the whole consecutive strikethrough run");
    }

    [Fact]
    public void Strikethrough_TextRemainsExtractable()
    {
        var builder = Author(strikethrough: true);
        Assert.NotEmpty(CascadeTestSupport.HorizontalSpans(CascadeTestSupport.FirstPageContent(builder)));

        var text = BuildTestSupport.Reload(builder).ExtractText();
        Assert.Contains("Struck", text, StringComparison.Ordinal);
        Assert.Contains("words", text, StringComparison.Ordinal);
    }
}
