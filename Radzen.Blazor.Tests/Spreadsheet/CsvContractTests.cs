using System;
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// Contract for the CSV reader and writer. One [Fact] per behavior we expose,
// matching the design proposal in the CSV research synthesis.
public class CsvContractTests
{
    private static byte[] WriteToBytes(Workbook wb, CsvExportOptions? options = null)
    {
        using var ms = new MemoryStream();
        wb.SaveAsCsv(ms, options);
        return ms.ToArray();
    }

    private static string Write(Workbook wb, CsvExportOptions? options = null)
    {
        // Decode using the same encoding to round-trip BOM stripping correctly.
        var bytes = WriteToBytes(wb, options);
        var enc = options?.Encoding ?? new UTF8Encoding(true);
        var preambleLen = enc.GetPreamble().Length;
        // Skip the BOM if present so tests can assert on body text directly.
        if (preambleLen > 0 && bytes.Length >= preambleLen
            && enc.GetPreamble().AsSpan().SequenceEqual(bytes.AsSpan(0, preambleLen)))
        {
            return enc.GetString(bytes, preambleLen, bytes.Length - preambleLen);
        }
        return enc.GetString(bytes);
    }

    private static Workbook OneCell(object value)
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 3, 3);
        s.Cells[0, 0].Value = value;
        return wb;
    }

    // ── Writer ──────────────────────────────────────────────────────────────

    [Fact]
    public void Writer_ShouldEmitCommaSeparatedFieldsByDefault()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 3, 3);
        s.Cells[0, 0].SetValue("a"); s.Cells[0, 1].SetValue("b"); s.Cells[0, 2].SetValue("c");

        Assert.Equal("a,b,c\r\n", Write(wb));
    }

    [Fact]
    public void Writer_ShouldRespectCustomSeparator()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 3, 3);
        s.Cells[0, 0].SetValue("a"); s.Cells[0, 1].SetValue("b");

        Assert.Equal("a;b\r\n", Write(wb, new CsvExportOptions { Separator = ';' }));
    }

    [Fact]
    public void Writer_ShouldUseCrlfByDefault_AndRespectCustomLineEnding()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 3, 3);
        s.Cells[0, 0].SetValue("a");
        s.Cells[1, 0].SetValue("b");

        Assert.Equal("a\r\nb\r\n", Write(wb));
        Assert.Equal("a\nb\n", Write(wb, new CsvExportOptions { LineEnding = "\n" }));
    }

    [Fact]
    public void Writer_ShouldEmitUtf8BomByDefault()
    {
        var bytes = WriteToBytes(OneCell("a"));

        // UTF-8 BOM = EF BB BF
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }

    [Fact]
    public void Writer_ShouldNotEmitBom_WhenEncodingHasNoPreamble()
    {
        var bytes = WriteToBytes(OneCell("a"),
            new CsvExportOptions { Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) });

        Assert.Equal((byte)'a', bytes[0]);
    }

    [Fact]
    public void Writer_ShouldQuote_FieldContainingSeparator()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 1, 2);
        s.Cells[0, 0].SetValue("a,b");
        s.Cells[0, 1].SetValue("c");

        Assert.Equal("\"a,b\",c\r\n", Write(wb));
    }

    [Fact]
    public void Writer_ShouldDoubleEmbeddedQuotes_AndWrapField()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 1, 1);
        s.Cells[0, 0].SetValue("she said \"hi\"");

        Assert.Equal("\"she said \"\"hi\"\"\"\r\n", Write(wb));
    }

    [Fact]
    public void Writer_ShouldQuote_FieldContainingNewline()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 1, 1);
        s.Cells[0, 0].SetValue("line1\nline2");

        Assert.Equal("\"line1\nline2\"\r\n", Write(wb));
    }

    [Fact]
    public void Writer_ShouldNotQuote_PlainField_InMinimalMode()
    {
        Assert.Equal("hello\r\n", Write(OneCell("hello")));
    }

    [Fact]
    public void Writer_ShouldQuoteEveryField_WhenQuotingIsAlways()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 1, 2);
        s.Cells[0, 0].SetValue("a"); s.Cells[0, 1].SetValue("b");

        Assert.Equal("\"a\",\"b\"\r\n", Write(wb, new CsvExportOptions { Quoting = CsvQuoting.Always }));
    }

    [Fact]
    public void Writer_ShouldEmitNumberInInvariantCulture()
    {
        Assert.Equal("3.14\r\n", Write(OneCell(3.14)));
    }

    [Fact]
    public void Writer_ShouldEmitDateInIsoFormat()
    {
        Assert.Equal("2024-01-15\r\n", Write(OneCell(new DateTime(2024, 1, 15))));
        Assert.Equal("2024-01-15T13:45:00\r\n",
            Write(OneCell(new DateTime(2024, 1, 15, 13, 45, 0))));
    }

    [Fact]
    public void Writer_ShouldEmitBooleansAsTrueFalse()
    {
        Assert.Equal("TRUE\r\nFALSE\r\n",
            Write(BuildBooleans()));

        static Workbook BuildBooleans()
        {
            var wb = new Workbook();
            var s = wb.AddSheet("Sheet1", 2, 1);
            s.Cells[0, 0].Value = true;
            s.Cells[1, 0].Value = false;
            return wb;
        }
    }

    [Fact]
    public void Writer_FormulaCell_ShouldEmitCachedValue_NotFormulaText()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 1, 3);
        s.Cells[0, 0].Value = 2.0;
        s.Cells[0, 1].Value = 3.0;
        s.Cells[0, 2].Formula = "=A1+B1";

        Assert.Equal("2,3,5\r\n", Write(wb));
    }

    [Fact]
    public void Writer_ShouldUseFirstSheet_WhenNoSheetSpecified()
    {
        var wb = new Workbook();
        var first = wb.AddSheet("First", 1, 1);
        var second = wb.AddSheet("Second", 1, 1);
        first.Cells[0, 0].SetValue("from-first");
        second.Cells[0, 0].SetValue("from-second");

        Assert.Equal("from-first\r\n", Write(wb));
    }

    [Fact]
    public void Writer_ShouldExportSpecifiedSheet()
    {
        var wb = new Workbook();
        var first = wb.AddSheet("First", 1, 1);
        var second = wb.AddSheet("Second", 1, 1);
        first.Cells[0, 0].SetValue("from-first");
        second.Cells[0, 0].SetValue("from-second");

        Assert.Equal("from-second\r\n", Write(wb, new CsvExportOptions { Sheet = second }));
    }

    [Fact]
    public void Writer_EmptyCells_ShouldProduceEmptyFields()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 2, 3);
        s.Cells[0, 0].SetValue("a");
        s.Cells[0, 2].SetValue("c");
        s.Cells[1, 1].SetValue("y");

        Assert.Equal("a,,c\r\n,y,\r\n", Write(wb));
    }

    // ── Reader ──────────────────────────────────────────────────────────────

    private static Workbook Read(string text, CsvImportOptions? options = null)
    {
        var bytes = (options?.Encoding ?? Encoding.UTF8).GetBytes(text);
        using var ms = new MemoryStream(bytes);
        return Workbook.LoadFromCsv(ms, options);
    }

    [Fact]
    public void Reader_ShouldParseSimpleRows()
    {
        var wb = Read("a,b,c\r\nd,e,f\r\n");

        Assert.Single(wb.Sheets);
        var s = wb.Sheets[0];
        Assert.Equal("a", s.Cells[0, 0].Value);
        Assert.Equal("b", s.Cells[0, 1].Value);
        Assert.Equal("c", s.Cells[0, 2].Value);
        Assert.Equal("d", s.Cells[1, 0].Value);
        Assert.Equal("e", s.Cells[1, 1].Value);
        Assert.Equal("f", s.Cells[1, 2].Value);
    }

    [Fact]
    public void Reader_ShouldRespectCustomSeparator()
    {
        var wb = Read("a;b;c", new CsvImportOptions { Separator = ';' });
        Assert.Equal("a", wb.Sheets[0].Cells[0, 0].Value);
        Assert.Equal("b", wb.Sheets[0].Cells[0, 1].Value);
        Assert.Equal("c", wb.Sheets[0].Cells[0, 2].Value);
    }

    [Fact]
    public void Reader_ShouldHandleQuotedFieldsContainingSeparator()
    {
        var wb = Read("\"a,b\",c\r\n");
        Assert.Equal("a,b", wb.Sheets[0].Cells[0, 0].Value);
        Assert.Equal("c", wb.Sheets[0].Cells[0, 1].Value);
    }

    [Fact]
    public void Reader_ShouldHandleEscapedQuotes()
    {
        var wb = Read("\"she said \"\"hi\"\"\"\r\n");
        Assert.Equal("she said \"hi\"", wb.Sheets[0].Cells[0, 0].Value);
    }

    [Fact]
    public void Reader_ShouldHandleEmbeddedNewlinesInQuotedFields()
    {
        var wb = Read("\"line1\nline2\",x\r\n");
        Assert.Equal("line1\nline2", wb.Sheets[0].Cells[0, 0].Value);
        Assert.Equal("x", wb.Sheets[0].Cells[0, 1].Value);
    }

    [Fact]
    public void Reader_ShouldStripUtf8Bom()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }
            .Concat(Encoding.UTF8.GetBytes("a,b\r\n"))
            .ToArray();
        using var ms = new MemoryStream(bytes);
        var wb = Workbook.LoadFromCsv(ms);

        // Without BOM stripping, the first cell would be "﻿a".
        Assert.Equal("a", wb.Sheets[0].Cells[0, 0].Value);
    }

    [Fact]
    public void Reader_ShouldParseNumbers_WhenParseValuesIsTrue()
    {
        var wb = Read("1,2.5,3\r\n");
        Assert.Equal(1d, wb.Sheets[0].Cells[0, 0].Value);
        Assert.Equal(2.5, wb.Sheets[0].Cells[0, 1].Value);
        Assert.Equal(3d, wb.Sheets[0].Cells[0, 2].Value);
    }

    [Fact]
    public void Reader_ShouldKeepStrings_WhenParseValuesIsFalse()
    {
        var wb = Read("1,2.5\r\n", new CsvImportOptions { ParseValues = false });
        Assert.Equal("1", wb.Sheets[0].Cells[0, 0].Value);
        Assert.Equal("2.5", wb.Sheets[0].Cells[0, 1].Value);
        Assert.Equal(CellDataType.String, wb.Sheets[0].Cells[0, 0].ValueType);
    }

    [Fact]
    public void Reader_ShouldStoreFormulas_WhenParseFormulasIsTrue()
    {
        var wb = Read("=A2+B2\r\n2,3\r\n");
        Assert.Equal("=A2+B2", wb.Sheets[0].Cells[0, 0].Formula);
    }

    [Fact]
    public void Reader_ShouldStoreFormulasAsString_WhenParseFormulasIsFalse()
    {
        var wb = Read("=A2+B2\r\n", new CsvImportOptions { ParseFormulas = false });
        Assert.Null(wb.Sheets[0].Cells[0, 0].Formula);
        Assert.Equal("=A2+B2", wb.Sheets[0].Cells[0, 0].Value);
    }

    [Fact]
    public void Reader_ShouldHandleFinalRowWithoutTrailingNewline()
    {
        var wb = Read("a,b,c");
        Assert.Equal("a", wb.Sheets[0].Cells[0, 0].Value);
        Assert.Equal("b", wb.Sheets[0].Cells[0, 1].Value);
        Assert.Equal("c", wb.Sheets[0].Cells[0, 2].Value);
    }

    [Fact]
    public void Reader_ShouldUseConfiguredSheetName()
    {
        var wb = Read("a\r\n", new CsvImportOptions { SheetName = "Imported" });
        Assert.Equal("Imported", wb.Sheets[0].Name);
    }

    [Fact]
    public void Reader_ShouldAcceptLfOnlyLineEndings()
    {
        var wb = Read("a,b\nc,d\n");
        Assert.Equal("a", wb.Sheets[0].Cells[0, 0].Value);
        Assert.Equal("d", wb.Sheets[0].Cells[1, 1].Value);
    }

    // ── Round-trip ──────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_ShouldPreserveStringsNumbersAndQuotedSpecials()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Sheet1", 3, 3);
        s.Cells[0, 0].SetValue("plain");
        s.Cells[0, 1].SetValue("with,comma");
        s.Cells[0, 2].SetValue("with \"quotes\"");
        s.Cells[1, 0].Value = 42.0;
        s.Cells[1, 1].SetValue("line1\nline2");

        using var ms = new MemoryStream();
        wb.SaveAsCsv(ms);
        ms.Position = 0;

        var loaded = Workbook.LoadFromCsv(ms);
        var ls = loaded.Sheets[0];

        Assert.Equal("plain", ls.Cells[0, 0].Value);
        Assert.Equal("with,comma", ls.Cells[0, 1].Value);
        Assert.Equal("with \"quotes\"", ls.Cells[0, 2].Value);
        Assert.Equal(42d, ls.Cells[1, 0].Value);
        Assert.Equal("line1\nline2", ls.Cells[1, 1].Value);
    }
}
