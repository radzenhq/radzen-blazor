using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable
/// <summary>
/// Represents the mode of the editor in a spreadsheet.
/// </summary>
public enum EditMode
{
    /// <summary>
    /// Indicates that the editor is not in edit mode.
    /// </summary>
    None,
    /// <summary>
    /// Indicates that the editor is in cell edit mode, allowing direct editing of cell values.
    /// </summary>
    Cell,
    /// <summary>
    /// Indicates that the editor is in formula edit mode, allowing editing of formulas within cells.
    /// </summary>
    Formula
}

/// <summary>
/// Represents a command to accept edits made in the spreadsheet editor.
/// </summary>
/// <param name="sheet"></param>
public class AcceptEditCommand(Sheet sheet) : ICommand
{
    private RangeRef range = RangeRef.Invalid;
    private CellRef cell = CellRef.Invalid;

    private string? value;

    /// <inheritdoc/>
    public bool Execute()
    {
        if (range != RangeRef.Invalid && cell != CellRef.Invalid)
        {
            sheet.Selection.Select(cell, range);
        }

        range = sheet.Selection.Range;

        cell = sheet.Selection.Cell;

        if (value != null)
        {
            sheet.Editor.Value = value;
            sheet.Editor.Address = cell;
        }

        value = sheet.Editor.Cell?.GetValue();
        return sheet.Editor.Accept();
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        var currentValue = sheet.Cells[cell].GetValue();
        sheet.Cells[cell].SetValue(value);
        sheet.Selection.Select(cell, range);
        value = currentValue;
    }
}

/// <summary>
/// Represents an editor for a spreadsheet, allowing users to edit cell values or formulas.
/// </summary>
/// <param name="sheet"></param>
public class Editor(Sheet sheet)
{
    /// <summary>
    /// Gets or sets the address of the cell being edited in the spreadsheet.
    /// </summary>
    public CellRef Address { get; set; } = CellRef.Invalid;

    /// <summary>
    /// Gets the cell at the current address in the spreadsheet, or null if the address is invalid.
    /// </summary>
    public Cell? Cell => Address != CellRef.Invalid ? sheet.Cells[Address] : null;

    /// <summary>
    /// Gets or sets the mode of the editor, indicating whether it is in cell edit mode or formula edit mode.
    /// </summary>
    public EditMode Mode { get; private set; } = EditMode.None;

    /// <summary>
    /// Occurs when the editor's state changes, such as when it starts or ends editing a cell.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Starts editing a cell in the spreadsheet with the specified address and value.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="value"></param>
    /// <param name="mode"></param>
    public void StartEdit(CellRef address, string? value, EditMode mode = EditMode.Cell)
    {
        Address = address;

        Mode = mode;

        Value = value;

        Changed?.Invoke();
    }

    private string? value;

    /// <summary>
    /// Gets or sets the value being edited in the spreadsheet editor.
    /// </summary>
    public string? Value
    {
        get => value;
        set
        {
            this.value = value;

            ValueChanged?.Invoke();
        }
    }

    /// <summary>
    /// Occurs when the value being edited in the spreadsheet editor changes.
    /// </summary>
    public event Action? ValueChanged;

    /// <summary>
    /// Accepts the current value in the editor, applying it to the cell if valid.
    /// </summary>
    /// <returns></returns>
    public bool Accept()
    {
        var result = true;

        if (Cell is not null)
        {
            Cell.SetValue(Value);
            Cell.Validate();
            result = !Cell.HasValidationErrors;
        }

        if (result)
        {
            Value = null;

            EndEdit();
        }

        return result;
    }

    /// <summary>
    /// Cancels the current edit operation, resetting the editor's value and mode without applying changes.
    /// </summary>
    public void Cancel()
    {
        Value = null;

        EndEdit();
    }

    /// <summary>
    /// Ends the current edit operation, resetting the editor's mode and address without applying changes.
    /// </summary>
    public void EndEdit()
    {
        Mode = EditMode.None;

        Address = CellRef.Invalid;

        Changed?.Invoke();
    }
}