using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    public partial class RadzenDataGrid<TItem> where TItem : notnull
    {
        const double DefaultVirtualColumnWidth = 200;
        const double DefaultVirtualViewportWidth = 1920;
        const string VirtualSpacerClass = "rz-grid-virtual-spacer";
        const string VirtualColumnIndexAttribute = "data-virtual-column-index";

        /// <summary>
        /// Gets or sets whether the DataGrid renders only the columns that are scrolled into view, plus the frozen columns.
        /// Columns outside the view are replaced with spacer cells, which keeps grids with hundreds of columns responsive.
        /// Every column gets a pixel width: a <c>px</c> or <c>%</c> <see cref="RadzenDataGridColumn{TItem}.Width"/>, the grid
        /// <see cref="ColumnWidth"/>, or 200px when neither is set. Ignored when <see cref="Responsive"/> is enabled.
        /// </summary>
        /// <value><c>true</c> to enable column virtualization; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool AllowColumnVirtualization { get; set; }

        /// <summary>
        /// Gets or sets the number of additional columns rendered before and after the visible columns when
        /// <see cref="AllowColumnVirtualization"/> is enabled. A higher value reduces blank space during fast horizontal scrolling.
        /// </summary>
        /// <value>The number of extra columns to render on each side. Default is 3.</value>
        [Parameter]
        public int ColumnVirtualizationOverscanCount { get; set; } = 3;

        internal sealed class ColumnSlot
        {
            public RadzenDataGridColumn<TItem>? Column { get; init; }
            public int Index { get; init; }
            public double Width { get; set; }
            public string Key { get; init; } = string.Empty;
        }

        internal sealed class ColumnWindow
        {
            public List<RadzenDataGridColumn<TItem>> All { get; init; } = new();
            public List<RadzenDataGridColumn<TItem>> Columns { get; } = new();
            public List<ColumnSlot> Slots { get; } = new();
            public Dictionary<RadzenDataGridColumn<TItem>, int> Indexes { get; } = new();
            public Dictionary<RadzenDataGridColumn<TItem>, ColumnSlot> SpacersBefore { get; } = new();
            public Dictionary<RadzenDataGridColumn<TItem>, double> Widths { get; } = new();
            public bool[] Rendered { get; init; } = Array.Empty<bool>();
            public ColumnSlot? TrailingSpacer { get; set; }
            public int SpacerCount { get; set; }
            public string Signature { get; set; } = string.Empty;
        }

        internal ColumnWindow? columnWindow;

        double columnScrollLeft;
        double columnViewportWidth;
        int focusedVirtualColumn = -1;
        int activeVirtualColumn = -1;
        bool pendingVirtualCellFocus;
        bool columnVirtualizationAttached;

        internal bool IsColumnVirtualizationAllowed()
        {
            return AllowColumnVirtualization && !Responsive;
        }

        internal IList<RadzenDataGridColumn<TItem>> UpdateColumnWindow(List<RadzenDataGridColumn<TItem>> visibleColumns)
        {
            columnWindow = IsColumnVirtualizationAllowed() && visibleColumns.Count > 0 ? BuildColumnWindow(visibleColumns) : null;

            return columnWindow != null ? columnWindow.Columns : visibleColumns;
        }

        ColumnWindow BuildColumnWindow(List<RadzenDataGridColumn<TItem>> visibleColumns)
        {
            var count = visibleColumns.Count;
            var viewport = columnViewportWidth > 0 ? columnViewportWidth : DefaultVirtualViewportWidth;
            var widths = new double[count];
            var window = new ColumnWindow { All = visibleColumns, Rendered = new bool[count] };

            var start = columnScrollLeft;
            var end = start + viewport;
            var first = -1;
            var last = -1;
            var lastScrollable = -1;
            var offset = 0d;

            for (var index = 0; index < count; index++)
            {
                var column = visibleColumns[index];
                var width = ResolveColumnWidth(column, viewport, window.Widths);

                widths[index] = width;
                window.Indexes[column] = index;

                if (column.IsFrozen())
                {
                    window.Rendered[index] = true;
                }
                else
                {
                    if (offset + width > start && offset < end)
                    {
                        if (first < 0)
                        {
                            first = index;
                        }

                        last = index;
                    }

                    if (offset < end)
                    {
                        lastScrollable = index;
                    }
                }

                offset += width;
            }

            if (first < 0)
            {
                first = last = lastScrollable;
            }

            if (first >= 0)
            {
                var overscan = Math.Max(0, ColumnVirtualizationOverscanCount);

                first = Math.Max(0, first - overscan);
                last = Math.Min(count - 1, last + overscan);

                for (var index = first; index <= last; index++)
                {
                    window.Rendered[index] = true;
                }
            }

            if (focusedVirtualColumn >= 0 && focusedVirtualColumn < count)
            {
                window.Rendered[focusedVirtualColumn] = true;
            }

            if (activeVirtualColumn >= 0 && activeVirtualColumn < count)
            {
                window.Rendered[activeVirtualColumn] = true;
            }

            ColumnSlot? spacer = null;

            for (var index = 0; index < count; index++)
            {
                if (window.Rendered[index])
                {
                    var column = visibleColumns[index];

                    if (spacer != null)
                    {
                        window.SpacersBefore[column] = spacer;
                        spacer = null;
                    }

                    window.Columns.Add(column);
                    window.Slots.Add(new ColumnSlot { Column = column, Index = index, Width = widths[index] });
                }
                else
                {
                    if (spacer == null)
                    {
                        spacer = new ColumnSlot { Index = index, Key = $"rz-virtual-spacer-{window.SpacerCount}" };
                        window.SpacerCount++;
                        window.Slots.Add(spacer);
                    }

                    spacer.Width += widths[index];
                }
            }

            window.TrailingSpacer = spacer;
            window.Signature = string.Join(";", window.Slots.Select(s => s.Column != null
                ? s.Index.ToString(CultureInfo.InvariantCulture)
                : $"s{s.Width.ToString("0.##", CultureInfo.InvariantCulture)}")) + $"|{viewport.ToString("0.##", CultureInfo.InvariantCulture)}";

            return window;
        }

        double ResolveColumnWidth(RadzenDataGridColumn<TItem> column, double viewport, Dictionary<RadzenDataGridColumn<TItem>, double> widths, double? inherited = null)
        {
            double width;

            if (column.Columns != null)
            {
                var children = VisibleChildColumns(column).ToList();

                if (children.Count > 0)
                {
                    double? share = TryParseLength(column.GetWidth(), viewport, out var own) ? own / children.Count : inherited;

                    width = 0;

                    foreach (var child in children)
                    {
                        width += ResolveColumnWidth(child, viewport, widths, share);
                    }

                    widths[column] = width;

                    return width;
                }
            }

            if (!TryParseLength(column.GetWidth(), viewport, out width))
            {
                if (inherited.HasValue)
                {
                    width = inherited.Value;
                }
                else if (!TryParseLength(ColumnWidth, viewport, out width))
                {
                    width = DefaultVirtualColumnWidth;
                }
            }

            if (TryParseLength(column.MinWidth, viewport, out var min) && width < min)
            {
                width = min;
            }

            if (TryParseLength(column.MaxWidth, viewport, out var max) && width > max)
            {
                width = max;
            }

            widths[column] = width;

            return width;
        }

        IEnumerable<RadzenDataGridColumn<TItem>> VisibleChildColumns(RadzenDataGridColumn<TItem> column)
        {
            return childColumns.Where(c => c.GetVisible() && c.Parent == column);
        }

        static bool TryParseLength(string? value, double viewport, out double pixels)
        {
            pixels = 0;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var text = value.Trim();
            var factor = 1d;

            if (text.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            {
                text = text[..^2];
            }
            else if (text[^1] == '%')
            {
                text = text[..^1];
                factor = viewport / 100;
            }
            else if (text.EndsWith("rem", StringComparison.OrdinalIgnoreCase))
            {
                text = text[..^3];
                factor = 16;
            }
            else if (text.EndsWith("em", StringComparison.OrdinalIgnoreCase))
            {
                text = text[..^2];
                factor = 16;
            }

            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || number <= 0)
            {
                return false;
            }

            pixels = number * factor;

            return true;
        }

        static string ToVirtualWidthStyle(double width)
        {
            var pixels = ToPixels(width);

            return $"width:{pixels};min-width:{pixels};max-width:{pixels}";
        }

        internal IEnumerable<(string? Id, string Style)> GetVirtualColumnDefinitions()
        {
            var window = columnWindow;

            if (window == null)
            {
                yield break;
            }

            foreach (var slot in window.Slots)
            {
                if (slot.Column == null)
                {
                    yield return (null, ToVirtualWidthStyle(slot.Width));
                }
                else if (slot.Column.Columns == null)
                {
                    yield return (getColumnUniqueId(slot.Index) + "-col", $"width:{ToPixels(slot.Width)}");
                }
                else
                {
                    foreach (var leaf in VisibleLeafColumns(slot.Column))
                    {
                        yield return (null, $"width:{ToPixels(window.Widths.TryGetValue(leaf, out var width) ? width : DefaultVirtualColumnWidth)}");
                    }
                }
            }
        }

        IEnumerable<RadzenDataGridColumn<TItem>> VisibleLeafColumns(RadzenDataGridColumn<TItem> column)
        {
            var children = VisibleChildColumns(column).ToList();

            if (children.Count == 0)
            {
                yield return column;
                yield break;
            }

            foreach (var child in children)
            {
                foreach (var leaf in VisibleLeafColumns(child))
                {
                    yield return leaf;
                }
            }
        }

        internal int GetVisibleColumnIndex(List<RadzenDataGridColumn<TItem>> visibleColumns, RadzenDataGridColumn<TItem> column)
        {
            return columnWindow != null && columnWindow.Indexes.TryGetValue(column, out var index) ? index : visibleColumns.IndexOf(column);
        }

        internal int ColumnSpacerCount => columnWindow?.SpacerCount ?? 0;

        internal string? GridAriaColCount()
        {
            return columnWindow != null && deepestChildColumnLevel == 0
                ? (columnWindow.All.Count + LeadingCellCount()).ToString(CultureInfo.InvariantCulture)
                : null;
        }

        int LeadingCellCount()
        {
            return (ShowGroupExpandColumn ? Groups.Count : 0) + (Template != null && ShowExpandColumn ? 1 : 0);
        }

        internal RenderFragment? ColumnSpacerBefore(RadzenDataGridColumn<TItem> column, string tag, int rowIndex = 0)
        {
            return columnWindow != null && rowIndex == 0 && columnWindow.SpacersBefore.TryGetValue(column, out var spacer)
                ? RenderColumnSpacer(spacer, tag, deepestChildColumnLevel + 1)
                : null;
        }

        internal RenderFragment? ColumnSpacerAfter(string tag, int rowIndex = 0)
        {
            return columnWindow?.TrailingSpacer != null && rowIndex == 0
                ? RenderColumnSpacer(columnWindow.TrailingSpacer, tag, deepestChildColumnLevel + 1)
                : null;
        }

        static RenderFragment RenderColumnSpacer(ColumnSlot spacer, string tag, int rowSpan) => builder =>
        {
            AddColumnSpacer(builder, spacer, tag, rowSpan);
        };

        static void AddColumnSpacer(RenderTreeBuilder builder, ColumnSlot spacer, string tag, int rowSpan)
        {
            builder.OpenElement(0, tag);
            builder.SetKey(spacer.Key);
            builder.AddAttribute(1, "class", VirtualSpacerClass);
            builder.AddAttribute(2, "aria-hidden", "true");

            if (rowSpan > 1)
            {
                builder.AddAttribute(3, "rowspan", rowSpan);
            }

            builder.CloseElement();
        }

        internal IReadOnlyDictionary<string, object> AddVirtualColumnAttributes(IReadOnlyDictionary<string, object> attributes, RadzenDataGridColumn<TItem> column)
        {
            if (columnWindow == null || !columnWindow.Indexes.TryGetValue(column, out var index))
            {
                return attributes;
            }

            var result = new Dictionary<string, object>(attributes.Count + 2);

            foreach (var attribute in attributes)
            {
                result[attribute.Key] = attribute.Value;
            }

            AddVirtualColumnAttributes(result, index);

            return result;
        }

        void AddVirtualColumnAttributes(Dictionary<string, object> attributes, int index)
        {
            attributes[VirtualColumnIndexAttribute] = index;

            if (deepestChildColumnLevel == 0)
            {
                attributes["aria-colindex"] = index + LeadingCellCount() + 1;
            }
        }

        internal RenderFragment RenderVirtualCells(TItem item, Tuple<Radzen.RowRenderEventArgs<TItem>, IReadOnlyDictionary<string, object>> rowArgs, int rowIndex, bool renderSpacers, int spacerRowSpan) => builder =>
        {
            var window = columnWindow;

            if (window == null)
            {
                return;
            }

            if (CellRender == null && rowSpans.Count == 0)
            {
                foreach (var slot in window.Slots)
                {
                    if (slot.Column != null)
                    {
                        RenderCell(slot.Column, item, EmptyAttributes, rowArgs, rowIndex, true)(builder);
                    }
                    else if (renderSpacers)
                    {
                        AddColumnSpacer(builder, slot, "td", spacerRowSpan);
                    }
                }

                return;
            }

            var all = window.All;
            var count = all.Count;
            var origins = new int[count];
            var ends = new int[count];
            var attributes = new IReadOnlyDictionary<string, object>?[count];

            for (var index = 0; index < count; index++)
            {
                if (rowSpans.TryGetValue(index, out var remaining))
                {
                    remaining--;

                    if (remaining > 0)
                    {
                        rowSpans[index] = remaining;
                        origins[index] = -1;
                        continue;
                    }

                    rowSpans.Remove(index);
                }

                var cellAttributes = CellAttributes(item, all[index]);
                var colSpan = Math.Max(1, GetSpan(cellAttributes, "colspan") ?? 1);
                var rowSpan = GetSpan(cellAttributes, "rowspan");
                var end = Math.Min(count - 1, index + colSpan - 1);

                if (rowSpan.HasValue)
                {
                    for (var covered = index; covered <= end; covered++)
                    {
                        rowSpans[covered] = rowSpan.Value;
                    }
                }

                attributes[index] = cellAttributes;

                for (var covered = index; covered <= end; covered++)
                {
                    origins[covered] = index;
                    ends[covered] = end;
                }

                index = end;
            }

            foreach (var slot in window.Slots)
            {
                if (slot.Column == null)
                {
                    if (renderSpacers)
                    {
                        AddColumnSpacer(builder, slot, "td", spacerRowSpan);
                    }

                    continue;
                }

                var index = slot.Index;
                var origin = origins[index];

                if (origin < 0 || index > origin && window.Rendered[index - 1] && origins[index - 1] == origin)
                {
                    continue;
                }

                var length = 1;

                while (index + length <= ends[index] && window.Rendered[index + length])
                {
                    length++;
                }

                var first = true;

                for (var previous = origin; previous < index; previous++)
                {
                    if (window.Rendered[previous])
                    {
                        first = false;
                        break;
                    }
                }

                var source = attributes[origin] ?? EmptyAttributes;

                if (first)
                {
                    RenderCell(all[origin], item, WithColSpan(source, length), rowArgs, rowIndex, true)(builder);
                }
                else
                {
                    builder.OpenElement(0, "td");
                    builder.SetKey((origin, index));
                    builder.AddAttribute(1, "role", "gridcell");

                    if (length > 1)
                    {
                        builder.AddAttribute(2, "colspan", length);
                    }

                    if (source.TryGetValue("rowspan", out var rowSpanValue))
                    {
                        builder.AddAttribute(3, "rowspan", rowSpanValue);
                    }

                    builder.CloseElement();
                }
            }
        };

        static int? GetSpan(IReadOnlyDictionary<string, object> attributes, string name)
        {
            return attributes.TryGetValue(name, out var value) && value != null
                ? (int)Convert.ChangeType(value, TypeCode.Int32, CultureInfo.InvariantCulture)
                : null;
        }

        static IReadOnlyDictionary<string, object> WithColSpan(IReadOnlyDictionary<string, object> attributes, int colSpan)
        {
            var current = GetSpan(attributes, "colspan");

            if (current == null || current == colSpan)
            {
                return attributes;
            }

            var result = new Dictionary<string, object>(attributes.Count);

            foreach (var attribute in attributes)
            {
                if (attribute.Key != "colspan")
                {
                    result[attribute.Key] = attribute.Value;
                }
            }

            if (colSpan > 1)
            {
                result["colspan"] = colSpan;
            }

            return result;
        }

        /// <summary>
        /// Called from JS when the grid is scrolled horizontally or resized while <see cref="AllowColumnVirtualization"/> is enabled.
        /// </summary>
        /// <param name="scrollLeft">The horizontal scroll offset from the inline start edge.</param>
        /// <param name="width">The width of the scrollable area.</param>
        [JSInvokable("RadzenGrid.OnColumnScroll")]
        public void OnColumnScroll(double scrollLeft, double width)
        {
            columnScrollLeft = Math.Max(0, scrollLeft);
            columnViewportWidth = width;

            RefreshColumnWindow();
        }

        /// <summary>
        /// Called from JS when focus moves to a cell of another column while <see cref="AllowColumnVirtualization"/> is enabled.
        /// </summary>
        /// <param name="columnIndex">Index of the visible column that contains the focus, or -1 when focus left the cells.</param>
        [JSInvokable("RadzenGrid.OnColumnFocus")]
        public void OnColumnFocus(int columnIndex)
        {
            activeVirtualColumn = columnIndex;

            RefreshColumnWindow();
        }

        void RefreshColumnWindow()
        {
            if (!IsColumnVirtualizationAllowed() || columnWindow == null)
            {
                return;
            }

            if (BuildColumnWindow(columnWindow.All).Signature != columnWindow.Signature)
            {
                StateHasChanged();
            }
        }

        async Task FocusVirtualCell(int rowIndex, int columnIndex)
        {
            var window = columnWindow;

            if (window == null || window.All.Count == 0)
            {
                return;
            }

            focusedVirtualColumn = Math.Clamp(columnIndex, 0, window.All.Count - 1);

            if (window.Rendered[focusedVirtualColumn])
            {
                return;
            }

            pendingVirtualCellFocus = true;

            await InvokeAsync(StateHasChanged);
        }

        async Task ApplyPendingVirtualCellFocus()
        {
            if (!pendingVirtualCellFocus || JSRuntime == null)
            {
                return;
            }

            pendingVirtualCellFocus = false;

            try
            {
                await JSRuntime.InvokeAsync<int[]>("Radzen.focusTableRow", UniqueID, "FocusCell", focusedIndex, focusedVirtualColumn, IsVirtualizationAllowed(), columnWindow?.All.Count);
            }
            catch (Exception)
            {
            }
        }
    }
}
