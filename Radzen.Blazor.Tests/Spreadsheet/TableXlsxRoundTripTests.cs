using System.IO;
using System.IO.Compression;
using System.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// Round-trip a workbook with Table metadata through XlsxWriter + XlsxReader.
// Each test saves to a MemoryStream, reloads, and asserts the Table comes back intact.
public class TableXlsxRoundTripTests
{
    private static (Workbook wb, Worksheet ws) Build(string tableName = "Sales", bool hasHeaders = true,
                                                     int rows = 6, int cols = 5)
    {
        var wb = new Workbook();
        // Allocate a few extra rows so toggling ShowTotalsRow on later (which expands
        // the table range) doesn't run off the bottom of the worksheet.
        var ws = wb.AddSheet("Sheet1", rows + 5, cols);
        if (hasHeaders)
        {
            ws.Cells[0, 0].SetValue("Region");
            ws.Cells[0, 1].SetValue("Product");
            ws.Cells[0, 2].SetValue("Q1");
            ws.Cells[0, 3].SetValue("Q2");
            ws.Cells[0, 4].SetValue("Total");
        }
        for (var r = (hasHeaders ? 1 : 0); r < rows - 1; r++)
        {
            ws.Cells[r, 0].SetValue($"R{r}");
            ws.Cells[r, 1].SetValue($"P{r}");
            ws.Cells[r, 2].Value = (double)(r * 100);
            ws.Cells[r, 3].Value = (double)(r * 200);
        }
        ws.AddTable(tableName, new RangeRef(new CellRef(0, 0), new CellRef(rows - 1, cols - 1)), hasHeaders);
        return (wb, ws);
    }

    private static Workbook Roundtrip(Workbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;
        return Workbook.LoadFromStream(ms);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTableExistenceAndRange()
    {
        var (wb, _) = Build();
        var loaded = Roundtrip(wb).Sheets[0];
        Assert.Single(loaded.Tables);
        var t = loaded.Tables[0];
        Assert.Equal(new RangeRef(new CellRef(0, 0), new CellRef(5, 4)), t.Range);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveNameAndDisplayName()
    {
        var (wb, ws) = Build("Sales");
        ws.Tables[0].DisplayName = "Sales (Q1-Q2)";
        var t = Roundtrip(wb).Sheets[0].Tables[0];
        Assert.Equal("Sales", t.Name);
        Assert.Equal("Sales (Q1-Q2)", t.DisplayName);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveHeaderRowToggle()
    {
        var (wb, _) = Build("T", hasHeaders: false);
        var t = Roundtrip(wb).Sheets[0].Tables[0];
        Assert.False(t.ShowHeaderRow);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTotalsRowToggleAndCalculation()
    {
        var (wb, ws) = Build("Sales");
        var t = ws.Tables[0];
        t.ShowTotalsRow = true;
        t.Columns[2].TotalsCalculation = TotalsCalculation.Sum;
        t.Columns[3].TotalsCalculation = TotalsCalculation.Average;

        var loaded = Roundtrip(wb).Sheets[0].Tables[0];
        Assert.True(loaded.ShowTotalsRow);
        Assert.Equal(TotalsCalculation.Sum, loaded.Columns[2].TotalsCalculation);
        Assert.Equal(TotalsCalculation.Average, loaded.Columns[3].TotalsCalculation);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveStyleAndStripingToggles()
    {
        var (wb, ws) = Build("T");
        var t = ws.Tables[0];
        t.TableStyle = "TableStyleLight15";
        t.ShowBandedRows = false;
        t.ShowBandedColumns = true;
        t.HighlightFirstColumn = true;
        t.HighlightLastColumn = true;

        var loaded = Roundtrip(wb).Sheets[0].Tables[0];
        Assert.Equal("TableStyleLight15", loaded.TableStyle);
        Assert.False(loaded.ShowBandedRows);
        Assert.True(loaded.ShowBandedColumns);
        Assert.True(loaded.HighlightFirstColumn);
        Assert.True(loaded.HighlightLastColumn);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveColumnNames_IncludingRenames()
    {
        var (wb, ws) = Build("T");
        var t = ws.Tables[0];
        t.Columns[2].Name = "First Quarter";

        var loaded = Roundtrip(wb).Sheets[0].Tables[0];
        // header cell still says "Q1" but the column is renamed via XLSX metadata
        Assert.Equal("First Quarter", loaded.Columns[2].Name);
        Assert.Equal("Region", loaded.Columns[0].Name);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveFilterButtonToggle()
    {
        var (wb, ws) = Build("T");
        ws.Tables[0].ShowFilterButton = false;
        var t = Roundtrip(wb).Sheets[0].Tables[0];
        Assert.False(t.ShowFilterButton);
    }

    [Fact]
    public void Package_ShouldContainTablePart_InContentTypesAndArchive()
    {
        var (wb, _) = Build();
        using var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;

        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        Assert.Contains(zip.Entries, e => e.FullName == "xl/tables/table1.xml");

        // Confirm Content_Types Override is present
        var ct = zip.GetEntry("[Content_Types].xml")!;
        using var s = ct.Open();
        var ctText = new StreamReader(s).ReadToEnd();
        Assert.Contains("/xl/tables/table1.xml", ctText, System.StringComparison.Ordinal);
        Assert.Contains("application/vnd.openxmlformats-officedocument.spreadsheetml.table+xml",
            ctText, System.StringComparison.Ordinal);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveMultipleTables()
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("Sheet1", 20, 10);
        ws.AddTable("Sales", new RangeRef(new CellRef(0, 0), new CellRef(5, 4)));
        ws.AddTable("Inventory", new RangeRef(new CellRef(0, 5), new CellRef(5, 9)));

        var loaded = Roundtrip(wb).Sheets[0];
        Assert.Equal(2, loaded.Tables.Count);
        Assert.Contains(loaded.Tables, t => t.Name == "Sales");
        Assert.Contains(loaded.Tables, t => t.Name == "Inventory");
    }
}
