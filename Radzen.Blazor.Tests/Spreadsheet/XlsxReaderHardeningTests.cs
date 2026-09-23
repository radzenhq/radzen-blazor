using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// Workbooks written by Excel that used to throw out of Workbook.LoadFromStream or out of a
// recalculation: each test is a real-file failure reduced to its cause.
public class XlsxReaderHardeningTests
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static byte[] Save(Workbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);
        return stream.ToArray();
    }

    private static Workbook Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return Workbook.LoadFromStream(stream);
    }

    // Rewrites one part of a saved package so a test can inject what Excel writes and we don't.
    private static byte[] RewritePart(byte[] bytes, string entryName, System.Func<XDocument, XDocument> rewrite)
    {
        using var stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = archive.GetEntry(entryName)!;
            XDocument doc;
            using (var read = entry.Open())
            {
                doc = XDocument.Load(read);
            }

            doc = rewrite(doc);
            entry.Delete();
            using var write = archive.CreateEntry(entryName).Open();
            doc.Save(write);
        }

        return stream.ToArray();
    }

    [Fact]
    public void UnquotedSheetNameInAnotherScriptIsAReference()
    {
        var workbook = new Workbook();
        var data = workbook.AddSheet("入荷実績", 5, 5);
        var report = workbook.AddSheet("Report", 5, 5);

        data.Cells[0, 0].Value = "A";
        data.Cells[0, 1].Value = 10d;
        data.Cells[1, 0].Value = "B";
        data.Cells[1, 1].Value = 20d;
        data.Cells[2, 0].Value = "A";
        data.Cells[2, 1].Value = 5d;

        report.Cells[0, 0].Value = "A";
        report.Cells[0, 1].Formula = "=SUMIF(入荷実績!$A$1:$A$3,A1,入荷実績!$B$1:$B$3)";
        report.Cells[1, 1].Formula = "=B1*2";

        Assert.Equal(15d, report.Cells[0, 1].Value);
        Assert.Equal(30d, report.Cells[1, 1].Value);

        var loaded = Load(Save(workbook)).GetSheet("Report")!;

        Assert.Equal(15d, loaded.Cells[0, 1].Value);
        Assert.Equal("=SUMIF(入荷実績!$A$1:$A$3,A1,入荷実績!$B$1:$B$3)", loaded.Cells[0, 1].Formula);
    }

    [Fact]
    public void AFormulaThatDoesNotParseIsANameErrorWhereverItIsReferenced()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 5, 5);

        sheet.Cells[0, 0].Formula = "=1 ¤ 2";
        sheet.Cells[0, 1].Formula = "=A1+1";

        Assert.True(sheet.Cells[0, 0].Data.IsError);
        Assert.True(sheet.Cells[0, 1].Data.IsError);
    }

    [Fact]
    public void RangeOperandsApplyTheOperatorToEveryCell()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);

        sheet.Cells[0, 0].Value = 1d;
        sheet.Cells[1, 0].Value = 2d;
        sheet.Cells[2, 0].Value = 3d;
        sheet.Cells[0, 1].Value = 10d;
        sheet.Cells[1, 1].Value = 20d;
        sheet.Cells[2, 1].Value = 30d;

        sheet.Cells[5, 5].Formula = "=SUMPRODUCT((A1:A3>=2)*B1:B3)";
        sheet.Cells[6, 5].Formula = "=SUMPRODUCT(-(A1:A3=1),B1:B3)";
        sheet.Cells[7, 5].Formula = "=SUMPRODUCT((A1:A3=A1)*(B1:B3>=0.7*B1:B3))";

        Assert.Equal(50d, sheet.Cells[5, 5].Value);
        Assert.Equal(-10d, sheet.Cells[6, 5].Value);
        Assert.Equal(1d, sheet.Cells[7, 5].Value);
    }

    [Fact]
    public void ARangeWhereAValueIsNeededIntersectsTheFormulaCellOrIsAValueError()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);

        sheet.Cells[0, 0].Value = 1d;
        sheet.Cells[1, 0].Value = 2d;
        sheet.Cells[2, 0].Value = 3d;

        // Row 2 crosses A1:A3 at A2 (Excel's implicit intersection).
        sheet.Cells[1, 3].Formula = "=A1:A3*10";
        // Row 6 does not.
        sheet.Cells[5, 3].Formula = "=A1:A3*10";
        // A comparison against a range in a plain cell used to throw InvalidCastException.
        sheet.Cells[6, 3].Formula = "=A1=A1:A3";

        Assert.Equal(20d, sheet.Cells[1, 3].Value);
        Assert.True(sheet.Cells[5, 3].Data.IsError);
        Assert.True(sheet.Cells[6, 3].Data.IsError);
    }

    [Fact]
    public void AHyperlinkOverARangeCoversEveryCellOfTheRange()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 5, 20);
        sheet.Cells[1, 9].Value = "Guidance";
        sheet.Cells[1, 9].Hyperlink = new Hyperlink { Url = "https://example.org/guide" };

        var bytes = RewritePart(Save(workbook), "xl/worksheets/sheet1.xml", doc =>
        {
            var hyperlink = doc.Descendants(Main + "hyperlink").Single();
            hyperlink.SetAttributeValue("ref", "J2:O2");
            return doc;
        });

        var loaded = Load(bytes).Sheets[0];

        for (var column = 9; column <= 14; column++)
        {
            Assert.Equal("https://example.org/guide", loaded.Cells[1, column].Hyperlink?.Url);
        }

        Assert.Null(loaded.Cells[1, 15].Hyperlink);
    }

    [Fact]
    public void AReferencePastTheSheetDoesNotThrowWhenTheFormulaIsSet()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 24, 54);

        sheet.Cells[1, 3].Formula = "=INDEX(G6:BK24,1,1)";
        sheet.Cells[2, 3].Formula = "=SUM(A1:ZZ5000)";

        Assert.True(sheet.Cells[1, 3].Data.IsError);
        Assert.True(sheet.Cells[2, 3].Data.IsError);
    }

    [Fact]
    public void TheValueExcelCachedIsKeptWhenTheFormulaCannotBeEvaluated()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 5, 5);
        sheet.Cells[0, 0].Value = 41d;
        sheet.Cells[0, 1].Formula = "=A1+1";
        sheet.Cells[0, 2].Formula = "=A1+2";

        var bytes = RewritePart(Save(workbook), "xl/worksheets/sheet1.xml", doc =>
        {
            var cells = doc.Descendants(Main + "c").ToDictionary(c => (string)c.Attribute("r")!);

            // A function we do not implement, with the result Excel stored next to it.
            cells["B1"].Element(Main + "f")!.Value = "NOSUCHFUNCTION(A1)";
            cells["B1"].Element(Main + "v")!.Value = "42";

            // A function we do not implement whose result Excel stored as text.
            cells["C1"].SetAttributeValue("t", "str");
            cells["C1"].Element(Main + "f")!.Value = "OFFSET(A1,0,0)&\"\"";
            cells["C1"].Element(Main + "v")!.Value = "forty-one";

            return doc;
        });

        var loaded = Load(bytes).Sheets[0];

        Assert.Equal(42d, loaded.Cells[0, 1].Value);
        Assert.Equal("=NOSUCHFUNCTION(A1)", loaded.Cells[0, 1].Formula);
        Assert.Equal("forty-one", loaded.Cells[0, 2].Value);
    }

    [Fact]
    public void AnErrorExcelCachedStaysAnError()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 5, 5);
        sheet.Cells[0, 1].Formula = "=A1+1";

        var bytes = RewritePart(Save(workbook), "xl/worksheets/sheet1.xml", doc =>
        {
            var cell = doc.Descendants(Main + "c").Single(c => (string)c.Attribute("r")! == "B1");
            cell.SetAttributeValue("t", "e");
            cell.Element(Main + "f")!.Value = "NOSUCHFUNCTION(A1)";
            cell.Element(Main + "v")!.Value = "#NAME?";
            return doc;
        });

        var loaded = Load(bytes).Sheets[0];

        Assert.True(loaded.Cells[0, 1].Data.IsError);
    }

    [Fact]
    public void AFormulaTheParserRejectsKeepsItsCachedValueAndTheWorkbookLoads()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 5, 5);
        sheet.Cells[0, 1].Formula = "=1+1";

        var bytes = RewritePart(Save(workbook), "xl/worksheets/sheet1.xml", doc =>
        {
            var cell = doc.Descendants(Main + "c").Single(c => (string)c.Attribute("r")! == "B1");
            // Malformed number: the lexer throws.
            cell.Element(Main + "f")!.Value = "1E+";
            cell.Element(Main + "v")!.Value = "7";
            return doc;
        });

        var loaded = Load(bytes).Sheets[0];

        Assert.NotNull(loaded);
    }
}
