using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a validator for cell values in a spreadsheet.
/// </summary>
public interface ICellValidator
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
public class NumberValidator : ICellValidator
{
    /// <inheritdoc/>
    public string Error { get; } = "Value must be a number";

    /// <inheritdoc/>
    public bool Validate(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        return cell.ValueType == CellDataType.Number || cell.ValueType == CellDataType.Empty;
    }
}

/// <summary>
/// Class that manages validation rules for cells in a spreadsheet.
/// </summary>
public class ValidationStore
{
    private readonly Dictionary<RangeRef, List<ICellValidator>> validators = [];

    /// <summary>
    /// Gets all ranges that have validation rules.
    /// </summary>
    public IEnumerable<RangeRef> Ranges => validators.Keys;

    /// <summary>
    /// Adds a validation rule for a specific range of cells in the spreadsheet.
    /// </summary>
    public void Add(RangeRef range, params ICellValidator[] validators)
    {
        ArgumentNullException.ThrowIfNull(validators);
        foreach (var validator in validators)
        {
            Add(range, validator);
        }
    }

    /// <summary>
    /// Adds a single validation rule for a specific range of cells in the spreadsheet.
    /// </summary>
    public void Add(RangeRef range, ICellValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        if (!validators.TryGetValue(range, out var list))
        {
            list = [];
            validators[range] = list;
        }

        list.Add(validator);
    }

    /// <summary>
    /// Removes a validation rule from a specific range.
    /// </summary>
    public bool Remove(RangeRef range, ICellValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        if (validators.TryGetValue(range, out var list))
        {
            var removed = list.Remove(validator);
            if (list.Count == 0)
            {
                validators.Remove(range);
            }
            return removed;
        }
        return false;
    }

    /// <summary>
    /// Gets all validators for a specific range.
    /// </summary>
    public IReadOnlyList<ICellValidator> GetValidators(RangeRef range)
    {
        return validators.TryGetValue(range, out var list) ? list : [];
    }

    /// <summary>
    /// Gets all validators that apply to a specific cell.
    /// </summary>
    public IReadOnlyList<ICellValidator> GetValidatorsForCell(CellRef cellRef)
    {
        var result = new List<ICellValidator>();
        foreach (var kvp in validators)
        {
            if (kvp.Key.Contains(cellRef))
            {
                result.AddRange(kvp.Value);
            }
        }
        return result;
    }

    /// <summary>
    /// Removes all validators for a specific range and returns them.
    /// </summary>
    public List<ICellValidator> RemoveAll(RangeRef range)
    {
        if (validators.Remove(range, out var list))
        {
            return list;
        }
        return [];
    }

    /// <summary>
    /// Gets the strictest <see cref="DataValidationErrorStyle"/> among failing validators for a cell.
    /// Plain <see cref="ICellValidator"/> instances default to <see cref="DataValidationErrorStyle.Stop"/>.
    /// </summary>
    public DataValidationErrorStyle GetErrorStyleForCell(CellRef cellRef)
    {
        var style = DataValidationErrorStyle.Information;

        foreach (var kvp in validators)
        {
            if (kvp.Key.Contains(cellRef))
            {
                foreach (var validator in kvp.Value)
                {
                    var validatorStyle = validator is DataValidationRule rule
                        ? rule.ErrorStyle
                        : DataValidationErrorStyle.Stop;

                    if (validatorStyle == DataValidationErrorStyle.Stop)
                    {
                        return DataValidationErrorStyle.Stop;
                    }

                    if (validatorStyle == DataValidationErrorStyle.Warning && style == DataValidationErrorStyle.Information)
                    {
                        style = DataValidationErrorStyle.Warning;
                    }
                }
            }
        }

        return style;
    }

    /// <summary>
    /// Validates the specified cell against all validation rules defined for its range in the spreadsheet.
    /// </summary>
    public IReadOnlyList<string> Validate(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        var errors = new List<string>();

        foreach (var kvp in validators)
        {
            if (kvp.Key.Contains(cell.Address))
            {
                foreach (var validator in kvp.Value)
                {
                    var valid = validator is DataValidationRule rule
                        ? rule.Validate(cell, kvp.Key.Start)
                        : validator.Validate(cell);

                    if (!valid)
                    {
                        errors.Add(validator.Error);
                    }
                }
            }
        }

        return errors;
    }
}
