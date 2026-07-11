#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// P3(a): the effective font and alignment of a run are resolved at layout time by
// cascading run -> paragraph -> cell -> row -> named Style (BaseStyle chain) ->
// document default. Assertions read the real emitted content stream (Tf sizes,
// Td positions) and the embedded font set.
public class StyleCascadeTests
{
    private static DocumentBuilder Builder(out Section section)
    {
        var builder = new DocumentBuilder();
        section = builder.Sections.Add();
        return builder;
    }

    [Fact]
    public void ParagraphFont_SizeAppliesToRunsWithDefaultFont()
    {
        var builder = Builder(out var section);
        var paragraph = section.Blocks.AddParagraph("Cascade");
        paragraph.Font.Size = 20;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(20.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void ParagraphFont_FamilyAppliesToRunsWithDefaultFont()
    {
        var builder = Builder(out var section);
        BuildTestSupport.RegisterLatin(builder);
        var paragraph = section.Blocks.AddParagraph("Hello");
        paragraph.Font.Name = BuildTestSupport.Latin;

        var reader = BuildTestSupport.Read(builder);

        Assert.Single(BuildTestSupport.Type0Fonts(reader));
    }

    [Fact]
    public void NamedStyle_FontSizeApplies()
    {
        var builder = Builder(out var section);
        var style = builder.Styles.Add("Title");
        style.Font.Size = 30;
        var paragraph = section.Blocks.AddParagraph("Styled");
        paragraph.StyleName = "Title";

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(30.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void NamedStyle_BaseStyleChainResolves()
    {
        var builder = Builder(out var section);
        var baseStyle = builder.Styles.Add("Big");
        baseStyle.Font.Size = 24;
        builder.Styles.Add("Derived", "Big");
        var paragraph = section.Blocks.AddParagraph("Inherited");
        paragraph.StyleName = "Derived";

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(24.0, sizes);
    }

    [Fact]
    public void NamedStyle_AlignmentApplies()
    {
        var reference = Builder(out var referenceSection);
        var referenceParagraph = referenceSection.Blocks.AddParagraph("Centered text");
        referenceParagraph.Alignment = HorizontalAlignment.Center;
        var expected = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(reference));

        var builder = Builder(out var section);
        var style = builder.Styles.Add("Middle");
        style.Alignment = HorizontalAlignment.Center;
        var paragraph = section.Blocks.AddParagraph("Centered text");
        paragraph.StyleName = "Middle";
        var actual = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(builder));

        Assert.NotEmpty(expected);
        Assert.NotEmpty(actual);
        Assert.Equal(expected[0].X, actual[0].X, 1);
    }

    [Fact]
    public void CellFont_SizeAppliesToCellParagraphs()
    {
        var builder = Builder(out var section);
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        var cell = row.Cells[0];
        cell.Text = "Cell";
        cell.Font.Size = 16;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(16.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void RowFont_SizeAppliesToRowCells()
    {
        var builder = Builder(out var section);
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        row.Cells[0].Text = "Row";
        row.Font.Size = 14;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(14.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void RowAlignment_AppliesToRowCells()
    {
        var reference = Builder(out var referenceSection);
        var referenceTable = referenceSection.Blocks.AddTable();
        referenceTable.Columns.Add();
        var referenceRow = referenceTable.Rows.Add();
        referenceRow.Cells[0].Text = "Right";
        referenceRow.Cells[0].Alignment = HorizontalAlignment.Right;
        var expected = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(reference));

        var builder = Builder(out var section);
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        row.Cells[0].Text = "Right";
        row.Alignment = HorizontalAlignment.Right;
        var actual = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(builder));

        Assert.NotEmpty(expected);
        Assert.NotEmpty(actual);
        Assert.Equal(expected[0].X, actual[0].X, 1);
    }

    [Fact]
    public void TableFont_SizeAppliesToCellWithNoExplicitFont()
    {
        var builder = Builder(out var section);
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Font.Size = 18;
        table.Rows.Add().Cells[0].Text = "Cell";

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(18.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void TableFont_OverriddenByExplicitCellFont()
    {
        var builder = Builder(out var section);
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Font.Size = 18;
        var cell = table.Rows.Add().Cells[0];
        cell.Text = "Cell";
        cell.Font.Size = 22;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(22.0, sizes);
        Assert.DoesNotContain(18.0, sizes);
    }

    [Fact]
    public void CellStyleName_FontSizeApplies()
    {
        var builder = Builder(out var section);
        var style = builder.Styles.Add("Number");
        style.Font.Size = 26;
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var cell = table.Rows.Add().Cells[0];
        cell.Text = "42";
        cell.StyleName = "Number";

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(26.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void CellStyleName_OverriddenByExplicitCellFont()
    {
        var builder = Builder(out var section);
        var style = builder.Styles.Add("Number");
        style.Font.Size = 26;
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var cell = table.Rows.Add().Cells[0];
        cell.Text = "42";
        cell.StyleName = "Number";
        cell.Font.Size = 8;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(8.0, sizes);
        Assert.DoesNotContain(26.0, sizes);
    }

    [Fact]
    public void ExplicitRunFont_WinsOverParagraphAndCellFonts()
    {
        var builder = Builder(out var section);
        var paragraph = section.Blocks.AddParagraph();
        paragraph.Font.Size = 20;
        var run = paragraph.Inlines.Add("Precise");
        run.Font.Size = 9;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(9.0, sizes);
        Assert.DoesNotContain(20.0, sizes);
    }
}
