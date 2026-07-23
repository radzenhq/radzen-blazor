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
    [Parameter]
    public RadzenSpreadsheet Spreadsheet { get; set; } = default!;

    private readonly EventBinding<SpreadsheetClipboard> clipboardBinding;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopyOverlay"/> class.
    /// </summary>
    public CopyOverlay()
    {
        clipboardBinding = new EventBinding<SpreadsheetClipboard>(
            c => c.Changed += OnClipboardChanged,
            c => c.Changed -= OnClipboardChanged);
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        clipboardBinding.Bind(Spreadsheet.clipboard);
    }

    private void OnClipboardChanged(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    void IDisposable.Dispose()
    {
        clipboardBinding.Dispose();
    }
}