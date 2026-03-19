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

    // Custom formula validation tests

    [Fact]
    public void Custom_FormulaGreaterThanZero_Valid()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 5;
        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "=A1>0" };
        Assert.True(rule.Validate(sheet.Cells[0, 0]));
    }

    [Fact]
    public void Custom_FormulaGreaterThanZero_Invalid()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = -1;
        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "=A1>0" };
        Assert.False(rule.Validate(sheet.Cells[0, 0]));
    }

    [Fact]
    public void Custom_FormulaReturningNumber_Valid()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 5;
        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "=A1" };
        Assert.True(rule.Validate(sheet.Cells[0, 0])); // non-zero = valid
    }

    [Fact]
    public void Custom_FormulaReturningNumber_Zero_Invalid()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 0;
        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "=A1" };
        Assert.False(rule.Validate(sheet.Cells[0, 0])); // zero = invalid
    }

    [Fact]
    public void Custom_EmptyFormula_ReturnsTrue()
    {
        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "" };
        Assert.True(rule.Validate(CreateCell(5)));
    }

    [Fact]
    public void Custom_InvalidFormula_ReturnsFalse()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 5;
        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "=NONEXISTENTFUNCTION()" };
        Assert.False(rule.Validate(sheet.Cells[0, 0]));
    }

    // Error style tests for ValidationStore

    [Fact]
    public void GetErrorStyleForCell_Stop()
    {
        var store = new ValidationStore();
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.WholeNumber,
            Operator = DataValidationOperator.GreaterThan,
            Formula1 = "5",
            ErrorStyle = DataValidationErrorStyle.Stop
        };
        store.Add(range, rule);

        Assert.Equal(DataValidationErrorStyle.Stop, store.GetErrorStyleForCell(new CellRef(0, 0)));
    }

    [Fact]
    public void GetErrorStyleForCell_Warning()
    {
        var store = new ValidationStore();
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.WholeNumber,
            Operator = DataValidationOperator.GreaterThan,
            Formula1 = "5",
            ErrorStyle = DataValidationErrorStyle.Warning
        };
        store.Add(range, rule);

        Assert.Equal(DataValidationErrorStyle.Warning, store.GetErrorStyleForCell(new CellRef(0, 0)));
    }

    [Fact]
    public void GetErrorStyleForCell_Information()
    {
        var store = new ValidationStore();
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.WholeNumber,
            Operator = DataValidationOperator.GreaterThan,
            Formula1 = "5",
            ErrorStyle = DataValidationErrorStyle.Information
        };
        store.Add(range, rule);

        Assert.Equal(DataValidationErrorStyle.Information, store.GetErrorStyleForCell(new CellRef(0, 0)));
    }

    [Fact]
    public void GetErrorStyleForCell_MixedStyles_StrictestWins()
    {
        var store = new ValidationStore();
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        var infoRule = new DataValidationRule
        {
            Type = DataValidationType.WholeNumber,
            Operator = DataValidationOperator.GreaterThan,
            Formula1 = "5",
            ErrorStyle = DataValidationErrorStyle.Information
        };
        var warningRule = new DataValidationRule
        {
            Type = DataValidationType.Decimal,
            Operator = DataValidationOperator.LessThan,
            Formula1 = "100",
            ErrorStyle = DataValidationErrorStyle.Warning
        };
        store.Add(range, infoRule);
        store.Add(range, warningRule);

        Assert.Equal(DataValidationErrorStyle.Warning, store.GetErrorStyleForCell(new CellRef(0, 0)));
    }

    [Fact]
    public void GetErrorStyleForCell_PlainValidator_DefaultsToStop()
    {
        var store = new ValidationStore();
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        store.Add(range, new NumberValidator());

        Assert.Equal(DataValidationErrorStyle.Stop, store.GetErrorStyleForCell(new CellRef(0, 0)));
    }

    [Fact]
    public void ShowInputMessage_FoundViaGetValidatorsForCell()
    {
        var sheet = new Sheet(40, 40);
        var range = RangeRef.Parse("J4:J10");
        var rule = new DataValidationRule
        {
            Type = DataValidationType.WholeNumber,
            Operator = DataValidationOperator.GreaterThan,
            Formula1 = "0",
            ShowInputMessage = true,
            PromptTitle = "Test Title",
            Prompt = "Test Prompt"
        };
        sheet.Validation.Add(range, rule);

        // J4 = CellRef(3, 9)
        var validators = sheet.Validation.GetValidatorsForCell(new CellRef(3, 9));
        Assert.Single(validators);
        var found = Assert.IsType<DataValidationRule>(validators[0]);
        Assert.True(found.ShowInputMessage);
        Assert.Equal("Test Title", found.PromptTitle);
        Assert.Equal("Test Prompt", found.Prompt);
    }

    // Custom formula with cross-cell reference

    [Fact]
    public void Custom_FormulaCrossCell_Valid()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 100; // A1
        sheet.Cells[0, 1].Value = 50;  // B1
        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "=A1>B1" };
        Assert.True(rule.Validate(sheet.Cells[0, 0])); // 100 > 50 = true
    }

    [Fact]
    public void Custom_FormulaCrossCell_Invalid()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 10;  // A1
        sheet.Cells[0, 1].Value = 50;  // B1
        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "=A1>B1" };
        Assert.False(rule.Validate(sheet.Cells[0, 0])); // 10 > 50 = false
    }

    [Fact]
    public void Custom_AllowBlank_True()
    {
        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "=A1>0", AllowBlank = true };
        Assert.True(rule.Validate(CreateCell(null)));
    }

    [Fact]
    public void Custom_AllowBlank_False()
    {
        var sheet = new Sheet(10, 10);
        // Cell with no value and AllowBlank = false
        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "=A1>0", AllowBlank = false };
        Assert.False(rule.Validate(sheet.Cells[0, 0]));
    }

    // ClearValidationErrors

    [Fact]
    public void ClearValidationErrors_RemovesErrors()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 3;
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        sheet.Validation.Add(range, new DataValidationRule
        {
            Type = DataValidationType.WholeNumber,
            Operator = DataValidationOperator.GreaterThan,
            Formula1 = "5"
        });
        sheet.Cells[0, 0].Validate();
        Assert.True(sheet.Cells[0, 0].HasValidationErrors);

        sheet.Cells[0, 0].ClearValidationErrors();
        Assert.False(sheet.Cells[0, 0].HasValidationErrors);
    }

    // XLSX round-trip for Custom type

    [Fact]
    public void Xlsx_RoundTrip_Custom()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 0));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.Custom,
            Formula1 = "=A1>0"
        };
        sheet.Validation.Add(range, rule);

        var loaded = SaveAndLoad(workbook);
        var loadedSheet = loaded.Sheets[0];

        var validators = loadedSheet.Validation.GetValidators(range);
        Assert.Single(validators);
        var loadedRule = Assert.IsType<DataValidationRule>(validators[0]);
        Assert.Equal(DataValidationType.Custom, loadedRule.Type);
        Assert.Equal("=A1>0", loadedRule.Formula1);
    }

    // XLSX round-trip preserves input prompt fields

    [Fact]
    public void Xlsx_RoundTrip_InputPromptFields()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 0));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.WholeNumber,
            Operator = DataValidationOperator.GreaterThan,
            Formula1 = "0",
            ShowInputMessage = true,
            PromptTitle = "Enter a number",
            Prompt = "Please enter a positive number"
        };
        sheet.Validation.Add(range, rule);

        var loaded = SaveAndLoad(workbook);
        var loadedSheet = loaded.Sheets[0];

        var validators = loadedSheet.Validation.GetValidators(range);
        var loadedRule = Assert.IsType<DataValidationRule>(validators[0]);
        Assert.True(loadedRule.ShowInputMessage);
        Assert.Equal("Enter a number", loadedRule.PromptTitle);
        Assert.Equal("Please enter a positive number", loadedRule.Prompt);
    }

    // Custom formula relative reference tests

    [Fact]
    public void Custom_RelativeRef_ShiftsPerCell()
    {
        var sheet = new Sheet(10, 15);
        // H4 = 200, H5 = 50
        sheet.Cells[3, 7].Value = 200;  // H4
        sheet.Cells[4, 7].Value = 50;   // H5
        sheet.Cells[3, 11].Value = 100; // L4
        sheet.Cells[4, 11].Value = 100; // L5

        var range = RangeRef.Parse("L4:L10");
        var rule = new DataValidationRule
        {
            Type = DataValidationType.Custom,
            Formula1 = "=L4>H4"
        };
        sheet.Validation.Add(range, rule);

        // L4: =L4>H4 → 100>200 = false
        sheet.Cells[3, 11].Validate();
        Assert.True(sheet.Cells[3, 11].HasValidationErrors);

        // L5: =L5>H5 → 100>50 = true (formula shifts!)
        sheet.Cells[4, 11].Validate();
        Assert.False(sheet.Cells[4, 11].HasValidationErrors);
    }

    [Fact]
    public void Custom_AbsoluteRef_DoesNotShift()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 100;  // A1
        sheet.Cells[0, 1].Value = 50;   // B1 (threshold)
        sheet.Cells[1, 0].Value = 30;   // A2

        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 0));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.Custom,
            Formula1 = "=A1>$B$1"  // B1 is absolute
        };
        sheet.Validation.Add(range, rule);

        // A1: =A1>$B$1 → 100>50 = true
        Assert.True(rule.Validate(sheet.Cells[0, 0], range.Start));

        // A2: =A2>$B$1 → 30>50 = false (A shifts, $B$1 stays)
        Assert.False(rule.Validate(sheet.Cells[1, 0], range.Start));
    }

    // List from cell range tests

    [Fact]
    public void List_FromCellRange_Valid()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = "Red";
        sheet.Cells[1, 0].Value = "Green";
        sheet.Cells[2, 0].Value = "Blue";

        var range = new RangeRef(new CellRef(0, 1), new CellRef(5, 1));
        var rule = new DataValidationRule
        {
            Type = DataValidationType.List,
            Formula1 = "=$A$1:$A$3"
        };
        sheet.Validation.Add(range, rule);

        sheet.Cells[0, 1].Value = "Red";
        Assert.True(rule.Validate(sheet.Cells[0, 1]));

        sheet.Cells[1, 1].Value = "Yellow";
        Assert.False(rule.Validate(sheet.Cells[1, 1]));
    }

    [Fact]
    public void List_GetListItems_FromCellRange()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = "Apple";
        sheet.Cells[1, 0].Value = "Banana";
        sheet.Cells[2, 0].Value = "Cherry";

        var rule = new DataValidationRule
        {
            Type = DataValidationType.List,
            Formula1 = "=$A$1:$A$3"
        };

        var items = rule.GetListItems(sheet);
        Assert.Equal(3, items.Count);
        Assert.Equal("Apple", items[0]);
        Assert.Equal("Banana", items[1]);
        Assert.Equal("Cherry", items[2]);
    }

    [Fact]
    public void List_GetListItems_CommaSeparated_Unchanged()
    {
        var rule = new DataValidationRule
        {
            Type = DataValidationType.List,
            Formula1 = "Yes,No,Maybe"
        };

        var items = rule.GetListItems(null);
        Assert.Equal(3, items.Count);
        Assert.Equal("Yes", items[0]);
    }

    [Fact]
    public void List_ListItems_ReturnsEmpty_ForRangeFormula()
    {
        var rule = new DataValidationRule
        {
            Type = DataValidationType.List,
            Formula1 = "=$A$1:$A$3"
        };

        // ListItems property returns empty for range formulas (no sheet context)
        Assert.Empty(rule.ListItems);
    }

    private static Workbook SaveAndLoad(Workbook workbook)
    {
        using var stream = new System.IO.MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;
        return Workbook.LoadFromStream(stream);
    }
}
