using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders an overlay for the selection in a spreadsheet.
/// </summary>
public partial class SelectionOverlay
{
    /// <summary>
    /// Gets or sets the sheet that contains the selection overlay.
    /// </summary>
    [Parameter]
    public Sheet Sheet { get; set; }

    /// <summary>
    /// Gets or sets the context for the virtual grid that contains the selection overlay.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Sheet != null)
        {
            Sheet.Selection.Changed -= OnSelectionChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Sheet != null)
        {
            Sheet.Selection.Changed += OnSelectionChanged;
            StateHasChanged();
        }
    }

 
    private void OnSelectionChanged()
    {
        StateHasChanged();
    }
}