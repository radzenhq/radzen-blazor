using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Tests.Spreadsheet;

public class XlsxRightToLeftTests
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static XElement SheetView(Workbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        using var entry = archive.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        return XDocument.Load(entry).Descendants(Main + "sheetView").Single();
    }

    private static Workbook RoundTrip(Workbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;
        return Workbook.LoadFromStream(stream);
    }

    [Fact]
    public void Save_WritesRightToLeftOnTheSheetView()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 1, 1);
        sheet.RightToLeft = true;

        Assert.Equal("1", (string?)SheetView(workbook).Attribute("rightToLeft"));
    }

    [Fact]
    public void Save_OmitsRightToLeftForALeftToRightSheet()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 1, 1);

        Assert.Null(SheetView(workbook).Attribute("rightToLeft"));
    }

    [Fact]
    public void Load_ReadsRightToLeftWithFrozenPanes()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 5, 5);
        sheet.RightToLeft = true;
        sheet.Rows.Frozen = 1;

        var loaded = RoundTrip(workbook).Sheets[0];

        Assert.True(loaded.RightToLeft);
        Assert.Equal(1, loaded.Rows.Frozen);
    }

    [Fact]
    public void Load_LeavesALeftToRightSheetLeftToRight()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 1, 1);

        Assert.False(RoundTrip(workbook).Sheets[0].RightToLeft);
    }
}
