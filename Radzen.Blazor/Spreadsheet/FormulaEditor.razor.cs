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
            Worksheet.Editor.ValueChanged -= OnEditorValueChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Worksheet != null)
        {
            Worksheet.Selection.Changed += OnSelectionChanged;
            Worksheet.Editor.ValueChanged += OnEditorValueChanged;

            Render();
        }
    }

    private void OnFocus()
    {
        if (Worksheet.Selection.Cell != CellRef.Invalid)
        {
            var cell = Worksheet.Cells[Worksheet.Selection.Cell];
            Worksheet.Editor.StartEdit(cell.Address, Worksheet.Editor.Mode != EditMode.None ? Worksheet.Editor.Value : cell.GetValue(), EditMode.Formula);
        }
    }

    private void Render()
    {
        if (Worksheet.Selection.Cell != CellRef.Invalid)
        {
            var cell = Worksheet.Cells[Worksheet.Selection.Cell];

            Worksheet.Editor.Value = cell.GetValue();
        }
    }

    private void OnSelectionChanged()
    {
        Render();

        StateHasChanged();
    }

    private void OnEditorValueChanged()
    {
        if (Worksheet.Editor.Mode == EditMode.Cell)
        {
            StateHasChanged();
        }
    }

    void IDisposable.Dispose()
    {
        Worksheet.Selection.Changed -= OnSelectionChanged;
        Worksheet.Editor.ValueChanged -= OnEditorValueChanged;
    }

    private async Task OnBlurAsync()
    {
        if (Worksheet.Editor.Mode == EditMode.Formula)
        {
            await Spreadsheet.AcceptAsync();
        }
    }
}