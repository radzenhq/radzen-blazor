using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class XlsxWriterSheetOrderTests
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static XDocument Part(Workbook workbook, string name)
    {
        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;

        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        using var part = zip.GetEntry(name)!.Open();

        return XDocument.Load(part);
    }

    private static Workbook FilledBackwards()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 4, 4);

        for (var row = 2; row >= 0; row--)
        {
            for (var column = 2; column >= 0; column--)
            {
                sheet.Cells[row, column].SetValue($"r{row}c{column}");
            }
        }

        return workbook;
    }

    [Fact]
    public void Save_WritesRowsAndCellsInAscendingOrder()
    {
        var sheetData = Part(FilledBackwards(), "xl/worksheets/sheet1.xml")
            .Descendants(Main + "sheetData").Single();

        var rows = sheetData.Elements(Main + "row").ToList();

        Assert.Equal(new[] { "1", "2", "3" }, rows.Select(r => (string?)r.Attribute("r")));

        foreach (var row in rows)
        {
            var references = row.Elements(Main + "c").Select(c => (string?)c.Attribute("r")).ToList();

            Assert.Equal(references.OrderBy(r => r, System.StringComparer.Ordinal), references);
        }
    }

    [Fact]
    public void Save_MergesPlaceholdersAndStyledRowsIntoTheCellOrder()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 8, 8);

        sheet.Cells[2, 5].SetValue("right");
        sheet.Cells[2, 0].SetValue("left");
        sheet.Cells[6, 1].SetValue("last");

        sheet.MergedCells.Add(new RangeRef(new CellRef(2, 3), new CellRef(2, 6)));

        sheet.Rows[4] = 40.0;
        sheet.Rows.Hide(4);
        sheet.Rows.Hide(7);

        var sheetData = Part(workbook, "xl/worksheets/sheet1.xml")
            .Descendants(Main + "sheetData").Single();

        var rows = sheetData.Elements(Main + "row").ToList();

        Assert.Equal(new[] { "3", "5", "7", "8" }, rows.Select(r => (string?)r.Attribute("r")));

        var merged = rows[0];

        Assert.Equal(
            new[] { "A3", "E3", "F3", "G3" },
            merged.Elements(Main + "c").Select(c => (string?)c.Attribute("r")));

        Assert.Equal("1:7", (string?)merged.Attribute("spans"));

        Assert.NotNull(merged.Elements(Main + "c")
            .Single(c => (string?)c.Attribute("r") == "F3").Element(Main + "v"));

        Assert.All(
            merged.Elements(Main + "c").Where(c => (string?)c.Attribute("r") is "E3" or "G3"),
            c => Assert.Empty(c.Elements()));

        var styled = rows[1];

        Assert.Equal("1", (string?)styled.Attribute("customHeight"));
        Assert.Equal("1", (string?)styled.Attribute("hidden"));
        Assert.Null(styled.Attribute("spans"));
        Assert.Empty(styled.Elements());
    }

    [Fact]
    public void Save_NumbersCellStylesInTheOrderTheCellsAreWritten()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 4, 4);

        sheet.Cells[2, 0].Value = 3;
        sheet.Cells[2, 0].Format.Italic = true;

        sheet.Cells[0, 0].Value = 1;
        sheet.Cells[0, 0].Format.Bold = true;

        var cells = Part(workbook, "xl/worksheets/sheet1.xml").Descendants(Main + "c")
            .ToDictionary(c => (string)c.Attribute("r")!, c => int.Parse((string)c.Attribute("s")!, CultureInfo.InvariantCulture));

        Assert.True(cells["A1"] < cells["A3"]);

        var fonts = Part(workbook, "xl/styles.xml").Root!.Element(Main + "fonts")!.Elements(Main + "font").ToList();
        var xfs = Part(workbook, "xl/styles.xml").Root!.Element(Main + "cellXfs")!.Elements(Main + "xf").ToList();

        int FontOf(string reference) =>
            int.Parse((string)xfs[cells[reference]].Attribute("fontId")!, CultureInfo.InvariantCulture);

        Assert.NotNull(fonts[FontOf("A1")].Element(Main + "b"));
        Assert.NotNull(fonts[FontOf("A3")].Element(Main + "i"));
    }

    [Fact]
    public void Save_NumbersAMergeAnchorsStyleWhereTheAnchorIsWritten()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 4, 4);

        sheet.Cells[2, 0].Value = 3;
        sheet.Cells[2, 0].Format.Italic = true;
        sheet.MergedCells.Add(new RangeRef(new CellRef(2, 0), new CellRef(2, 1)));

        sheet.Cells[0, 0].Value = 1;
        sheet.Cells[0, 0].Format.Bold = true;

        var cells = Part(workbook, "xl/worksheets/sheet1.xml").Descendants(Main + "c")
            .ToDictionary(c => (string)c.Attribute("r")!, c => int.Parse((string)c.Attribute("s")!, CultureInfo.InvariantCulture));

        Assert.True(cells["A1"] < cells["A3"]);
        Assert.Equal(cells["A3"], cells["B3"]);
    }

    [Fact]
    public void Save_WritesAFormattedEmptyMergeAnchorInTheStyleItsPlaceholdersShare()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 4, 4);

        sheet.Cells[2, 0].Format.Italic = true;
        sheet.MergedCells.Add(new RangeRef(new CellRef(2, 0), new CellRef(2, 2)));

        sheet.Cells[0, 0].Value = 1;
        sheet.Cells[0, 0].Format.Bold = true;

        var cells = Part(workbook, "xl/worksheets/sheet1.xml").Descendants(Main + "c")
            .ToDictionary(c => (string)c.Attribute("r")!, c => int.Parse((string)c.Attribute("s")!, CultureInfo.InvariantCulture));

        Assert.True(cells["A1"] < cells["A3"]);
        Assert.Equal(cells["A3"], cells["B3"]);
        Assert.Equal(cells["B3"], cells["C3"]);
    }

    [Fact]
    public void Save_WritesAMergeThatReachesPastTheSheetBounds()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 1);

        sheet.Cells[9, 0].SetValue("last");
        sheet.MergedCells.Add(new RangeRef(new CellRef(0, 0), new CellRef(9, 0)));

        sheet.Rows.Count = 5;

        var references = Part(workbook, "xl/worksheets/sheet1.xml").Descendants(Main + "c")
            .Select(c => (string?)c.Attribute("r"))
            .ToList();

        Assert.Equal(new[] { "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "A10" }, references);
    }

    [Fact]
    public void Save_DoesNotAddACellForAnEmptyMergeAnchor()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 4, 4);

        sheet.MergedCells.Add(new RangeRef(new CellRef(1, 1), new CellRef(2, 2)));

        Part(workbook, "xl/worksheets/sheet1.xml");

        Assert.Equal(0, sheet.Cells.PopulatedCount);
    }

    [Fact]
    public void Save_StylesAMergeWhoseAnchorIsPastTheSheetBounds()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 1);

        sheet.Cells[7, 0].SetValue("anchor");
        sheet.Cells[7, 0].Format.Bold = true;
        sheet.MergedCells.Add(new RangeRef(new CellRef(7, 0), new CellRef(9, 0)));

        sheet.Rows.Count = 5;

        var cells = Part(workbook, "xl/worksheets/sheet1.xml").Descendants(Main + "c")
            .ToDictionary(c => (string)c.Attribute("r")!, c => (string?)c.Attribute("s"));

        Assert.Equal(new[] { "A8", "A9", "A10" }, cells.Keys);
        Assert.NotNull(cells["A8"]);
        Assert.Equal(cells["A8"], cells["A9"]);
        Assert.Equal(cells["A8"], cells["A10"]);
    }

    [Fact]
    public void Save_WritesAnAddressTwoMergesCoverOnceWithTheFirstMergesStyle()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 8, 24);

        sheet.Cells[0, 0].SetValue("first");
        sheet.Cells[0, 0].Format.Bold = true;
        sheet.MergedCells.Add(new RangeRef(new CellRef(0, 0), new CellRef(1, 20)));

        sheet.Cells[1, 1].SetValue("second");
        sheet.Cells[1, 1].Format.Italic = true;
        sheet.MergedCells.Add(new RangeRef(new CellRef(1, 1), new CellRef(2, 3)));

        var cells = Part(workbook, "xl/worksheets/sheet1.xml").Descendants(Main + "c").ToList();
        var references = cells.Select(c => (string?)c.Attribute("r")).ToList();

        Assert.Equal(references.Distinct().Count(), references.Count);

        var styles = cells.ToDictionary(c => (string)c.Attribute("r")!, c => (string?)c.Attribute("s"));

        Assert.Equal(styles["A1"], styles["C2"]);
        Assert.Equal(styles["A1"], styles["D2"]);
        Assert.Equal(styles["B2"], styles["C3"]);
        Assert.NotEqual(styles["A1"], styles["B2"]);
    }

    [Fact]
    public void Save_SpansARowFromAPlaceholderBeforeItsFirstCell()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 8, 8);

        sheet.MergedCells.Add(new RangeRef(new CellRef(4, 0), new CellRef(4, 1)));
        sheet.Cells[4, 3].SetValue("cell");

        var row = Part(workbook, "xl/worksheets/sheet1.xml").Descendants(Main + "row").Single();

        Assert.Equal("5", (string?)row.Attribute("r"));
        Assert.Equal("2:4", (string?)row.Attribute("spans"));
        Assert.Equal(new[] { "B5", "D5" }, row.Elements(Main + "c").Select(c => (string?)c.Attribute("r")));
    }

    [Fact]
    public void Save_SpansARowThatHoldsOnlyPlaceholders()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 8, 8);

        sheet.MergedCells.Add(new RangeRef(new CellRef(0, 0), new CellRef(1, 2)));

        var rows = Part(workbook, "xl/worksheets/sheet1.xml").Descendants(Main + "row").ToList();

        Assert.Equal(new[] { "1", "2" }, rows.Select(r => (string?)r.Attribute("r")));
        Assert.Equal("2:3", (string?)rows[0].Attribute("spans"));
        Assert.Equal("1:3", (string?)rows[1].Attribute("spans"));
        Assert.Equal(new[] { "A2", "B2", "C2" }, rows[1].Elements(Main + "c").Select(c => (string?)c.Attribute("r")));
    }

    [Fact]
    public void Save_IndexesSharedStringsInTheOrderTheCellsAreWritten()
    {
        var strings = Part(FilledBackwards(), "xl/sharedStrings.xml")
            .Root!.Elements(Main + "si")
            .Select(si => si.Element(Main + "t")!.Value);

        Assert.Equal(new[] { "r0c0", "r0c1", "r0c2", "r1c0", "r1c1", "r1c2", "r2c0", "r2c1", "r2c2" }, strings);
    }
}
