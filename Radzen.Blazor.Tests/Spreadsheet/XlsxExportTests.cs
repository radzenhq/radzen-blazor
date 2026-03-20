using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class XlsxExportTests
{
    private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static XDocument GetSheetXml(Workbook workbook, int sheetIndex = 0)
    {
        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = archive.GetEntry($"xl/worksheets/sheet{sheetIndex + 1}.xml");
        Assert.NotNull(entry);

        using var entryStream = entry!.Open();
        return XDocument.Load(entryStream);
    }

    private static Workbook RoundTrip(Workbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;
        return Workbook.LoadFromStream(stream);
    }

    // List data validation formula format

    [Fact]
    public void Export_ListValidation_InlineItems_UseSingleQuotedFormat()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 0));
        sheet.Validation.Add(range, new DataValidationRule
        {
            Type = DataValidationType.List,
            Formula1 = "Yes,No,Maybe"
        });

        var doc = GetSheetXml(workbook);
        var formula1 = doc.Descendants(SpreadsheetNs + "dataValidation")
            .First()
            .Element(SpreadsheetNs + "formula1");

        Assert.NotNull(formula1);
        // Must be "Yes,No,Maybe" (single pair of quotes), not "Yes","No","Maybe"
        Assert.Equal("\"Yes,No,Maybe\"", formula1!.Value);
    }

    [Fact]
    public void Export_ListValidation_CellRangeFormula_NotQuoted()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 0));
        sheet.Validation.Add(range, new DataValidationRule
        {
            Type = DataValidationType.List,
            Formula1 = "=$A$1:$A$3"
        });

        var doc = GetSheetXml(workbook);
        var formula1 = doc.Descendants(SpreadsheetNs + "dataValidation")
            .First()
            .Element(SpreadsheetNs + "formula1");

        Assert.NotNull(formula1);
        Assert.Equal("=$A$1:$A$3", formula1!.Value);
    }

    [Fact]
    public void RoundTrip_ListValidation_PreservesItems()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 0));
        sheet.Validation.Add(range, new DataValidationRule
        {
            Type = DataValidationType.List,
            Formula1 = "Red,Green,Blue"
        });

        var loaded = RoundTrip(workbook);
        var validators = loaded.Sheets[0].Validation.GetValidators(range);
        var rule = Assert.IsType<DataValidationRule>(validators[0]);
        Assert.Equal("Red,Green,Blue", rule.Formula1);
    }

    // Frozen pane activePane value

    [Fact]
    public void Export_FrozenBothAxes_ActivePaneIsBottomRight()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        sheet.Columns.Frozen = 2;
        sheet.Rows.Frozen = 3;

        var doc = GetSheetXml(workbook);
        var pane = doc.Descendants(SpreadsheetNs + "pane").FirstOrDefault();

        Assert.NotNull(pane);
        Assert.Equal("bottomRight", pane!.Attribute("activePane")?.Value);
        Assert.Equal("2", pane.Attribute("xSplit")?.Value);
        Assert.Equal("3", pane.Attribute("ySplit")?.Value);
    }

    [Fact]
    public void Export_FrozenColumnsOnly_ActivePaneIsTopRight()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        sheet.Columns.Frozen = 2;

        var doc = GetSheetXml(workbook);
        var pane = doc.Descendants(SpreadsheetNs + "pane").FirstOrDefault();

        Assert.NotNull(pane);
        Assert.Equal("topRight", pane!.Attribute("activePane")?.Value);
        Assert.Equal("2", pane.Attribute("xSplit")?.Value);
        Assert.Null(pane.Attribute("ySplit"));
    }

    [Fact]
    public void Export_FrozenRowsOnly_ActivePaneIsBottomLeft()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        sheet.Rows.Frozen = 3;

        var doc = GetSheetXml(workbook);
        var pane = doc.Descendants(SpreadsheetNs + "pane").FirstOrDefault();

        Assert.NotNull(pane);
        Assert.Equal("bottomLeft", pane!.Attribute("activePane")?.Value);
        Assert.Null(pane.Attribute("xSplit"));
        Assert.Equal("3", pane.Attribute("ySplit")?.Value);
    }

    [Fact]
    public void Export_NoFrozenPanes_NoPaneElement()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 10, 10);

        var doc = GetSheetXml(workbook);
        var pane = doc.Descendants(SpreadsheetNs + "pane").FirstOrDefault();

        Assert.Null(pane);
    }

    [Fact]
    public void RoundTrip_FrozenPanes_Preserved()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        sheet.Columns.Frozen = 2;
        sheet.Rows.Frozen = 3;

        var loaded = RoundTrip(workbook);
        Assert.Equal(2, loaded.Sheets[0].Columns.Frozen);
        Assert.Equal(3, loaded.Sheets[0].Rows.Frozen);
    }

    // Column width always present

    [Fact]
    public void Export_Columns_AlwaysHaveWidthAttribute()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 5, 5);

        var doc = GetSheetXml(workbook);
        var cols = doc.Descendants(SpreadsheetNs + "col").ToList();

        Assert.Equal(5, cols.Count);
        foreach (var col in cols)
        {
            Assert.NotNull(col.Attribute("width"));
        }
    }

    [Fact]
    public void Export_CustomColumnWidth_HasCustomWidthAttribute()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 5, 5);
        sheet.Columns[0] = 200;

        var doc = GetSheetXml(workbook);
        var firstCol = doc.Descendants(SpreadsheetNs + "col").First();

        Assert.NotNull(firstCol.Attribute("width"));
        Assert.Equal("1", firstCol.Attribute("customWidth")?.Value);
    }

    [Fact]
    public void Export_DefaultColumnWidth_NoCustomWidthAttribute()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 5, 5);

        var doc = GetSheetXml(workbook);
        var firstCol = doc.Descendants(SpreadsheetNs + "col").First();

        Assert.NotNull(firstCol.Attribute("width"));
        Assert.Null(firstCol.Attribute("customWidth"));
    }

    // sheetFormatPr element present

    [Fact]
    public void Export_SheetFormatPr_Present()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 5, 5);

        var doc = GetSheetXml(workbook);
        var sheetFormatPr = doc.Descendants(SpreadsheetNs + "sheetFormatPr").FirstOrDefault();

        Assert.NotNull(sheetFormatPr);
        Assert.Equal("15", sheetFormatPr!.Attribute("defaultRowHeight")?.Value);
    }

    [Fact]
    public void Export_ElementOrder_SheetFormatPrAfterSheetViewsBeforeCols()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 5, 5);

        var doc = GetSheetXml(workbook);
        var worksheet = doc.Root!;
        var children = worksheet.Elements().Select(e => e.Name.LocalName).ToList();

        var sheetViewsIndex = children.IndexOf("sheetViews");
        var sheetFormatPrIndex = children.IndexOf("sheetFormatPr");
        var colsIndex = children.IndexOf("cols");

        Assert.True(sheetViewsIndex < sheetFormatPrIndex, "sheetFormatPr must come after sheetViews");
        Assert.True(sheetFormatPrIndex < colsIndex, "sheetFormatPr must come before cols");
    }

    // Empty rows not emitted

    [Fact]
    public void Export_EmptyRows_NotIncludedInSheetData()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        sheet.Cells[0, 0].Value = "Hello";
        sheet.Cells[4, 0].Value = "World";

        var doc = GetSheetXml(workbook);
        var rows = doc.Descendants(SpreadsheetNs + "row").ToList();

        // Only rows 1 and 5 should be present (0-indexed rows 0 and 4)
        Assert.Equal(2, rows.Count);
        Assert.Equal("1", rows[0].Attribute("r")?.Value);
        Assert.Equal("5", rows[1].Attribute("r")?.Value);
    }

    [Fact]
    public void Export_RowWithCustomHeight_IncludedEvenIfEmpty()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        sheet.Cells[0, 0].Value = "Hello";
        sheet.Rows[3] = sheet.Rows.Size + 10; // Custom height on row 4

        var doc = GetSheetXml(workbook);
        var rows = doc.Descendants(SpreadsheetNs + "row").ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("1", rows[0].Attribute("r")?.Value);
        Assert.Equal("4", rows[1].Attribute("r")?.Value);
    }
}
