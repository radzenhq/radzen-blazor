using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class XlsxReaderSheetSizeTests
{
    private static MemoryStream Save(Workbook wb)
    {
        var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void RoundTrip_SheetTallerThan100Rows_PreservesGridSizeAndValue()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Tall", 200, 10);
        sheet.Cells[149, 0].Value = "row150";

        using var ms = Save(wb);

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal(200, loaded.RowCount);
        Assert.Equal(10, loaded.ColumnCount);
        Assert.Equal("row150", loaded.Cells[149, 0].Value);
    }

    [Fact]
    public void RoundTrip_SheetWiderThan100Columns_PreservesGridSizeAndValue()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Wide", 10, 130);
        sheet.Cells[0, 119].Value = "col120";

        using var ms = Save(wb);

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal(10, loaded.RowCount);
        Assert.Equal(130, loaded.ColumnCount);
        Assert.Equal("col120", loaded.Cells[0, 119].Value);
    }

    [Fact]
    public void RoundTrip_SmallSheet_PreservesGridSize()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Small", 5, 5);
        sheet.Cells[0, 0].Value = "hi";

        using var ms = Save(wb);

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal(5, loaded.RowCount);
        Assert.Equal(5, loaded.ColumnCount);
        Assert.Equal("hi", loaded.Cells[0, 0].Value);
    }

    [Fact]
    public void RoundTrip_DefaultSizedSheet_PreservesGridSize()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sheet1", 50, 26);
        sheet.Cells[0, 0].Value = "hi";

        using var ms = Save(wb);

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal(50, loaded.RowCount);
        Assert.Equal(26, loaded.ColumnCount);
    }

    [Fact]
    public void Load_SingleCellDimension_FallsBackToDefaultGrid()
    {
        var wb = new Workbook();
        wb.AddSheet("Sheet1", 40, 20);

        using var ms = Save(wb);

        using var rewritten = RewriteDimension(ms, "A1");

        var loaded = Workbook.LoadFromStream(rewritten).Sheets[0];

        Assert.Equal(100, loaded.RowCount);
        Assert.Equal(100, loaded.ColumnCount);
    }

    [Fact]
    public void Load_UnderstatedDimension_GrowsToFitCellContent()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sheet1", 200, 10);
        sheet.Cells[149, 0].Value = "row150";

        using var ms = Save(wb);

        using var rewritten = RewriteDimension(ms, "A1:B2");

        var loaded = Workbook.LoadFromStream(rewritten).Sheets[0];

        Assert.True(loaded.RowCount >= 150, $"RowCount {loaded.RowCount}");
        Assert.Equal("row150", loaded.Cells[149, 0].Value);
    }

    private static MemoryStream RewriteDimension(MemoryStream source, string dimensionRef)
    {
        var result = new MemoryStream();
        source.CopyTo(result);
        result.Position = 0;

        using (var archive = new ZipArchive(result, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = archive.GetEntry("xl/worksheets/sheet1.xml")!;
            XDocument doc;
            using (var stream = entry.Open())
            {
                doc = XDocument.Load(stream);
            }

            var ns = doc.Root!.Name.Namespace;
            doc.Descendants(ns + "dimension").Single().SetAttributeValue("ref", dimensionRef);

            entry.Delete();
            var newEntry = archive.CreateEntry("xl/worksheets/sheet1.xml");
            using var output = newEntry.Open();
            doc.Save(output);
        }

        result.Position = 0;
        return result;
    }
}
