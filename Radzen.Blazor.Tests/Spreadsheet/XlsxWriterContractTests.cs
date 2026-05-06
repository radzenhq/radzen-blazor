using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Radzen;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// One [Fact] per finding from the Excel-vs-Radzen XLSX comparison report
// (see ~/xlsx-compare/reports/REPORT.md). Tests fail today; each turns
// green as XlsxWriter is brought closer to what Excel emits.
public class XlsxWriterContractTests
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rels = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Ct   = "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly XNamespace Rid  = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private sealed class Package
    {
        public required Dictionary<string, XDocument> Parts { get; init; }
        public required HashSet<string> Names { get; init; }

        public XDocument Part(string name) => Parts[name];
        public bool Has(string name) => Names.Contains(name);
    }

    private static Package Save(Workbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;

        var parts = new Dictionary<string, XDocument>();
        var names = new HashSet<string>();
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        foreach (var entry in zip.Entries)
        {
            names.Add(entry.FullName);
            if (entry.FullName.EndsWith(".xml") || entry.FullName.EndsWith(".rels"))
            {
                using var s = entry.Open();
                parts[entry.FullName] = XDocument.Load(s);
            }
        }
        return new Package { Parts = parts, Names = names };
    }

    private static Workbook MakeBookWithStrings()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 10, 5);
        s.Cells[0, 0].SetValue("Name");
        s.Cells[0, 1].SetValue("Qty");
        s.Cells[1, 0].SetValue("Apple");
        s.Cells[1, 1].SetValue("3");
        return wb;
    }

    // ── #1 Formula cell cached value ────────────────────────────────────────
    [Fact]
    public void XlsxWriter_ShouldEmitCorrectCachedValue_WhenFormulaIsNumeric()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 5, 5);
        s.Cells[0, 0].Value = 2.0;
        s.Cells[0, 1].Value = 3.0;
        s.Cells[0, 2].Formula = "=A1+B1";

        var pkg = Save(wb);
        var c = pkg.Part("xl/worksheets/sheet1.xml")
                   .Descendants(Main + "c")
                   .Single(x => (string?)x.Attribute("r") == "C1");

        var v = (string?)c.Element(Main + "v");
        var t = (string?)c.Attribute("t");

        // The cached <v> must be either omitted (so Excel recalcs on open) or
        // the actual evaluated number. It must never be a stale string from
        // somewhere else in the sheet.
        Assert.False(t == "str" && v == "Name",
            $"Formula cell C1 has stale cached value: t='{t}' v='{v}'");
        if (v is not null)
        {
            Assert.Equal("5", v);
            Assert.NotEqual("str", t);
        }
    }

    // ── #2 Per-part overrides in [Content_Types].xml ────────────────────────
    [Fact]
    public void ContentTypes_ShouldEmitOverride_ForEachWorksheet()
    {
        var wb = new Workbook();
        wb.AddSheet("A", 1, 1);
        wb.AddSheet("B", 1, 1);

        var pkg = Save(wb);
        var overrides = pkg.Part("[Content_Types].xml")
                           .Root!.Elements(Ct + "Override")
                           .Select(o => ((string)o.Attribute("PartName")!, (string)o.Attribute("ContentType")!))
                           .ToList();

        Assert.Contains(("/xl/worksheets/sheet1.xml",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"), overrides);
        Assert.Contains(("/xl/worksheets/sheet2.xml",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"), overrides);
    }

    // ── #3a Workbook root must declare r: namespace; sheet uses r:id ────────
    [Fact]
    public void Workbook_ShouldDeclareRelationshipsNamespace_OnRoot()
    {
        var wb = MakeBookWithStrings();
        var pkg = Save(wb);
        var root = pkg.Part("xl/workbook.xml").Root!;

        // r namespace declared at the root, not on individual <sheet> elements
        // under a synthetic prefix like p3:.
        Assert.Equal("r", root.GetPrefixOfNamespace(Rid));

        var sheet = root.Element(Main + "sheets")!.Element(Main + "sheet")!;
        Assert.NotNull(sheet.Attribute(Rid + "id"));
    }

    // ── #3b Workbook should contain Excel's standard skeleton ───────────────
    [Fact]
    public void Workbook_ShouldEmitStandardSkeletonElements()
    {
        var wb = MakeBookWithStrings();
        var pkg = Save(wb);
        var root = pkg.Part("xl/workbook.xml").Root!;

        Assert.NotNull(root.Element(Main + "fileVersion"));
        Assert.NotNull(root.Element(Main + "workbookPr"));
        Assert.NotNull(root.Element(Main + "bookViews"));
        Assert.NotNull(root.Element(Main + "calcPr"));
    }

    // ── #4 docProps + theme parts ───────────────────────────────────────────
    [Fact]
    public void Package_ShouldContainDocPropsCore()
    {
        var pkg = Save(MakeBookWithStrings());

        Assert.True(pkg.Has("docProps/core.xml"),
            "Package must contain docProps/core.xml");

        var rootRels = pkg.Part("_rels/.rels").Root!.Elements(Rels + "Relationship");
        Assert.Contains(rootRels, r => (string)r.Attribute("Target")! == "docProps/core.xml");
    }

    [Fact]
    public void Package_ShouldContainDocPropsApp()
    {
        var pkg = Save(MakeBookWithStrings());

        Assert.True(pkg.Has("docProps/app.xml"),
            "Package must contain docProps/app.xml");

        var rootRels = pkg.Part("_rels/.rels").Root!.Elements(Rels + "Relationship");
        Assert.Contains(rootRels, r => (string)r.Attribute("Target")! == "docProps/app.xml");
    }

    [Fact]
    public void Package_ShouldContainTheme()
    {
        var pkg = Save(MakeBookWithStrings());

        Assert.True(pkg.Has("xl/theme/theme1.xml"),
            "Package must contain xl/theme/theme1.xml");

        var wbRels = pkg.Part("xl/_rels/workbook.xml.rels")
                        .Root!.Elements(Rels + "Relationship");
        Assert.Contains(wbRels, r => (string)r.Attribute("Target")! == "theme/theme1.xml");
    }

    // ── #5 Numeric cells should not emit t="n" (it is the default) ──────────
    [Fact]
    public void NumericCell_ShouldOmitTypeAttribute()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 3, 3);
        s.Cells[0, 0].Value = 42.0;

        var pkg = Save(wb);
        var c = pkg.Part("xl/worksheets/sheet1.xml")
                   .Descendants(Main + "c")
                   .Single(x => (string?)x.Attribute("r") == "A1");

        Assert.Null(c.Attribute("t"));
        Assert.Equal("42", (string?)c.Element(Main + "v"));
    }

    // ── #6 No <pane> when there is no freeze ────────────────────────────────
    [Fact]
    public void SheetView_ShouldOmitPane_WhenNoFreeze()
    {
        var wb = MakeBookWithStrings();
        // default Frozen = 0 / 0
        var pkg = Save(wb);
        var sv = pkg.Part("xl/worksheets/sheet1.xml")
                    .Descendants(Main + "sheetView").Single();

        Assert.Null(sv.Element(Main + "pane"));
    }

    // ── #7 Empty <sheetPr/> should be omitted ───────────────────────────────
    [Fact]
    public void SheetPr_ShouldBeOmitted_WhenEmpty()
    {
        var wb = MakeBookWithStrings();
        var pkg = Save(wb);
        var ws = pkg.Part("xl/worksheets/sheet1.xml").Root!;

        // sheetPr only carries content (e.g. filterMode) when filters exist.
        // For a sheet with no filters/properties Excel omits the element.
        Assert.Null(ws.Element(Main + "sheetPr"));
    }

    // ── #8 <dimension> should be the data extent, not the whole grid ────────
    [Fact]
    public void Dimension_ShouldEqualDataExtent()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 100, 50);
        s.Cells[0, 0].SetValue("a");
        s.Cells[2, 2].SetValue("b");

        var pkg = Save(wb);
        var dim = (string?)pkg.Part("xl/worksheets/sheet1.xml")
                              .Descendants(Main + "dimension").Single().Attribute("ref");

        Assert.Equal("A1:C3", dim);
    }

    // ── #9 sheetFormatPr + pageMargins + row/@spans ─────────────────────────
    [Fact]
    public void Worksheet_ShouldEmit_SheetFormatPr_And_PageMargins()
    {
        var wb = MakeBookWithStrings();
        var pkg = Save(wb);
        var ws = pkg.Part("xl/worksheets/sheet1.xml").Root!;

        Assert.NotNull(ws.Element(Main + "sheetFormatPr"));
        Assert.NotNull(ws.Element(Main + "pageMargins"));
    }

    [Fact]
    public void Row_ShouldEmit_SpansAttribute()
    {
        var wb = MakeBookWithStrings();
        var pkg = Save(wb);
        var firstRow = pkg.Part("xl/worksheets/sheet1.xml")
                          .Descendants(Main + "row").First();

        Assert.NotNull(firstRow.Attribute("spans"));
    }

    // ── #10 Merged cells should include empty placeholders ──────────────────
    [Fact]
    public void MergedCells_ShouldEmit_PlaceholderCells_ForNonAnchorCells()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 5, 5);
        s.Cells[0, 0].SetValue("Title");
        s.Cells[0, 0].Format = new Format { Bold = true };
        s.MergedCells.Add(new RangeRef(new CellRef(0, 0), new CellRef(0, 2)));

        var pkg = Save(wb);
        var refs = pkg.Part("xl/worksheets/sheet1.xml")
                      .Descendants(Main + "c")
                      .Select(c => (string?)c.Attribute("r"))
                      .ToList();

        Assert.Contains("A1", refs);
        Assert.Contains("B1", refs);
        Assert.Contains("C1", refs);
    }

    // ── #11 sharedStrings.xml only when needed ──────────────────────────────
    [Fact]
    public void SharedStrings_ShouldBeOmitted_WhenWorkbookHasNoStrings()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 3, 3);
        s.Cells[0, 0].Value = 1.0;
        s.Cells[0, 1].Value = 2.0;

        var pkg = Save(wb);

        Assert.False(pkg.Has("xl/sharedStrings.xml"),
            "sharedStrings.xml should not be written when there are no shared strings");

        var wbRels = pkg.Part("xl/_rels/workbook.xml.rels")
                        .Root!.Elements(Rels + "Relationship");
        Assert.DoesNotContain(wbRels, r => (string)r.Attribute("Target")! == "sharedStrings.xml");
    }

    // ── #12 styles.xml should include dxfs + tableStyles ────────────────────
    [Fact]
    public void Styles_ShouldEmit_Dxfs_And_TableStyles()
    {
        var pkg = Save(MakeBookWithStrings());
        var ss = pkg.Part("xl/styles.xml").Root!;

        Assert.NotNull(ss.Element(Main + "dxfs"));
        Assert.NotNull(ss.Element(Main + "tableStyles"));
    }

    // ── #13 Default font should reference theme color and scheme ────────────
    [Fact]
    public void DefaultFont_ShouldReference_ThemeColorAndScheme()
    {
        var pkg = Save(MakeBookWithStrings());
        var font = pkg.Part("xl/styles.xml")
                      .Root!.Element(Main + "fonts")!
                      .Element(Main + "font")!;

        var color = font.Element(Main + "color");
        var scheme = font.Element(Main + "scheme");

        Assert.NotNull(color);
        Assert.Equal("1", (string?)color!.Attribute("theme"));
        Assert.NotNull(scheme);
        Assert.Equal("minor", (string?)scheme!.Attribute("val"));
    }
}
