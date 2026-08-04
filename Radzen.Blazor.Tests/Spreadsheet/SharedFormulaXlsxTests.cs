using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class SharedFormulaXlsxTests
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

    [Fact]
    public void Read_SharedFormula_ExpandsFollowerCellsWithShiftedReferences()
    {
        using var ms = BuildXlsxWithSheetData("""
            <sheetData>
              <row r="1"><c r="B1"><v>1</v></c><c r="C1"><v>2</v></c><c r="D1"><v>3</v></c></row>
              <row r="2"><c r="B2"><v>10</v></c><c r="C2"><v>20</v></c><c r="D2"><v>30</v></c></row>
              <row r="3">
                <c r="B3"><f t="shared" ref="B3:D3" si="0">SUM(B1:B2)</f><v>11</v></c>
                <c r="C3"><f t="shared" si="0"/><v>22</v></c>
                <c r="D3"><f t="shared" si="0"/><v>33</v></c>
              </row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("=SUM(B1:B2)", sheet.Cells["B3"].Formula);
        Assert.Equal("=SUM(C1:C2)", sheet.Cells["C3"].Formula);
        Assert.Equal("=SUM(D1:D2)", sheet.Cells["D3"].Formula);

        Assert.Equal(11d, sheet.Cells["B3"].Value);
        Assert.Equal(22d, sheet.Cells["C3"].Value);
        Assert.Equal(33d, sheet.Cells["D3"].Value);
    }

    [Fact]
    public void Read_SharedFormula_KeepsAbsoluteReferencesInFollowerCells()
    {
        using var ms = BuildXlsxWithSheetData("""
            <sheetData>
              <row r="1"><c r="B1"><v>2</v></c><c r="C1"><v>3</v></c></row>
              <row r="2"><c r="B2"><v>100</v></c></row>
              <row r="4">
                <c r="B4"><f t="shared" ref="B4:C4" si="7">B1*$B$2</f><v>200</v></c>
                <c r="C4"><f t="shared" si="7"/><v>300</v></c>
              </row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("=B1*$B$2", sheet.Cells["B4"].Formula);
        Assert.Equal("=C1*$B$2", sheet.Cells["C4"].Formula);
        Assert.Equal(300d, sheet.Cells["C4"].Value);
    }

    [Fact]
    public void Read_SharedFormula_SpanningRows_ShiftsRowReferences()
    {
        using var ms = BuildXlsxWithSheetData("""
            <sheetData>
              <row r="1">
                <c r="A1"><v>1</v></c><c r="B1"><v>2</v></c>
                <c r="C1"><f t="shared" ref="C1:C2" si="0">SUM(A1:B1)</f><v>3</v></c>
              </row>
              <row r="2">
                <c r="A2"><v>3</v></c><c r="B2"><v>4</v></c>
                <c r="C2"><f t="shared" si="0"/><v>7</v></c>
              </row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("=SUM(A1:B1)", sheet.Cells["C1"].Formula);
        Assert.Equal("=SUM(A2:B2)", sheet.Cells["C2"].Formula);
        Assert.Equal(7d, sheet.Cells["C2"].Value);
    }

    private static XDocument SaveAndReadSheetXml(Workbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;

        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        using var stream = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        return XDocument.Load(stream);
    }

    private static XElement FormulaElement(XDocument doc, string cellRef) =>
        doc.Descendants(Ns + "c").Single(c => c.Attribute("r")?.Value == cellRef).Element(Ns + "f")!;

    [Fact]
    public void Write_ColumnRunOfShiftedFormulas_EmitsSharedFormula()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sheet1", 20, 10);
        sheet.Cells["B1"].Value = 1d;
        sheet.Cells["C1"].Value = 2d;
        sheet.Cells["B2"].Value = 10d;
        sheet.Cells["C2"].Value = 20d;
        sheet.Cells["B3"].Formula = "=SUM(B1:B2)";
        sheet.Cells["C3"].Formula = "=SUM(C1:C2)";

        var doc = SaveAndReadSheetXml(wb);

        var master = FormulaElement(doc, "B3");
        Assert.Equal("shared", master.Attribute("t")?.Value);
        Assert.Equal("B3:C3", master.Attribute("ref")?.Value);
        Assert.Equal("0", master.Attribute("si")?.Value);
        Assert.Equal("SUM(B1:B2)", master.Value);

        var follower = FormulaElement(doc, "C3");
        Assert.Equal("shared", follower.Attribute("t")?.Value);
        Assert.Null(follower.Attribute("ref"));
        Assert.Equal("0", follower.Attribute("si")?.Value);
        Assert.Empty(follower.Value);
    }

    [Fact]
    public void Write_RowRunOfShiftedFormulas_EmitsSharedFormulaWithMasterOnTop()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sheet1", 20, 10);
        sheet.Cells["A1"].Value = 1d;
        sheet.Cells["B1"].Value = 2d;
        sheet.Cells["A2"].Value = 3d;
        sheet.Cells["B2"].Value = 4d;
        sheet.Cells["C1"].Formula = "=SUM(A1:B1)";
        sheet.Cells["C2"].Formula = "=SUM(A2:B2)";
        sheet.Cells["C3"].Formula = "=SUM(A3:B3)";

        var doc = SaveAndReadSheetXml(wb);

        var master = FormulaElement(doc, "C1");
        Assert.Equal("C1:C3", master.Attribute("ref")?.Value);
        Assert.Equal("SUM(A1:B1)", master.Value);
        Assert.Empty(FormulaElement(doc, "C2").Value);
        Assert.Empty(FormulaElement(doc, "C3").Value);
    }

    [Fact]
    public void Write_ShiftedFormulasWithAbsoluteReferences_ShareWhenAbsolutePartMatches()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sheet1", 20, 10);
        sheet.Cells["B1"].Value = 2d;
        sheet.Cells["C1"].Value = 3d;
        sheet.Cells["B2"].Value = 100d;
        sheet.Cells["B4"].Formula = "=B1*$B$2";
        sheet.Cells["C4"].Formula = "=C1*$B$2";

        var doc = SaveAndReadSheetXml(wb);

        var master = FormulaElement(doc, "B4");
        Assert.Equal("B4:C4", master.Attribute("ref")?.Value);
        Assert.Equal("B1*$B$2", master.Value);
        Assert.Empty(FormulaElement(doc, "C4").Value);
    }

    [Fact]
    public void Write_AdjacentUnrelatedFormulas_StayPlain()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sheet1", 20, 10);
        sheet.Cells["A1"].Value = 1d;
        sheet.Cells["A2"].Value = 2d;
        sheet.Cells["B1"].Formula = "=A1+1";
        sheet.Cells["B2"].Formula = "=A1+2";

        var doc = SaveAndReadSheetXml(wb);

        var first = FormulaElement(doc, "B1");
        Assert.Null(first.Attribute("t"));
        Assert.Null(first.Attribute("si"));
        Assert.Equal("A1+1", first.Value);

        var second = FormulaElement(doc, "B2");
        Assert.Null(second.Attribute("t"));
        Assert.Equal("A1+2", second.Value);
    }

    [Fact]
    public void Write_SeparateRuns_GetDistinctSharedIndexes()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sheet1", 20, 10);
        sheet.Cells["A3"].Formula = "=A1+A2";
        sheet.Cells["B3"].Formula = "=B1+B2";
        sheet.Cells["E5"].Formula = "=E4*2";
        sheet.Cells["F5"].Formula = "=F4*2";

        var doc = SaveAndReadSheetXml(wb);

        var firstSi = FormulaElement(doc, "A3").Attribute("si")?.Value;
        var secondSi = FormulaElement(doc, "E5").Attribute("si")?.Value;

        Assert.NotNull(firstSi);
        Assert.NotNull(secondSi);
        Assert.NotEqual(firstSi, secondSi);
        Assert.Equal(firstSi, FormulaElement(doc, "B3").Attribute("si")?.Value);
        Assert.Equal(secondSi, FormulaElement(doc, "F5").Attribute("si")?.Value);
    }

    [Fact]
    public void RoundTrip_SharedFormulas_PreserveFormulasAndValues()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sheet1", 20, 10);
        sheet.Cells["B1"].Value = 1d;
        sheet.Cells["C1"].Value = 2d;
        sheet.Cells["D1"].Value = 3d;
        sheet.Cells["B2"].Value = 10d;
        sheet.Cells["C2"].Value = 20d;
        sheet.Cells["D2"].Value = 30d;
        sheet.Cells["B3"].Formula = "=SUM(B1:B2)";
        sheet.Cells["C3"].Formula = "=SUM(C1:C2)";
        sheet.Cells["D3"].Formula = "=SUM(D1:D2)";

        using var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("=SUM(B1:B2)", loaded.Cells["B3"].Formula);
        Assert.Equal("=SUM(C1:C2)", loaded.Cells["C3"].Formula);
        Assert.Equal("=SUM(D1:D2)", loaded.Cells["D3"].Formula);
        Assert.Equal(11d, loaded.Cells["B3"].Value);
        Assert.Equal(22d, loaded.Cells["C3"].Value);
        Assert.Equal(33d, loaded.Cells["D3"].Value);
    }

    [Fact]
    public void Read_EmptyFormulaWithoutSharedIndex_FallsBackToCachedValue()
    {
        using var ms = BuildXlsxWithSheetData("""
            <sheetData>
              <row r="1"><c r="A1"><f/><v>42</v></c></row>
            </sheetData>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Null(sheet.Cells["A1"].Formula);
        Assert.Equal(42d, sheet.Cells["A1"].Value);
    }
}
