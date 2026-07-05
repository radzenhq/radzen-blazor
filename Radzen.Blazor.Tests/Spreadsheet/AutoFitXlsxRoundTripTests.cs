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
    public void Read_BestFitRangeBeyondSheet_IsCapped()
    {
        using var ms = PatchSheetXml(Save(Build()), """<cols><col min="1" max="16384" width="12" bestFit="1" customWidth="1"/></cols>""");

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];
        Assert.True(loaded.Columns.IsAutoFit(0));
        Assert.True(loaded.Columns.IsAutoFit(loaded.Columns.Count - 1));
    }
}
