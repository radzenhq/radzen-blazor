using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents the state of frozen rows and columns in a virtual grid.
/// </summary>
[Flags]
public enum FrozenState
{
    /// <summary>
    /// Not frozen.
    /// </summary>
    None = 0,
    /// <summary>
    /// Frozen in the row direction.
    /// </summary>
    Row = 1,
    /// <summary>
    /// Frozen in the column direction.
    /// </summary>
    Column = 2,
    /// <summary>
    /// Frozen in both row and column directions.
    /// </summary>
    Both = Row | Column
}

internal class VirtualRegion
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double ScrollWidth { get; set; }
    public double ScrollHeight { get; set; }
}

/// <summary>
/// Represents a virtual item in a virtual grid.
/// </summary>
public abstract class VirtualItem
{
    /// <summary>
    /// Gets or sets the pixel rectangle that defines the area occupied by the virtual item in the grid.
    /// </summary>
    public PixelRectangle Rect { get; set; }
}

/// <summary>
/// Represents a virtual data item in a virtual grid, which can be a cell in the grid.
/// </summary>
public class VirtualDataItem : VirtualItem
{
    /// <summary>
    /// Gets or sets the row index of the virtual data item.
    /// </summary>
    public int Row { get; set; }
    /// <summary>
    /// Gets or sets the column index of the virtual data item.
    /// </summary>
    public int Column { get; set; }
    /// <summary>
    /// Gets or sets the frozen state of the virtual data item, indicating whether it is frozen in the row or column direction.
    /// </summary>
    public FrozenState FrozenState { get; set; }
}

/// <summary>
/// Represents a row header or column header in a virtual grid.
/// </summary>
public class VirtualRowHeader : VirtualItem
{
    /// <summary>
    /// Gets or sets the row index for the row header.
    /// </summary>
    public int Row { get; set; }
    /// <summary>
    /// Gets or sets the frozen state of the row header, indicating whether it is frozen in the row or column direction.
    /// </summary>
    public FrozenState FrozenState { get; set; }
}

/// <summary>
/// Represents a column header in a virtual grid, which can be used to display column titles or other information.
/// </summary>
public class VirtualColumnHeader : VirtualItem
{
    /// <summary>
    /// Gets or sets the column index for the column header.
    /// </summary>
    public int Column { get; set; }
    /// <summary>
    /// Gets or sets the frozen state of the column header, indicating whether it is frozen in the row or column direction.
    /// </summary>
    public FrozenState FrozenState { get; set; }
}

/// <summary>
/// Represents a corner in a virtual grid, which is typically the intersection of row and column headers.
/// </summary>
public class VirtualCorner : VirtualItem
{
}

/// <summary>
/// Represents a vertical splitter in a virtual grid, which is used to separate frozen and non-frozen rows or columns.
/// </summary>
public class VirtualVerticalSplitter : VirtualItem
{
}

/// <summary>
/// Represents a horizontal splitter in a virtual grid, which is used to separate frozen and non-frozen rows or columns.
/// </summary>
public class VirtualHorizontalSplitter : VirtualItem
{
}

/// <summary>
/// Represents the context for a virtual grid, providing methods to retrieve pixel rectangles for specific rows and columns.
/// </summary>
public interface IVirtualGridContext
{
    /// <summary>
    /// Gets the pixel rectangle for a specific row and column in the virtual grid.
    /// </summary>
    PixelRectangle GetRectangle(int row, int column);
    /// <summary>
    /// Gets the pixel rectangle for a specific range of rows and columns in the virtual grid.
    /// </summary>
    PixelRectangle GetRectangle(int top, int left, int bottom, int right);
    /// <summary>
    /// Splits a range into regions based on frozen pane boundaries for rendering.
    /// </summary>
    IEnumerable<RangeInfo> GetRanges(RangeRef range);
}

/// <summary>
/// Represents a virtual grid component that supports virtualization for large datasets.
/// </summary>
public partial class VirtualGrid : ComponentBase, IAsyncDisposable, IVirtualGridContext
{
    /// <summary>
    /// Gets or sets the sheet view that provides rendering state and access to the document model.
    /// </summary>
    [Parameter]
    public SheetView View { get; set; } = default!;

    private Axis Rows => View.Worksheet.Rows;
    private Axis Columns => View.Worksheet.Columns;
    private MergedCellStore MergedCells => View.Worksheet.MergedCells;

    /// <summary>
    /// Gets or sets splitter size in pixels. The splitter is used to separate frozen and non-frozen rows and columns.
    /// </summary>
    [Parameter]
    public double SplitterSize { get; set; } = 4;

    private double SplitterWidth => Columns.Frozen > 0 ? SplitterSize : 0;

    private double SplitterHeight => Rows.Frozen > 0 ? SplitterSize : 0;

    private string SpacerStyle => $"width: {(View.TotalWidth + SplitterWidth).ToPx()}; height: {(View.TotalHeight + SplitterHeight).ToPx()};";

    /// <summary>
    /// Gets or sets additional attributes that will be rendered in the root element of the virtual grid.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

    private ElementReference scrollable;
    private ElementReference content;

    private DotNetObjectReference<VirtualGrid>? reference;

    private VirtualRegion region = new();

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            reference = DotNetObjectReference.Create(this);

            region = await JSRuntime.InvokeAsync<VirtualRegion>("Radzen.createVirtualItemContainer", scrollable, content, reference);

            Render(0, 0);
        }
    }

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var previousView = View;

        if (View is not null)
        {
            View.Worksheet.Rows.Changed -= OnChanged;
            View.Worksheet.Columns.Changed -= OnChanged;
        }

        await base.SetParametersAsync(parameters);

        if (View is not null)
        {
            View.Worksheet.Rows.Changed += OnChanged;
            View.Worksheet.Columns.Changed += OnChanged;

            if (previousView is not null && View != previousView && region is not null)
            {
                previousView.ScrollLeft = ScrollLeft;
                previousView.ScrollTop = ScrollTop;

                Render(View.ScrollLeft, View.ScrollTop);
            }
        }
    }

    private void OnChanged()
    {
        Render(ScrollLeft, ScrollTop);
    }

    private readonly List<VirtualItem> items = [];

    /// <summary>
    /// Gets or sets the template used to render each virtual item in the grid.
    /// </summary>
    [Parameter]
    public RenderFragment<VirtualItem>? Template { get; set; }

    /// <summary>
    /// Gets or sets additional child content that will be rendered inside the virtual grid.
    /// </summary>
    [Parameter]
    public RenderFragment<IVirtualGridContext>? ChildContent { get; set; }

    /// <summary>
    /// Invoked by JS interop to notify the component about scroll events in the virtual grid.
    /// </summary>
    [JSInvokable]
    public void OnScroll(double scrollX, double scrollY)
    {
        Render(scrollX, scrollY);
    }

    /// <summary>
    /// Invoked by JS interop to notify the component about resize events in the virtual grid.
    /// </summary>
    [JSInvokable]
    public void OnResize(double width, double height)
    {
        region.Width = width;
        region.Height = height;

        Render(ScrollLeft, ScrollTop);
    }

    /// <summary>
    /// Returns the current horizontal scroll position of the virtual grid.
    /// </summary>
    public double ScrollLeft { get; private set; }

    /// <summary>
    /// Returns the current vertical scroll position of the virtual grid.
    /// </summary>
    public double ScrollTop { get; private set; }

    private string ContentStyle => $"width: {region.Width.ToPx()}; height: {region.Height.ToPx()};";

    /// <summary>
    /// Scrolls the virtual grid to the specified row and column asynchronously, ensuring that the specified cell is visible within the viewport.
    /// </summary>
    public async Task ScrollToAsync(int row, int column)
    {
        var columnRange = View.GetColumnPixelRange(column);
        var rowRange = View.GetRowPixelRange(row);

        var frozenColumnRange = View.GetColumnPixelRange(Columns.Frozen);
        var frozenRowRange = View.GetRowPixelRange(Rows.Frozen);

        var visibleLeft = ScrollLeft + frozenColumnRange.End - View.ColumnHeaderOffset;
        var visibleRight = ScrollLeft + region.Width;
        var visibleTop = ScrollTop + frozenRowRange.End - View.RowHeaderOffset;
        var visibleBottom = ScrollTop + region.Height;

        var scrollX = ScrollLeft;
        var scrollY = ScrollTop;

        if (visibleLeft > columnRange.Start)
        {
            scrollX = ScrollLeft - (visibleLeft - columnRange.Start);
        }
        else if (visibleRight < columnRange.End)
        {
            scrollX = ScrollLeft + columnRange.End - visibleRight;
        }
        if (visibleTop > rowRange.Start)
        {
            scrollY = ScrollTop - (visibleTop - rowRange.Start);
        }
        else if (visibleBottom < rowRange.End)
        {
            scrollY = ScrollTop + rowRange.End - visibleBottom;
        }

        if (ScrollLeft != scrollX || ScrollTop != scrollY)
        {
            await JSRuntime.InvokeVoidAsync("Radzen.scrollElementTo", scrollable, scrollX, scrollY);
        }
    }

    private void Render(double scrollX, double scrollY)
    {
        var columnRange = View.GetColumnRange(scrollX, scrollX + region.Width);
        var rowRange = View.GetRowRange(scrollY, scrollY + region.Height);

        items.Clear();

        if (View.RowHeaderOffset > 0)
        {
            if (View.ColumnHeaderOffset > 0)
            {
                var item = new VirtualCorner
                {
                    Rect = new PixelRectangle(0, 0, View.RowHeaderOffset, View.ColumnHeaderOffset)
                };
                items.Add(item);
            }

            var left = View.ColumnHeaderOffset;

            for (var x = 0; x < Columns.Frozen; x++)
            {
                if (Columns.IsHidden(x))
                {
                    continue;
                }

                var item = new VirtualColumnHeader
                {
                    Column = x,
                    Rect = new PixelRectangle(0, left, View.RowHeaderOffset, left + Columns[x]),
                    FrozenState = FrozenState.Column,
                };
                items.Add(item);
                left += Columns[x];
            }

            if (Columns.Frozen > 0)
            {
                var item = new VirtualVerticalSplitter
                {
                    Rect = new PixelRectangle(0, left, region.Height, left + SplitterSize)
                };
                items.Add(item);
            }

            left = -columnRange.Offset;

            for (var x = columnRange.Start; x <= columnRange.End; x++)
            {
                if (x < Columns.Frozen || Columns.IsHidden(x))
                {
                    continue;
                }
                var item = new VirtualColumnHeader
                {
                    Column = x,
                    Rect = new PixelRectangle(0, left + SplitterWidth, View.RowHeaderOffset, left + SplitterWidth + Columns[x]),
                };
                items.Add(item);
                left += Columns[x];
            }
        }

        if (View.ColumnHeaderOffset > 0)
        {
            var headerTop = View.RowHeaderOffset;

            for (var y = 0; y < Rows.Frozen; y++)
            {
                if (Rows.IsHidden(y))
                {
                    continue;
                }

                var item = new VirtualRowHeader
                {
                    Row = y,
                    Rect = new PixelRectangle(headerTop, 0, headerTop + Rows[y], View.ColumnHeaderOffset),
                    FrozenState = FrozenState.Row,
                };
                items.Add(item);
                headerTop += Rows[y];
            }

            if (Rows.Frozen > 0)
            {
                var item = new VirtualHorizontalSplitter
                {
                    Rect = new PixelRectangle(headerTop, 0, headerTop + SplitterSize, region.Width)
                };
                items.Add(item);
            }

            headerTop = -rowRange.Offset;

            for (var y = rowRange.Start; y <= rowRange.End; y++)
            {
                if (y < Rows.Frozen || Rows.IsHidden(y))
                {
                    continue;
                }
                var item = new VirtualRowHeader
                {
                    Row = y,
                    Rect = new PixelRectangle(headerTop + SplitterHeight, 0, headerTop + SplitterHeight + Rows[y], View.ColumnHeaderOffset),
                };
                items.Add(item);
                headerTop += Rows[y];
            }
        }

        var frozenTop = View.RowHeaderOffset;

        for (var y = 0; y < Rows.Frozen; y++)
        {
            if (Rows.IsHidden(y))
            {
                continue;
            }

            var frozenLeft = View.ColumnHeaderOffset;

            for (var x = 0; x < Columns.Frozen; x++)
            {
                if (Columns.IsHidden(x))
                {
                    continue;
                }

                var cell = new CellRef(y, x);

                if (!MergedCells.Contains(cell))
                {
                    var item = new VirtualDataItem
                    {
                        Row = y,
                        Column = x,
                        Rect = new PixelRectangle(frozenTop, frozenLeft, frozenTop + Rows[y], frozenLeft + Columns[x]),
                        FrozenState = FrozenState.Both
                    };
                    items.Add(item);
                }
                frozenLeft += Columns[x];
            }

            var left = -columnRange.Offset;
            for (var x = columnRange.Start; x <= columnRange.End; x++)
            {
                if (x < Columns.Frozen || Columns.IsHidden(x))
                {
                    continue;
                }

                var cell = new CellRef(y, x);

                if (!MergedCells.Contains(cell))
                {
                    var item = new VirtualDataItem
                    {
                        Row = y,
                        Column = x,
                        Rect = new PixelRectangle(frozenTop, left + SplitterWidth, frozenTop + Rows[y], left + SplitterWidth + Columns[x]),
                        FrozenState = FrozenState.Row,
                    };
                    items.Add(item);
                }

                left += Columns[x];
            }
            frozenTop += Rows[y];
        }

        var top = -rowRange.Offset;
        for (var y = rowRange.Start; y <= rowRange.End; y++)
        {
            if (Rows.IsHidden(y))
            {
                continue;
            }

            var frozenLeft = View.ColumnHeaderOffset;
            for (var x = 0; x < Columns.Frozen; x++)
            {
                if (Columns.IsHidden(x))
                {
                    continue;
                }

                var cell = new CellRef(y, x);

                if (!MergedCells.Contains(cell))
                {
                    var item = new VirtualDataItem
                    {
                        Row = y,
                        Column = x,
                        Rect = new PixelRectangle(top + SplitterHeight, frozenLeft, top + SplitterHeight + Rows[y], frozenLeft + Columns[x]),
                        FrozenState = FrozenState.Column,
                    };
                    items.Add(item);
                }
                frozenLeft += Columns[x];
            }

            var left = -columnRange.Offset;

            for (var x = columnRange.Start; x <= columnRange.End; x++)
            {
                if (x < Columns.Frozen || Columns.IsHidden(x))
                {
                    continue;
                }

                var cell = new CellRef(y, x);

                if (!MergedCells.Contains(cell))
                {
                    var item = new VirtualDataItem
                    {
                        Row = y,
                        Column = x,
                        Rect = new PixelRectangle(top + SplitterHeight, left + SplitterWidth, top + SplitterHeight + Rows[y], left + SplitterWidth + Columns[x])
                    };
                    items.Add(item);
                }
                left += Columns[x];
            }
            top += Rows[y];
        }

        var visibleRange = new RangeRef(new CellRef(0, 0), new CellRef(rowRange.End + Rows.Frozen, columnRange.End + Columns.Frozen));

        var visibleMergedCells = MergedCells.GetOverlappingRanges(visibleRange);

        foreach (var range in visibleMergedCells)
        {
            // Adjust the range to start from the first visible row and column
            var adjustedRange = AdjustRangeForHiddenRowsAndColumns(range);
            
            // Skip if the entire range is hidden
            if (adjustedRange is null)
            {
                continue;
            }

            var ranges = MergedCells.SplitRange(adjustedRange.Value, Rows.Frozen, Columns.Frozen);

            foreach (var splitRange in ranges)
            {
                var rectangle = GetRectangle(splitRange, scrollY, scrollX);
                var frozenState = GetFrozenState(splitRange);
                var item = new VirtualDataItem
                {
                    Row = splitRange.Start.Row,
                    Column = splitRange.Start.Column,
                    Rect = rectangle,
                    FrozenState = frozenState
                };
                items.Add(item);
            }
        }

        ScrollLeft = scrollX;
        ScrollTop = scrollY;

        StateHasChanged();
    }

    private FrozenState GetFrozenState(RangeRef range)
    {
        var state = FrozenState.None;
        
        if (range.Start.Row < Rows.Frozen && range.End.Row < Rows.Frozen)
        {
            state |= FrozenState.Row;
        }
        
        if (range.Start.Column < Columns.Frozen && range.End.Column < Columns.Frozen)
        {
            state |= FrozenState.Column;
        }
        
        return state;
    }

    private RangeRef? AdjustRangeForHiddenRowsAndColumns(RangeRef range)
    {
        var row = range.Start.Row;

        while (row <= range.End.Row && Rows.IsHidden(row))
        {
            row++;
        }

        var column = range.Start.Column;

        while (column <= range.End.Column && Columns.IsHidden(column))
        {
            column++;
        }

        if (row > range.End.Row || column > range.End.Column)
        {
            return null;
        }

        return new RangeRef(new CellRef(row, column), range.End);
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        reference?.Dispose();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public PixelRectangle GetRectangle(int top, int left)
    {
        var mergedRange = MergedCells.GetMergedRange(new CellRef(top, left));

        if (mergedRange != RangeRef.Invalid)
        {
            return GetRectangle(mergedRange, ScrollTop, ScrollLeft);
        }

        var columnRange = View.GetColumnPixelRange(left);
        var rowRange = View.GetRowPixelRange(top);

        if (left >= Columns.Frozen)
        {
            columnRange = columnRange.OffsetStart(-ScrollLeft + SplitterWidth).OffsetEnd(-ScrollLeft + SplitterWidth);
        }
        
        if (top >= Rows.Frozen)
        {
            rowRange = rowRange.OffsetStart(-ScrollTop + SplitterHeight).OffsetEnd(-ScrollTop + SplitterHeight);
        }

        return new PixelRectangle(columnRange, rowRange);
    }

    private PixelRectangle GetRectangle(RangeRef range, double scrollTop, double scrollLeft) => GetRectangle(range.Start.Row, range.Start.Column, range.End.Row, range.End.Column, scrollTop, scrollLeft);

    private PixelRectangle GetRectangle(int top, int left, int bottom, int right, double scrollTop, double scrollLeft)
    {
        var columnRange = View.GetColumnPixelRange(left, right);
        var rowRange = View.GetRowPixelRange(top, bottom);

        if (left >= Columns.Frozen)
        {
            columnRange = columnRange.OffsetStart(-scrollLeft + SplitterWidth);
        }

        if (right >= Columns.Frozen)
        {
            columnRange = columnRange.OffsetEnd(-scrollLeft + SplitterWidth);
        }

        if (top >= Rows.Frozen)
        {
            rowRange = rowRange.OffsetStart(-scrollTop + SplitterHeight);
        }

        if (bottom >= Rows.Frozen)
        {
            rowRange = rowRange.OffsetEnd(-scrollTop + SplitterHeight);
        }

        return new PixelRectangle(columnRange, rowRange);
    }

    /// <inheritdoc/>
    public PixelRectangle GetRectangle(int top, int left, int bottom, int right) => GetRectangle(top, left, bottom, right, ScrollTop, ScrollLeft);

    /// <inheritdoc/>
    public IEnumerable<RangeInfo> GetRanges(RangeRef range) => View.GetRanges(range);
}