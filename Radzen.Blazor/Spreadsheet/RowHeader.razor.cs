using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Renders a row header in a spreadsheet.
/// </summary>
public partial class RowHeader : HeaderBase
{
    /// <summary>
    /// Gets or sets the row index for the row header.
    /// </summary>
    [Parameter]
    public int Row { get; set; }

    /// <inheritdoc/>
    protected override int Index => Row;

    /// <inheritdoc/>
    protected override string IndexParameterName => nameof(Row);

    /// <inheritdoc/>
    [SuppressMessage("Design", "CA1062", Justification = "Base class guarantees non-null.")]
    protected override bool CheckIsActive(Selection selection) => selection.IsActive(new RowRef(Row));

    /// <inheritdoc/>
    [SuppressMessage("Design", "CA1062", Justification = "Base class guarantees non-null.")]
    protected override bool CheckIsSelected(Selection selection) => selection.IsSelected(new RowRef(Row));

    private string Class => ClassList.Create("rz-spreadsheet-row-header")
                                     .Add("rz-spreadsheet-frozen-row", FrozenState.HasFlag(FrozenState.Row))
                                     .Add("rz-spreadsheet-frozen-column", FrozenState.HasFlag(FrozenState.Column))
                                     .Add("rz-spreadsheet-header-active", Active)
                                     .Add("rz-spreadsheet-header-selected", Selected)
                                     .ToString();

    private string ResizeHandleStyle => $"top: {Rect.Bottom.ToPx()}; left: {Rect.Left.ToPx()}; width: {Rect.Width.ToPx()};";
}
