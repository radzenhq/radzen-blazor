using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class InlineStringXlsxTests
{
    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static MemoryStream BuildXlsxWithSheetData(string sheetDataXml)
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
            var newEntry = zip.CreateEntry("xl/worksheets/sheet1.xml");
            using var output = newEntry.Open();
            doc.Save(output);
        }

        ms.Position = 0;
        return ms;
    }

    private static MemoryStream BuildXlsxWithSharedStrings(string sheetDataXml, string sharedStringsXml)
    {
        var ms = BuildXlsxWithSheetData(sheetDataXml);

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            zip.GetEntry("xl/sharedStrings.xml")?.Delete();

            var sst = XElement.Parse(sharedStringsXml);
            foreach (var element in sst.DescendantsAndSelf())
            {
                element.Name = Ns + element.Name.LocalName;
            }

            var entry = zip.CreateEntry("xl/sharedStrings.xml");
            using var output = entry.Open();
            new XDocument(sst).Save(output);
        }

        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void SharedStringThatLooksNumericLoadsAsTextWithoutQuotePrefix()
    {
        using var ms = BuildXlsxWithSharedStrings("""
            <sheetData>
              <row r="1">
                <c r="A1" t="s"><v>0</v></c>
                <c r="B1" t="s"><v>1</v></c>
                <c r="C1"><v>5</v></c>
              </row>
            </sheetData>
            """, """
            <sst>
              <si><t>05</t></si>
              <si><t>1.50</t></si>
            </sst>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("05", sheet.Cells["A1"].Value);
        Assert.False(sheet.Cells["A1"].QuotePrefix);
        Assert.Equal("1.50", sheet.Cells["B1"].Value);
        Assert.Equal(5d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void Read_InlineString_ReadsTheTextTheCellCarries()
    {
        using var ms = BuildXlsxWithSheetData("""
            <sheetData>
              <row r="1">
                <c r="A1" t="inlineStr"><is><t>Hello</t></is></c>
                <c r="B1" t="inlineStr"><is><t xml:space="preserve"> spaced </t></is></c>
              </row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("Hello", sheet.Cells["A1"].Value);
        Assert.Equal(" spaced ", sheet.Cells["B1"].Value);
    }

    [Fact]
    public void Read_InlineString_JoinsRichTextRuns()
    {
        using var ms = BuildXlsxWithSheetData("""
            <sheetData>
              <row r="1">
                <c r="A1" t="inlineStr"><is><r><t>Hel</t></r><r><t>lo</t></r></is></c>
              </row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("Hello", sheet.Cells["A1"].Value);
    }

    [Fact]
    public void Read_InlineString_LeavesOtherCellsUnaffected()
    {
        using var ms = BuildXlsxWithSheetData("""
            <sheetData>
              <row r="1">
                <c r="A1" t="inlineStr"><is><t>Text</t></is></c>
                <c r="B1"><v>42</v></c>
                <c r="C1" t="b"><v>1</v></c>
              </row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("Text", sheet.Cells["A1"].Value);
        Assert.Equal(42d, sheet.Cells["B1"].Value);
        Assert.Equal(true, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void Read_InlineString_KeepsTheLeadingTextBeforeItsRuns()
    {
        using var ms = BuildXlsxWithSheetData("""
            <sheetData>
              <row r="1">
                <c r="A1" t="inlineStr"><is><t>A</t><r><t>B</t></r><r><t>C</t></r></is></c>
                <c r="B1" t="inlineStr"><is/></c>
              </row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("ABC", sheet.Cells["A1"].Value);
        Assert.Equal("", sheet.Cells["B1"].Value);
    }

    [Fact]
    public void Read_InlineString_KeepsAQuotePrefix()
    {
        var wb = new Workbook();
        wb.AddSheet("Sheet1", 20, 10).Cells[0, 0].SetText("0012");

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

            var cell = doc.Descendants(Ns + "c").Single(c => (string?)c.Attribute("r") == "A1");
            cell.SetAttributeValue("t", "inlineStr");
            cell.RemoveNodes();
            cell.Add(new XElement(Ns + "is", new XElement(Ns + "t", "0012")));

            entry.Delete();
            var rewritten = zip.CreateEntry("xl/worksheets/sheet1.xml");
            using var output = rewritten.Open();
            doc.Save(output);
        }

        ms.Position = 0;

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.True(sheet.Cells["A1"].QuotePrefix);
        Assert.Equal("0012", sheet.Cells["A1"].Value);

        ms.Dispose();
    }

    [Fact]
    public void Read_InlineString_LeavesPhoneticRunsOutOfTheValue()
    {
        using var ms = BuildXlsxWithSheetData("""
            <sheetData>
              <row r="1">
                <c r="A1" t="inlineStr"><is><t>東京</t><rPh sb="0" eb="2"><t>トウキョウ</t></rPh><phoneticPr fontId="1"/></is></c>
                <c r="B1" t="inlineStr"><is><r><t>大</t></r><r><t>阪</t></r><rPh sb="0" eb="2"><t>オオサカ</t></rPh></is></c>
              </row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("東京", sheet.Cells["A1"].Value);
        Assert.Equal("大阪", sheet.Cells["B1"].Value);
    }

    [Fact]
    public void Read_SharedString_KeepsOneEntryPerItemSoIndexesDoNotShift()
    {
        using var ms = BuildXlsxWithSharedStrings("""
            <sheetData>
              <row r="1">
                <c r="A1" t="s"><v>0</v></c>
                <c r="B1" t="s"><v>1</v></c>
                <c r="C1" t="s"><v>2</v></c>
              </row>
            </sheetData>
            """, """
            <sst count="3" uniqueCount="3">
              <si><t>Hello</t></si>
              <si><r><t>Wor</t></r><r><t>ld</t></r></si>
              <si><t>Last</t></si>
            </sst>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("Hello", sheet.Cells["A1"].Value);
        Assert.Equal("World", sheet.Cells["B1"].Value);
        Assert.Equal("Last", sheet.Cells["C1"].Value);
    }

    [Fact]
    public void Read_SharedString_LeavesPhoneticRunsOutOfTheValue()
    {
        using var ms = BuildXlsxWithSharedStrings("""
            <sheetData>
              <row r="1">
                <c r="A1" t="s"><v>0</v></c>
                <c r="B1" t="s"><v>1</v></c>
              </row>
            </sheetData>
            """, """
            <sst count="2" uniqueCount="2">
              <si><t>東京</t><rPh sb="0" eb="2"><t>トウキョウ</t></rPh><phoneticPr fontId="1"/></si>
              <si><t>Next</t></si>
            </sst>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("東京", sheet.Cells["A1"].Value);
        Assert.Equal("Next", sheet.Cells["B1"].Value);
    }

    [Fact]
    public void Read_InlineString_TreatsAnEmptyRunAsEmptyText()
    {
        using var ms = BuildXlsxWithSheetData("""
            <sheetData>
              <row r="1">
                <c r="A1" t="inlineStr"><is><t></t></is></c>
              </row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("", sheet.Cells["A1"].Value);
    }
}
