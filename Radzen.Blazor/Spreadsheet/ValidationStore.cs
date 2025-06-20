using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a validator for cell values in a spreadsheet.
/// </summary>
public interface ICellVaidator
{
    /// <summary>
    /// Validates the specified cell against the validation rules defined by the validator.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns>True if the cell value is valid according to the validator's rules; otherwise, false.</returns>
    bool Validate(Cell cell);

    /// <summary>
    /// Gets the error message to be displayed when the cell value is invalid according to the validator's rules.
    /// </summary>
    string Error { get; }
}

/// <summary>
/// Represents a validator that checks if the cell value is a number.
/// </summary>
public class NumberValidator : ICellVaidator
{
    /// <inheritdoc/>
    public string Error { get; } = "Value must be a number";

    /// <inheritdoc/>
    public bool Validate(Cell cell)
    {
        return cell.ValueType == CellValueType.Number || cell.ValueType == CellValueType.Empty;
    }
}

/// <summary>
/// Class that manages validation rules for cells in a spreadsheet.
/// </summary>
public class ValidationStore
{
    private readonly Dictionary<RangeRef, List<ICellVaidator>> validators = [];

    /// <summary>
    /// Adds a validation rule for a specific range of cells in the spreadsheet.
    /// </summary>
    public void Add(RangeRef range, params ICellVaidator[] validators)
    {
        foreach (var validator in validators)
        {
            Add(range, validator);
        }
    }

    /// <summary>
    /// Adds a single validation rule for a specific range of cells in the spreadsheet.
    /// </summary>
    public void Add(RangeRef range, ICellVaidator validator)
    {
        if (!validators.TryGetValue(range, out var list))
        {
            list = [];
            validators[range] = list;
        }

        list.Add(validator);
    }

    /// <summary>
    /// Validates the specified cell against all validation rules defined for its range in the spreadsheet.
    /// </summary>
    public IReadOnlyList<string> Validate(Cell cell)
    {
        var errors = new List<string>();

        foreach (var kvp in validators)
        {
            if (kvp.Key.Contains(cell.Address))
            {
                foreach (var validator in kvp.Value)
                {
                    if (!validator.Validate(cell))
                    {
                        errors.Add(validator.Error);
                    }
                }
            }
        }

        return errors;
    }
}