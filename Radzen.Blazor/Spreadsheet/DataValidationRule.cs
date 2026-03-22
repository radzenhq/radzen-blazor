using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Specifies the type of data validation to apply.
/// </summary>
public enum DataValidationType
{
    /// <summary>Whole number validation.</summary>
    WholeNumber,
    /// <summary>Decimal number validation.</summary>
    Decimal,
    /// <summary>List validation with predefined values.</summary>
    List,
    /// <summary>Date validation.</summary>
    Date,
    /// <summary>Time validation.</summary>
    Time,
    /// <summary>Text length validation.</summary>
    TextLength,
    /// <summary>Custom formula validation.</summary>
    Custom
}

/// <summary>
/// Specifies the comparison operator for data validation.
/// </summary>
public enum DataValidationOperator
{
    /// <summary>Value must be between two values.</summary>
    Between,
    /// <summary>Value must not be between two values.</summary>
    NotBetween,
    /// <summary>Value must be equal to a value.</summary>
    Equal,
    /// <summary>Value must not be equal to a value.</summary>
    NotEqual,
    /// <summary>Value must be greater than a value.</summary>
    GreaterThan,
    /// <summary>Value must be less than a value.</summary>
    LessThan,
    /// <summary>Value must be greater than or equal to a value.</summary>
    GreaterThanOrEqual,
    /// <summary>Value must be less than or equal to a value.</summary>
    LessThanOrEqual
}

/// <summary>
/// Specifies the error style for data validation.
/// </summary>
public enum DataValidationErrorStyle
{
    /// <summary>Stop - prevents entry of invalid data.</summary>
    Stop,
    /// <summary>Warning - warns the user but allows entry.</summary>
    Warning,
    /// <summary>Information - displays information but allows entry.</summary>
    Information
}

/// <summary>
/// Represents a data validation rule that can be applied to a range of cells.
/// </summary>
public class DataValidationRule : ICellValidator
{
    /// <summary>
    /// Gets or sets the type of data validation.
    /// </summary>
    public DataValidationType Type { get; set; }

    /// <summary>
    /// Gets or sets the comparison operator.
    /// </summary>
    public DataValidationOperator Operator { get; set; }

    /// <summary>
    /// Gets or sets the first formula or value for comparison.
    /// </summary>
    public string? Formula1 { get; set; }

    /// <summary>
    /// Gets or sets the second formula or value for comparison (used with Between/NotBetween).
    /// </summary>
    public string? Formula2 { get; set; }

    /// <summary>
    /// Gets or sets whether blank cells are allowed.
    /// </summary>
    public bool AllowBlank { get; set; } = true;

    /// <summary>
    /// Gets or sets the error dialog title.
    /// </summary>
    public string? ErrorTitle { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Error { get; set; } = "The value you entered is not valid.";

    /// <summary>
    /// Gets or sets the input prompt title.
    /// </summary>
    public string? PromptTitle { get; set; }

    /// <summary>
    /// Gets or sets the input prompt message.
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Gets or sets whether to show the input message.
    /// </summary>
    public bool ShowInputMessage { get; set; }

    /// <summary>
    /// Gets or sets whether to show the error message.
    /// </summary>
    public bool ShowErrorMessage { get; set; } = true;

    /// <summary>
    /// Gets or sets the error style.
    /// </summary>
    public DataValidationErrorStyle ErrorStyle { get; set; } = DataValidationErrorStyle.Stop;

    /// <summary>
    /// Gets the list items for List type validation, parsed from Formula1.
    /// </summary>
    public IReadOnlyList<string> ListItems
    {
        get
        {
            if (Type != DataValidationType.List || string.IsNullOrEmpty(Formula1))
            {
                return [];
            }

            if (Formula1.StartsWith('='))
            {
                return [];
            }

            return Formula1.Split(',').Select(s => s.Trim().Trim('"')).ToList();
        }
    }

    /// <summary>
    /// Gets the list items for List type validation, resolving cell range references from the given sheet.
    /// </summary>
    public IReadOnlyList<string> GetListItems(Worksheet? sheet)
    {
        if (Type != DataValidationType.List || string.IsNullOrEmpty(Formula1))
        {
            return [];
        }

        if (Formula1.StartsWith('=') && sheet != null)
        {
            return ResolveListFromRange(sheet);
        }

        return Formula1.Split(',').Select(s => s.Trim().Trim('"')).ToList();
    }

    private IReadOnlyList<string> ResolveListFromRange(Worksheet sheet)
    {
        try
        {
            var rangeText = Formula1![1..]; // strip leading '='
            var range = RangeRef.Parse(rangeText);

            var items = new List<string>();

            for (var row = range.Start.Row; row <= range.End.Row; row++)
            {
                for (var col = range.Start.Column; col <= range.End.Column; col++)
                {
                    if (sheet.Cells.TryGet(row, col, out var cell) && cell.Value != null)
                    {
                        items.Add(cell.Value.ToString() ?? "");
                    }
                    else
                    {
                        items.Add("");
                    }
                }
            }

            return items;
        }
        catch
        {
            return [];
        }
    }

    /// <inheritdoc/>
    public bool Validate(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        return Validate(cell, cell.Address);
    }

    /// <summary>
    /// Validates the specified cell, adjusting formula references relative to the given range start.
    /// </summary>
    public bool Validate(Cell cell, CellRef rangeStart)
    {
        ArgumentNullException.ThrowIfNull(cell);

        if (AllowBlank && (cell.Value == null || (cell.Value is string s && string.IsNullOrEmpty(s)) || cell.ValueType == CellDataType.Empty))
        {
            return true;
        }

        return Type switch
        {
            DataValidationType.WholeNumber => ValidateWholeNumber(cell),
            DataValidationType.Decimal => ValidateDecimal(cell),
            DataValidationType.List => ValidateList(cell),
            DataValidationType.Date => ValidateDate(cell),
            DataValidationType.Time => ValidateTime(cell),
            DataValidationType.TextLength => ValidateTextLength(cell),
            DataValidationType.Custom => ValidateCustom(cell, rangeStart),
            _ => true
        };
    }

    private bool ValidateWholeNumber(Cell cell)
    {
        if (cell.ValueType != CellDataType.Number)
        {
            return false;
        }

        var value = Convert.ToDouble(cell.Value, CultureInfo.InvariantCulture);

        if (value != Math.Floor(value))
        {
            return false;
        }

        return CompareValue(value);
    }

    private bool ValidateDecimal(Cell cell)
    {
        if (cell.ValueType != CellDataType.Number)
        {
            return false;
        }

        var value = Convert.ToDouble(cell.Value, CultureInfo.InvariantCulture);
        return CompareValue(value);
    }

    private bool ValidateList(Cell cell)
    {
        var items = GetListItems(cell.Worksheet);
        var cellValue = cell.Value?.ToString() ?? "";
        return items.Any(item => string.Equals(item, cellValue, StringComparison.OrdinalIgnoreCase));
    }

    private bool ValidateDate(Cell cell)
    {
        if (cell.Value is DateTime dateValue)
        {
            var serialDate = dateValue.ToOADate();
            return CompareValue(serialDate);
        }

        if (cell.ValueType == CellDataType.Number)
        {
            var value = Convert.ToDouble(cell.Value, CultureInfo.InvariantCulture);
            return CompareValue(value);
        }

        return false;
    }

    private bool ValidateTime(Cell cell)
    {
        if (cell.ValueType == CellDataType.Number)
        {
            var value = Convert.ToDouble(cell.Value, CultureInfo.InvariantCulture);
            return CompareValue(value);
        }

        return false;
    }

    private bool ValidateTextLength(Cell cell)
    {
        var text = cell.Value?.ToString() ?? "";
        return CompareValue(text.Length);
    }

    private bool ValidateCustom(Cell cell, CellRef rangeStart)
    {
        if (string.IsNullOrEmpty(Formula1))
        {
            return true;
        }

        try
        {
            var formula = Formula1.StartsWith('=') ? Formula1 : "=" + Formula1;

            var rowDelta = cell.Address.Row - rangeStart.Row;
            var colDelta = cell.Address.Column - rangeStart.Column;

            if (rowDelta != 0 || colDelta != 0)
            {
                formula = Worksheet.AdjustFormulaForCopy(formula, rowDelta, colDelta);
            }

            var tree = FormulaParser.Parse(formula);

            if (tree.Errors.Count > 0)
            {
                return false;
            }

            var evaluator = new FormulaEvaluator(cell.Worksheet, cell);
            var result = evaluator.Evaluate(tree.Root);

            if (result.Value is bool boolValue)
            {
                return boolValue;
            }

            if (result.Type == CellDataType.Number)
            {
                return result.GetValueOrDefault<double>() != 0;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool CompareValue(double value)
    {
        var formula1Value = ParseDouble(Formula1);
        var formula2Value = ParseDouble(Formula2);

        return Operator switch
        {
            DataValidationOperator.Between => formula1Value.HasValue && formula2Value.HasValue && value >= formula1Value.Value && value <= formula2Value.Value,
            DataValidationOperator.NotBetween => formula1Value.HasValue && formula2Value.HasValue && (value < formula1Value.Value || value > formula2Value.Value),
            DataValidationOperator.Equal => formula1Value.HasValue && value == formula1Value.Value,
            DataValidationOperator.NotEqual => formula1Value.HasValue && value != formula1Value.Value,
            DataValidationOperator.GreaterThan => formula1Value.HasValue && value > formula1Value.Value,
            DataValidationOperator.LessThan => formula1Value.HasValue && value < formula1Value.Value,
            DataValidationOperator.GreaterThanOrEqual => formula1Value.HasValue && value >= formula1Value.Value,
            DataValidationOperator.LessThanOrEqual => formula1Value.HasValue && value <= formula1Value.Value,
            _ => true
        };
    }

    private static double? ParseDouble(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        // Try parsing as a date and converting to OLE Automation date
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateResult))
        {
            return dateResult.ToOADate();
        }

        return null;
    }
}
