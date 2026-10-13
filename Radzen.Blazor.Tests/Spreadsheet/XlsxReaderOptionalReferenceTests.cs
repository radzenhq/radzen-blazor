using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class XlsxReaderOptionalReferenceTests
{
    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static MemoryStream BuildXlsx(string sheetDataXml, bool absoluteSheetTarget = false)
    {
        var wb = new Workbook();
        wb.AddSheet("Sheet1", 20, 10);

        var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = zip.GetEntry("xl/worksheets/sheet1.xml")!;
            XDocument doc;
            using (var stream = entry.Open())
            {
                doc = XDocument.Load(stream);
            }

            var sheetData = XElement.Parse(sheetDataXml);
            foreach (var element in sheetData.DescendantsAndSelf())
            {
                element.Name = Ns + element.Name.LocalName;
            }
            doc.Root!.Element(Ns + "sheetData")!.ReplaceWith(sheetData);

            entry.Delete();
            using (var output = zip.CreateEntry("xl/worksheets/sheet1.xml").Open())
            {
                doc.Save(output);
            }

            if (absoluteSheetTarget)
            {
                var rels = zip.GetEntry("xl/_rels/workbook.xml.rels")!;
                string xml;
                using (var reader = new StreamReader(rels.Open()))
                {
                    xml = reader.ReadToEnd();
                }

                Assert.Contains("worksheets/sheet1.xml", xml);
                xml = xml.Replace("Target=\"worksheets/sheet1.xml\"", "Target=\"/xl/worksheets/sheet1.xml\"");
                Assert.Contains("Target=\"/xl/worksheets/sheet1.xml\"", xml);

                rels.Delete();
                using var writer = new StreamWriter(zip.CreateEntry("xl/_rels/workbook.xml.rels").Open());
                writer.Write(xml);
            }
        }

        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void Read_AbsoluteWorksheetTarget_LoadsTheSheet()
    {
        using var ms = BuildXlsx("""
            <sheetData>
              <row r="1">
                <c r="A1" t="inlineStr"><is><t>A</t></is></c>
                <c r="B1"><v>1</v></c>
              </row>
            </sheetData>
            """, absoluteSheetTarget: true);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("A", sheet.Cells["A1"].Value);
        Assert.Equal(1d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void Read_CellsWithoutReference_FollowThePreviousCellInTheRow()
    {
        using var ms = BuildXlsx("""
            <sheetData>
              <row r="1">
                <c t="inlineStr"><is><t>A</t></is></c>
                <c><v>1</v></c>
                <c r="D1"><v>4</v></c>
                <c><v>5</v></c>
              </row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("A", sheet.Cells["A1"].Value);
        Assert.Equal(1d, sheet.Cells["B1"].Value);
        Assert.Equal(4d, sheet.Cells["D1"].Value);
        Assert.Equal(5d, sheet.Cells["E1"].Value);
    }

    [Fact]
    public void Read_RowsWithoutReference_FollowThePreviousRow()
    {
        using var ms = BuildXlsx("""
            <sheetData>
              <row><c t="inlineStr"><is><t>first</t></is></c></row>
              <row><c><v>2</v></c><c><v>3</v></c></row>
              <row r="5"><c><v>5</v></c></row>
              <row><c><v>6</v></c></row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("first", sheet.Cells["A1"].Value);
        Assert.Equal(2d, sheet.Cells["A2"].Value);
        Assert.Equal(3d, sheet.Cells["B2"].Value);
        Assert.Equal(5d, sheet.Cells["A5"].Value);
        Assert.Equal(6d, sheet.Cells["A6"].Value);
    }

    [Fact]
    public void Read_CellsWithoutReferenceBeyondTheGrid_GrowTheSheet()
    {
        using var ms = BuildXlsx("""
            <sheetData>
              <row r="120"><c><v>1</v></c></row>
              <row><c><v>2</v></c></row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal(1d, sheet.Cells["A120"].Value);
        Assert.Equal(2d, sheet.Cells["A121"].Value);
    }
}
