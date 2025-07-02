using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders a frame for a table in a spreadsheet.
/// </summary>
public partial class TableFrame
{
    /// <summary>
    /// Gets or sets the table to be rendered in the frame.
    /// </summary>
    [Parameter]
    public Table Table { get; set; } = default!;

    /// <summary>
    /// Gets or sets the sheet that contains the table.
    /// </summary>
    [Parameter]
    public Sheet Sheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the context for the virtual grid that contains the table.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;
}