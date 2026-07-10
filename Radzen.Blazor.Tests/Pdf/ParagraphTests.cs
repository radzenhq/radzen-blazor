#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ParagraphTests
{
    [Fact]
    public void Paragraph_Defaults()
    {
        var p = new Paragraph();
        Assert.Null(p.Text);
        Assert.Empty(p.Inlines);
        Assert.NotNull(p.Font);
        Assert.Equal(HorizontalAlignment.Left, p.Alignment);
        Assert.Equal(0, p.SpacingBefore.Point, 9);
        Assert.Equal(0, p.SpacingAfter.Point, 9);
        Assert.Equal(1.0, p.LineSpacing, 9);
        Assert.Null(p.StyleName);
        Assert.False(p.KeepTogether);
        Assert.False(p.KeepWithNext);
        Assert.Equal(2, p.Widows);
        Assert.Equal(2, p.Orphans);
    }

    [Fact]
    public void Inlines_IsReadOnlyListOfRun()
    {
        var p = new Paragraph();
        Assert.IsAssignableFrom<IReadOnlyList<Run>>(p.Inlines);
    }

    [Fact]
    public void TextSet_ReplacesInlinesWithSingleRun()
    {
        var p = new Paragraph();
        p.Inlines.Add("a");
        p.Inlines.Add("b");
        p.Text = "only";
        Assert.Single(p.Inlines);
        Assert.Equal("only", p.Inlines[0].Text);
        Assert.Equal("only", p.Text);
    }

    [Fact]
    public void TextGet_ConcatenatesInlines()
    {
        var p = new Paragraph();
        p.Inlines.Add("Hello, ");
        p.Inlines.Add("world");
        Assert.Equal("Hello, world", p.Text);
    }

    [Fact]
    public void TextSet_Null_ClearsInlines()
    {
        var p = new Paragraph();
        p.Text = "something";
        p.Text = null;
        Assert.Empty(p.Inlines);
        Assert.Null(p.Text);
    }

    [Fact]
    public void Inlines_AddStringReturnsRun()
    {
        var p = new Paragraph();
        var run = p.Inlines.Add("hi");
        Assert.IsType<Run>(run);
        Assert.Equal("hi", run.Text);
        Assert.Same(run, p.Inlines[0]);
    }

    [Fact]
    public void Inlines_AddRunReturnsSameInstance()
    {
        var p = new Paragraph();
        var run = new Run("custom");
        var returned = p.Inlines.Add(run);
        Assert.Same(run, returned);
        Assert.Same(run, p.Inlines[0]);
    }

    [Fact]
    public void Run_Constructor_SetsText()
    {
        var run = new Run("abc");
        Assert.Equal("abc", run.Text);
        Assert.NotNull(run.Font);
    }

    [Fact]
    public void Run_TextIsSettable()
    {
        var run = new Run("abc");
        run.Text = "xyz";
        Assert.Equal("xyz", run.Text);
    }

    [Fact]
    public void Run_FontIndependentOfParagraphFont()
    {
        var p = new Paragraph();
        p.Font.Bold = true;
        var run = p.Inlines.Add("r");
        Assert.False(run.Font.Bold);
        run.Font.Italic = true;
        Assert.False(p.Font.Italic);
    }

    [Fact]
    public void Paragraph_PropertiesSettable()
    {
        var p = new Paragraph();
        p.Alignment = HorizontalAlignment.Center;
        p.SpacingBefore = Unit.FromPoint(6);
        p.SpacingAfter = Unit.FromPoint(12);
        p.LineSpacing = 1.5;
        p.StyleName = "Heading1";
        p.KeepTogether = true;
        p.KeepWithNext = true;
        p.Widows = 3;
        p.Orphans = 4;
        Assert.Equal(HorizontalAlignment.Center, p.Alignment);
        Assert.Equal(6, p.SpacingBefore.Point, 9);
        Assert.Equal(12, p.SpacingAfter.Point, 9);
        Assert.Equal(1.5, p.LineSpacing, 9);
        Assert.Equal("Heading1", p.StyleName);
        Assert.True(p.KeepTogether);
        Assert.True(p.KeepWithNext);
        Assert.Equal(3, p.Widows);
        Assert.Equal(4, p.Orphans);
    }
}
