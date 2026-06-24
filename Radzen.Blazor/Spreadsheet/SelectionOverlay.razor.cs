using System;
using Microsoft.AspNetCore.Components;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders an overlay for the selection in a spreadsheet.
/// </summary>
public partial class SelectionOverlay : IDisposable
{
    /// <summary>
    /// Gets or sets the sheet that contains the selection overlay.
    /// </summary>
    [Parameter]
    public Worksheet Worksheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the context for the virtual grid that contains the selection overlay.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;

    private readonly EventBinding<Selection> selectionBinding;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionOverlay"/> class.
    /// </summary>
    public SelectionOverlay()
    {
        selectionBinding = new EventBinding<Selection>(
            s => s.Changed += OnSelectionChanged,
            s => s.Changed -= OnSelectionChanged);
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        selectionBinding.Bind(Worksheet?.Selection);
    }

    private void OnSelectionChanged()
    {
        StateHasChanged();
    }

    void IDisposable.Dispose()
    {
        selectionBinding.Dispose();
    }
}
