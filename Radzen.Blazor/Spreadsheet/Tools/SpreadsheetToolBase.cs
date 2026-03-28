using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tools;

#nullable enable

/// <summary>
/// Base class for spreadsheet toolbar buttons that need to react to selection changes.
/// </summary>
public abstract class SpreadsheetToolBase : ComponentBase, IDisposable
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
    /// Gets whether the tool should be disabled.
    /// </summary>
    protected virtual bool IsDisabled => Worksheet is null || Worksheet.Selection.Cell == CellRef.Invalid;

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Worksheet?.Selection is not null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Worksheet?.Selection is not null)
        {
            Worksheet.Selection.Changed += OnSelectionChanged;
        }
    }

    private void OnSelectionChanged()
    {
        StateHasChanged();
    }

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
        if (Worksheet?.Selection is not null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }
    }
}
