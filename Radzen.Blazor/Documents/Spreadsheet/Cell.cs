using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a cell in a spreadsheet.
/// </summary>
public class Cell
{
    /// <summary>
    /// Gets the sheet that contains this cell.
    /// </summary>
    public Worksheet Worksheet { get; private set; }

    private Format? format;

    /// <summary>
    /// Gets or sets the format of the cell.
    /// </summary>
    public Format Format
    {
        get
        {
            if (format is null)
            {
                format = new Format();
                format.Changed += OnFormatChanged;
            }
            return format;
        }

        set
        {
            if (ReferenceEquals(format, value))
            {
                return;
            }
            format?.Changed -= OnFormatChanged;

            format = value;

            format?.Changed += OnFormatChanged;

            OnFormatChanged();
        }
    }

    /// <summary>
    /// Clones the cell, creating a new instance with the same properties.
    /// </summary>

    public Cell Clone()
    {
        var clone = new Cell(Worksheet, Address)
        {
            Data = new CellData(Value),
            QuotePrefix = QuotePrefix,
            Hyperlink = Hyperlink?.Clone(),
        };
        // Assign the formula via the backing field, not the property: the setter
        // registers the cell in the worksheet dependency graph and triggers a recalc.
        // A clone is a detached snapshot/transport copy and must stay out of the graph.
        clone.formula = formula;
        clone.FormulaSyntaxTree = FormulaSyntaxTree;
        if (format is not null)
        {
            clone.format = format.Clone();
            clone.format.Changed += clone.OnFormatChanged;
        }
        return clone;
    }

    /// <summary>
    /// Copies the properties from another cell to this cell.
    /// </summary>
    public void CopyFrom(Cell other)
    {
        ArgumentNullException.ThrowIfNull(other);
        Data = other.Data;
        Formula = other.Formula;
        QuotePrefix = other.QuotePrefix;

        format?.Changed -= OnFormatChanged;

        format = other.format;

        format?.Changed += OnFormatChanged;

        Hyperlink = other.Hyperlink?.Clone();

        Changed?.Invoke(this);
    }

    internal Format? GetEffectiveFormat()
    {
        var effectiveFormat = format;

        var conditionalFormat = Worksheet.ConditionalFormats.Calculate(this);

        if (conditionalFormat is not null)
        {
            effectiveFormat = format?.Merge(conditionalFormat) ?? conditionalFormat;
        }

        return effectiveFormat;
    }

    private void OnFormatChanged()
    {
        Changed?.Invoke(this);
    }

    /// <summary>
    /// Gets or sets the hyperlink associated with this cell.
    /// </summary>
    public Hyperlink? Hyperlink { get; set; }

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
            if (Equals(Data.Value, value) && !QuotePrefix)
            {
                return;
            }
            Data = new CellData(value);
            QuotePrefix = false;

            Worksheet.OnCellValueChanged(this);
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

        var text = GetValueAsString();
        return QuotePrefix && text is not null ? "'" + text : text;
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
    /// A leading apostrophe escapes the value — the apostrophe is stripped,
    /// the remainder is stored as text, and <see cref="QuotePrefix"/> is set.
    /// Otherwise, a string starting with '=' is treated as a formula.
    /// </summary>
    public void SetValue(string? value)
    {
        if (value is not null && value.StartsWith('\''))
        {
            if (Formula is not null)
            {
                Formula = null;
            }
            Value = value[1..];
            QuotePrefix = true;
        }
        else if (value?.StartsWith('=') == true && value != "=")
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

    /// <summary>
    /// Gets or sets a value indicating whether the cell value was entered with
    /// a leading apostrophe and should be treated as literal text even if it
    /// looks like a formula. Mirrors Excel's <c>quotePrefix</c> cell flag.
    /// </summary>
    public bool QuotePrefix { get; set; }

    private string? formula;

    /// <summary>
    /// Gets or sets the formula of the cell.
    /// </summary>
    public string? Formula
    {
        get => formula;
        set
        {
            if (formula == value)
            {
                return;
            }
            formula = value;
            FormulaSyntaxTree = value is not null ? FormulaParser.Parse(value) : null;
            if (value is not null)
            {
                QuotePrefix = false;
            }
            Worksheet.OnCellFormulaChanged(this);
        }
    }

    /// <summary>
    /// Gets a value indicating whether this cell has no meaningful content (no value, formula, format, or hyperlink).
    /// </summary>
    public bool IsEmpty => Value is null && Formula is null && format is null && Hyperlink is null && !QuotePrefix;

    /// <summary>
    /// Gets the address of the cell.
    /// </summary>

    public CellRef Address { get; internal set; }

    /// <summary>
    /// Validates the cell's value against the sheet's validation rules.
    /// </summary>
    public void Validate()
    {
        ValidationErrors = Worksheet.Validation.Validate(this);
    }

    /// <summary>
    /// Gets a value indicating whether the cell has validation errors.
    /// </summary>
    public bool HasValidationErrors => ValidationErrors.Count > 0;

    /// <summary>
    /// Gets the validation errors for the cell.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; private set; } = [];

    internal void ClearValidationErrors()
    {
        ValidationErrors = [];
    }

    internal Cell(Worksheet sheet, CellRef address)
    {
        Address = address;
        Worksheet = sheet;
    }
}