using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents the type of value contained in a cell.
/// </summary>
public enum CellValueType
{
    /// <summary>
    /// The cell contains a numeric value.
    /// </summary>
    Number,
    /// <summary>
    /// The cell contains a string value.
    /// </summary>
    String,
    /// <summary>
    /// The cell contains an error value.
    /// </summary>
    Error,
    /// <summary>
    /// The cell is empty.
    /// </summary>
    Empty
}

/// <summary>
/// Represents a cell in a spreadsheet.
/// </summary>
public class Cell
{
    /// <summary>
    /// Gets the sheet that contains this cell.
    /// </summary>
    public Sheet Sheet { get; private set; }

    private object? value;
    private CellValueType valueType = CellValueType.Empty;

    private Format? format;

    /// <summary>
    /// Gets the format of the cell.
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
    }

    /// <summary>
    /// Clones the cell, creating a new instance with the same properties.
    /// </summary>

    public Cell Clone() => new(Sheet, Address)
    {
        value = value,
        valueType = valueType,
        Formula = formula
    };

    /// <summary>
    /// Copies the properties from another cell to this cell.
    /// </summary>
    public void CopyFrom(Cell other)
    {
        value = other.value;
        valueType = other.valueType;
        Formula = other.Formula;
        format = other.format;

        Changed?.Invoke(this);
    }

    private void OnFormatChanged()
    {
        Changed?.Invoke(this);
    }

    /// <summary>
    /// Gets or sets the value of the cell.
    /// </summary>
    public object? Value
    {
        get => value;
        set
        {
            this.value = value;

            valueType = DetermineValueType(value);

            if (valueType == CellValueType.Number)
            {
                this.value = value switch
                {
                    int i => (double)i,
                    uint ui => (double)ui,
                    float f => (double)f,
                    decimal d => (double)d,
                    long l => (double)l,
                    ulong ul => (double)ul,
                    short s => (double)s,
                    ushort us => (double)us,
                    byte b => (double)b,
                    sbyte sb => (double)sb,
                    _ => value
                };
            }

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

        return value switch
        {
            null => null,
            CellError error => error.ToString(),
            string str => str,
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Sets the value of the cell based on a string input.
    /// If the string starts with '=', it is treated as a formula.
    /// Otherwise, it attempts to parse the string as a number or keeps it as a string.
    /// </summary>
    public void SetValue(string? value)
    {
        if (value?.StartsWith('=') == true)
        {
            Formula = value;
        }
        else
        {
            if (double.TryParse(value, out var number))
            {
                Value = number;
            }
            else
            {
                Value = value;
            }
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
    public CellValueType ValueType => valueType;

    internal FormulaSyntaxNode? FormulaSyntaxNode { get; private set; }

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
            FormulaSyntaxNode = value != null ? FormulaParser.Parse(value) : null;
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

    private static CellValueType DetermineValueType(object? value)
    {
        return value switch
        {
            null => CellValueType.Empty,
            CellError => CellValueType.Error,
            string => CellValueType.String,
            int or double or float or decimal or long or ulong => CellValueType.Number,
            _ => CellValueType.String
        };
    }
}