using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class SpreadsheetCultureTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Brazilian = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private static Worksheet CreateSheet(CultureInfo culture)
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sheet1", 20, 10);
        wb.Culture = culture;
        return sheet;
    }

    [Theory]
    [InlineData("10,50", 10.5)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1.234", 1234d)]
    public void SetValue_ParsesNumbersWithGermanCulture(string text, double expected)
    {
        var sheet = CreateSheet(German);
        sheet.Cells["A1"].SetValue(text);

        Assert.Equal(CellDataType.Number, sheet.Cells["A1"].ValueType);
        Assert.Equal(expected, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void SetValue_ParsesGermanDateDespiteGroupSeparatorCollision()
    {
        var sheet = CreateSheet(German);
        sheet.Cells["A1"].SetValue("17.08.2024");

        Assert.Equal(CellDataType.Date, sheet.Cells["A1"].ValueType);
        Assert.Equal(new DateTime(2024, 8, 17), sheet.Cells["A1"].Value);
    }

    [Fact]
    public void SetValue_ParsesGermanShortDate()
    {
        var sheet = CreateSheet(German);
        sheet.Cells["A1"].SetValue("12.11");

        Assert.Equal(CellDataType.Date, sheet.Cells["A1"].ValueType);
        var date = Assert.IsType<DateTime>(sheet.Cells["A1"].Value);
        Assert.Equal(11, date.Month);
        Assert.Equal(12, date.Day);
    }

    [Fact]
    public void SetValue_InvariantDateStillParsesUnderGermanCulture()
    {
        var sheet = CreateSheet(German);
        sheet.Cells["A1"].SetValue("08/17/2024");

        Assert.Equal(CellDataType.Date, sheet.Cells["A1"].ValueType);
        Assert.Equal(new DateTime(2024, 8, 17), sheet.Cells["A1"].Value);
    }

    [Fact]
    public void SetValue_AmbiguousDateFollowsTheCulture()
    {
        var sheet = CreateSheet(German);
        sheet.Cells["A1"].SetValue("01/02/2024");

        Assert.Equal(new DateTime(2024, 2, 1), sheet.Cells["A1"].Value);
    }

    [Theory]
    [InlineData("10,50")]
    [InlineData("1.234,56")]
    public void SetValueGetValue_RoundTripsNumbersUnderGermanCulture(string text)
    {
        var sheet = CreateSheet(German);
        sheet.Cells["A1"].SetValue(text);
        var first = sheet.Cells["A1"].Value;

        sheet.Cells["A1"].SetValue(sheet.Cells["A1"].GetValue());

        Assert.Equal(first, sheet.Cells["A1"].Value);
        Assert.Equal(CellDataType.Number, sheet.Cells["A1"].ValueType);
    }

    [Theory]
    [InlineData("de-DE", "17.08.2024")]
    [InlineData("pt-BR", "17/08/2024")]
    public void SetValueGetValue_RoundTripsDates(string cultureName, string text)
    {
        var sheet = CreateSheet(CultureInfo.GetCultureInfo(cultureName));
        sheet.Cells["A1"].SetValue(text);
        Assert.Equal(CellDataType.Date, sheet.Cells["A1"].ValueType);
        var first = sheet.Cells["A1"].Value;

        sheet.Cells["A1"].SetValue(sheet.Cells["A1"].GetValue());

        Assert.Equal(first, sheet.Cells["A1"].Value);
        Assert.Equal(CellDataType.Date, sheet.Cells["A1"].ValueType);
    }

    [Fact]
    public void GetValue_FormatsNumberWithWorkbookCulture()
    {
        var sheet = CreateSheet(German);
        sheet.Cells["A1"].Value = 10.5;

        Assert.Equal("10,5", sheet.Cells["A1"].GetValue());
    }

    [Fact]
    public void FreshWorkbook_DefaultsToCurrentCulture()
    {
        Assert.Same(CultureInfo.CurrentCulture, new Workbook().Culture);
    }

    [Fact]
    public void InvariantCulture_ReproducesLegacyParsing()
    {
        var sheet = CreateSheet(CultureInfo.InvariantCulture);
        sheet.Cells["A1"].SetValue("10,50");

        Assert.Equal(CellDataType.Number, sheet.Cells["A1"].ValueType);
        Assert.Equal(1050d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void PublicCellDataConstructor_StaysInvariant()
    {
        var data = new CellData("10,50");

        Assert.Equal(CellDataType.Number, data.Type);
        Assert.Equal(1050d, data.Value);
    }

    [Fact]
    public void Editor_RoundTripsEditUnderGermanCulture()
    {
        var sheet = CreateSheet(German);
        sheet.Cells["A1"].Value = 10.5;

        var editor = new Editor(sheet);
        editor.StartEdit(CellRef.Parse("A1"), sheet.Cells["A1"].GetValue());

        Assert.False(editor.HasChanges);

        editor.Value = "1.234,56";
        Assert.True(editor.Accept());
        Assert.Equal(1234.56, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void Editor_LocalizedFormulaRoundTripsWithoutSpuriousChanges()
    {
        var sheet = CreateSheet(German);
        sheet.Cells["A1"].SetValue("=SUM(B1;1,5)");

        Assert.Equal("=SUM(B1,1.5)", sheet.Cells["A1"].Formula);
        Assert.Equal("=SUM(B1;1,5)", sheet.Cells["A1"].GetValue());

        var editor = new Editor(sheet);
        editor.StartEdit(CellRef.Parse("A1"), sheet.Cells["A1"].GetValue());
        Assert.False(editor.HasChanges);
    }

    [Fact]
    public void Editor_LenientCommaFormulaCanonicalizesOnCommit()
    {
        var sheet = CreateSheet(German);
        sheet.Cells["A1"].SetValue("=SUM(B1,B2)");

        Assert.Equal("=SUM(B1,B2)", sheet.Cells["A1"].Formula);
        Assert.Equal("=SUM(B1;B2)", sheet.Cells["A1"].GetValue());
    }

    [Fact]
    public void Clipboard_RoundTripsUnderGermanCulture()
    {
        var sheet = CreateSheet(German);
        sheet.Cells["A1"].Value = 10.5;
        sheet.Cells["A2"].SetValue("17.08.2024");
        sheet.Cells["A3"].SetValue("=SUM(A1;1,5)");

        var text = sheet.GetDelimitedString(RangeRef.Parse("A1:A3"));
        sheet.InsertDelimitedString(CellRef.Parse("C1"), text);

        Assert.Equal(10.5, sheet.Cells["C1"].Value);
        Assert.Equal(sheet.Cells["A2"].Value, sheet.Cells["C2"].Value);
        Assert.Equal("=SUM(A1,1.5)", sheet.Cells["C3"].Formula);
    }

    [Fact]
    public void NumberFormat_RendersWithCultureSeparators()
    {
        Assert.Equal("1.234,50", NumberFormat.Apply("#,##0.00", 1234.5, CellDataType.Number, German));
        Assert.Equal("1,234.50", NumberFormat.Apply("#,##0.00", 1234.5, CellDataType.Number));
    }

    [Fact]
    public void NumberFormat_RendersMultiCharGroupSeparator()
    {
        var expected = 1234.5.ToString("#,##0.00", French);
        Assert.Equal(expected, NumberFormat.Apply("#,##0.00", 1234.5, CellDataType.Number, French));
    }

    [Fact]
    public void NumberFormat_RendersGermanMonthNames()
    {
        var date = new DateTime(2024, 3, 17);
        var text = NumberFormat.Apply("dd-mmm-yyyy", date, CellDataType.Date, German);

        Assert.Equal($"17-{date.ToString("MMM", German)}-2024", text);
        Assert.Equal("17-Mar-2024", NumberFormat.Apply("dd-mmm-yyyy", date, CellDataType.Date));
    }

    [Fact]
    public void NumberFormat_ScientificUsesCultureDecimalSeparator()
    {
        Assert.Equal("1,23E+03", NumberFormat.Apply("0.00E+00", 1234.5, CellDataType.Number, German));
    }

    [Fact]
    public void Ingestion_ParsesInvariantRegardlessOfWorkbookCulture()
    {
        using var stream = new MemoryStream();

        var source = new Workbook();
        var sourceSheet = source.AddSheet("Sheet1", 5, 5);
        sourceSheet.Cells["A1"].Value = 10.5;
        sourceSheet.Cells["A2"].SetValue("hello");
        source.SaveToStream(stream);

        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = German;

        try
        {
            stream.Position = 0;
            var loaded = Workbook.LoadFromStream(stream);

            Assert.Equal(10.5, loaded.Sheets[0].Cells["A1"].Value);
            Assert.Equal(CellDataType.Number, loaded.Sheets[0].Cells["A1"].ValueType);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void XlsxRoundTrip_IsPristineUnderGermanCulture()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sheet1", 10, 5);
        sheet.Cells["A1"].Value = 1234.56;
        sheet.Cells["A1"].Format.NumberFormat = "#,##0.00";
        sheet.Cells["A2"].SetValue("17.08.2024");
        sheet.Cells["A3"].Formula = "=SUM(A1,1.5)";
        wb.Culture = German;

        using var germanStream = new MemoryStream();
        wb.SaveToStream(germanStream);

        wb.Culture = CultureInfo.InvariantCulture;
        using var invariantStream = new MemoryStream();
        wb.SaveToStream(invariantStream);

        // The archive embeds timestamps/uids; culture independence is about the sheet XML.
        Assert.Equal(ReadSheetXml(invariantStream), ReadSheetXml(germanStream));

        germanStream.Position = 0;
        var reloaded = Workbook.LoadFromStream(germanStream);

        Assert.Equal(1234.56, reloaded.Sheets[0].Cells["A1"].Value);
        Assert.Equal("=SUM(A1,1.5)", reloaded.Sheets[0].Cells["A3"].Formula);
    }

    private static string ReadSheetXml(MemoryStream stream)
    {
        stream.Position = 0;
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        // The worksheet element carries a random per-save uid.
        return Regex.Replace(reader.ReadToEnd(), "uid=\"\\{[0-9A-F-]+\\}\"", "uid=\"\"");
    }

    [Fact]
    public void ValidationList_ItemsAndValidationShareTheWorkbookCulture()
    {
        var sheet = CreateSheet(German);
        sheet.Cells["B1"].Value = 1.5;
        sheet.Cells["B2"].Value = 2.5;

        var rule = new DataValidationRule
        {
            Type = DataValidationType.List,
            Formula1 = "=B1:B2"
        };

        var items = rule.GetListItems(sheet);
        Assert.Contains("1,5", items);

        sheet.Cells["A1"].SetValue("1,5");
        sheet.Validation.Add(RangeRef.Parse("A1:A1"), rule);
        Assert.True(rule.Validate(sheet.Cells["A1"]));
    }

    [Fact]
    public void Autofill_PlainCopyDoesNotReinferStringType()
    {
        var sheet = CreateSheet(German);
        // Text under invariant inference; a Date if re-inferred under de-DE.
        sheet.Cells["A1"].SetValueInvariant("31.12.2024");
        Assert.Equal(CellDataType.String, sheet.Cells["A1"].ValueType);

        var command = new AutofillCommand(sheet, RangeRef.Parse("A1:A1"), RangeRef.Parse("A1:A3"), AutofillDirection.Down);
        command.Execute();

        Assert.Equal(CellDataType.String, sheet.Cells["A3"].ValueType);
        Assert.Equal("31.12.2024", sheet.Cells["A3"].Value);
    }

    [Fact]
    public void HyperlinkCommand_UndoRestoresExactCellData()
    {
        var sheet = CreateSheet(German);
        sheet.Cells["A1"].SetValueInvariant("31.12.2024");
        var before = sheet.Cells["A1"].Data;

        var command = new HyperlinkCommand(sheet, CellRef.Parse("A1"), new Hyperlink { Url = "https://radzen.com", Text = "Radzen" });
        command.Execute();
        Assert.Equal("Radzen", sheet.Cells["A1"].Value);

        command.Unexecute();

        Assert.Same(before, sheet.Cells["A1"].Data);
        Assert.Equal(CellDataType.String, sheet.Cells["A1"].ValueType);
    }

    [Fact]
    public void FormatParseCache_StaysBounded()
    {
        for (var i = 0; i < 600; i++)
        {
            NumberFormat.Apply($"\"c{i}\"0.00", 1.5, CellDataType.Number);
        }

        // Bounded, not exact: the capacity guard admits a small race overshoot.
        Assert.InRange(NumberFormatParser.CacheCount, 0, 520);
    }
}
