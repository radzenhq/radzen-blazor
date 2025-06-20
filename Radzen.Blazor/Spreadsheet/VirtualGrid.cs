using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

class VirtualRegion
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
}

/// <summary>
/// Represents a virtual grid component that supports virtualization for large datasets.
/// </summary>
public partial class VirtualGrid : ComponentBase, IAsyncDisposable, IVirtualGridContext
{
    /// <summary>
    /// Gets or sets the axis store for rows in the virtual grid.
    /// </summary>
    [Parameter, EditorRequired]
    public Axis Rows { get; set; } = default!;

    /// <summary>
    /// Gets or sets the axis store for columns in the virtual grid.
    /// </summary>
    [Parameter, EditorRequired]
    public Axis Columns { get; set; } = default!;

    /// <summary>
    /// Gets or sets the store for merged cells in the virtual grid.
    /// </summary>
    [Parameter, EditorRequired]
    public MergedCellStore MergedCells { get; set; } = default!;

    /// <summary>
    /// Gets or sets splitter size in pixels. The splitter is used to separate frozen and non-frozen rows and columns.
    /// </summary>
    [Parameter]
    public double SplitterSize { get; set; } = 4;

    private double SplitterWidth => Columns.Frozen > 0 ? SplitterSize : 0;

    private double SplitterHeight => Rows.Frozen > 0 ? SplitterSize : 0;

    private string SpacerStyle => $"width: {(Columns.Total + SplitterWidth).ToPx()}; height: {(Rows.Total + SplitterHeight).ToPx()};";

    /// <summary>
    /// Gets or sets additional attributes that will be rendered in the root element of the virtual grid.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

    ElementReference scrollable;
    ElementReference content;

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
        if (Rows != null)
        {
            Rows.Changed -= OnChanged;
        }

        if (Columns != null)
        {
            Columns.Changed -= OnChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Rows != null)
        {
            Rows.Changed += OnChanged;
        }

        if (Columns != null)
        {
            Columns.Changed += OnChanged;
        }
    }

    private void OnChanged()
    {
        Render(ScrollLeft, ScrollTop);
    }

    private List<VirtualItem>? items;

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
        var columnRange = Columns.GetPixelRange(column);
        var rowRange = Rows.GetPixelRange(row);

        var frozenColumnRange = Columns.GetPixelRange(Columns.Frozen);
        var frozenRowRange = Rows.GetPixelRange(Rows.Frozen);

        var visibleLeft = ScrollLeft + frozenColumnRange.End - Columns.Offset;
        var visibleRight = ScrollLeft + region.Width;
        var visibleTop = ScrollTop + frozenRowRange.End - Rows.Offset;
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
        var columnRange = Columns.GetIndexRange(scrollX, scrollX + region.Width);
        var rowRange = Rows.GetIndexRange(scrollY, scrollY + region.Height);

        items = [];

        if (Rows.Offset > 0)
        {
            if (Columns.Offset > 0)
            {
                var item = new VirtualCorner
                {
                    Rect = new PixelRectangle(0, 0, Rows.Offset, Columns.Offset)
                };
                items.Add(item);
            }

            var left = Columns.Offset;

            for (var x = 0; x < Columns.Frozen; x++)
            {
                if (Columns.IsHidden(x))
                {
                    continue;
                }

                var item = new VirtualColumnHeader
                {
                    Column = x,
                    Rect = new PixelRectangle(0, left, Rows.Offset, left + Columns[x]),
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
                    Rect = new PixelRectangle(0, left + SplitterWidth, Rows.Offset, left + SplitterWidth + Columns[x]),
                };
                items.Add(item);
                left += Columns[x];
            }
        }

        if (Columns.Offset > 0)
        {
            var headerTop = Rows.Offset;

            for (var y = 0; y < Rows.Frozen; y++)
            {
                if (Rows.IsHidden(y))
                {
                    continue;
                }

                var item = new VirtualRowHeader
                {
                    Row = y,
                    Rect = new PixelRectangle(headerTop, 0, headerTop + Rows[y], Columns.Offset),
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
                    Rect = new PixelRectangle(headerTop + SplitterHeight, 0, headerTop + SplitterHeight + Rows[y], Columns.Offset),
                };
                items.Add(item);
                headerTop += Rows[y];
            }
        }

        var frozenTop = Rows.Offset;

        for (var y = 0; y < Rows.Frozen; y++)
        {
            if (Rows.IsHidden(y))
            {
                continue;
            }

            var frozenLeft = Columns.Offset;

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

            var frozenLeft = Columns.Offset;
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
            var ranges = MergedCells.SplitRange(range);

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

        var columnRange = Columns.GetPixelRange(left);
        var rowRange = Rows.GetPixelRange(top);

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
        var columnRange = Columns.GetPixelRange(left, right);
        var rowRange = Rows.GetPixelRange(top, bottom);

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
}