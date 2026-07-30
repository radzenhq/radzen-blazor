#nullable enable
using System.Linq;
using Radzen.Documents;
using Radzen.Documents.Geometry;
using Radzen.Documents.Layout;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class ShapedTextRunTests
{
    private static LineBox FirstLine(Document document)
        => Assert.Single(DocumentLayouter.Layout(document).Pages).Body.Lines[0].Line;

    private static Document Paragraph(string text)
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(20));
        section.Blocks.AddParagraph().Inlines.Add(text);
        return document;
    }

    [Fact]
    public void FragmentsCarryTheirOwnGlyphRunOnly()
    {
        var line = FirstLine(Paragraph("body one"));

        Assert.Equal(
            new[] { "body", "one" },
            line.Fragments.Select(fragment => fragment.Text).ToArray());
        Assert.Equal(
            new[] { "body", "one" },
            line.Fragments.Select(fragment => fragment.GlyphRun.Text).ToArray());
    }

    [Fact]
    public void AdjacentFragmentsOfOneSourceShapeIntoASingleRun()
    {
        var line = FirstLine(Paragraph("body one"));

        var run = Assert.Single(line.ShapedRuns);
        Assert.Equal("body one", run.GlyphRun.Text);
        Assert.Equal(0, run.FirstFragment);
    }

    [Fact]
    public void AShapedRunAttributesEveryFragmentItCovers()
    {
        var line = FirstLine(Paragraph("body one"));

        var run = Assert.Single(line.ShapedRuns);

        Assert.Equal(
            line.Fragments.Select(fragment => new ShapedRunSource(fragment.Source, fragment.Start, fragment.Length)),
            run.Sources);
    }

    [Fact]
    public void AShapedRunSpansTheSourceRangeOfItsFragments()
    {
        var line = FirstLine(Paragraph("body one"));

        var run = Assert.Single(line.ShapedRuns);
        var first = run.Sources[0];
        var last = run.Sources[^1];

        Assert.Equal(0, first.Start);
        Assert.Equal("body one".Length, last.Start + last.Length);
        Assert.Equal(last.Start + last.Length - first.Start, run.GlyphRun.Text.Length);
    }

    [Fact]
    public void SeparateSourcesShapeIntoSeparateRuns()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(20));
        var paragraph = section.Blocks.AddParagraph();
        paragraph.Inlines.Add("body");
        paragraph.Inlines.Add("one").Font.Bold = true;

        var line = FirstLine(document);

        Assert.Equal(2, line.ShapedRuns.Length);
        Assert.All(line.ShapedRuns, run => Assert.Single(run.Sources));
        Assert.Equal(
            line.ShapedRuns.Select(run => run.GlyphRun.Text).ToArray(),
            new[] { "body", "one" });
    }

    [Fact]
    public void EveryShapedRunStartsAtADistinctFragmentInOrder()
    {
        var line = FirstLine(Paragraph("one two three four five"));

        var starts = line.ShapedRuns.Select(run => run.FirstFragment).ToArray();

        Assert.Equal(starts.OrderBy(start => start).Distinct().ToArray(), starts);
        Assert.All(starts, start => Assert.InRange(start, 0, line.Fragments.Length - 1));
    }
}
