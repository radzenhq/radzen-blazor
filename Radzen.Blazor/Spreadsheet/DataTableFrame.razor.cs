using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders a frame for a data table in a spreadsheet.
/// </summary>
public partial class DataTableFrame
{
    /// <summary>
    /// Gets or sets the data table to be rendered in the frame.
    /// </summary>
    [Parameter]
    public DataTable DataTable { get; set; } = default!;

    /// <summary>
    /// Gets or sets the sheet that contains the data table.
    /// </summary>
    [Parameter]
    public Sheet Sheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the context for the virtual grid that contains the data table.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;
}