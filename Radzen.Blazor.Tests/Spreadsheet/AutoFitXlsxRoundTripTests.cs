using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// Round-trip the bestFit (auto fit) column flag through XlsxWriter + XlsxReader.
public class AutoFitXlsxRoundTripTests
{
    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static Workbook Build()
    {
        var wb = new Workbook();
        wb.AddSheet("Sheet1", 10, 10);
        return wb;
    }

    private static MemoryStream Save(Workbook wb)
    {
        var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;
        return ms;
    }

    private static XDocument ReadSheetXml(MemoryStream ms)
    {
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: true);
        using var stream = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        var doc = XDocument.Load(stream);
        ms.Position = 0;
        return doc;
    }

    private static MemoryStream PatchSheetXml(MemoryStream ms, string colsXml)
    {
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = zip.GetEntry("xl/worksheets/sheet1.xml")!;
            XDocument doc;
            using (var stream = entry.Open())
            {
                doc = XDocument.Load(stream);
            }

            doc.Descendants(Ns + "cols").Remove();
            var cols = XElement.Parse(colsXml);
            foreach (var element in cols.DescendantsAndSelf())
            {
                element.Name = Ns + element.Name.LocalName;
            }
            doc.Root!.Element(Ns + "sheetData")!.AddBeforeSelf(cols);

            entry.Delete();
            var newEntry = zip.CreateEntry("xl/worksheets/sheet1.xml");
            using var output = newEntry.Open();
            doc.Save(output);
        }

        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void RoundTrip_PreservesWidthAndBestFit()
    {
        var wb = Build();
        wb.Sheets[0].Columns[1] = 240;
        wb.Sheets[0].Columns.SetAutoFit(1);

        using var ms = Save(wb);

        var col = ReadSheetXml(ms).Descendants(Ns + "col").Single();
        Assert.Equal("2", col.Attribute("min")?.Value);
        Assert.Equal("1", col.Attribute("bestFit")?.Value);
        Assert.Equal("1", col.Attribute("customWidth")?.Value);
        Assert.NotNull(col.Attribute("width"));

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];
        Assert.True(System.Math.Abs(loaded.Columns[1] - 240) < 0.5);
        Assert.True(loaded.Columns.IsAutoFit(1));
        Assert.False(loaded.Columns.IsAutoFit(0));
    }

    [Fact]
    public void Write_AutoFitAtDefaultWidth_StillEmitsColumn()
    {
        var wb = Build();
        wb.Sheets[0].Columns.SetAutoFit(2);

        using var ms = Save(wb);

        var col = ReadSheetXml(ms).Descendants(Ns + "col").Single();
        Assert.Equal("3", col.Attribute("min")?.Value);
        Assert.Equal("1", col.Attribute("bestFit")?.Value);

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];
        Assert.True(loaded.Columns.IsAutoFit(2));
    }

    [Fact]
    public void Read_BestFitWithoutWidth_SetsFlagAndKeepsDefaultWidth()
    {
        using var ms = PatchSheetXml(Save(Build()), """<cols><col min="2" max="3" bestFit="1"/></cols>""");

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];
        Assert.Equal(loaded.Columns.Size, loaded.Columns[1]);
        Assert.True(loaded.Columns.IsAutoFit(1));
        Assert.True(loaded.Columns.IsAutoFit(2));
        Assert.False(loaded.Columns.IsAutoFit(3));
    }

    [Fact]
    public void Write_AutoFitColumn_WidensToFitContentWithExcelMetrics()
    {
        var wb = Build();
        var sheet = wb.Sheets[0];
        sheet.Cells[0, 0].Value = 157400d;
        sheet.Cells[0, 0].Format.NumberFormat = "$#,##0.00";
        sheet.Cells[0, 0].Format.Bold = true;
        sheet.Columns[0] = 78; // snug browser-font fit, too narrow for Excel's font
        sheet.Columns.SetAutoFit(0);

        using var ms = Save(wb);

        var col = ReadSheetXml(ms).Descendants(Ns + "col").Single();
        var width = double.Parse(col.Attribute("width")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        // "$157,400.00" needs 76.7px + 5px padding in Aptos Narrow 11
        Assert.True(width * 7.5 > 81, $"exported width {width * 7.5}px");

        // the model width is not changed by the export
        Assert.Equal(78, sheet.Columns[0]);
    }

    [Fact]
    public void Write_AutoFitColumn_KeepsWiderModelWidth()
    {
        var wb = Build();
        var sheet = wb.Sheets[0];
        sheet.Cells[0, 0].Value = "abc";
        sheet.Columns[0] = 200;
        sheet.Columns.SetAutoFit(0);

        using var ms = Save(wb);

        var col = ReadSheetXml(ms).Descendants(Ns + "col").Single();
        var width = double.Parse(col.Attribute("width")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(200 / 7.5, width, 3);
    }

    [Fact]
    public void Write_AutoFitColumn_SizesWideCharactersAtDoubleDigitWidth()
    {
        var wb = Build();
        var sheet = wb.Sheets[0];
        sheet.Cells[0, 0].Value = "漢字とかな"; // 5 wide glyphs = 5 * 15.25px + padding
        sheet.Columns[0] = 40;
        sheet.Columns.SetAutoFit(0);

        using var ms = Save(wb);

        var col = ReadSheetXml(ms).Descendants(Ns + "col").Single();
        var width = double.Parse(col.Attribute("width")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(width * 7.5 > 80, $"exported width {width * 7.5}px");
    }

    [Fact]
    public void Write_NonAutoFitColumn_KeepsModelWidth()
    {
        var wb = Build();
        var sheet = wb.Sheets[0];
        sheet.Cells[0, 0].Value = 157400d;
        sheet.Cells[0, 0].Format.NumberFormat = "$#,##0.00";
        sheet.Columns[0] = 78;

        using var ms = Save(wb);

        var col = ReadSheetXml(ms).Descendants(Ns + "col").Single();
        var width = double.Parse(col.Attribute("width")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(78 / 7.5, width, 3);
    }

    [Fact]
    public void Read_BestFitRangeBeyondSheet_IsCapped()
    {
        using var ms = PatchSheetXml(Save(Build()), """<cols><col min="1" max="16384" width="12" bestFit="1" customWidth="1"/></cols>""");

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];
        Assert.True(loaded.Columns.IsAutoFit(0));
        Assert.True(loaded.Columns.IsAutoFit(loaded.Columns.Count - 1));
    }
}
