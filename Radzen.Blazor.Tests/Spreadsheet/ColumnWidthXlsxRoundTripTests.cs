using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// Round-trip column sizing through XlsxWriter + XlsxReader: the default column width via
// sheetFormatPr and the per-font maximum digit width used by the width unit conversion.
public class ColumnWidthXlsxRoundTripTests
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

    private static XDocument ReadEntry(MemoryStream ms, string path)
    {
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: true);
        using var stream = zip.GetEntry(path)!.Open();
        var doc = XDocument.Load(stream);
        ms.Position = 0;
        return doc;
    }

    private static MemoryStream PatchNormalFont(MemoryStream ms, string fontName)
    {
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = zip.GetEntry("xl/styles.xml")!;
            XDocument doc;
            using (var stream = entry.Open())
            {
                doc = XDocument.Load(stream);
            }

            var font = doc.Descendants(Ns + "font").First();
            font.Element(Ns + "name")!.SetAttributeValue("val", fontName);

            entry.Delete();
            using var output = zip.CreateEntry("xl/styles.xml").Open();
            doc.Save(output);
        }

        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void Write_EmitsDefaultColWidth()
    {
        using var ms = Save(Build());

        var sheetFormatPr = ReadEntry(ms, "xl/worksheets/sheet1.xml").Descendants(Ns + "sheetFormatPr").Single();
        var value = double.Parse(sheetFormatPr.Attribute("defaultColWidth")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(100.0 / 7.5, value, 6);
    }

    [Fact]
    public void RoundTrip_PreservesDefaultColumnWidth()
    {
        using var ms = Save(Build());

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];
        Assert.True(Math.Abs(loaded.Columns.Size - 100) < 1, $"default width {loaded.Columns.Size}");
        Assert.True(Math.Abs(loaded.Columns[5] - 100) < 1);
    }

    [Fact]
    public void RoundTrip_PreservesExplicitWidth()
    {
        var wb = Build();
        wb.Sheets[0].Columns[1] = 240;

        using var ms = Save(wb);

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];
        Assert.True(Math.Abs(loaded.Columns[1] - 240) < 1, $"width {loaded.Columns[1]}");
    }

    [Fact]
    public void Read_CalibriNormalFont_UsesCalibriDigitWidth()
    {
        var wb = Build();
        wb.Sheets[0].Columns[1] = 240;

        using var ms = PatchNormalFont(Save(wb), "Calibri");

        // The stored width is 240 / 7.5 = 32 characters; with Calibri's 7px digit that is 224px.
        var loaded = Workbook.LoadFromStream(ms).Sheets[0];
        Assert.True(Math.Abs(loaded.Columns[1] - 224) < 1, $"width {loaded.Columns[1]}");
        Assert.True(Math.Abs(loaded.Columns.Size - 100.0 / 7.5 * 7) < 1, $"default {loaded.Columns.Size}");
    }

    [Fact]
    public void Read_NormalFontSize_ScalesDigitWidth()
    {
        var wb = Build();
        wb.Sheets[0].Columns[1] = 240;

        using var ms = Save(wb);

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = zip.GetEntry("xl/styles.xml")!;
            XDocument doc;
            using (var stream = entry.Open())
            {
                doc = XDocument.Load(stream);
            }

            doc.Descendants(Ns + "font").First().Element(Ns + "sz")!.SetAttributeValue("val", "22");

            entry.Delete();
            using var output = zip.CreateEntry("xl/styles.xml").Open();
            doc.Save(output);
        }
        ms.Position = 0;

        // Doubled font size doubles the digit width: 32 characters read back as 480px.
        var loaded = Workbook.LoadFromStream(ms).Sheets[0];
        Assert.True(Math.Abs(loaded.Columns[1] - 480) < 2, $"width {loaded.Columns[1]}");
    }
}
