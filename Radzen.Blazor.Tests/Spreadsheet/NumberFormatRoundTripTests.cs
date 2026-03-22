using System;
using System.IO;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class NumberFormatRoundTripTests
{
    [Fact]
    public void RoundTrip_CurrencyFormat_Preserved()
    {
        var workbook = CreateWorkbookWithFormats();
        var reimported = ExportAndReimport(workbook);

        var sheet = reimported.Sheets[0];
        Assert.Equal("$#,##0.00", sheet.Cells[0, 0].Format.NumberFormat);
    }

    [Fact]
    public void RoundTrip_PercentageFormat_Preserved()
    {
        var workbook = CreateWorkbookWithFormats();
        var reimported = ExportAndReimport(workbook);

        var sheet = reimported.Sheets[0];
        Assert.Equal("0%", sheet.Cells[1, 0].Format.NumberFormat);
    }

    [Fact]
    public void RoundTrip_NumberFormat_Preserved()
    {
        var workbook = CreateWorkbookWithFormats();
        var reimported = ExportAndReimport(workbook);

        var sheet = reimported.Sheets[0];
        Assert.Equal("#,##0.00", sheet.Cells[2, 0].Format.NumberFormat);
    }

    [Fact]
    public void RoundTrip_DateFormat_Preserved()
    {
        var workbook = CreateWorkbookWithFormats();
        var reimported = ExportAndReimport(workbook);

        var sheet = reimported.Sheets[0];
        Assert.Equal("mm/dd/yyyy", sheet.Cells[3, 0].Format.NumberFormat);
    }

    [Fact]
    public void RoundTrip_CurrencyValue_DisplaysCorrectly()
    {
        var workbook = CreateWorkbookWithFormats();
        var reimported = ExportAndReimport(workbook);

        var sheet = reimported.Sheets[0];
        var cell = sheet.Cells[0, 0];
        var display = NumberFormat.Apply(cell.Format.NumberFormat, cell.Value, cell.ValueType);
        Assert.Equal("$1,234.50", display);
    }

    [Fact]
    public void RoundTrip_PercentageValue_DisplaysCorrectly()
    {
        var workbook = CreateWorkbookWithFormats();
        var reimported = ExportAndReimport(workbook);

        var sheet = reimported.Sheets[0];
        var cell = sheet.Cells[1, 0];
        var display = NumberFormat.Apply(cell.Format.NumberFormat, cell.Value, cell.ValueType);
        Assert.Equal("12%", display);
    }

    [Fact]
    public void RoundTrip_DateValue_DisplaysCorrectly()
    {
        var workbook = CreateWorkbookWithFormats();
        var reimported = ExportAndReimport(workbook);

        var sheet = reimported.Sheets[0];
        var cell = sheet.Cells[3, 0];
        var display = NumberFormat.Apply(cell.Format.NumberFormat, cell.Value, cell.ValueType);
        Assert.Equal("03/19/2024", display);
    }

    [Fact]
    public void RoundTrip_OtherFormatting_StillPreserved()
    {
        var workbook = CreateWorkbookWithFormats();
        var reimported = ExportAndReimport(workbook);

        var sheet = reimported.Sheets[0];
        // Row 0 was bold + currency
        Assert.True(sheet.Cells[0, 0].Format.Bold);
    }

    [Fact]
    public void RoundTrip_ScientificFormat_Preserved()
    {
        var workbook = CreateWorkbookWithFormats();
        var reimported = ExportAndReimport(workbook);

        var sheet = reimported.Sheets[0];
        Assert.Equal("0.00E+00", sheet.Cells[4, 0].Format.NumberFormat);
    }

    [Fact]
    public void RoundTrip_ScientificValue_DisplaysCorrectly()
    {
        var workbook = CreateWorkbookWithFormats();
        var reimported = ExportAndReimport(workbook);

        var sheet = reimported.Sheets[0];
        var cell = sheet.Cells[4, 0];
        var display = NumberFormat.Apply(cell.Format.NumberFormat, cell.Value, CellDataType.Number);
        Assert.Equal("1.23E+04", display);
    }

    [Fact]
    public void RoundTrip_AccountingFormat_Preserved()
    {
        var workbook = CreateWorkbookWithFormats();
        var reimported = ExportAndReimport(workbook);

        var sheet = reimported.Sheets[0];
        Assert.Equal("_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)", sheet.Cells[5, 0].Format.NumberFormat);
    }

    private static Workbook CreateWorkbookWithFormats()
    {
        var workbook = new Workbook();
        var sheet = new Sheet(10, 5);
        workbook.AddSheet(sheet);

        // Currency + Bold
        sheet.Cells[0, 0].Value = 1234.5;
        sheet.Cells[0, 0].Format.NumberFormat = "$#,##0.00";
        sheet.Cells[0, 0].Format.Bold = true;

        // Percentage
        sheet.Cells[1, 0].Value = 0.1234;
        sheet.Cells[1, 0].Format.NumberFormat = "0%";

        // Number with thousands
        sheet.Cells[2, 0].Value = 42.0;
        sheet.Cells[2, 0].Format.NumberFormat = "#,##0.00";

        // Date (serial number for 2024-03-19)
        sheet.Cells[3, 0].Value = 45370.0;
        sheet.Cells[3, 0].Format.NumberFormat = "mm/dd/yyyy";

        // Scientific
        sheet.Cells[4, 0].Value = 12345.0;
        sheet.Cells[4, 0].Format.NumberFormat = "0.00E+00";

        // Accounting
        sheet.Cells[5, 0].Value = 1234.5;
        sheet.Cells[5, 0].Format.NumberFormat = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";

        return workbook;
    }

    private static Workbook ExportAndReimport(Workbook workbook)
    {
        using var ms = new MemoryStream();
        workbook.SaveToStream(ms);
        ms.Position = 0;
        return Workbook.LoadFromStream(ms);
    }
}
