#nullable enable
using System;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf;
using Radzen.Documents.Codes;
using System.Linq;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class StyleCascadeTests
{
    private static Document Builder(out Section section)
    {
        var document = new Document();
        section = document.Sections.Add();
        return document;
    }

    [Fact]
    public void ParagraphFont_SizeAppliesToRunsWithDefaultFont()
    {
        var document = Builder(out var section);
        var paragraph = section.Blocks.AddParagraph("Cascade");
        paragraph.Font.Size = 20;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(20.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void ParagraphFont_FamilyAppliesToRunsWithDefaultFont()
    {
        var document = Builder(out var section);
        BuildTestSupport.RegisterLatin(document);
        var paragraph = section.Blocks.AddParagraph("Hello");
        paragraph.Font.Family = BuildTestSupport.Latin;

        var reader = BuildTestSupport.Read(document);

        Assert.Single(BuildTestSupport.Type0Fonts(reader));
    }

    [Fact]
    public void NamedStyle_FontSizeApplies()
    {
        var document = Builder(out var section);
        var style = document.Styles.Add("Title");
        style.Font.Size = 30;
        var paragraph = section.Blocks.AddParagraph("Styled");
        paragraph.StyleName = "Title";

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(30.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void NamedStyle_BaseStyleChainResolves()
    {
        var document = Builder(out var section);
        var baseStyle = document.Styles.Add("Big");
        baseStyle.Font.Size = 24;
        document.Styles.Add("Derived", "Big");
        var paragraph = section.Blocks.AddParagraph("Inherited");
        paragraph.StyleName = "Derived";

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(24.0, sizes);
    }

    [Fact]
    public void NamedStyle_AlignmentApplies()
    {
        var reference = Builder(out var referenceSection);
        var referenceParagraph = referenceSection.Blocks.AddParagraph("Centered text");
        referenceParagraph.Alignment = HorizontalAlignment.Center;
        var expected = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(reference));

        var document = Builder(out var section);
        var style = document.Styles.Add("Middle");
        style.Alignment = HorizontalAlignment.Center;
        var paragraph = section.Blocks.AddParagraph("Centered text");
        paragraph.StyleName = "Middle";
        var actual = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(document));

        Assert.NotEmpty(expected);
        Assert.NotEmpty(actual);
        Assert.Equal(expected[0].X, actual[0].X, 1);
    }

    [Fact]
    public void CellFont_SizeAppliesToCellParagraphs()
    {
        var document = Builder(out var section);
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        var cell = row.Cells[0];
        cell.Text = "Cell";
        cell.Font.Size = 16;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(16.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void RowFont_SizeAppliesToRowCells()
    {
        var document = Builder(out var section);
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        row.Cells[0].Text = "Row";
        row.Font.Size = 14;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

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

        var document = Builder(out var section);
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        row.Cells[0].Text = "Right";
        row.Alignment = HorizontalAlignment.Right;
        var actual = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(document));

        Assert.NotEmpty(expected);
        Assert.NotEmpty(actual);
        Assert.Equal(expected[0].X, actual[0].X, 1);
    }

    [Fact]
    public void TableFont_SizeAppliesToCellWithNoExplicitFont()
    {
        var document = Builder(out var section);
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Font.Size = 18;
        table.Rows.Add().Cells[0].Text = "Cell";

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(18.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void TableFont_OverriddenByExplicitCellFont()
    {
        var document = Builder(out var section);
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Font.Size = 18;
        var cell = table.Rows.Add().Cells[0];
        cell.Text = "Cell";
        cell.Font.Size = 22;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(22.0, sizes);
        Assert.DoesNotContain(18.0, sizes);
    }

    [Fact]
    public void CellStyleName_FontSizeApplies()
    {
        var document = Builder(out var section);
        var style = document.Styles.Add("Number");
        style.Font.Size = 26;
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var cell = table.Rows.Add().Cells[0];
        cell.Text = "42";
        cell.StyleName = "Number";

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(26.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void CellStyleName_OverriddenByExplicitCellFont()
    {
        var document = Builder(out var section);
        var style = document.Styles.Add("Number");
        style.Font.Size = 26;
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var cell = table.Rows.Add().Cells[0];
        cell.Text = "42";
        cell.StyleName = "Number";
        cell.Font.Size = 8;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(8.0, sizes);
        Assert.DoesNotContain(26.0, sizes);
    }

    [Fact]
    public void ParagraphStyleName_WinsOverAmbientTableFont()
    {
        var document = Builder(out var section);
        var style = document.Styles.Add("Title");
        style.Font.Size = 20;
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Font.Size = 8;
        var paragraph = table.Rows.Add().Cells[0].Blocks.AddParagraph("Heading");
        paragraph.StyleName = "Title";

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(20.0, sizes);
        Assert.DoesNotContain(8.0, sizes);
    }

    [Fact]
    public void ExplicitRunFont_WinsOverParagraphAndCellFonts()
    {
        var document = Builder(out var section);
        var paragraph = section.Blocks.AddParagraph();
        paragraph.Font.Size = 20;
        var run = paragraph.Inlines.Add("Precise");
        run.Font.Size = 9;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(9.0, sizes);
        Assert.DoesNotContain(20.0, sizes);
    }

    private static ResolvedParagraphFormat Format(Document document, Paragraph paragraph)
        => StyleResolver.Resolve(document).Format(paragraph);

    [Fact]
    public void ResettingALocalParagraphValue_FallsBackToTheNamedStyle()
    {
        var document = Builder(out var section);
        var style = document.Styles.Add("Quote");
        style.SpacingBefore = Unit.FromPoint(7);
        var paragraph = section.Blocks.AddParagraph("Styled");
        paragraph.StyleName = "Quote";
        paragraph.SpacingBefore = Unit.FromPoint(3);

        Assert.Equal(3, Format(document, paragraph).SpacingBefore.Point);

        paragraph.SpacingBefore = null;

        Assert.Equal(7, Format(document, paragraph).SpacingBefore.Point);
    }

    [Fact]
    public void ResettingALocalStyleValue_FallsBackToTheBaseStyle()
    {
        var document = Builder(out var section);
        var root = document.Styles.Add("Root");
        root.LeftIndent = Unit.FromPoint(30);
        var leaf = document.Styles.Add("Leaf", "Root");
        leaf.LeftIndent = Unit.FromPoint(12);
        var paragraph = section.Blocks.AddParagraph("Chained");
        paragraph.StyleName = "Leaf";

        Assert.Equal(12, Format(document, paragraph).LeftIndent.Point);

        leaf.LeftIndent = null;

        Assert.Equal(30, Format(document, paragraph).LeftIndent.Point);
    }

    [Fact]
    public void NamedStyle_SpacingBeforeMovesTheRenderedBaseline()
    {
        var reference = Builder(out var referenceSection);
        referenceSection.Blocks.AddParagraph("Spaced").SpacingBefore = Unit.FromPoint(40);
        var expected = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(reference));

        var document = Builder(out var section);
        document.Styles.Add("Spaced").SpacingBefore = Unit.FromPoint(40);
        section.Blocks.AddParagraph("Spaced").StyleName = "Spaced";
        var actual = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(document));

        Assert.NotEmpty(expected);
        Assert.Equal(expected[0].Y, actual[0].Y, 3);
    }

    [Fact]
    public void NamedStyle_LeftIndentMovesTheRenderedText()
    {
        var reference = Builder(out var referenceSection);
        referenceSection.Blocks.AddParagraph("Indented").LeftIndent = Unit.FromPoint(36);
        var expected = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(reference));

        var document = Builder(out var section);
        document.Styles.Add("Indented").LeftIndent = Unit.FromPoint(36);
        section.Blocks.AddParagraph("Indented").StyleName = "Indented";
        var actual = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(document));

        Assert.NotEmpty(expected);
        Assert.Equal(expected[0].X, actual[0].X, 3);
    }

    private static Cell SingleCell(Document document, out Table table)
    {
        var section = document.Sections.Add();
        table = section.Blocks.AddTable();
        table.Columns.Add();
        return table.Rows.Add().Cells[0];
    }

    [Fact]
    public void CellStyleName_AlignmentApplies()
    {
        var reference = new Document();
        var referenceCell = SingleCell(reference, out _);
        referenceCell.Text = "42";
        referenceCell.Alignment = HorizontalAlignment.Right;
        var expected = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(reference));

        var document = new Document();
        document.Styles.Add("Number").Alignment = HorizontalAlignment.Right;
        var cell = SingleCell(document, out _);
        cell.Text = "42";
        cell.StyleName = "Number";
        var actual = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(document));

        Assert.NotEmpty(expected);
        Assert.Equal(expected[0].X, actual[0].X, 3);
        Assert.NotEqual(0, expected[0].X);
    }

    [Fact]
    public void CellAlignment_WinsOverTheCellStyleName()
    {
        var reference = new Document();
        var referenceCell = SingleCell(reference, out _);
        referenceCell.Text = "42";
        var expected = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(reference));

        var document = new Document();
        document.Styles.Add("Number").Alignment = HorizontalAlignment.Right;
        var cell = SingleCell(document, out _);
        cell.Text = "42";
        cell.StyleName = "Number";
        cell.Alignment = HorizontalAlignment.Left;
        var actual = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(document));

        Assert.NotEmpty(expected);
        Assert.Equal(expected[0].X, actual[0].X, 3);
    }

    [Fact]
    public void MissingParagraphStyleName_FailsAtLayoutNamingTheStyle()
    {
        var document = Builder(out var section);
        section.Blocks.AddParagraph("Body").StyleName = "Absent";

        var error = Assert.Throws<InvalidOperationException>(() => new DocumentRenderer().ToArray(document));

        Assert.Contains("'Absent'", error.Message, StringComparison.Ordinal);
        Assert.Contains("Absent", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingCellStyleName_FailsAtLayoutNamingTheStyle()
    {
        var document = new Document();
        var cell = SingleCell(document, out _);
        cell.Text = "42";
        cell.StyleName = "Absent";

        var error = Assert.Throws<InvalidOperationException>(() => new DocumentRenderer().ToArray(document));

        Assert.Contains("'Absent'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingBaseStyle_FailsAtLayoutNamingTheReferenceChain()
    {
        var document = Builder(out var section);
        document.Styles.Add("Leaf").BaseStyle = "Gone";
        section.Blocks.AddParagraph("Body").StyleName = "Leaf";

        var error = Assert.Throws<InvalidOperationException>(() => StyleResolver.Resolve(document));

        Assert.Contains("'Gone'", error.Message, StringComparison.Ordinal);
        Assert.Contains("Leaf -> Gone", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CyclicBaseStyleChain_FailsAtLayoutNamingTheReferenceChain()
    {
        var document = Builder(out var section);
        var first = document.Styles.Add("First");
        var second = document.Styles.Add("Second", "First");
        first.BaseStyle = "Second";
        _ = second;
        section.Blocks.AddParagraph("Body").StyleName = "First";

        var error = Assert.Throws<InvalidOperationException>(() => StyleResolver.Resolve(document));

        Assert.Contains("cyclic", error.Message, StringComparison.Ordinal);
        Assert.Contains("First -> Second -> First", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResettingALocalFontValue_IsObservedByChangeTracking()
    {
        var font = new Radzen.Documents.Fonts.Font { Bold = true };
        font.AcceptChanges();

        font.Bold = null;

        Assert.True(font.IsModified);
        Assert.Null(font.Bold);
    }

    [Fact]
    public void NamedStyle_AppliesToParagraphsInsideAContainer()
    {
        var document = Builder(out var section);
        var style = document.Styles.Add("Boxed");
        style.Font.Size = 27;
        style.LeftIndent = Unit.FromPoint(17);
        var container = section.Blocks.Add(new Container { Padding = Unit.FromPoint(5) });
        var paragraph = container.Blocks.AddParagraph("Inside");
        paragraph.StyleName = "Boxed";

        Assert.Equal(17, Format(document, paragraph).LeftIndent.Point, 9);
        Assert.Contains(27.0, CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document)));
    }

    [Fact]
    public void NormalStyle_CascadesIntoNestedContainerParagraphs()
    {
        var document = Builder(out var section);
        document.Styles.Normal.Font.Size = 23;
        var outer = section.Blocks.Add(new Container { Padding = Unit.FromPoint(5) });
        var inner = outer.Blocks.Add(new Container { Padding = Unit.FromPoint(5) });
        inner.Blocks.AddParagraph("Nested");

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(23.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void NamedStyle_AppliesToHeaderAndFooterBandParagraphs()
    {
        var document = Builder(out var section);
        var style = document.Styles.Add("Band");
        style.Font.Size = 21;
        section.Header.Blocks.AddParagraph("Head").StyleName = "Band";
        section.Footer.Blocks.AddParagraph("Foot").StyleName = "Band";
        section.Blocks.AddParagraph("Body");

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Equal(2, sizes.Count(size => size == 21.0));
        Assert.Contains(10.0, sizes);
    }

    [Fact]
    public void BarcodeCaption_InheritsTheNormalStyleFontSizeInsideAContainer()
    {
        var document = Builder(out var section);
        document.Styles.Normal.Font.Size = 19;
        var container = section.Blocks.Add(new Container { Padding = Unit.FromPoint(5) });
        container.Blocks.AddBarcode(
            BarcodeType.Code128, "RADZEN", Unit.FromPoint(200), Unit.FromPoint(40), showText: true);

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(19.0, sizes);
    }

    [Fact]
    public void BarcodeCaption_UsesItsOwnFontOverTheNormalStyle()
    {
        var document = Builder(out var section);
        document.Styles.Normal.Font.Size = 19;
        var barcode = section.Blocks.AddBarcode(
            BarcodeType.Code128, "RADZEN", Unit.FromPoint(200), Unit.FromPoint(40), showText: true);
        barcode.Font.Size = 8;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(8.0, sizes);
        Assert.DoesNotContain(19.0, sizes);
    }
}
