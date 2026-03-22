using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

using Radzen.Documents.Spreadsheet;
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
    public Worksheet Worksheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the editor.
    /// </summary>
    [Parameter]
    public Editor Editor { get; set; } = default!;

    /// <summary>
    /// Gets or sets the spreadsheet instance that contains the sheet.
    /// </summary>
    [CascadingParameter]
    public ISpreadsheet Spreadsheet { get; set; } = default!;

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Worksheet != null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
            Editor.ValueChanged -= OnEditorValueChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Worksheet != null)
        {
            Worksheet.Selection.Changed += OnSelectionChanged;
            Editor.ValueChanged += OnEditorValueChanged;

            Render();
        }
    }

    private void OnFocus()
    {
        if (Worksheet.Selection.Cell != CellRef.Invalid)
        {
            var cell = Worksheet.Cells[Worksheet.Selection.Cell];
            Editor.StartEdit(cell.Address, Editor.Mode != EditMode.None ? Editor.Value : cell.GetValue(), EditMode.Formula);
        }
    }

    private void Render()
    {
        if (Worksheet.Selection.Cell != CellRef.Invalid)
        {
            var cell = Worksheet.Cells[Worksheet.Selection.Cell];

            Editor.Value = cell.GetValue();
        }
    }

    private void OnSelectionChanged()
    {
        Render();

        StateHasChanged();
    }

    private void OnEditorValueChanged()
    {
        if (Editor.Mode == EditMode.Cell)
        {
            StateHasChanged();
        }
    }

    void IDisposable.Dispose()
    {
        if (Worksheet != null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }

        if (Editor != null)
        {
            Editor.ValueChanged -= OnEditorValueChanged;
        }
    }

    private async Task OnBlurAsync()
    {
        if (Editor.Mode == EditMode.Formula)
        {
            await Spreadsheet.AcceptAsync();
        }
    }
}