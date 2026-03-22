using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable
/// <summary>
/// Renders a column header in a spreadsheet.
/// </summary>
public partial class ColumnHeader : HeaderBase
{
    /// <summary>
    /// Gets or sets the column index of the column header.
    /// </summary>
    [Parameter]
    public int Column { get; set; }

    /// <inheritdoc/>
    protected override int Index => Column;

    /// <inheritdoc/>
    protected override string IndexParameterName => nameof(Column);

    /// <inheritdoc/>
    [SuppressMessage("Design", "CA1062", Justification = "Base class guarantees non-null.")]
    protected override bool CheckIsActive(Selection selection) => selection.IsActive(new ColumnRef(Column));

    /// <inheritdoc/>
    [SuppressMessage("Design", "CA1062", Justification = "Base class guarantees non-null.")]
    protected override bool CheckIsSelected(Selection selection) => selection.IsSelected(new ColumnRef(Column));

    private string Class => ClassList.Create("rz-spreadsheet-column-header")
                                     .Add("rz-spreadsheet-frozen-row", FrozenState.HasFlag(FrozenState.Row))
                                     .Add("rz-spreadsheet-frozen-column", FrozenState.HasFlag(FrozenState.Column))
                                     .Add("rz-spreadsheet-header-active", Active)
                                     .Add("rz-spreadsheet-header-selected", Selected)
                                     .ToString();

    private string ResizeHandleStyle => $"left: {Rect.Right.ToPx()}; top: {Rect.Top.ToPx()}; height: {Rect.Height.ToPx()};";
}
