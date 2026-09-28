using Xunit;

using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class TextFormatEntryTests
{
    readonly Worksheet sheet = new(5, 5);

    Cell TextCell(string address)
    {
        var cell = sheet.Cells[address];
        cell.Format = new Format { NumberFormat = "@" };
        return cell;
    }

    [Theory]
    [InlineData("0071")]
    [InlineData("123.")]
    [InlineData("12/34")]
    [InlineData("3/4/2026")]
    [InlineData("TRUE")]
    [InlineData("1e5")]
    public void SetValue_OnTextFormattedCell_StoresInputAsLiteralText(string input)
    {
        var cell = TextCell("A1");

        cell.SetValue(input);

        Assert.Equal(input, cell.Value);
        Assert.Equal(CellDataType.String, cell.ValueType);
        Assert.False(cell.QuotePrefix);
        Assert.Equal(input, cell.GetValue());
    }

    [Theory]
    [InlineData("@", true)]
    [InlineData("@@", true)]
    [InlineData("\"ID-\"@", true)]
    [InlineData("@\" kg\"", true)]
    [InlineData("\"ab\"@\"cd\"", true)]
    [InlineData("\"ID-\"@_)", true)]
    [InlineData("[Red]@", true)]
    [InlineData("[Blue]\"ID-\"@", true)]
    [InlineData(";@", true)]
    [InlineData(";;@", true)]
    [InlineData(";;;@", true)]
    [InlineData(";;;\"T:\"@", true)]
    [InlineData("\"x\";;;@", true)]
    [InlineData("\"a\";\"b\";\"c\";@", true)]
    [InlineData("\"0\";;;@", true)]
    [InlineData("\\0;;;@", true)]
    [InlineData("[Red];;;@", true)]
    [InlineData("[=1]\"one\";;;@", true)]
    [InlineData("[>5]\"big\";;;@", true)]
    [InlineData("General;@", true)]
    [InlineData("General;;@", true)]
    [InlineData("General;;;@", true)]
    [InlineData("General;General;General;@", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("General", false)]
    [InlineData(";;;", false)]
    [InlineData("0;@", false)]
    [InlineData("0;0;0;@", false)]
    [InlineData("0;-0;0;\"T:\"@", false)]
    [InlineData("#;;;@", false)]
    [InlineData("?;;;@", false)]
    [InlineData("0.00;;;@", false)]
    [InlineData("\"x\"0;;;@", false)]
    [InlineData("0%;;;@", false)]
    [InlineData("0E+0;;;@", false)]
    [InlineData("mm/dd/yyyy;;;@", false)]
    [InlineData("yyyy;;;@", false)]
    [InlineData("h:mm;;;@", false)]
    [InlineData("[h]:mm;;;@", false)]
    [InlineData("[h];;;@", false)]
    [InlineData("0%", false)]
    [InlineData("# ?/?", false)]
    [InlineData("00000", false)]
    [InlineData("#,##0.00", false)]
    [InlineData("0.00E+00", false)]
    [InlineData("h:mm AM/PM", false)]
    [InlineData("_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)", false)]
    public void IsTextFormat_MatchesExcelTextEntry(string format, bool expected)
    {
        Assert.Equal(expected, NumberFormat.IsTextFormat(format));
    }

    [Theory]
    [InlineData("\"ID-\"@")]
    [InlineData("General;;;@")]
    [InlineData("[Red]@")]
    public void SetValue_OnCustomTextFormattedCell_StoresInputAsLiteralText(string format)
    {
        var cell = sheet.Cells["A1"];
        cell.Format = new Format { NumberFormat = format };

        cell.SetValue("0071");

        Assert.Equal("0071", cell.Value);
        Assert.Equal(CellDataType.String, cell.ValueType);
    }

    [Fact]
    public void SetValue_OnNumberFormatWithTextSection_StillInfersNumber()
    {
        var cell = sheet.Cells["A1"];
        cell.Format = new Format { NumberFormat = "0;0;0;@" };

        cell.SetValue("0071");

        Assert.Equal(71d, cell.Value);
    }

    [Fact]
    public void SetValue_OnTextFormattedCell_StoresFormulaInputAsText()
    {
        var cell = TextCell("A1");

        cell.SetValue("=1+1");

        Assert.Null(cell.Formula);
        Assert.Equal("=1+1", cell.Value);
        Assert.Equal(CellDataType.String, cell.ValueType);
    }

    [Fact]
    public void SetValue_OnTextFormattedFormulaCell_ReplacesFormulaWithText()
    {
        var cell = sheet.Cells["A1"];
        cell.Formula = "=1+1";
        cell.Format = new Format { NumberFormat = "@" };

        cell.SetValue("0071");

        Assert.Null(cell.Formula);
        Assert.Equal("0071", cell.Value);
    }

    [Fact]
    public void SetValue_OnTextFormattedCell_StripsLeadingApostropheAndSetsQuotePrefix()
    {
        var cell = TextCell("A1");

        cell.SetValue("'0071");

        Assert.Equal("0071", cell.Value);
        Assert.True(cell.QuotePrefix);
        Assert.Equal("'0071", cell.GetValue());
    }

    [Fact]
    public void SetValue_EmptyOnTextFormattedCell_ClearsValue()
    {
        var cell = TextCell("A1");
        cell.SetValue("0071");

        cell.SetValue("");

        Assert.Null(cell.Value);
        Assert.Equal("@", cell.Format.NumberFormat);
    }

    [Fact]
    public void SetValue_OnGeneralCell_StillInfersNumber()
    {
        var cell = sheet.Cells["A1"];

        cell.SetValue("0071");

        Assert.Equal(71d, cell.Value);
        Assert.Equal(CellDataType.Number, cell.ValueType);
    }

    [Fact]
    public void Editor_AcceptingInputOnTextFormattedCell_KeepsLeadingZeros()
    {
        TextCell("A1");
        var editor = new Editor(sheet);

        editor.StartEdit(CellRef.Parse("A1"), "0071");
        editor.Accept();

        Assert.Equal("0071", sheet.Cells["A1"].Value);
        Assert.Equal(CellDataType.String, sheet.Cells["A1"].ValueType);
    }

    [Fact]
    public void ExternalPaste_IntoTextFormattedCells_KeepsLiteralText()
    {
        TextCell("A1");
        TextCell("B1");
        TextCell("C1");
        var clipboard = new SpreadsheetClipboard();

        Assert.True(new PasteCommand(clipboard, sheet, RangeRef.Parse("A1"), "0071\t=1+1\t12/34\r\n0071\t=1+1\t12/34").Execute());

        Assert.Equal("0071", sheet.Cells["A1"].Value);
        Assert.Null(sheet.Cells["B1"].Formula);
        Assert.Equal("=1+1", sheet.Cells["B1"].Value);
        Assert.Equal("12/34", sheet.Cells["C1"].Value);
        Assert.Equal(71d, sheet.Cells["A2"].Value);
        Assert.Equal("=1+1", sheet.Cells["B2"].Formula);
    }

    [Fact]
    public void AcceptEditUndo_OnTextFormattedCell_RestoresPreviousNumber()
    {
        var view = new SheetView(sheet);
        var cell = sheet.Cells["A1"];
        cell.Value = 71d;
        cell.Format = new Format { NumberFormat = "@" };
        sheet.Selection.Select(CellRef.Parse("A1"));
        view.Editor.StartEdit(CellRef.Parse("A1"), "0071");
        var command = new AcceptEditCommand(view);

        Assert.True(command.Execute());
        Assert.Equal("0071", cell.Value);

        command.Unexecute();

        Assert.Equal(71d, cell.Value);
        Assert.Equal(CellDataType.Number, cell.ValueType);
    }

    [Fact]
    public void AcceptEditUndo_OnTextFormattedCell_RestoresPreviousFormula()
    {
        var view = new SheetView(sheet);
        sheet.Cells["B1"].Value = 5d;
        var cell = sheet.Cells["A1"];
        cell.Formula = "=B1*2";
        cell.Format = new Format { NumberFormat = "@" };
        sheet.Selection.Select(CellRef.Parse("A1"));
        view.Editor.StartEdit(CellRef.Parse("A1"), "abc");
        var command = new AcceptEditCommand(view);

        Assert.True(command.Execute());
        Assert.Null(cell.Formula);

        command.Unexecute();

        Assert.Equal("=B1*2", cell.Formula);
        Assert.Equal(10d, cell.Value);
    }

    [Fact]
    public void AcceptEditUndo_RecalculatesDependents()
    {
        var view = new SheetView(sheet);
        sheet.Cells["A1"].Value = 1d;
        sheet.Cells["B1"].Formula = "=A1+1";
        sheet.Selection.Select(CellRef.Parse("A1"));
        view.Editor.StartEdit(CellRef.Parse("A1"), "5");
        var command = new AcceptEditCommand(view);

        Assert.True(command.Execute());
        Assert.Equal(6d, sheet.Cells["B1"].Value);

        command.Unexecute();

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void AcceptEditRedo_OnTextFormattedCell_ReappliesText()
    {
        var view = new SheetView(sheet);
        var cell = sheet.Cells["A1"];
        cell.Value = 71d;
        cell.Format = new Format { NumberFormat = "@" };
        sheet.Selection.Select(CellRef.Parse("A1"));
        view.Editor.StartEdit(CellRef.Parse("A1"), "0071");
        var command = new AcceptEditCommand(view);

        command.Execute();
        command.Unexecute();
        command.Execute();

        Assert.Equal("0071", cell.Value);
        Assert.Equal(CellDataType.String, cell.ValueType);

        command.Unexecute();

        Assert.Equal(71d, cell.Value);
    }
}
