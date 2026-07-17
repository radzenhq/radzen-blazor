#nullable enable
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

public class TextLayoutFindingTests
{
    private const double Tol = 0.5;

    private static void Sized(Run run)
    {
        run.Font.Name = LineLayoutSupport.Family;
        run.Font.Size = 12;
    }

    private static double Width(FontCollection fonts, string text)
        => fonts.MeasureText(text, LineLayoutSupport.FontAt(12));

    [Fact]
    public void Justify_InlineImageAdjacentToText_StaysGlued()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = new Paragraph { Alignment = HorizontalAlignment.Justify };
        Sized(paragraph.Inlines.Add("value"));
        var image = paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(20);
        image.Height = Unit.FromPoint(12);
        Sized(paragraph.Inlines.Add("unit more words to force a wrap here"));

        var valueWidth = Width(fonts, "value");
        var unitWidth = Width(fonts, "unit");
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);
        var moreWidth = Width(fonts, "more");
        var max = valueWidth + 20 + unitWidth + space + moreWidth + 2;

        var lines = LineBreaker.Break(paragraph, max, fonts);

        Assert.True(lines.Count >= 2, "the paragraph must wrap so the image line is justified");
        var imageLine = lines.First(l => l.Fragments.Any(f => f.Run is InlineImage));
        Assert.NotSame(lines[^1], imageLine);

        var fragments = imageLine.Fragments;
        var imageIndex = -1;
        for (var i = 0; i < fragments.Count; i++)
        {
            if (fragments[i].Run is InlineImage)
            {
                imageIndex = i;
                break;
            }
        }

        var before = fragments[imageIndex - 1];
        var img = fragments[imageIndex];
        var after = fragments[imageIndex + 1];

        Assert.Equal("value", before.Text);
        Assert.Equal("unit", after.Text);
        Assert.Equal(before.XOffset + before.Advance, img.XOffset, Tol);
        Assert.Equal(img.XOffset + img.Advance, after.XOffset, Tol);
    }

    [Fact]
    public void SoftHyphen_RightAligned_HyphenStaysWithinMeasure()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun(
            "aaaaaa\u00ADbbbbbb", alignment: HorizontalAlignment.Right);
        var full = Width(fonts, "aaaaaabbbbbb");
        var max = full - 1;

        var lines = LineBreaker.Break(paragraph, max, fonts);

        Assert.Equal(2, lines.Count);
        Assert.Contains(lines[0].Fragments, f => f.Text == "-");
        var rightEdge = lines[0].Fragments.Max(f => f.XOffset + f.Advance);
        Assert.True(rightEdge <= max + 0.01, $"the hyphen must not spill past the measure ({rightEdge} > {max})");
    }

    [Fact]
    public void ListMarker_TextStartsWithBreak_AttachesToFirstContentLine()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = LineLayoutSupport.SingleRun("\nSecond line");
        paragraph.MarkerText = "1.";

        var lines = LineBreaker.Break(paragraph, 400, fonts);

        Assert.True(lines.Count >= 2);
        Assert.Empty(lines[0].Fragments);
        Assert.Contains(lines.SelectMany(l => l.Fragments), f => f.IsMarker && f.Text == "1.");
    }

    [Fact]
    public void RightTabStop_SegmentWiderThanGap_DoesNotOverlapPrevious()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = new Paragraph();
        Sized(paragraph.Inlines.Add("A\tVeryLongValueTextThatOverflows"));
        paragraph.TabStops.AddTabStop(Unit.FromPoint(40), TabAlignment.Right);

        var lines = LineBreaker.Break(paragraph, 400, fonts);

        var fragments = lines[0].Fragments;
        var label = fragments.First(f => f.Text == "A");
        var value = fragments.First(f => f.Text == "VeryLongValueTextThatOverflows");
        var labelEnd = label.XOffset + label.Advance;

        Assert.True(value.XOffset >= labelEnd - 0.01,
            $"the value ({value.XOffset}) must not overlap the label end ({labelEnd})");
    }
}
