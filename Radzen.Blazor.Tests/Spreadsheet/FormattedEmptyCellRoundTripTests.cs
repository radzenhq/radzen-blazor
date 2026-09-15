using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class FormattedEmptyCellRoundTripTests
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static MemoryStream Save(Workbook workbook)
    {
        var stream = new MemoryStream();

        workbook.SaveToStream(stream);
        stream.Position = 0;

        return stream;
    }

    private static XElement[] Cells(MemoryStream stream)
    {
        stream.Position = 0;

        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        using var content = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();

        return XDocument.Load(content).Descendants(Main + "c").ToArray();
    }

    private static Worksheet Load(MemoryStream stream)
    {
        stream.Position = 0;

        return Workbook.LoadFromStream(stream).Sheets[0];
    }

    [Fact]
    public void AnEmptyCellKeepsItsFormat()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 2, 3);

        sheet.Cells[0, 0].SetValue("Name");
        sheet.Cells[1, 1].Format.BackgroundColor = "#EEEEEE";
        sheet.Cells[1, 1].Format.BorderBottom = new BorderStyle { LineStyle = BorderLineStyle.Thin, Color = "#000000" };

        var loaded = Load(Save(workbook));

        Assert.Null(loaded.Cells[1, 1].Value);
        Assert.Equal("#EEEEEE", loaded.Cells[1, 1].Format.BackgroundColor);
        Assert.Equal(BorderLineStyle.Thin, loaded.Cells[1, 1].Format.BorderBottom?.LineStyle);
    }

    [Fact]
    public void AnEmptyCellKeepsItsQuotePrefix()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 2, 2);

        sheet.Cells[0, 0].SetValue("Name");
        sheet.Cells[1, 1].QuotePrefix = true;

        var loaded = Load(Save(Load(Save(workbook)).Workbook));

        Assert.Null(loaded.Cells[1, 1].Value);
        Assert.True(loaded.Cells[1, 1].QuotePrefix);
    }

    [Fact]
    public void ACoveredCellOfAMergeKeepsItsOwnFormat()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 2, 3);

        sheet.Cells[0, 0].SetValue("Title");
        sheet.Cells[0, 0].Format.Bold = true;
        sheet.Cells[0, 2].Format.BorderRight = new BorderStyle { LineStyle = BorderLineStyle.Thin, Color = "#000000" };
        sheet.MergedCells.Add(new RangeRef(new CellRef(0, 0), new CellRef(0, 2)));

        var styles = Cells(Save(workbook)).ToDictionary(c => (string)c.Attribute("r")!, c => (string?)c.Attribute("s"));

        Assert.Equal(styles["A1"], styles["B1"]);
        Assert.NotEqual(styles["A1"], styles["C1"]);
        Assert.NotNull(styles["C1"]);
    }

    [Fact]
    public void AnEmptyCellWithADefaultFormatIsNotWritten()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 2, 3);

        sheet.Cells[0, 0].SetValue("Name");
        Assert.True(sheet.Cells[1, 1].Format.IsDefault);
        sheet.Cells[1, 2].Format.Bold = true;

        var cells = Cells(Save(workbook));

        Assert.Equal(new[] { "A1", "C2" }, cells.Select(c => (string?)c.Attribute("r")));
        Assert.Empty(cells[1].Nodes());
        Assert.NotNull(cells[1].Attribute("s"));
    }

    [Fact]
    public void AnEmptyCellAnotherWriterWroteKeepsItsFormat()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 2, 2);

        sheet.Cells[0, 0].SetValue("Name");
        sheet.Cells[0, 0].Format.Bold = true;

        var stream = Save(workbook);
        var style = (string)Cells(stream).Single().Attribute("s")!;

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = zip.GetEntry("xl/worksheets/sheet1.xml")!;
            XDocument document;

            using (var content = entry.Open())
            {
                document = XDocument.Load(content);
            }

            document.Descendants(Main + "sheetData").Single().ReplaceNodes(
                new XElement(Main + "row", new XAttribute("r", "2"),
                    new XElement(Main + "c", new XAttribute("r", "B2"), new XAttribute("s", style))));

            entry.Delete();

            using var output = zip.CreateEntry("xl/worksheets/sheet1.xml").Open();
            document.Save(output);
        }

        var loaded = Load(stream);

        Assert.Null(loaded.Cells[1, 1].Value);
        Assert.True(loaded.Cells[1, 1].Format.Bold);
    }
}
