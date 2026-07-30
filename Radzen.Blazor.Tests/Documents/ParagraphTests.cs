#nullable enable
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Documents.Tests;

public class ParagraphTests
{
    [Fact]
    public void TextSet_ReplacesInlinesWithSingleRun()
    {
        var p = new Paragraph();
        p.Inlines.Add("a");
        p.Inlines.Add("b");
        p.Text = "only";
        Assert.Single(p.Inlines);
        Assert.Equal("only", Assert.IsType<Run>(p.Inlines[0]).Text);
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
    public void Run_FontIndependentOfParagraphFont()
    {
        var p = new Paragraph();
        p.Font.Bold = true;
        var run = p.Inlines.Add("r");
        Assert.Null(run.Font.Bold);
        run.Font.Italic = true;
        Assert.Null(p.Font.Italic);
    }
}
