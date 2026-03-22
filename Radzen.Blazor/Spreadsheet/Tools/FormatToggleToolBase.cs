using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tools;

#nullable enable

/// <summary>
/// Base class for toggle-style format toolbar buttons (Bold, Italic, Underline, Strikethrough, TextWrap).
/// Subclasses only need to override <see cref="GetValue"/> and <see cref="WithValue"/>.
/// </summary>
public abstract class FormatToggleToolBase : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the worksheet.
    /// </summary>
    [Parameter]
    public Worksheet? Worksheet { get; set; }

    /// <summary>
    /// Gets or sets the spreadsheet instance.
    /// </summary>
    [CascadingParameter]
    public ISpreadsheet? Spreadsheet { get; set; }

    /// <summary>
    /// Reads the boolean property from the cell's format (e.g. <c>format.Bold</c>).
    /// </summary>
    protected abstract bool GetValue(Format format);

    /// <summary>
    /// Returns a new format with the boolean property set (e.g. <c>format.WithBold(value)</c>).
    /// </summary>
    protected abstract Format WithValue(Format format, bool value);

    /// <summary>
    /// Gets or sets the current toggle state, reading from and writing to the selected cell's format.
    /// </summary>
    protected bool IsToggled
    {
        get
        {
            if (Worksheet?.Cells.TryGet(Worksheet.Selection.Cell.Row, Worksheet.Selection.Cell.Column, out var cell) == true)
            {
                return GetValue(cell.Format);
            }
            return false;
        }
        set
        {
            if (Worksheet != null && Worksheet.Selection.Cell != CellRef.Invalid)
            {
                if (Worksheet.Cells.TryGet(Worksheet.Selection.Cell.Row, Worksheet.Selection.Cell.Column, out var cell))
                {
                    var newFormat = WithValue(cell.Format, value);
                    var command = new FormatCommand(Worksheet, Worksheet.Selection.Range, newFormat);
                    Spreadsheet?.Execute(command);
                }
            }
        }
    }

    /// <summary>
    /// Gets whether the tool should be disabled.
    /// </summary>
    protected bool IsDisabled => Worksheet?.Selection.Cell == CellRef.Invalid;

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Worksheet?.Selection != null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Worksheet?.Selection != null)
        {
            Worksheet.Selection.Changed += OnSelectionChanged;
        }
    }

    private void OnSelectionChanged()
    {
        StateHasChanged();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Worksheet?.Selection != null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }
    }
}
