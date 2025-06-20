using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders a range selection item in a spreadsheet.
/// </summary>
public partial class RangeSelectionItem
{
    /// <summary>
    /// Gets or sets the context for the virtual grid that contains the range selection item.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; }

    /// <summary>
    /// Gets or sets the range reference that defines the area of the range selection item in the spreadsheet.
    /// </summary>
    [Parameter]
    public RangeRef Range { get; set; }

    /// <summary>
    /// Gets or sets the sheet that contains the range selection item.
    /// </summary>
    [Parameter]
    public Sheet Sheet { get; set; }

    /// <summary>
    /// Gets or sets the cell reference that defines the position of the range selection item in the spreadsheet.
    /// </summary>
    [Parameter]
    public CellRef Cell { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the range selection item is frozen in the row direction.
    /// </summary>
    [Parameter]
    public bool FrozenRow { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the range selection item is frozen in the column direction.
    /// </summary>
    [Parameter]
    public bool FrozenColumn { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the range selection item is positioned at the top of the spreadsheet.
    /// </summary>
    [Parameter]
    public bool Top { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the range selection item is positioned on the left side of the spreadsheet.
    /// </summary>
    [Parameter]
    public bool Left { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the range selection item is positioned at the bottom of the spreadsheet.
    /// </summary>
    [Parameter]
    public bool Bottom { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the range selection item is positioned on the right side of the spreadsheet.
    /// </summary>
    [Parameter]
    public bool Right { get; set; }

    private string Class => ClassList.Create("rz-spreadsheet-selection-range")
            .Add("rz-spreadsheet-selection-range-mask", Range.Overlaps(Sheet.MergedCells.GetMergedRangeOrSelf(Cell)))
            .Add("rz-spreadsheet-selection-range-top", Top)
            .Add("rz-spreadsheet-selection-range-left", Left)
            .Add("rz-spreadsheet-selection-range-bottom", Bottom)
            .Add("rz-spreadsheet-selection-range-right", Right)
            .Add("rz-spreadsheet-frozen-row", FrozenRow)
            .Add("rz-spreadsheet-frozen-column", FrozenColumn)
            .ToString();

    private string Style
    {
        get
        {
            var rect = Context.GetRectangle(Range.Start.Row, Range.Start.Column, Range.End.Row, Range.End.Column);
            var cellRect = Context.GetRectangle(Cell.Row, Cell.Column);

            var intersectionRect = rect.Intersection(cellRect);

            var offsetX = intersectionRect.Left - rect.Left;
            var offsetY = intersectionRect.Top - rect.Top;

            return $@"transform: translate({rect.Left.ToPx()}, {rect.Top.ToPx()}); width: {rect.Width.ToPx()}; height: {rect.Height.ToPx()}; mask-size: 100% 100%, {intersectionRect.Width.ToPx()} {intersectionRect.Height.ToPx()}; mask-position: 0 0, {offsetX.ToPx()} {offsetY.ToPx()};";
        }
    }
} 