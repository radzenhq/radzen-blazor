#nullable enable
using System.Linq;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

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
        section.Blocks.Add(new Paragraph()).Inlines.Add(text);
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
        Assert.Equal(new[] { "body", "one" }, run.Fragments.Select(fragment => fragment.Text).ToArray());
    }

    [Fact]
    public void AShapedRunOwnsEveryFragmentItCovers()
    {
        var line = FirstLine(Paragraph("body one"));

        var run = Assert.Single(line.ShapedRuns);

        Assert.Equal(line.Fragments, run.Fragments);
    }

    [Fact]
    public void AShapedRunSpansTheSourceRangeOfItsFragments()
    {
        var line = FirstLine(Paragraph("body one"));

        var run = Assert.Single(line.ShapedRuns);
        var first = run.Fragments[0];
        var last = run.Fragments[^1];

        Assert.Equal(0, first.Start);
        Assert.Equal("body one".Length, last.Start + last.Length);
        Assert.Equal(last.Start + last.Length - first.Start, run.GlyphRun.Text.Length);
    }

    [Fact]
    public void ShapedRunsPartitionTheLineFragmentsInOrder()
    {
        var line = FirstLine(Paragraph("one two three four five"));

        Assert.Equal(
            line.Fragments,
            line.ShapedRuns.SelectMany(run => run.Fragments).ToArray());
    }

    [Fact]
    public void SeparateSourcesShapeIntoSeparateRuns()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(20));
        var paragraph = section.Blocks.Add(new Paragraph());
        paragraph.Inlines.Add("body");
        paragraph.Inlines.Add("one").Font.Bold = true;

        var line = FirstLine(document);

        Assert.Equal(2, line.ShapedRuns.Length);
        Assert.All(line.ShapedRuns, run => Assert.Single(run.Fragments));
        Assert.Equal(
            line.ShapedRuns.Select(run => run.GlyphRun.Text).ToArray(),
            new[] { "body", "one" });
    }

    [Fact]
    public void FragmentsAreFlattenedOnceForMultipleShapedRuns()
    {
        var document = new Document();
        var paragraph = document.Sections.Add().Blocks.Add(new Paragraph());
        paragraph.Inlines.Add("body");
        paragraph.Inlines.Add("one").Font.Bold = true;
        var line = FirstLine(document);

        var first = line.Fragments;
        var second = line.Fragments;

        Assert.True(first == second);
    }
}
