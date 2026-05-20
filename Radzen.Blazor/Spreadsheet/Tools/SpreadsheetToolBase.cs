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
    /// The active worksheet, cascaded from the host <see cref="Radzen.Blazor.RadzenSpreadsheet"/>.
    /// </summary>
    [CascadingParameter]
    public Worksheet? Worksheet { get; set; }

    /// <summary>
    /// The host spreadsheet, cascaded from <see cref="Radzen.Blazor.RadzenSpreadsheet"/>.
    /// </summary>
    [CascadingParameter]
    public ISpreadsheet? Spreadsheet { get; set; }

    /// <summary>
    /// Gets the feature this tool drives, or <c>null</c> for tools that never need to
    /// grey out beyond the default selection check.
    /// </summary>
    protected virtual SpreadsheetFeature? Feature => null;

    /// <summary>
    /// Gets whether the tool should be disabled.
    /// </summary>
    protected virtual bool IsDisabled
        => Worksheet is null
        || Worksheet.Selection.Cell == CellRef.Invalid
        || (Feature is SpreadsheetFeature f && Spreadsheet is not null && !Spreadsheet.IsFeatureAllowed(f));

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

    void IDisposable.Dispose()
    {
        if (Worksheet?.Selection is not null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }
    }
}
