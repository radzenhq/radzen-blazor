using System.Linq;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class DataValidationTests
{
    private static Cell CreateCell(object? value)
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = value;
        return sheet.Cells[0, 0];
    }

    // WholeNumber validation tests

    [Fact]
    public void WholeNumber_Between_Valid()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.Between, Formula1 = "1", Formula2 = "10" };
        Assert.True(rule.Validate(CreateCell(5)));
        Assert.True(rule.Validate(CreateCell(1)));
        Assert.True(rule.Validate(CreateCell(10)));
    }

    [Fact]
    public void WholeNumber_Between_Invalid()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.Between, Formula1 = "1", Formula2 = "10" };
        Assert.False(rule.Validate(CreateCell(0)));
        Assert.False(rule.Validate(CreateCell(11)));
        Assert.False(rule.Validate(CreateCell(5.5)));
    }

    [Fact]
    public void WholeNumber_NotBetween()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.NotBetween, Formula1 = "1", Formula2 = "10" };
        Assert.True(rule.Validate(CreateCell(0)));
        Assert.True(rule.Validate(CreateCell(11)));
        Assert.False(rule.Validate(CreateCell(5)));
    }

    [Fact]
    public void WholeNumber_Equal()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.Equal, Formula1 = "5" };
        Assert.True(rule.Validate(CreateCell(5)));
        Assert.False(rule.Validate(CreateCell(6)));
    }

    [Fact]
    public void WholeNumber_NotEqual()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.NotEqual, Formula1 = "5" };
        Assert.True(rule.Validate(CreateCell(6)));
        Assert.False(rule.Validate(CreateCell(5)));
    }

    [Fact]
    public void WholeNumber_GreaterThan()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThan, Formula1 = "5" };
        Assert.True(rule.Validate(CreateCell(6)));
        Assert.False(rule.Validate(CreateCell(5)));
        Assert.False(rule.Validate(CreateCell(4)));
    }

    [Fact]
    public void WholeNumber_LessThan()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.LessThan, Formula1 = "5" };
        Assert.True(rule.Validate(CreateCell(4)));
        Assert.False(rule.Validate(CreateCell(5)));
        Assert.False(rule.Validate(CreateCell(6)));
    }

    [Fact]
    public void WholeNumber_GreaterThanOrEqual()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThanOrEqual, Formula1 = "5" };
        Assert.True(rule.Validate(CreateCell(5)));
        Assert.True(rule.Validate(CreateCell(6)));
        Assert.False(rule.Validate(CreateCell(4)));
    }

    [Fact]
    public void WholeNumber_LessThanOrEqual()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.LessThanOrEqual, Formula1 = "5" };
        Assert.True(rule.Validate(CreateCell(5)));
        Assert.True(rule.Validate(CreateCell(4)));
        Assert.False(rule.Validate(CreateCell(6)));
    }

    [Fact]
    public void WholeNumber_RejectsNonNumbers()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.Between, Formula1 = "1", Formula2 = "10", AllowBlank = false };
        Assert.False(rule.Validate(CreateCell("text")));
    }

    // Decimal validation tests

    [Fact]
    public void Decimal_Between_Valid()
    {
        var rule = new DataValidationRule { Type = DataValidationType.Decimal, Operator = DataValidationOperator.Between, Formula1 = "1.5", Formula2 = "10.5" };
        Assert.True(rule.Validate(CreateCell(5.5)));
        Assert.True(rule.Validate(CreateCell(1.5)));
        Assert.True(rule.Validate(CreateCell(10.5)));
    }

    [Fact]
    public void Decimal_Between_Invalid()
    {
        var rule = new DataValidationRule { Type = DataValidationType.Decimal, Operator = DataValidationOperator.Between, Formula1 = "1.5", Formula2 = "10.5" };
        Assert.False(rule.Validate(CreateCell(1.4)));
        Assert.False(rule.Validate(CreateCell(10.6)));
    }

    // List validation tests

    [Fact]
    public void List_ValidValue()
    {
        var rule = new DataValidationRule { Type = DataValidationType.List, Formula1 = "Yes,No,Maybe" };
        Assert.True(rule.Validate(CreateCell("Yes")));
        Assert.True(rule.Validate(CreateCell("No")));
        Assert.True(rule.Validate(CreateCell("Maybe")));
    }

    [Fact]
    public void List_InvalidValue()
    {
        var rule = new DataValidationRule { Type = DataValidationType.List, Formula1 = "Yes,No,Maybe" };
        Assert.False(rule.Validate(CreateCell("Other")));
    }

    [Fact]
    public void List_CaseInsensitive()
    {
        var rule = new DataValidationRule { Type = DataValidationType.List, Formula1 = "Yes,No" };
        Assert.True(rule.Validate(CreateCell("yes")));
        Assert.True(rule.Validate(CreateCell("YES")));
    }

    [Fact]
    public void ListItems_Property()
    {
        var rule = new DataValidationRule { Type = DataValidationType.List, Formula1 = "A,B,C" };
        var items = rule.ListItems;
        Assert.Equal(3, items.Count);
        Assert.Equal("A", items[0]);
        Assert.Equal("B", items[1]);
        Assert.Equal("C", items[2]);
    }

    [Fact]
    public void ListItems_NotListType_ReturnsEmpty()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Formula1 = "1" };
        Assert.Empty(rule.ListItems);
    }

    // TextLength validation tests

    [Fact]
    public void TextLength_Between()
    {
        var rule = new DataValidationRule { Type = DataValidationType.TextLength, Operator = DataValidationOperator.Between, Formula1 = "1", Formula2 = "5" };
        Assert.True(rule.Validate(CreateCell("abc")));
        Assert.True(rule.Validate(CreateCell("a")));
        Assert.True(rule.Validate(CreateCell("abcde")));
        Assert.False(rule.Validate(CreateCell("abcdef")));
    }

    [Fact]
    public void TextLength_GreaterThan()
    {
        var rule = new DataValidationRule { Type = DataValidationType.TextLength, Operator = DataValidationOperator.GreaterThan, Formula1 = "3" };
        Assert.True(rule.Validate(CreateCell("abcd")));
        Assert.False(rule.Validate(CreateCell("abc")));
    }

    // AllowBlank tests

    [Fact]
    public void AllowBlank_True_AllowsEmpty()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThan, Formula1 = "5", AllowBlank = true };
        Assert.True(rule.Validate(CreateCell(null)));
        Assert.True(rule.Validate(CreateCell("")));
    }

    [Fact]
    public void AllowBlank_False_RejectsEmpty()
    {
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThan, Formula1 = "5", AllowBlank = false };
        Assert.False(rule.Validate(CreateCell(null)));
    }

    // ValidationStore tests

    [Fact]
    public void ValidationStore_Add_Remove()
    {
        var store = new ValidationStore();
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThan, Formula1 = "5" };

        store.Add(range, rule);
        Assert.True(store.Remove(range, rule));
        Assert.False(store.Remove(range, rule));
    }

    [Fact]
    public void ValidationStore_Ranges()
    {
        var store = new ValidationStore();
        var range1 = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        var range2 = new RangeRef(new CellRef(6, 6), new CellRef(10, 10));
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThan, Formula1 = "5" };

        store.Add(range1, rule);
        store.Add(range2, rule);

        Assert.Equal(2, store.Ranges.Count());
    }

    [Fact]
    public void ValidationStore_GetValidatorsForCell()
    {
        var store = new ValidationStore();
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThan, Formula1 = "5" };

        store.Add(range, rule);

        var validators = store.GetValidatorsForCell(new CellRef(3, 3));
        Assert.Single(validators);

        var outside = store.GetValidatorsForCell(new CellRef(6, 6));
        Assert.Empty(outside);
    }

    [Fact]
    public void ValidationStore_RemoveAll()
    {
        var store = new ValidationStore();
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        var rule1 = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThan, Formula1 = "5" };
        var rule2 = new NumberValidator();

        store.Add(range, rule1);
        store.Add(range, rule2);

        var removed = store.RemoveAll(range);
        Assert.Equal(2, removed.Count);
        Assert.Empty(store.Ranges);
    }

    [Fact]
    public void ValidationStore_GetValidators()
    {
        var store = new ValidationStore();
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThan, Formula1 = "5" };

        store.Add(range, rule);
        Assert.Single(store.GetValidators(range));

        var noRange = new RangeRef(new CellRef(10, 10), new CellRef(15, 15));
        Assert.Empty(store.GetValidators(noRange));
    }

    // Command tests

    [Fact]
    public void DataValidationCommand_ExecuteAndUnexecute()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 3;
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThan, Formula1 = "5" };

        var command = new DataValidationCommand(sheet, range, rule);
        Assert.True(command.Execute());

        // Cell should now have validation errors
        Assert.True(sheet.Cells[0, 0].HasValidationErrors);

        command.Unexecute();

        // Re-validate after rule removal
        sheet.Cells[0, 0].Validate();
        Assert.False(sheet.Cells[0, 0].HasValidationErrors);
    }

    [Fact]
    public void DataValidationCommand_ValidValue()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 10;
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThan, Formula1 = "5" };

        var command = new DataValidationCommand(sheet, range, rule);
        Assert.True(command.Execute());

        Assert.False(sheet.Cells[0, 0].HasValidationErrors);
    }

    [Fact]
    public void ClearValidationCommand_ExecuteAndUnexecute()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 3;
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThan, Formula1 = "5" };

        sheet.Validation.Add(range, rule);
        sheet.Cells[0, 0].Validate();
        Assert.True(sheet.Cells[0, 0].HasValidationErrors);

        var clearCommand = new ClearValidationCommand(sheet, range);
        clearCommand.Execute();

        sheet.Cells[0, 0].Validate();
        Assert.False(sheet.Cells[0, 0].HasValidationErrors);

        clearCommand.Unexecute();

        sheet.Cells[0, 0].Validate();
        Assert.True(sheet.Cells[0, 0].HasValidationErrors);
    }

    [Fact]
    public void Command_IntegrationWithUndoRedo()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 3;
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.GreaterThan, Formula1 = "5" };

        var command = new DataValidationCommand(sheet, range, rule);
        sheet.Commands.Execute(command);

        Assert.True(sheet.Cells[0, 0].HasValidationErrors);
        Assert.True(sheet.Commands.CanUndo);

        sheet.Commands.Undo();
        sheet.Cells[0, 0].Validate();
        Assert.False(sheet.Cells[0, 0].HasValidationErrors);

        sheet.Commands.Redo();
        Assert.True(sheet.Cells[0, 0].HasValidationErrors);
    }

    // XLSX round-trip tests

    [Fact]
    public void Xlsx_RoundTrip_WholeNumber()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.WholeNumber,
            Operator = DataValidationOperator.Between,
            Formula1 = "1",
            Formula2 = "100",
            Error = "Must be between 1 and 100",
            ErrorTitle = "Invalid",
            ShowErrorMessage = true
        };
        sheet.Validation.Add(range, rule);

        var loaded = SaveAndLoad(workbook);
        var loadedSheet = loaded.Sheets[0];

        var validators = loadedSheet.Validation.GetValidators(range);
        Assert.Single(validators);
        var loadedRule = Assert.IsType<DataValidationRule>(validators[0]);
        Assert.Equal(DataValidationType.WholeNumber, loadedRule.Type);
        Assert.Equal(DataValidationOperator.Between, loadedRule.Operator);
        Assert.Equal("1", loadedRule.Formula1);
        Assert.Equal("100", loadedRule.Formula2);
        Assert.Equal("Must be between 1 and 100", loadedRule.Error);
        Assert.Equal("Invalid", loadedRule.ErrorTitle);
    }

    [Fact]
    public void Xlsx_RoundTrip_List()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 0));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.List,
            Formula1 = "Yes,No,Maybe"
        };
        sheet.Validation.Add(range, rule);

        var loaded = SaveAndLoad(workbook);
        var loadedSheet = loaded.Sheets[0];

        var validators = loadedSheet.Validation.GetValidators(range);
        Assert.Single(validators);
        var loadedRule = Assert.IsType<DataValidationRule>(validators[0]);
        Assert.Equal(DataValidationType.List, loadedRule.Type);
        Assert.Equal("Yes,No,Maybe", loadedRule.Formula1);
    }

    [Fact]
    public void Xlsx_RoundTrip_Decimal()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.Decimal,
            Operator = DataValidationOperator.GreaterThan,
            Formula1 = "3.14"
        };
        sheet.Validation.Add(range, rule);

        var loaded = SaveAndLoad(workbook);
        var loadedSheet = loaded.Sheets[0];

        var validators = loadedSheet.Validation.GetValidators(range);
        Assert.Single(validators);
        var loadedRule = Assert.IsType<DataValidationRule>(validators[0]);
        Assert.Equal(DataValidationType.Decimal, loadedRule.Type);
        Assert.Equal(DataValidationOperator.GreaterThan, loadedRule.Operator);
        Assert.Equal("3.14", loadedRule.Formula1);
    }

    [Fact]
    public void Xlsx_RoundTrip_TextLength()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.TextLength,
            Operator = DataValidationOperator.LessThanOrEqual,
            Formula1 = "50"
        };
        sheet.Validation.Add(range, rule);

        var loaded = SaveAndLoad(workbook);
        var loadedSheet = loaded.Sheets[0];

        var validators = loadedSheet.Validation.GetValidators(range);
        Assert.Single(validators);
        var loadedRule = Assert.IsType<DataValidationRule>(validators[0]);
        Assert.Equal(DataValidationType.TextLength, loadedRule.Type);
        Assert.Equal(DataValidationOperator.LessThanOrEqual, loadedRule.Operator);
        Assert.Equal("50", loadedRule.Formula1);
    }

    [Fact]
    public void Xlsx_RoundTrip_PreservesErrorStyle()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.WholeNumber,
            Operator = DataValidationOperator.GreaterThan,
            Formula1 = "0",
            ErrorStyle = DataValidationErrorStyle.Warning
        };
        sheet.Validation.Add(range, rule);

        var loaded = SaveAndLoad(workbook);
        var loadedSheet = loaded.Sheets[0];

        var validators = loadedSheet.Validation.GetValidators(range);
        var loadedRule = Assert.IsType<DataValidationRule>(validators[0]);
        Assert.Equal(DataValidationErrorStyle.Warning, loadedRule.ErrorStyle);
    }

    [Fact]
    public void Xlsx_NoValidation_NoElement()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 10, 10);

        var loaded = SaveAndLoad(workbook);
        var loadedSheet = loaded.Sheets[0];

        Assert.Empty(loadedSheet.Validation.Ranges);
    }

    [Fact]
    public void Xlsx_RoundTrip_AllowBlank()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.WholeNumber,
            Operator = DataValidationOperator.GreaterThan,
            Formula1 = "0",
            AllowBlank = false
        };
        sheet.Validation.Add(range, rule);

        var loaded = SaveAndLoad(workbook);
        var loadedSheet = loaded.Sheets[0];

        var validators = loadedSheet.Validation.GetValidators(range);
        var loadedRule = Assert.IsType<DataValidationRule>(validators[0]);
        Assert.False(loadedRule.AllowBlank);
    }

    // Date validation with date string formulas

    [Fact]
    public void Date_Between_WithDateStrings()
    {
        var rule = new DataValidationRule
        {
            Type = DataValidationType.Date,
            Operator = DataValidationOperator.Between,
            Formula1 = "2024-01-01",
            Formula2 = "2024-12-31"
        };
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = new System.DateTime(2024, 6, 15);
        Assert.True(rule.Validate(sheet.Cells[0, 0]));

        sheet.Cells[0, 1].Value = new System.DateTime(2025, 1, 1);
        Assert.False(rule.Validate(sheet.Cells[0, 1]));
    }

    [Fact]
    public void Date_GreaterThan_WithDateString()
    {
        var rule = new DataValidationRule
        {
            Type = DataValidationType.Date,
            Operator = DataValidationOperator.GreaterThan,
            Formula1 = "2024-06-01"
        };
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = new System.DateTime(2024, 7, 1);
        Assert.True(rule.Validate(sheet.Cells[0, 0]));

        sheet.Cells[0, 1].Value = new System.DateTime(2024, 5, 1);
        Assert.False(rule.Validate(sheet.Cells[0, 1]));
    }

    // XLSX list round-trip with per-item quoting

    [Fact]
    public void Xlsx_RoundTrip_List_PreservesItems()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 0));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.List,
            Formula1 = "Red,Green,Blue"
        };
        sheet.Validation.Add(range, rule);

        var loaded = SaveAndLoad(workbook);
        var loadedSheet = loaded.Sheets[0];

        var validators = loadedSheet.Validation.GetValidators(range);
        var loadedRule = Assert.IsType<DataValidationRule>(validators[0]);
        Assert.Equal(DataValidationType.List, loadedRule.Type);
        var items = loadedRule.ListItems;
        Assert.Equal(3, items.Count);
        Assert.Equal("Red", items[0]);
        Assert.Equal("Green", items[1]);
        Assert.Equal("Blue", items[2]);
    }

    private static Workbook SaveAndLoad(Workbook workbook)
    {
        using var stream = new System.IO.MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;
        return Workbook.LoadFromStream(stream);
    }
}
