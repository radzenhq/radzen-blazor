using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

#nullable enable
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
    public Sheet Sheet { get; set; } = default!;

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
        if (Sheet != null)
        {
            Sheet.Editor.Changed -= OnEditModeChanged;
            Sheet.Editor.ValueChanged -= OnEditorValueChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Sheet != null)
        {
            Sheet.Editor.Changed += OnEditModeChanged;
            Sheet.Editor.ValueChanged += OnEditorValueChanged;

            Render();
        }
    }

    private async Task OnBlurAsync()
    {
        if (Sheet.Editor.Mode == EditMode.Cell)
        {
            await Spreadsheet.AcceptAsync();
        }
    }

    private void OnEditorValueChanged()
    {
        if (Sheet.Editor.Mode == EditMode.Formula)
        {
            StateHasChanged();
        }
    }

    private void Render()
    {
        var address = Sheet.Selection.Cell;

        if (address != CellRef.Invalid)
        {
            var rect = Context.GetRectangle(address.Row, address.Column);
            var sb = StringBuilderCache.Acquire();
            rect.AppendStyle(sb);

            var cell = Sheet.Cells[address];

            cell.Format?.AppendStyle(sb);

            cellStyle = StringBuilderCache.GetStringAndRelease(sb);
        }
        else
        {
            cellStyle = null;
        }

        className = ClassList.Create("rz-spreadsheet-cell-editor")
            .Add("rz-spreadsheet-frozen-column", Sheet.Selection.Cell != CellRef.Invalid && Sheet.Selection.Cell.Column < Sheet.Columns.Frozen)
            .Add("rz-spreadsheet-frozen-row", Sheet.Selection.Cell != CellRef.Invalid && Sheet.Selection.Cell.Row < Sheet.Rows.Frozen)
            .ToString();
    }

    private void OnEditModeChanged()
    {
        Render();
        StateHasChanged();
    }

    void IDisposable.Dispose()
    {
        Sheet.Editor.Changed -= OnEditModeChanged;
        Sheet.Editor.ValueChanged -= OnEditorValueChanged;
    }
}