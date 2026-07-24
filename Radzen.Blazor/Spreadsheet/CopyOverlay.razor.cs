using System;
using Microsoft.AspNetCore.Components;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders an overlay for the copied selection in a spreadsheet.
/// </summary>
public partial class CopyOverlay : IDisposable
{
    /// <summary>
    /// Gets or sets the sheet that contains the copy overlay.
    /// </summary>
    [Parameter]
    public Worksheet Worksheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the context for the virtual grid that contains the copy overlay.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;

    /// <summary>
    /// Gets or sets the parent spreadsheet component.
    /// </summary>
    [CascadingParameter]
    internal SpreadsheetClipboard Clipboard { get; set; } = default!;
    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        if (Clipboard != null)
        {
            Clipboard.Changed += StateHasChanged;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Clipboard != null)
        {
            Clipboard.Changed -= StateHasChanged;
        }
    }
}
