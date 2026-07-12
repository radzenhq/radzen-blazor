#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Coverage matrix for block dispatch inside a table cell: every concrete Block
// subclass must either render in a cell or be an explicit, documented no-op
// (PageBreak). Anything else must throw instead of silently vanishing. The
// reflection test at the bottom trips when a new Block subclass is added
// without wiring TableLayout.LayoutContent.
public class TableCellBlockDispatchTests
{
    private static (Table Table, Cell Cell) CellTable()
    {
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(200));
        var row = table.Rows.Add();
        var cell = row.Cells[0];
        cell.Blocks.Clear();
        return (table, cell);
    }

    private static LaidOutCell LayOut(Table table)
        => TableLayoutSupport.CellAt(TableLayout.Layout(table, 400, TableLayoutSupport.Fonts()), 0, 0);

    private static byte[] Png() => PdfTestResources.ReadAllBytes("Images/rgb.png");

    [Fact]
    public void ParagraphInCell_ProducesLines()
    {
        var (table, cell) = CellTable();
        TableLayoutSupport.Fill(cell, "Alpha");

        var laid = LayOut(table);

        Assert.NotEmpty(laid.Lines);
    }

    [Fact]
    public void ImageInCell_ProducesImageItem()
    {
        var (table, cell) = CellTable();
        cell.Blocks.AddImage(new MemoryStream(Png()));

        var laid = LayOut(table);

        Assert.Single(laid.Images);
        Assert.True(laid.Images[0].Height > 0);
    }

    [Fact]
    public void QrCodeInCell_ProducesCodeItem()
    {
        var (table, cell) = CellTable();
        cell.Blocks.AddQrCode("RADZEN", Unit.FromPoint(60));

        var laid = LayOut(table);

        Assert.Single(laid.Codes);
        Assert.IsType<QrCode>(laid.Codes[0].Source);
    }

    [Fact]
    public void BarcodeInCell_ProducesCodeItem()
    {
        var (table, cell) = CellTable();
        cell.Blocks.AddBarcode(BarcodeType.Code128, "RADZEN", Unit.FromPoint(120), Unit.FromPoint(40));

        var laid = LayOut(table);

        Assert.Single(laid.Codes);
        Assert.IsType<Barcode>(laid.Codes[0].Source);
    }

    [Fact]
    public void NestedTableInCell_ProducesTableItem()
    {
        var (table, cell) = CellTable();
        var nested = cell.Blocks.AddTable();
        nested.Columns.Add(Unit.FromPoint(80));
        TableLayoutSupport.Fill(nested.Rows.Add().Cells[0], "Inner");

        var laid = LayOut(table);

        Assert.Single(laid.Tables);
    }

    [Fact]
    public void ListInCell_ProducesOneLinePerItem()
    {
        var (table, cell) = CellTable();
        var list = cell.Blocks.AddList(ListStyle.Number);
        list.Font.Name = TableLayoutSupport.Family;
        list.AddItem("Alpha");
        list.AddItem("Beta");
        list.AddItem("Gamma");

        var laid = LayOut(table);

        Assert.Equal(3, laid.Lines.Count);
    }

    [Fact]
    public void ListInCell_RowGrowsToFitAllItems()
    {
        var (table, cell) = CellTable();
        var fonts = TableLayoutSupport.Fonts();
        var list = cell.Blocks.AddList(ListStyle.Bullet);
        list.Font.Name = TableLayoutSupport.Family;
        list.Font.Size = 12;
        list.AddItem("Alpha");
        list.AddItem("Beta");
        list.AddItem("Gamma");

        var layout = TableLayout.Layout(table, 400, fonts);

        var lineHeight = TableLayoutSupport.LineHeight(fonts);
        var padding = cell.Padding.Point;
        Assert.Equal(3 * lineHeight + 2 * padding, layout.RowHeights[0], 3);
    }

    [Fact]
    public void ListInCell_EmitsMarkersAndItemTextWithHangingIndent()
    {
        // Default base-14 font so text is emitted as literal (text) Tj strings.
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        var cell = table.Rows.Add().Cells[0];
        var list = cell.Blocks.AddList(ListStyle.Number);
        list.HangingIndent = Unit.FromPoint(20);
        list.AddItem("Alpha");
        list.AddItem("Beta");

        var draws = TextDraws(builder);

        var one = Assert.Single(draws, d => d.Text == "1.");
        var two = Assert.Single(draws, d => d.Text == "2.");
        var alpha = Assert.Single(draws, d => d.Text == "Alpha");
        var beta = Assert.Single(draws, d => d.Text == "Beta");
        Assert.Equal(alpha.X, one.X + 20, 3);
        Assert.Equal(beta.X, two.X + 20, 3);
    }

    [Fact]
    public void PageBreakInCell_IsIgnoredWithoutContentOrThrow()
    {
        var (table, cell) = CellTable();
        TableLayoutSupport.Fill(cell, "Alpha");
        var withoutBreakHeight = TableLayout.Layout(table, 400, TableLayoutSupport.Fonts()).RowHeights[0];

        cell.Blocks.AddPageBreak();
        var laid = LayOut(table);

        Assert.Single(laid.Lines);
        Assert.Empty(laid.Images);
        Assert.Empty(laid.Codes);
        Assert.Empty(laid.Tables);
        Assert.Equal(withoutBreakHeight, TableLayout.Layout(table, 400, TableLayoutSupport.Fonts()).RowHeights[0], 6);
    }

    private sealed class UnknownBlock : Block
    {
    }

    [Fact]
    public void UnhandledBlockInCell_Throws()
    {
        var (table, cell) = CellTable();
        cell.Blocks.Add(new UnknownBlock());

        var exception = Assert.Throws<NotSupportedException>(() => TableLayout.Layout(table, 400, TableLayoutSupport.Fonts()));
        Assert.Contains("UnknownBlock", exception.Message);
    }

    [Fact]
    public void UnhandledBlockInSection_Throws()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        section.Blocks.Add(new UnknownBlock());

        var exception = Assert.Throws<NotSupportedException>(() => builder.Build());
        Assert.Contains("UnknownBlock", exception.Message);
    }

    // Systemic guard: every concrete Block subclass in the library must lay out
    // inside a table cell without throwing (or be PageBreak, an explicit no-op).
    // Adding a new Block subclass without wiring the cell path fails here.
    [Fact]
    public void EveryBlockSubclass_IsHandledInsideATableCell()
    {
        var blockTypes = typeof(Block).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Block)) && !t.IsAbstract)
            .ToList();

        Assert.NotEmpty(blockTypes);

        foreach (var type in blockTypes)
        {
            var (table, cell) = CellTable();
            AddSample(cell, type);
            var exception = Record.Exception(() => TableLayout.Layout(table, 400, TableLayoutSupport.Fonts()));
            Assert.True(exception is null, $"Block type '{type.Name}' failed to lay out inside a table cell: {exception}");
        }
    }

    private static void AddSample(Cell cell, Type type)
    {
        switch (type.Name)
        {
            case nameof(Paragraph):
                TableLayoutSupport.Fill(cell, "Alpha");
                break;
            case nameof(Table):
                var nested = cell.Blocks.AddTable();
                nested.Columns.Add(Unit.FromPoint(80));
                TableLayoutSupport.Fill(nested.Rows.Add().Cells[0], "Inner");
                break;
            case nameof(Image):
                cell.Blocks.AddImage(new MemoryStream(Png()));
                break;
            case nameof(List):
                var list = cell.Blocks.AddList();
                list.Font.Name = TableLayoutSupport.Family;
                list.AddItem("Alpha");
                break;
            case nameof(QrCode):
                cell.Blocks.AddQrCode("RADZEN", Unit.FromPoint(60));
                break;
            case nameof(Barcode):
                cell.Blocks.AddBarcode(BarcodeType.Code128, "RADZEN", Unit.FromPoint(120), Unit.FromPoint(40));
                break;
            case nameof(PageBreak):
                cell.Blocks.AddPageBreak();
                break;
            case nameof(Container):
                var container = cell.Blocks.Add(new Container { Padding = Unit.FromPoint(4) });
                var boxed = container.Blocks.AddParagraph();
                var run = boxed.Inlines.Add("Boxed");
                run.Font.Name = TableLayoutSupport.Family;
                break;
            default:
                Assert.Fail($"No cell sample for block type '{type.Name}'. Wire it into TableLayout.LayoutContent (and Paginator) and add a sample here.");
                break;
        }
    }

    [Fact]
    public void ListInHeaderBand_EmitsMarkersAndItemText()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "body", "Helvetica");
        var list = section.Header.Blocks.AddList(ListStyle.Number);
        list.AddItem("Alpha");
        list.AddItem("Beta");

        var draws = TextDraws(builder);

        Assert.Contains(draws, d => d.Text == "1.");
        Assert.Contains(draws, d => d.Text == "2.");
        Assert.Contains(draws, d => d.Text == "Alpha");
        Assert.Contains(draws, d => d.Text == "Beta");
    }

    [Fact]
    public void UnhandledBlockInBand_Throws()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "body", BuildTestSupport.Latin);
        section.Footer.Blocks.Add(new UnknownBlock());

        var exception = Assert.Throws<NotSupportedException>(() => builder.Build());
        Assert.Contains("UnknownBlock", exception.Message);
    }

    private static List<(double X, string Text)> TextDraws(DocumentBuilder builder)
    {
        var reader = BuildTestSupport.Read(builder);
        var leaves = BuildTestSupport.PageLeaves(reader);
        var content = Encoding.Latin1.GetString(BuildTestSupport.Content(reader, leaves[0].Page));

        var result = new List<(double, string)>();
        foreach (Match match in Regex.Matches(
            content,
            @"([-\d.]+)\s+([-\d.]+)\s+Td\s*\(((?:\\.|[^)\\])*)\)\s*Tj"))
        {
            result.Add((
                double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                match.Groups[3].Value));
        }

        return result;
    }
}
