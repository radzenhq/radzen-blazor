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
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Layout;
using Radzen.Documents.Codes;
using Radzen.Documents.Geometry;

namespace Radzen.Blazor.Pdf.Tests;

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

    private static LaidOutCell LayOut(Table table, LayoutCaptureContext? capture = null)
        => TableLayoutSupport.CellAt(
            TableLayout.LayoutIsolated(table, 400, TableLayoutSupport.Fonts(), capture: capture),
            0,
            0);

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
        var qr = cell.Blocks.AddQrCode("RADZEN", Unit.FromPoint(60));
        var capture = new LayoutCaptureContext();

        var laid = LayOut(table, capture);

        Assert.Single(laid.CodeSymbols);
        Assert.Equal(capture.Source(qr), laid.CodeSymbols[0].Source);
    }

    [Fact]
    public void BarcodeInCell_ProducesCodeItem()
    {
        var (table, cell) = CellTable();
        var barcode = cell.Blocks.AddBarcode(
            BarcodeType.Code128,
            "RADZEN",
            Unit.FromPoint(120),
            Unit.FromPoint(40));
        var capture = new LayoutCaptureContext();

        var laid = LayOut(table, capture);

        Assert.Single(laid.CodeSymbols);
        Assert.Equal(capture.Source(barcode), laid.CodeSymbols[0].Source);
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
        list.Font.Family = TableLayoutSupport.Family;
        list.AddItem("Alpha");
        list.AddItem("Beta");
        list.AddItem("Gamma");

        var laid = LayOut(table);

        Assert.Equal(3, laid.Lines.Length);
    }

    [Fact]
    public void ListInCell_RowGrowsToFitAllItems()
    {
        var (table, cell) = CellTable();
        var fonts = TableLayoutSupport.Fonts();
        var list = cell.Blocks.AddList(ListStyle.Bullet);
        list.Font.Family = TableLayoutSupport.Family;
        list.Font.Size = 12;
        list.AddItem("Alpha");
        list.AddItem("Beta");
        list.AddItem("Gamma");

        var layout = TableLayout.LayoutIsolated(table, 400, fonts);

        var lineHeight = TableLayoutSupport.LineHeight(fonts);
        var padding = cell.Padding.Point;
        Assert.Equal(3 * lineHeight + 2 * padding, layout.RowHeights[0], 3);
    }

    [Fact]
    public void ListInCell_EmitsMarkersAndItemTextWithHangingIndent()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        var cell = table.Rows.Add().Cells[0];
        var list = cell.Blocks.AddList(ListStyle.Number);
        list.HangingIndent = Unit.FromPoint(20);
        list.AddItem("Alpha");
        list.AddItem("Beta");

        var draws = TextDraws(document);

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
        var withoutBreakHeight = TableLayout.LayoutIsolated(table, 400, TableLayoutSupport.Fonts()).RowHeights[0];

        cell.Blocks.AddPageBreak();
        var laid = LayOut(table);

        Assert.Single(laid.Lines);
        Assert.Empty(laid.Images);
        Assert.Empty(laid.CodeSymbols);
        Assert.Empty(laid.Tables);
        Assert.Equal(withoutBreakHeight, TableLayout.LayoutIsolated(table, 400, TableLayoutSupport.Fonts()).RowHeights[0], 6);
    }

    private sealed class UnknownBlock : Block;

    [Fact]
    public void UnhandledBlockInCell_Throws()
    {
        var (table, cell) = CellTable();
        cell.Blocks.Add(new UnknownBlock());

        var exception = Assert.Throws<NotSupportedException>(() => TableLayout.LayoutIsolated(table, 400, TableLayoutSupport.Fonts()));
        Assert.Contains("UnknownBlock", exception.Message);
        Assert.Contains("reached layout before lowering", exception.Message);
    }

    [Fact]
    public void UnhandledBlockInSection_Throws()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.Blocks.Add(new UnknownBlock());

        var exception = Assert.Throws<NotSupportedException>(() => new DocumentRenderer().Render(document));
        Assert.Contains("UnknownBlock", exception.Message);
        Assert.Contains("reached layout before lowering", exception.Message);
    }

    [Fact]
    public void EveryBlockSubclass_IsHandledInsideATableCell()
    {
        var blockTypes = typeof(Block).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Block)) && !t.IsAbstract)
            .ToList();

        Assert.NotEmpty(blockTypes);

        foreach (var type in blockTypes)
        {
            if (type == typeof(TableOfContents))
            {
                continue;
            }

            var (table, cell) = CellTable();
            AddSample(cell, type);
            var exception = Record.Exception(() => TableLayout.LayoutIsolated(table, 400, TableLayoutSupport.Fonts()));
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
                list.Font.Family = TableLayoutSupport.Family;
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
                run.Font.Family = TableLayoutSupport.Family;
                break;
            default:
                Assert.Fail($"No cell sample for block type '{type.Name}'. Wire it into TableLayout.LayoutContent (and Paginator) and add a sample here.");
                break;
        }
    }

    [Fact]
    public void ListInHeaderBand_EmitsMarkersAndItemText()
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "body", "Helvetica");
        var list = section.Header.Blocks.AddList(ListStyle.Number);
        list.AddItem("Alpha");
        list.AddItem("Beta");

        var draws = TextDraws(document);

        Assert.Contains(draws, d => d.Text == "1.");
        Assert.Contains(draws, d => d.Text == "2.");
        Assert.Contains(draws, d => d.Text == "Alpha");
        Assert.Contains(draws, d => d.Text == "Beta");
    }

    [Fact]
    public void UnhandledBlockInBand_Throws()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "body", BuildTestSupport.Latin);
        section.Footer.Blocks.Add(new UnknownBlock());

        var exception = Assert.Throws<NotSupportedException>(() => new DocumentRenderer().Render(document));
        Assert.Contains("UnknownBlock", exception.Message);
        Assert.Contains("reached layout before lowering", exception.Message);
    }

    private static List<(double X, string Text)> TextDraws(Document document)
    {
        var reader = BuildTestSupport.Read(document);
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
