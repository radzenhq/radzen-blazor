using System;
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

public class XlsxWriterCellFormatTests
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static Dictionary<string, XDocument> Save(Workbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;

        var parts = new Dictionary<string, XDocument>();
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

        foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".xml", StringComparison.Ordinal)))
        {
            using var part = entry.Open();
            parts[entry.FullName] = XDocument.Load(part);
        }

        return parts;
    }

    private static XElement CellFormat(Dictionary<string, XDocument> parts, string reference)
    {
        var cell = parts["xl/worksheets/sheet1.xml"].Descendants(Main + "c")
            .Single(c => (string?)c.Attribute("r") == reference);

        var index = int.Parse((string)cell.Attribute("s")!, CultureInfo.InvariantCulture);

        return parts["xl/styles.xml"].Root!.Element(Main + "cellXfs")!
            .Elements(Main + "xf").ElementAt(index);
    }

    [Fact]
    public void Save_DoesNotGiveAnUnformattedCellAFormat()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 4, 1);

        sheet.Cells[0, 0].SetValue("text");
        sheet.Cells[1, 0].Value = 42;
        sheet.Cells[2, 0].Value = true;

        sheet.Cells[3, 0].Value = new DateTime(2020, 1, 1);

        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);

        for (var row = 0; row < 4; row++)
        {
            Assert.Null(sheet.Cells[row, 0].FormatOrNull);
        }
    }

    [Fact]
    public void Save_KeepsAFormatTheCellWasGiven()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 1, 1);

        sheet.Cells[0, 0].Value = 42;
        sheet.Cells[0, 0].Format.Bold = true;

        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);

        Assert.True(sheet.Cells[0, 0].FormatOrNull?.Bold);
    }

    [Fact]
    public void Save_StylesADateWithNoFormatAsADate()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 1, 1);

        sheet.Cells[0, 0].Value = new DateTime(2020, 1, 1);

        var xf = CellFormat(Save(workbook), "A1");

        Assert.Equal("14", (string?)xf.Attribute("numFmtId"));
        Assert.Equal("1", (string?)xf.Attribute("applyNumberFormat"));
    }

    [Fact]
    public void Save_MarksAQuotePrefixedCellWithNoFormat()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 1, 1);

        sheet.Cells[0, 0].SetValue("'4.00E+003");

        var xf = CellFormat(Save(workbook), "A1");

        Assert.Equal("1", (string?)xf.Attribute("quotePrefix"));
        Assert.Null(xf.Attribute("applyQuotePrefix"));
    }
}
