#nullable enable
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Markdown;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class MarkdownPdfTests
{
    [Fact]
    public void PlainParagraph_YieldsSingleParagraphWithText()
    {
        var blocks = new BlockCollection();

        MarkdownPdf.Render(blocks, "Hello world.");

        var paragraph = Assert.Single(blocks);
        var typed = Assert.IsType<Paragraph>(paragraph);
        Assert.Equal("Hello world.", typed.Text);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Heading_ProducesBoldParagraphAtExpectedSize(int level)
    {
        var blocks = new BlockCollection();
        var options = new MarkdownPdfOptions();

        MarkdownPdf.Render(blocks, new string('#', level) + " Title", options);

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(blocks));
        Assert.Equal("Title", paragraph.Text);
        Assert.True(paragraph.Font.Bold);
        Assert.Equal(options.HeadingFontSizes[level - 1], paragraph.Font.Size, 9);
    }

    [Fact]
    public void HeadingSizes_DecreaseAsLevelIncreases()
    {
        var options = new MarkdownPdfOptions();

        for (var i = 1; i < options.HeadingFontSizes.Length; i++)
        {
            Assert.True(options.HeadingFontSizes[i] < options.HeadingFontSizes[i - 1]);
        }
    }

    [Fact]
    public void Paragraph_WithBoldItalicAndLink_ProducesStyledRuns()
    {
        var blocks = new BlockCollection();

        MarkdownPdf.Render(blocks, "**bold** and *italic* and [link text](https://example.com/x)");

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(blocks));

        var boldRun = paragraph.Inlines.Single(r => r.Font.Bold);
        Assert.Equal("bold", boldRun.Text);

        var italicRun = paragraph.Inlines.Single(r => r.Font.Italic);
        Assert.Equal("italic", italicRun.Text);

        var linkRun = paragraph.Inlines.Single(r => r.Link != null);
        Assert.Equal("link text", linkRun.Text);
        Assert.Equal("https://example.com/x", linkRun.Link);
    }

    [Fact]
    public void InlineCode_RendersRunInMonospaceFont()
    {
        var blocks = new BlockCollection();
        var options = new MarkdownPdfOptions { MonospaceFontName = "Courier" };

        MarkdownPdf.Render(blocks, "Use `var x = 1;` here.", options);

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(blocks));
        var codeRun = paragraph.Inlines.Single(r => r.Text == "var x = 1;");
        Assert.Equal("Courier", codeRun.Font.Name);
    }

    [Fact]
    public void BulletList_ProducesListWithBulletStyleAndItems()
    {
        var blocks = new BlockCollection();

        MarkdownPdf.Render(blocks, "- Alpha\n- Beta\n- Gamma");

        var list = Assert.IsType<List>(Assert.Single(blocks));
        Assert.Equal(ListStyle.Bullet, list.Style);
        Assert.Equal(3, list.Items.Count);
        Assert.Equal(["Alpha", "Beta", "Gamma"], list.Items.Select(i => i.Text));
    }

    [Fact]
    public void OrderedList_ProducesListWithNumberStyleAndItems()
    {
        var blocks = new BlockCollection();

        MarkdownPdf.Render(blocks, "1. Alpha\n2. Beta\n3. Gamma");

        var list = Assert.IsType<List>(Assert.Single(blocks));
        Assert.Equal(ListStyle.Number, list.Style);
        Assert.Equal(3, list.Items.Count);
        Assert.Equal(["Alpha", "Beta", "Gamma"], list.Items.Select(i => i.Text));
    }

    [Fact]
    public void NestedList_IsFlattenedIntoParentListWithIndentedItems()
    {
        var blocks = new BlockCollection();

        MarkdownPdf.Render(blocks, "- A\n  - B\n- C");

        var list = Assert.IsType<List>(Assert.Single(blocks));
        Assert.Equal(3, list.Items.Count);
        Assert.Equal("A", list.Items[0].Text);
        Assert.Equal("    B", list.Items[1].Text);
        Assert.Equal("C", list.Items[2].Text);
    }

    [Fact]
    public void FencedCodeBlock_ProducesMonospaceParagraph()
    {
        var blocks = new BlockCollection();
        var options = new MarkdownPdfOptions { MonospaceFontName = "Courier" };

        MarkdownPdf.Render(blocks, "```\nvar x = 1;\n```", options);

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(blocks));
        Assert.Equal("Courier", paragraph.Font.Name);
        Assert.Equal("var x = 1;", paragraph.Text);
    }

    [Fact]
    public void BlockQuote_ProducesIndentedParagraph()
    {
        var blocks = new BlockCollection();
        var options = new MarkdownPdfOptions { BlockQuoteIndent = 24 };

        MarkdownPdf.Render(blocks, "> Quoted text", options);

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(blocks));
        Assert.Equal("Quoted text", paragraph.Text);
        Assert.Equal(24, paragraph.LeftIndent.Point, 9);
    }

    [Fact]
    public void ThematicBreak_ProducesOneRowOneColumnTableWithTopBorder()
    {
        var blocks = new BlockCollection();

        MarkdownPdf.Render(blocks, "***");

        var table = Assert.IsType<Table>(Assert.Single(blocks));
        Assert.Equal(1, table.Columns.Count);
        Assert.Equal(1, table.Rows.Count);
        Assert.Equal(BorderStyle.Solid, table.Rows[0].Cells[0].Borders.Top.Style);
    }

    [Fact]
    public void Table_ProducesTableBlockWithHeaderAndCells()
    {
        var blocks = new BlockCollection();

        MarkdownPdf.Render(blocks, "| Name | Age |\n| --- | --- |\n| Ann | 30 |\n| Bob | 40 |");

        var table = Assert.IsType<Table>(Assert.Single(blocks));
        Assert.Equal(2, table.Columns.Count);
        Assert.Equal(3, table.Rows.Count);

        Assert.True(table.Rows[0].IsHeader);
        Assert.Equal("Name", table.Rows[0].Cells[0].Text);
        Assert.Equal("Age", table.Rows[0].Cells[1].Text);

        Assert.False(table.Rows[1].IsHeader);
        Assert.Equal("Ann", table.Rows[1].Cells[0].Text);
        Assert.Equal("30", table.Rows[1].Cells[1].Text);

        Assert.Equal("Bob", table.Rows[2].Cells[0].Text);
        Assert.Equal("40", table.Rows[2].Cells[1].Text);
    }

    [Fact]
    public void UnresolvedImage_IsSkippedWithoutThrowing()
    {
        var blocks = new BlockCollection();
        var options = new MarkdownPdfOptions { ImageResolver = _ => null };

        var exception = Record.Exception(() => MarkdownPdf.Render(blocks, "![alt](missing.png)", options));

        Assert.Null(exception);
        Assert.Empty(blocks);
    }

    [Fact]
    public void ResolvedImage_AddsImageBlock()
    {
        var blocks = new BlockCollection();
        var bytes = PdfTestResources.ReadAllBytes("Images/rgb.jpg");
        var options = new MarkdownPdfOptions { ImageResolver = _ => bytes };

        MarkdownPdf.Render(blocks, "![alt](photo.jpg)", options);

        Assert.IsType<Image>(Assert.Single(blocks));
    }
}
