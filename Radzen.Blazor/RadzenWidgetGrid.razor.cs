using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// A dashboard grid that lays out <see cref="RadzenWidgetGridItem" /> widgets on a configurable
    /// column/row grid. When <see cref="EditMode" /> is enabled widgets can be moved by dragging their
    /// header and resized from the bottom-right corner. Widget positions are snapped to grid cells and
    /// reported through two-way bindable parameters and the <see cref="Change" /> callback.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenWidgetGrid Columns="12" RowHeight="80" EditMode="true" ShowGrid="true" Change=@OnChange&gt;
    ///     &lt;RadzenWidgetGridItem Title="Sales" Col="1" Row="1" ColSpan="6" RowSpan="2"&gt;
    ///         ...
    ///     &lt;/RadzenWidgetGridItem&gt;
    ///     &lt;RadzenWidgetGridItem Title="Visitors" Col="7" Row="1" ColSpan="6" RowSpan="2"&gt;
    ///         ...
    ///     &lt;/RadzenWidgetGridItem&gt;
    /// &lt;/RadzenWidgetGrid&gt;
    /// </code>
    /// </example>
    public partial class RadzenWidgetGrid : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the widgets to display. Should contain <see cref="RadzenWidgetGridItem" /> components.
        /// </summary>
        /// <value>The child content render fragment.</value>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the number of columns in the grid.
        /// </summary>
        /// <value>The column count. Default is <c>12</c>.</value>
        [Parameter]
        public int Columns { get; set; } = 12;

        /// <summary>
        /// Gets or sets the number of rows in the grid. When <c>0</c> the grid grows automatically to fit its widgets.
        /// </summary>
        /// <value>The row count. Default is <c>0</c> (auto).</value>
        [Parameter]
        public int Rows { get; set; }

        /// <summary>
        /// Gets or sets the height of a single row, in pixels.
        /// </summary>
        /// <value>The row height in pixels. Default is <c>80</c>.</value>
        [Parameter]
        public double RowHeight { get; set; } = 80;

        /// <summary>
        /// Gets or sets the gap between cells, in pixels.
        /// </summary>
        /// <value>The gap in pixels. Default is <c>8</c>.</value>
        [Parameter]
        public double Gap { get; set; } = 8;

        /// <summary>
        /// Gets or sets a value indicating whether widgets can be moved and resized.
        /// When <c>false</c> the grid is read-only.
        /// </summary>
        /// <value><c>true</c> to allow editing; otherwise <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool EditMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether widgets can be moved while in <see cref="EditMode" />.
        /// </summary>
        /// <value><c>true</c> to allow moving; otherwise <c>false</c>. Default is <c>true</c>.</value>
        [Parameter]
        public bool AllowMove { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether widgets can be resized while in <see cref="EditMode" />.
        /// </summary>
        /// <value><c>true</c> to allow resizing; otherwise <c>false</c>. Default is <c>true</c>.</value>
        [Parameter]
        public bool AllowResize { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to render a grid overlay.
        /// </summary>
        /// <value><c>true</c> to show the overlay; otherwise <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool ShowGrid { get; set; }

        /// <summary>
        /// Gets or sets the callback raised after a widget has been moved or resized.
        /// </summary>
        /// <value>The change callback. The argument is the affected widget.</value>
        [Parameter]
        public EventCallback<RadzenWidgetGridItem> Change { get; set; }

        private readonly List<RadzenWidgetGridItem> items = new();

        private ElementReference cellsElement;

        private RadzenWidgetGridItem? activeItem;
        private bool resizing;
        private double startClientX;
        private double startClientY;
        private int startCol;
        private int startRow;
        private int startColSpan;
        private int startRowSpan;
        private double strideX;
        private double strideY;
        private long pointerId;

        internal bool CanMove => EditMode && AllowMove;

        internal bool CanResize => EditMode && AllowResize;

        private int OverlayRows
        {
            get
            {
                if (Rows > 0)
                {
                    return Rows;
                }

                var max = 1;
                foreach (var item in items)
                {
                    max = Math.Max(max, item.CurrentRow + item.CurrentRowSpan - 1);
                }

                return max;
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-widget-grid";
        }

        internal void AddItem(RadzenWidgetGridItem item)
        {
            if (!items.Contains(item))
            {
                items.Add(item);
                StateHasChanged();
            }
        }

        internal void RemoveItem(RadzenWidgetGridItem item)
        {
            if (items.Remove(item))
            {
                if (activeItem == item)
                {
                    activeItem = null;
                }

                try
                {
                    InvokeAsync(StateHasChanged);
                }
                catch
                {
                    // The component is being disposed.
                }
            }
        }

        internal Task StartMove(RadzenWidgetGridItem item, PointerEventArgs args)
        {
            return StartDrag(item, args, false);
        }

        internal Task StartResize(RadzenWidgetGridItem item, PointerEventArgs args)
        {
            return StartDrag(item, args, true);
        }

        private async Task StartDrag(RadzenWidgetGridItem item, PointerEventArgs args, bool isResize)
        {
            if (JSRuntime == null)
            {
                return;
            }

            activeItem = item;
            resizing = isResize;
            startClientX = args.ClientX;
            startClientY = args.ClientY;
            startCol = item.CurrentCol;
            startRow = item.CurrentRow;
            startColSpan = item.CurrentColSpan;
            startRowSpan = item.CurrentRowSpan;
            pointerId = (long)args.PointerId;

            item.SetDragging(true);

            var rect = await JSRuntime.InvokeAsync<Rect>("Radzen.clientRect", cellsElement);

            var cellWidth = Columns > 0 ? (rect.Width - ((Columns - 1) * Gap)) / Columns : rect.Width;
            strideX = cellWidth + Gap;
            strideY = RowHeight + Gap;

            await JSRuntime.InvokeVoidAsync("Radzen.capturePointer", cellsElement, pointerId);
        }

        private async Task OnPointerMove(PointerEventArgs args)
        {
            if (activeItem == null)
            {
                return;
            }

            if (args.Buttons == 0)
            {
                await EndDrag();
                return;
            }

            var colDelta = strideX > 0 ? (int)Math.Round((args.ClientX - startClientX) / strideX, MidpointRounding.AwayFromZero) : 0;
            var rowDelta = strideY > 0 ? (int)Math.Round((args.ClientY - startClientY) / strideY, MidpointRounding.AwayFromZero) : 0;

            if (resizing)
            {
                var colSpan = Math.Max(1, startColSpan + colDelta);
                var rowSpan = Math.Max(1, startRowSpan + rowDelta);

                colSpan = Math.Min(colSpan, Math.Max(1, Columns - activeItem.CurrentCol + 1));

                if (Rows > 0)
                {
                    rowSpan = Math.Min(rowSpan, Math.Max(1, Rows - activeItem.CurrentRow + 1));
                }

                if (colSpan != activeItem.CurrentColSpan || rowSpan != activeItem.CurrentRowSpan)
                {
                    activeItem.SetSpan(colSpan, rowSpan);
                    StateHasChanged();
                }
            }
            else
            {
                var col = Math.Max(1, startCol + colDelta);
                var row = Math.Max(1, startRow + rowDelta);

                col = Math.Max(1, Math.Min(col, Columns - activeItem.CurrentColSpan + 1));

                if (Rows > 0)
                {
                    row = Math.Max(1, Math.Min(row, Rows - activeItem.CurrentRowSpan + 1));
                }

                if (col != activeItem.CurrentCol || row != activeItem.CurrentRow)
                {
                    activeItem.SetPosition(col, row);
                    StateHasChanged();
                }
            }
        }

        private Task OnPointerUp(PointerEventArgs args)
        {
            return EndDrag();
        }

        private async Task EndDrag()
        {
            if (activeItem == null)
            {
                return;
            }

            var item = activeItem;
            activeItem = null;
            item.SetDragging(false);

            if (JSRuntime != null)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.releasePointer", cellsElement, pointerId);
                }
                catch
                {
                    // Ignore - the pointer capture may already be released.
                }
            }

            var changed = await item.CommitAsync();

            if (changed)
            {
                await Change.InvokeAsync(item);
            }

            StateHasChanged();
        }

        private string GetGridStyle()
        {
            const string componentStyle = "box-sizing: border-box; position: relative; width: 100%; height: 100%; overflow: auto;";

            return string.IsNullOrWhiteSpace(Style)
                ? componentStyle
                : string.Create(CultureInfo.InvariantCulture, $"{componentStyle} {Style}");
        }

        private string GetGridTracksStyle()
        {
            var rows = Rows > 0
                ? string.Create(CultureInfo.InvariantCulture, $"grid-template-rows: repeat({Rows}, {RowHeight}px);")
                : string.Create(CultureInfo.InvariantCulture, $"grid-auto-rows: {RowHeight}px;");

            return string.Create(CultureInfo.InvariantCulture,
                $"grid-template-columns: repeat({Math.Max(1, Columns)}, minmax(0, 1fr)); gap: {Gap}px; {rows}");
        }

        private string GetCellsStyle()
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"box-sizing: border-box; display: grid; position: relative; width: 100%; min-height: 100%; padding: 0; border: 0; {GetGridTracksStyle()}");
        }

        private string GetOverlayStyle()
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"box-sizing: border-box; display: grid; position: absolute; inset: 0; width: 100%; min-height: 100%; padding: 0; border: 0; pointer-events: none; z-index: 2; {GetGridTracksStyle()}");
        }

        private static string GetOverlayCellStyle(int column, int row)
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"grid-column: {column}; grid-row: {row}; box-sizing: border-box; background-color: var(--rz-widget-grid-overlay-background-color, var(--rz-primary-lighter)); border: 1px dashed var(--rz-widget-grid-overlay-border-color, var(--rz-primary)); border-radius: var(--rz-widget-grid-item-border-radius, var(--rz-border-radius)); opacity: 0.35;");
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (activeItem != null && JSRuntime != null && IsJSRuntimeAvailable)
            {
                try
                {
                    JSRuntime.InvokeVoid("Radzen.releasePointer", cellsElement, pointerId);
                }
                catch
                {
                    // Ignore - the grid is being disposed.
                }
            }

            base.Dispose();
        }
    }
}
