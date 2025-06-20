using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a formula editor component for a spreadsheet.
/// </summary>
public partial class FormulaEditor : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the sheet that contains the formula editor.
    /// </summary>
    [Parameter]
    public Sheet Sheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the spreadsheet instance that contains the sheet.
    /// </summary>
    [CascadingParameter]
    public ISpreadsheet Spreadsheet { get; set; } = default!;

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Sheet != null)
        {
            Sheet.Selection.Changed -= OnSelectionChanged;
            Sheet.Editor.ValueChanged -= OnEditorValueChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Sheet != null)
        {
            Sheet.Selection.Changed += OnSelectionChanged;
            Sheet.Editor.ValueChanged += OnEditorValueChanged;

            Render();
        }
    }

    private void OnFocus()
    {
        if (Sheet.Selection.Cell != CellRef.Invalid)
        {
            var cell = Sheet.Cells[Sheet.Selection.Cell];
            Sheet.Editor.StartEdit(cell.Address, cell.GetValue(), EditMode.Formula);
        }
    }

    private void Render()
    {
        if (Sheet.Selection.Cell != CellRef.Invalid)
        {
            var cell = Sheet.Cells[Sheet.Selection.Cell];

            Sheet.Editor.Value = cell.GetValue();
        }
    }

    private void OnSelectionChanged()
    {
        Render();

        StateHasChanged();
    }

    private void OnEditorValueChanged()
    {
        if (Sheet.Editor.Mode == EditMode.Cell)
        {
            StateHasChanged();
        }
    }

    void IDisposable.Dispose()
    {
        Sheet.Selection.Changed -= OnSelectionChanged;
        Sheet.Editor.ValueChanged -= OnEditorValueChanged;
    }

    private async Task OnBlurAsync()
    {
        if (Sheet.Editor.Mode == EditMode.Formula)
        {
            await Spreadsheet.AcceptAsync();
        }
    }
}