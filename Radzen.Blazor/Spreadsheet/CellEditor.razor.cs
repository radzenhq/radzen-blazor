using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

#nullable enable
using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders an inline cell editor for a spreadsheet.
/// </summary>
public partial class CellEditor : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the sheet.
    /// </summary>
    [Parameter]
    public Worksheet Worksheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the editor.
    /// </summary>
    [Parameter]
    public Editor Editor { get; set; } = default!;

    /// <summary>
    /// Gets or sets the virtual grid context.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;

    /// <summary>
    /// Gets or sets the spreadsheet instance that contains this cell editor.
    /// </summary>
    [CascadingParameter]
    public ISpreadsheet Spreadsheet { get; set; } = default!;

    private string? cellStyle;
    private string? className;

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Worksheet is not null)
        {
            Editor.Changed -= OnEditModeChanged;
            Editor.ValueChanged -= OnEditorValueChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Worksheet is not null)
        {
            Editor.Changed += OnEditModeChanged;
            Editor.ValueChanged += OnEditorValueChanged;

            Render();
        }
    }

    private async Task OnBlurAsync()
    {
        if (Editor.Mode == EditMode.Cell)
        {
            await Spreadsheet.AcceptAsync();
        }
    }

    private void OnFocus()
    {
        if (Worksheet.Selection.Cell != CellRef.Invalid)
        {
            var cell = Worksheet.Cells[Worksheet.Selection.Cell];
            Editor.StartEdit(cell.Address, Editor.Mode != EditMode.None ? Editor.Value : cell.GetValue(), EditMode.Cell);
        }
    }

    private void OnEditorValueChanged()
    {
        if (Editor.Mode == EditMode.Formula)
        {
            StateHasChanged();
        }
    }

    private void Render()
    {
        var address = Worksheet.Selection.Cell;

        if (address == CellRef.Invalid)
        {
            cellStyle = null;
            className = "rz-spreadsheet-cell-editor";
            return;
        }

        var cell = Worksheet.Cells[address];

        if (address != CellRef.Invalid)
        {
            var rect = Context.GetRectangle(address.Row, address.Column);
            var sb = StringBuilderCache.Acquire();
            rect.AppendStyle(sb);

            cell = Worksheet.Cells[address];
            cell.ApplyFormat(sb);

            cellStyle = StringBuilderCache.GetStringAndRelease(sb);
        }
        else
        {
            cellStyle = null;
        }

        className = ClassList.Create("rz-spreadsheet-cell-editor")
            .Add("rz-spreadsheet-frozen-column", Worksheet.Selection.Cell != CellRef.Invalid && Worksheet.Selection.Cell.Column < Worksheet.Columns.Frozen)
            .Add("rz-spreadsheet-frozen-row", Worksheet.Selection.Cell != CellRef.Invalid && Worksheet.Selection.Cell.Row < Worksheet.Rows.Frozen)
            .Add($"rz-spreadsheet-cell-editor-{cell.ValueType.ToString().ToLowerInvariant()}", cell is not null)
            .ToString();
    }

    private void OnEditModeChanged()
    {
        Render();
        StateHasChanged();
    }

    void IDisposable.Dispose()
    {
        Editor.Changed -= OnEditModeChanged;
        Editor.ValueChanged -= OnEditorValueChanged;
    }
}