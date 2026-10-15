using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class XlsxReaderPackageTests
{
    private const string WorkbookXml = """
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets><sheet name="Sheet1" sheetId="1" r:id="rId1" /></sheets>
        </workbook>
        """;

    private static MemoryStream CreatePackage(string target, string sheetPath, string sheetData)
    {
        var ms = new MemoryStream();

        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(archive, "xl/workbook.xml", WorkbookXml);
            Add(archive, "xl/_rels/workbook.xml.rels", $"""
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="{target}" Id="rId1" />
                </Relationships>
                """);
            Add(archive, sheetPath, $"""
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <sheetData>{sheetData}</sheetData>
                </worksheet>
                """);
        }

        ms.Position = 0;
        return ms;
    }

    private static void Add(ZipArchive archive, string name, string content)
    {
        using var stream = archive.CreateEntry(name).Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static readonly XNamespace RelationshipsNs = "http://schemas.openxmlformats.org/package/2006/relationships";

    private static MemoryStream SaveWithTable()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sheet1", 10, 5);
        sheet.Cells[0, 0].SetValue("Name");
        sheet.Cells[1, 0].SetValue("A");
        sheet.AddTable("Data", new RangeRef(new CellRef(0, 0), new CellRef(1, 0)));

        var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;
        return ms;
    }

    private static void Move(ZipArchive archive, string from, string to)
    {
        var source = archive.GetEntry(from)!;
        using (var input = source.Open())
        using (var output = archive.CreateEntry(to).Open())
        {
            input.CopyTo(output);
        }
        source.Delete();
    }

    private static void RewriteRelationships(ZipArchive archive, string path, System.Action<XElement> rewrite)
    {
        var entry = archive.GetEntry(path)!;
        XDocument doc;
        using (var stream = entry.Open())
        {
            doc = XDocument.Load(stream);
        }

        foreach (var relationship in doc.Descendants(RelationshipsNs + "Relationship"))
        {
            rewrite(relationship);
        }

        entry.Delete();
        using var output = archive.CreateEntry(path).Open();
        doc.Save(output);
    }

    [Fact]
    public void Load_AbsoluteTableTarget_ReadsTable()
    {
        using var ms = SaveWithTable();
        string? tableTarget = null;

        using (var archive = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            RewriteRelationships(archive, "xl/worksheets/_rels/sheet1.xml.rels", relationship =>
            {
                if (((string)relationship.Attribute("Type")!).EndsWith("/table", System.StringComparison.Ordinal))
                {
                    tableTarget = "/xl/tables/" + Path.GetFileName((string)relationship.Attribute("Target")!);
                    relationship.SetAttributeValue("Target", tableTarget);
                }
            });
        }
        ms.Position = 0;

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("/xl/tables/table1.xml", tableTarget);
        Assert.Single(sheet.Tables);
    }

    [Fact]
    public void Load_WorksheetOutsideWorksheetsFolder_ReadsCellsAndTable()
    {
        using var ms = SaveWithTable();

        using (var archive = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            Move(archive, "xl/worksheets/sheet1.xml", "xl/data/sheet1.xml");
            Move(archive, "xl/worksheets/_rels/sheet1.xml.rels", "xl/data/_rels/sheet1.xml.rels");
            RewriteRelationships(archive, "xl/_rels/workbook.xml.rels", relationship =>
            {
                if ((string)relationship.Attribute("Target")! == "worksheets/sheet1.xml")
                {
                    relationship.SetAttributeValue("Target", "/xl/data/sheet1.xml");
                }
            });
        }
        ms.Position = 0;

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("A", sheet.Cells[1, 0].Value);
        Assert.Single(sheet.Tables);
    }

    [Fact]
    public void Load_AbsoluteWorksheetTarget_ReadsSheet()
    {
        using var ms = CreatePackage("/xl/worksheets/sheet1.xml", "xl/worksheets/sheet1.xml",
            """<row r="1"><c r="A1" t="inlineStr"><is><t>A</t></is></c></row>""");

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("A", sheet.Cells[0, 0].Value);
    }

    [Fact]
    public void Load_RelativeWorksheetTargetWithParentSegment_ReadsSheet()
    {
        using var ms = CreatePackage("../xl/worksheets/sheet1.xml", "xl/worksheets/sheet1.xml",
            """<row r="1"><c r="A1" t="inlineStr"><is><t>A</t></is></c></row>""");

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("A", sheet.Cells[0, 0].Value);
    }

    [Fact]
    public void Load_RowsAndCellsWithoutReference_FollowPreviousRowAndColumn()
    {
        using var ms = CreatePackage("/xl/worksheets/sheet1.xml", "xl/worksheets/sheet1.xml",
            """
            <row><c t="inlineStr"><is><t>A</t></is></c><c><v>1</v></c></row>
            <row><c><v>2</v></c><c r="D2"><v>3</v></c><c><v>4</v></c></row>
            <row r="5"><c><v>5</v></c></row>
            <row><c><v>6</v></c></row>
            """);

        var sheet = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.Equal("A", sheet.Cells["A1"].Value);
        Assert.Equal(1d, sheet.Cells["B1"].Value);
        Assert.Equal(2d, sheet.Cells["A2"].Value);
        Assert.Equal(3d, sheet.Cells["D2"].Value);
        Assert.Equal(4d, sheet.Cells["E2"].Value);
        Assert.Equal(5d, sheet.Cells["A5"].Value);
        Assert.Equal(6d, sheet.Cells["A6"].Value);
    }
}
