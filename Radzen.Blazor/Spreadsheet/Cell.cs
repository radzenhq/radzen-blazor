using System;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a cell in a spreadsheet.
/// </summary>
public class Cell
{
    /// <summary>
    /// Gets the sheet that contains this cell.
    /// </summary>
    public Sheet Sheet { get; private set; }

    private Format? format;

    /// <summary>
    /// Gets or sets the format of the cell.
    /// </summary>
    public Format Format
    {
        get
        {
            if (format == null)
            {
                format = new Format();
                format.Changed += OnFormatChanged;
            }
            return format;
        }

        set
        {
            if (format != null)
            {
                format.Changed -= OnFormatChanged;
            }

            format = value;

            if (format != null)
            {
                format.Changed += OnFormatChanged;
            }

            OnFormatChanged();
        }
    }

    /// <summary>
    /// Clones the cell, creating a new instance with the same properties.
    /// </summary>

    public Cell Clone() => new(Sheet, Address)
    {
        Data = new CellData(Value),
        Formula = Formula
    };

    /// <summary>
    /// Copies the properties from another cell to this cell.
    /// </summary>
    public void CopyFrom(Cell other)
    {
        Data = other.Data;
        Formula = other.Formula;
        format = other.format;

        Changed?.Invoke(this);
    }

    internal void ApplyFormat(StringBuilder sb)
    {
        var effectiveFormat = format;

        var conditionalFormat = Sheet.ConditionalFormats.Calculate(this);

        if (conditionalFormat != null)
        {
            effectiveFormat = format?.Merge(conditionalFormat) ?? conditionalFormat;
        }

        effectiveFormat?.AppendStyle(sb);
    }

    private void OnFormatChanged()
    {
        Changed?.Invoke(this);
    }

    /// <summary>
    /// Gets the current value and its type as a CellData object.
    /// </summary>
    public CellData Data { get; internal set; } = new CellData(null);


    /// <summary>
    /// Gets or sets the value of the cell.
    /// </summary>
    public object? Value
    {
        get => Data.Value;
        set
        {
            Data = new CellData(value);

            Sheet.OnCellValueChanged(this);
        }
    }

    /// <summary>
    /// Gets the value of the cell as a string, or the formula if it exists.
    /// </summary>
    public string? GetValue()
    {
        if (!string.IsNullOrEmpty(Formula))
        {
            return Formula;
        }

        return Value switch
        {
            null => null,
            CellError error => error.ToString(),
            string str => str,
            _ => Value.ToString()
        };
    }

    /// <summary>
    /// Gets the value of the cell as a string.
    /// </summary>
    public string? GetValueAsString()
    {
        return Value switch
        {
            null => null,
            CellError error => error.ToString(),
            string str => str,
            _ => Value.ToString()
        };
    }

    /// <summary>
    /// Sets the value of the cell based on a string input.
    /// If the string starts with '=', it is treated as a formula.
    /// Otherwise, it attempts to parse the string as a number or keeps it as a string.
    /// </summary>
    public void SetValue(string? value)
    {
        if (value?.StartsWith('=') == true && value != "=")
        {
            Formula = value;
        }
        else
        {
            Value = value;
        }
    }

    internal void OnChanged()
    {
        Changed?.Invoke(this);
    }

    internal event Action<Cell>? Changed;

    /// <summary>
    /// Gets the type of value contained in the cell.
    /// </summary>
    public CellDataType ValueType => Data.Type;

    internal FormulaSyntaxTree? FormulaSyntaxTree { get; private set; }

    private string? formula;

    /// <summary>
    /// Gets or sets the formula of the cell.
    /// </summary>
    public string? Formula
    {
        get => formula;
        set
        {
            formula = value;
            FormulaSyntaxTree = value != null ? FormulaParser.Parse(value) : null;
            Sheet.OnCellFormulaChanged(this);
        }
    }

    /// <summary>
    /// Gets the address of the cell.
    /// </summary>

    public CellRef Address { get; }

    /// <summary>
    /// Validates the cell's value against the sheet's validation rules.
    /// </summary>
    public void Validate()
    {
        ValidationErrors = Sheet.Validation.Validate(this);
    }

    /// <summary>
    /// Gets a value indicating whether the cell has validation errors.
    /// </summary>
    public bool HasValidationErrors => ValidationErrors.Count > 0;

    /// <summary>
    /// Gets the validation errors for the cell.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; private set; } = [];

    internal Cell(Sheet sheet, CellRef address)
    {
        Address = address;
        Sheet = sheet;
    }
}