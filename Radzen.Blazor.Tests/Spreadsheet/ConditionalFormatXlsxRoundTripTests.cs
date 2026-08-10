using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class ConditionalFormatXlsxRoundTripTests
{
    private static (Workbook wb, Worksheet ws) Build()
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("Sheet1", 10, 3);
        for (var r = 0; r < 8; r++)
        {
            ws.Cells[r, 0].Value = (double)(r + 1);
        }
        ws.Cells[0, 1].SetValue("hello world");
        ws.Cells[1, 1].SetValue("plain");
        return (wb, ws);
    }

    private static Worksheet Roundtrip(Workbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;
        return Workbook.LoadFromStream(ms).Sheets[0];
    }

    private static RangeRef Range(string range) => RangeRef.Parse(range);

    [Fact]
    public void RoundTrip_GreaterThanRule()
    {
        var (wb, ws) = Build();
        ws.ConditionalFormats.Add(Range("A1:A8"), new GreaterThanRule
        {
            Value = 5,
            Format = new Format { BackgroundColor = "#FFEB9C", Color = "#9C5700", Bold = true }
        });

        var loaded = Roundtrip(wb);
        var range = Assert.Single(loaded.ConditionalFormats.Ranges);
        var rule = Assert.IsType<GreaterThanRule>(Assert.Single(loaded.ConditionalFormats.GetRules(range)));
        Assert.Equal(5, rule.Value);
        Assert.Equal("#FFEB9C", rule.Format.BackgroundColor);
        Assert.Equal("#9C5700", rule.Format.Color);
        Assert.True(rule.Format.Bold);

        Assert.Null(loaded.ConditionalFormats.Calculate(loaded.Cells[4, 0]));
        Assert.NotNull(loaded.ConditionalFormats.Calculate(loaded.Cells[5, 0]));
    }

    [Fact]
    public void RoundTrip_LessThanRule()
    {
        var (wb, ws) = Build();
        ws.ConditionalFormats.Add(Range("A1:A8"), new LessThanRule
        {
            Value = 3.5,
            Format = new Format { Color = "#FF0000" }
        });

        var loaded = Roundtrip(wb);
        var rule = Assert.IsType<LessThanRule>(Assert.Single(loaded.ConditionalFormats.GetRules(Range("A1:A8"))));
        Assert.Equal(3.5, rule.Value);
        Assert.Equal("#FF0000", rule.Format.Color);
    }

    [Fact]
    public void RoundTrip_BetweenRule()
    {
        var (wb, ws) = Build();
        ws.ConditionalFormats.Add(Range("A1:A8"), new BetweenRule
        {
            Minimum = 2,
            Maximum = 6,
            Format = new Format { BackgroundColor = "#C6EFCE" }
        });

        var loaded = Roundtrip(wb);
        var rule = Assert.IsType<BetweenRule>(Assert.Single(loaded.ConditionalFormats.GetRules(Range("A1:A8"))));
        Assert.Equal(2, rule.Minimum);
        Assert.Equal(6, rule.Maximum);
        Assert.Equal("#C6EFCE", rule.Format.BackgroundColor);
    }

    [Fact]
    public void RoundTrip_EqualToRule_Number()
    {
        var (wb, ws) = Build();
        ws.ConditionalFormats.Add(Range("A1:A8"), new EqualToRule
        {
            Value = 4.0,
            Format = new Format { Italic = true }
        });

        var loaded = Roundtrip(wb);
        var rule = Assert.IsType<EqualToRule>(Assert.Single(loaded.ConditionalFormats.GetRules(Range("A1:A8"))));
        Assert.Equal(4.0, rule.Value);
        Assert.True(rule.Format.Italic);

        Assert.NotNull(loaded.ConditionalFormats.Calculate(loaded.Cells[3, 0]));
        Assert.Null(loaded.ConditionalFormats.Calculate(loaded.Cells[4, 0]));
    }

    [Fact]
    public void RoundTrip_EqualToRule_Text()
    {
        var (wb, ws) = Build();
        ws.ConditionalFormats.Add(Range("B1:B2"), new EqualToRule
        {
            Value = "say \"hi\"",
            Format = new Format { Underline = true }
        });

        var loaded = Roundtrip(wb);
        var rule = Assert.IsType<EqualToRule>(Assert.Single(loaded.ConditionalFormats.GetRules(Range("B1:B2"))));
        Assert.Equal("say \"hi\"", rule.Value);
        Assert.True(rule.Format.Underline);
    }

    [Fact]
    public void RoundTrip_TextContainsRule()
    {
        var (wb, ws) = Build();
        ws.ConditionalFormats.Add(Range("B1:B2"), new TextContainsRule
        {
            Text = "world",
            Format = new Format { BackgroundColor = "#FFC7CE" }
        });

        var loaded = Roundtrip(wb);
        var rule = Assert.IsType<TextContainsRule>(Assert.Single(loaded.ConditionalFormats.GetRules(Range("B1:B2"))));
        Assert.Equal("world", rule.Text);

        Assert.NotNull(loaded.ConditionalFormats.Calculate(loaded.Cells[0, 1]));
        Assert.Null(loaded.ConditionalFormats.Calculate(loaded.Cells[1, 1]));
    }

    [Fact]
    public void RoundTrip_Top10Rule_TopAndBottom()
    {
        var (wb, ws) = Build();
        ws.ConditionalFormats.Add(Range("A1:A8"),
            new Top10Rule { Count = 3, Bottom = false, Format = new Format { BackgroundColor = "#FF0000" } },
            new Top10Rule { Count = 2, Bottom = true, Format = new Format { Bold = true } });

        var loaded = Roundtrip(wb);
        var rules = loaded.ConditionalFormats.GetRules(Range("A1:A8"));
        Assert.Equal(2, rules.Count);

        var top = Assert.IsType<Top10Rule>(rules[0]);
        Assert.Equal(3, top.Count);
        Assert.False(top.Bottom);
        Assert.Equal("#FF0000", top.Format.BackgroundColor);

        var bottom = Assert.IsType<Top10Rule>(rules[1]);
        Assert.Equal(2, bottom.Count);
        Assert.True(bottom.Bottom);
        Assert.True(bottom.Format.Bold);

        Assert.NotNull(loaded.ConditionalFormats.Calculate(loaded.Cells[7, 0]));
        Assert.NotNull(loaded.ConditionalFormats.Calculate(loaded.Cells[0, 0]));
        Assert.Null(loaded.ConditionalFormats.Calculate(loaded.Cells[3, 0]));
    }

    [Fact]
    public void RoundTrip_BorderFormat()
    {
        var (wb, ws) = Build();
        ws.ConditionalFormats.Add(Range("A1:A8"), new GreaterThanRule
        {
            Value = 1,
            Format = new Format
            {
                BorderTop = new BorderStyle { Color = "#112233", LineStyle = BorderLineStyle.Medium },
                BorderBottom = new BorderStyle { Color = "#445566", LineStyle = BorderLineStyle.Dashed }
            }
        });

        var loaded = Roundtrip(wb);
        var rule = Assert.IsType<GreaterThanRule>(Assert.Single(loaded.ConditionalFormats.GetRules(Range("A1:A8"))));
        Assert.NotNull(rule.Format.BorderTop);
        Assert.Equal("#112233", rule.Format.BorderTop!.Color);
        Assert.Equal(BorderLineStyle.Medium, rule.Format.BorderTop.LineStyle);
        Assert.NotNull(rule.Format.BorderBottom);
        Assert.Equal(BorderLineStyle.Dashed, rule.Format.BorderBottom!.LineStyle);
        Assert.Null(rule.Format.BorderLeft);
    }

    [Fact]
    public void RoundTrip_PreservesRulePrecedenceAcrossRanges()
    {
        var (wb, ws) = Build();
        ws.ConditionalFormats.Add(Range("A1:A8"), new GreaterThanRule { Value = 2, Format = new Format { BackgroundColor = "#111111" } });
        ws.ConditionalFormats.Add(Range("A1:A4"), new GreaterThanRule { Value = 2, Format = new Format { BackgroundColor = "#222222" } });

        var loaded = Roundtrip(wb);
        Assert.Equal(2, loaded.ConditionalFormats.Ranges.Count());

        var overlay = loaded.ConditionalFormats.Calculate(loaded.Cells[3, 0]);
        Assert.NotNull(overlay);
        Assert.Equal("#222222", overlay!.BackgroundColor);

        overlay = loaded.ConditionalFormats.Calculate(loaded.Cells[6, 0]);
        Assert.NotNull(overlay);
        Assert.Equal("#111111", overlay!.BackgroundColor);
    }

    [Fact]
    public void Save_WritesExcelCompatibleXml()
    {
        var (wb, ws) = Build();
        ws.ConditionalFormats.Add(Range("A1:A8"), new GreaterThanRule
        {
            Value = 5,
            Format = new Format { BackgroundColor = "#FFEB9C", Bold = true }
        });
        ws.ConditionalFormats.Add(Range("B1:B2"), new TextContainsRule { Text = "world", Format = new Format { Color = "#FF0000" } });

        using var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;

        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        var sheetDoc = XDocument.Load(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var cfs = sheetDoc.Root!.Elements(ns + "conditionalFormatting").ToList();
        Assert.Equal(2, cfs.Count);
        Assert.Equal("A1:A8", cfs[0].Attribute("sqref")!.Value);

        var gtRule = cfs[0].Element(ns + "cfRule")!;
        Assert.Equal("cellIs", gtRule.Attribute("type")!.Value);
        Assert.Equal("greaterThan", gtRule.Attribute("operator")!.Value);
        Assert.Equal("5", gtRule.Element(ns + "formula")!.Value);
        Assert.Null(gtRule.Attribute("stopIfTrue"));

        var tcRule = cfs[1].Element(ns + "cfRule")!;
        Assert.Equal("containsText", tcRule.Attribute("type")!.Value);
        Assert.Equal("world", tcRule.Attribute("text")!.Value);
        Assert.Equal("NOT(ISERROR(SEARCH(\"world\",B1)))", tcRule.Element(ns + "formula")!.Value);
        Assert.True(int.Parse(tcRule.Attribute("priority")!.Value) < int.Parse(gtRule.Attribute("priority")!.Value));

        var stylesDoc = XDocument.Load(archive.GetEntry("xl/styles.xml")!.Open());
        var dxfs = stylesDoc.Root!.Element(ns + "dxfs")!;
        Assert.Equal("2", dxfs.Attribute("count")!.Value);

        var gtDxf = dxfs.Elements(ns + "dxf").ElementAt(int.Parse(gtRule.Attribute("dxfId")!.Value));
        var patternFill = gtDxf.Element(ns + "fill")!.Element(ns + "patternFill")!;
        Assert.Null(patternFill.Attribute("patternType"));
        Assert.Equal("FFFFEB9C", patternFill.Element(ns + "bgColor")!.Attribute("rgb")!.Value);
        Assert.NotNull(gtDxf.Element(ns + "font")!.Element(ns + "b"));
    }
}
