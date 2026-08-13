using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable
public class EmptyCellValidationTests
{
    [Fact]
    public void SetValue_EmptyString_ClearsCellToEmpty()
    {
        var sheet = new Worksheet(10, 10);
        var cell = sheet.Cells[0, 0];
        cell.SetValue("test");

        cell.SetValue("");

        Assert.Null(cell.Value);
        Assert.Equal(CellDataType.Empty, cell.ValueType);
    }

    [Fact]
    public void Editor_AcceptEmptyValue_ClearsCell()
    {
        var sheet = new Worksheet(10, 10);
        var cell = sheet.Cells[0, 0];
        cell.SetValue("test");

        var editor = new Editor(sheet);
        editor.StartEdit(cell.Address, "");

        Assert.True(editor.Accept());
        Assert.Equal(CellDataType.Empty, cell.ValueType);
    }

    [Fact]
    public void Editor_AcceptEmptyValue_PassesValidationWithAllowBlank()
    {
        var sheet = new Worksheet(10, 10);
        var cell = sheet.Cells[0, 0];
        cell.SetValue("10");

        sheet.Validation.Add(new RangeRef(cell.Address, cell.Address), new DataValidationRule
        {
            Type = DataValidationType.WholeNumber,
            Operator = DataValidationOperator.GreaterThan,
            Formula1 = "5",
            AllowBlank = true
        });

        var editor = new Editor(sheet);
        editor.StartEdit(cell.Address, "");

        Assert.True(editor.Accept());
        Assert.False(cell.HasValidationErrors);
    }

    [Fact]
    public void CustomFormula_EmptyCellEqualsEmptyString()
    {
        var sheet = new Worksheet(10, 10);
        var cell = sheet.Cells[0, 0];

        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "=A1=\"\"" };

        Assert.True(rule.Validate(cell));
    }
}
