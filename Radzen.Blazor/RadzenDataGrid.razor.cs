﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDataGrid component.
    /// </summary>
    /// <typeparam name="TItem">The type of the DataGrid data item.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenDataGrid @data=@orders TItem="Order" AllowSorting="true" AllowPaging="true" AllowFiltering="true"&gt;
    ///     &lt;Columns&gt;
    ///         &lt;RadzenDataGridColumn TItem="Order" Property="OrderId" Title="OrderId" /&gt;
    ///         &lt;RadzenDataGridColumn TItem="Order" Property="OrderDate" Title="OrderDate" /&gt;
    ///     &lt;/Columns&gt;
    /// &lt;/RadzenDataGrid&gt;
    /// </code>
    /// </example>
#if NET6_0_OR_GREATER
    [CascadingTypeParameter(nameof(TItem))]
#endif
    public partial class RadzenDataGrid<TItem> : PagedDataBoundComponent<TItem>
    {
        /// <summary>
        /// Returns the validity of the DataGrid.
        /// </summary>
        /// <value><c>true</c> if all validators in the DataGrid a valid; otherwise, <c>false</c>.</value>
        public bool IsValid
        {
            get
            {
                if (!editContexts.Any())
                {
                    return true;
                }

                return editContexts.All(c => !c.Value.GetValidationMessages().Any());
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is virtualized.
        /// </summary>
        /// <value><c>true</c> if this instance is virtualized; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowVirtualization { get; set; }

        /// <summary>
        /// Gets or sets a value that determines how many additional items will be rendered before and after the visible region. This help to reduce the frequency of rendering during scrolling. However, higher values mean that more elements will be present in the page.
        /// </summary>
        [Parameter]
        public int VirtualizationOverscanCount { get; set; }

        internal void SetAllowVirtualization(bool allowVirtualization)
        {
            AllowVirtualization = allowVirtualization;
        }

        internal Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem> virtualize;
        internal Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<GroupResult> groupVirtualize;

        /// <summary>
        /// Gets Virtualize component reference.
        /// </summary>
        public Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem> Virtualize
        {
            get
            {
                return virtualize;
            }
        }

        List<TItem> virtualDataItems = new List<TItem>();

        /// <summary>
        /// Clears the cache and refreshes the Virtualize component.
        /// </summary>
        public async Task RefreshDataAsync()
        {
            ResetLoadData();

            if (Virtualize != null)
            {
                await Virtualize.RefreshDataAsync();
            }
            else
            {
                await ReloadInternal();
            }
        }

        /// <summary>
        /// Reset the LoadData internal state
        /// </summary>
        public void ResetLoadData()
        {
            lastLoadDataArgs = null;
        }

        string lastLoadDataArgs;
        Task lastLoadDataTask = Task.CompletedTask;
        private async ValueTask<Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<TItem>> LoadItems(Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderRequest request)
        {
            var view = AllowPaging ? PagedView : View;
            var top = request.Count;

            if(top <= 0)
            {
                top = PageSize;
            }

            var filter = isOData == true ?
                    allColumns.ToList().ToODataFilterString<TItem>() : allColumns.ToList().ToFilterString<TItem>();
            var loadDataArgs = $"{request.StartIndex}|{top}{GetOrderBy()}{filter}";

            if (lastLoadDataArgs != loadDataArgs)
            {
                await (lastLoadDataTask = InvokeLoadData(request.StartIndex, top));

                lastLoadDataArgs = loadDataArgs;
            }

            var totalItemsCount = LoadData.HasDelegate ? Count : view.Count();

            virtualDataItems = (LoadData.HasDelegate ? Data : itemsToInsert.Count() > 0 ? itemsToInsert.ToList().Concat(view.Skip(request.StartIndex).Take(top)) : view.Skip(request.StartIndex).Take(top))?.ToList();

            return new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<TItem>(virtualDataItems, totalItemsCount);
        }

        private async ValueTask<Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<GroupResult>> LoadGroups(Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderRequest request)
        {
            var top = request.Count;

            if(top <= 0)
            {
                top = PageSize;
            }

            var view = AllowPaging ? PagedView : View;

            var query = Enumerable.Empty<TItem>().AsQueryable();
            var totalItemsCount = 0;
            _groupedPagedView = Enumerable.Empty<GroupResult>();

            if (Groups.Any())
            {
                query = view.AsQueryable().OrderBy(Groups.Any() ? string.Join(',', Groups.Select(g => g.Property)) : null);
                _groupedPagedView = await Task.FromResult(query.GroupByMany(Groups.Any() ? Groups.Select(g => g.Property).ToArray() : new string[] { "it" }).ToList());

                totalItemsCount = await Task.FromResult(_groupedPagedView.Count());
            }

            _view = view;

            return new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<GroupResult>(_groupedPagedView.Any() ? _groupedPagedView.Skip(request.StartIndex).Take(top) : _groupedPagedView, totalItemsCount);
        }

        RenderFragment DrawRows(IList<RadzenDataGridColumn<TItem>> visibleColumns)
        {
            return new RenderFragment(builder =>
            {
                if (AllowVirtualization)
                {
                    if(AllowGrouping && Groups.Any() && !LoadData.HasDelegate)
                    {
                        builder.OpenComponent(0, typeof(Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<GroupResult>));
                        builder.AddAttribute(1, "ItemsProvider", new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderDelegate<GroupResult>(LoadGroups));

                        builder.AddAttribute(2, "ChildContent", (RenderFragment<GroupResult>)((context) =>
                        {
                            return (RenderFragment)((b) =>
                            {
                                b.OpenComponent(3, typeof(RadzenDataGridGroupRow<TItem>));
                                b.AddAttribute(4, "Columns", visibleColumns);
                                b.AddAttribute(5, "Grid", this);
                                b.AddAttribute(6, "GroupResult", context);
                                b.AddAttribute(7, "Builder", b);
                                b.SetKey(context);
                                b.CloseComponent();
                            });
                        }));

                        builder.AddComponentReferenceCapture(8, c => { groupVirtualize = (Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<GroupResult>)c; });
                    }
                    else
                    {
                        builder.OpenComponent(0, typeof(Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem>));
                        builder.AddAttribute(1, "ItemsProvider", new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderDelegate<TItem>(LoadItems));

                        builder.AddAttribute(2, "ChildContent", (RenderFragment<TItem>)((context) =>
                        {
                            return (RenderFragment)((b) =>
                            {
                                b.OpenComponent<RadzenDataGridRow<TItem>>(3);
                                b.AddAttribute(4, "Columns", visibleColumns);
                                b.AddAttribute(5, "Grid", this);
                                b.AddAttribute(6, "TItem", typeof(TItem));
                                b.AddAttribute(7, "Item", context);
                                b.AddAttribute(8, "InEditMode", IsRowInEditMode(context));
                                b.AddAttribute(9, "Index", virtualDataItems.IndexOf(context));

                                if (editContexts.Keys.Any(i => ItemEquals(i, context)))
                                {
                                    b.AddAttribute(10, nameof(RadzenDataGridRow<TItem>.EditContext), editContexts[context]);
                                }

                                b.SetKey(context);
                                b.CloseComponent();
                            });
                        }));

                        if(VirtualizationOverscanCount != default(int))
                        {
                            builder.AddAttribute(1, "OverscanCount", VirtualizationOverscanCount);
                        }

                        builder.AddComponentReferenceCapture(8, c => { virtualize = (Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem>)c; });

                    }

                    builder.CloseComponent();
                }
                else
                {
                    DrawGroupOrDataRows(builder, visibleColumns);
                }
            });
        }

        internal void DrawGroupOrDataRows(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, IList<RadzenDataGridColumn<TItem>> visibleColumns)
        {
            if (Groups.Any())
            {
                foreach (var group in GroupedPagedView)
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridGroupRow<TItem>));
                    builder.AddAttribute(1, "Columns", visibleColumns);
                    builder.AddAttribute(3, "Grid", this);
                    builder.AddAttribute(5, "GroupResult", group);
                    builder.AddAttribute(6, "Builder", builder);
                    builder.CloseComponent();
                }
            }
            else
            {
                int i = 0;
                foreach (var item in PagedView)
                {
                    builder.OpenComponent<RadzenDataGridRow<TItem>>(0);
                    builder.AddAttribute(1, "Columns", visibleColumns);
                    builder.AddAttribute(2, "Index", i);
                    builder.AddAttribute(3, "Grid", this);
                    builder.AddAttribute(4, "TItem", typeof(TItem));
                    builder.AddAttribute(5, "Item", item);
                    builder.AddAttribute(6, "InEditMode", IsRowInEditMode(item));

                    if (editContexts.ContainsKey(item))
                    {
                        builder.AddAttribute(7, nameof(RadzenDataGridRow<TItem>.EditContext), editContexts[item]);
                    }

                    builder.SetKey(item);

                    builder.CloseComponent();
                    i++;
                }
            }
        }


        /// <summary>
        /// Gets or sets the callback used to load column filter data for DataGrid FilterMode.CheckBoxList filter mode.
        /// </summary>
        /// <value>The load filter data event callback.</value>
        [Parameter]
        public EventCallback<DataGridLoadColumnFilterDataEventArgs<TItem>> LoadColumnFilterData { get; set; }

        /// <summary>
        /// Gets or sets the load child data callback.
        /// </summary>
        /// <value>The load child data callback.</value>
        [Parameter]
        public EventCallback<Radzen.DataGridLoadChildDataEventArgs<TItem>> LoadChildData { get; set; }

        /// <summary>
        /// Gets or sets the expand child item aria label text.
        /// </summary>
        /// <value>The expand child item aria label text.</value>
        [Parameter]
        public string ExpandChildItemAriaLabel { get; set; } = "Expand child item";

        /// <summary>
        /// Gets or sets the expand group aria label text.
        /// </summary>
        /// <value>The expand group aria label text.</value>
        [Parameter]
        public string ExpandGroupAriaLabel { get; set; } = "Expand group";

        /// <summary>
        /// Gets or sets the date simple filter toggle aria label text.
        /// </summary>
        /// <value>The date simple filter toggle aria label text.</value>
        [Parameter]
        public string FilterToggleAriaLabel { get; set; } = "Toggle";

        /// <summary>
        /// Gets or sets a value indicating whether DataGrid data cells will follow the header cells structure in composite columns.
        /// </summary>
        /// <value><c>true</c> if DataGrid data cells will follow the header cells structure in composite columns; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowCompositeDataCells { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether DataGrid data body show empty message.
        /// </summary>
        [Parameter]
        public bool ShowEmptyMessage { get; set; } = true;

        /// <summary>
        /// Gets or sets value if headers are shown.
        /// </summary>
        /// <value>If headers are shown value.</value>
        [Parameter]
        public bool ShowHeader { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether DataGrid is responsive.
        /// </summary>
        /// <value><c>true</c> if DataGrid is Responsive; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Responsive { get; set; }

        /// <summary>
        /// Allows to define a custom function for enums DisplayAttribute Description property value translation in datagrid
        /// Enum filters.
        /// </summary>
        [Parameter]
        public Func<string, string> EnumFilterTranslationFunc { get; set; }

        /// <summary>
        /// The grouped and paged View
        /// </summary>
        IEnumerable<GroupResult> _groupedPagedView;
        /// <summary>
        /// Gets the view grouped and paged.
        /// </summary>
        /// <value>The grouped paged view.</value>
        public IEnumerable<GroupResult> GroupedPagedView
        {
            get
            {
                if (_groupedPagedView == null)
                {
                    var orderBy = GetOrderBy();
                    var query = Groups.Count(g => g.SortOrder == null) == Groups.Count || !string.IsNullOrEmpty(orderBy) ? View : View.OrderBy(string.Join(',', Groups.Select(g => $"{g.Property} {(g.SortOrder == null ? "" : g.SortOrder == SortOrder.Ascending ? " asc" : " desc")}")));
                    var v = (AllowPaging && !LoadData.HasDelegate ? query.Skip(skip).Take(PageSize) : query).ToList().AsQueryable();
                    _groupedPagedView = v.GroupByMany(Groups.Select(g => g.Property).ToArray()).ToList();
                }
                return _groupedPagedView;
            }
        }

        internal async Task RemoveGroupAsync(GroupDescriptor gd)
        {
            Groups.Remove(gd);
            _groupedPagedView = null;

            var column = columns.Where(c => c.GetGroupProperty() == gd.Property).FirstOrDefault();
            if (column != null)
            {
                await Group.InvokeAsync(new DataGridColumnGroupEventArgs<TItem>() { Column = column, GroupDescriptor = null });
            }

            if (IsVirtualizationAllowed())
            {
                StateHasChanged();
                await InvokeAsync(Reload);
            }
        }

        /// <summary>
        /// Gets or sets the column group callback.
        /// </summary>
        /// <value>The column group callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnGroupEventArgs<TItem>> Group { get; set; }

        internal string getFrozenColumnClass(RadzenDataGridColumn<TItem> column, IList<RadzenDataGridColumn<TItem>> visibleColumns)
        {
            if (!column.IsFrozen())
            {
                return "";
            }

            // Frozen columns are grouped to:
            // - left frozen columns: all of the frozen columns marked by FrozenColumnPosition.Left from left till the first not frozen column
            // - left inner frozen columns: any frozen column marked by FrozenColumnPosition.Left which are not "left frozen columns"
            // - right frozen columns: all of the frozen columns marked by FrozenColumnPosition.Right from right till the first not frozen column
            // - right inner frozen columns: any frozen column marked by FrozenColumnPosition.Right which are not "right frozen columns"

            // According to https://github.com/w3c/csswg-drafts/issues/1656 stucked columns cannot be detected by pseudo classes, so the stucked state had to be managed somehow.

            // A good solution for the problem, might be:
            // https://stackoverflow.com/questions/25308823/targeting-positionsticky-elements-that-are-currently-in-a-stuck-state
            // https://codepen.io/TomAnthony/pen/qBqgErK
            // It seemed too complicated, so left + right frozen columns problme has been solved by following css classes:
            // - rz-frozen-cell-left            all of the "left frozen columns" get this class
            // - rz-frozen-cell-left-end        the most right column of the "left frozen columns" get this class to draw the shadow for it
            // - rz-frozen-cell-left-inner      all of the "left inner frozen columns" get this class
            // - rz-frozen-cell-right           all of the "right frozen columns" get this class
            // - rz-frozen-cell-right-end       the most left column of the "right frozen columns" get this class to draw the shadow for it
            // - rz-frozen-cell-right-inner     all of the "right inner frozen columns" get this class

            if (column.FrozenPosition == FrozenColumnPosition.Left)
            {
                for(var i=0; i<ColumnsCollection.Count; i++)
                {
                    if (ColumnsCollection[i] == column)
                    {
                        if (i + 1 < ColumnsCollection.Count && (!ColumnsCollection[i + 1].IsFrozen() || ColumnsCollection[i + 1].FrozenPosition == FrozenColumnPosition.Right))
                        {
                            return "rz-frozen-cell rz-frozen-cell-left rz-frozen-cell-left-end";
                        }
                        else
                        {
                            return "rz-frozen-cell rz-frozen-cell-left";
                        }
                    }
                    if(!ColumnsCollection[i].IsFrozen())
                    {
                        break;
                    }
                }
                return "rz-frozen-cell rz-frozen-cell-left-inner";
            }

            for (var i = ColumnsCollection.Count-1; i >=0; i--)
            {
                if (ColumnsCollection[i] == column)
                {
                    if (i - 1 >=0 && (!ColumnsCollection[i-1].IsFrozen() || ColumnsCollection[i-1].FrozenPosition == FrozenColumnPosition.Left))
                    {
                        return "rz-frozen-cell rz-frozen-cell-right rz-frozen-cell-right-end";
                    }
                    else
                    {
                        return "rz-frozen-cell rz-frozen-cell-right";
                    }
                }
                if (!ColumnsCollection[i].IsFrozen())
                {
                    break;
                }
            }
            return "rz-frozen-cell rz-frozen-cell-right-inner";
        }

        internal string getColumnAlignClass(RadzenDataGridColumn<TItem> column)
        {
            return $"rz-text-align-{column.TextAlign.ToString().ToLower()}";
        }

        /// <summary>
        /// The filter operator style for dates.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        protected string DateFilterOperatorStyle(RadzenDataGridColumn<TItem> column, FilterOperator value)
        {
            return column.GetFilterOperator() == value ?
                "rz-listbox-item  rz-state-highlight" :
                "rz-listbox-item ";
        }

        bool preventKeyDown = true;
        int focusedIndex = -1;
        int focusedCellIndex = 0;

        internal string GridId()
        {
            return GetId();
        }

        async Task FocusRow(string key)
        {
            try
            {
                var result = await JSRuntime.InvokeAsync<int[]>("Radzen.focusTableRow", UniqueID, key, focusedIndex, focusedCellIndex, IsVirtualizationAllowed());
                focusedIndex = result[0];
                focusedCellIndex = result[1];
            }
            catch (Exception)
            {
                //
            }
        }

        /// <summary>
        /// Gets or sets key down callback.
        /// </summary>
        /// <value>The key down callback.</value>
        [Parameter]
        public EventCallback<KeyboardEventArgs> KeyDown { get; set; }

        /// <summary>
        /// Handles the <see cref="E:KeyDown" /> event.
        /// </summary>
        /// <param name="args">The <see cref="KeyboardEventArgs"/> instance containing the event data.</param>
        protected virtual async Task OnKeyDown(KeyboardEventArgs args)
        {
            await KeyDown.InvokeAsync(args);

            var key = args.Code != null ? args.Code : args.Key;

            if (key == "ArrowDown" || key == "ArrowUp" || key == "ArrowLeft" || key == "ArrowRight")
            {
                preventKeyDown = true;

                if (focusedIndex == 0 && AllowFiltering && FilterMode == FilterMode.Advanced && key == "ArrowDown" && args.AltKey)
                {
                    var column = ColumnsCollection.Where(c => c.GetVisible()).ElementAtOrDefault(focusedCellIndex);
                    if (column != null && column.headerCell != null && column.Filterable)
                    {
                        await column.headerCell.OpenFilter();
                    }
                    return;
                }

                await FocusRow(key);
            }
            else if (IsVirtualizationAllowed() && (key == "PageUp" || key == "PageDown" || key == "Home" || key == "End"))
            {
                preventKeyDown = true;

                await FocusRow(key);
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyDown = true;

                if (focusedIndex == 0)
                {
                    var column = ColumnsCollection.Where(c => c.GetVisible()).ElementAtOrDefault(focusedCellIndex);
                    if (column != null)
                    {
                        await OnSort(args, column);
                    }
                }
                else
                {
                    var itemToSelect = PagedView.ElementAtOrDefault(focusedIndex - 1);
                    if (itemToSelect != null)
                    {
                        if (SelectionMode == DataGridSelectionMode.Multiple && !args.ShiftKey)
                        {
                            selectedItems.Clear();
                        }

                        await SelectRow(itemToSelect, true);
                    }
                }
            }
            else
            {
                preventKeyDown = false;
            }
        }

        /// <summary>
        /// Called when filter key pressed.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <param name="column">The column.</param>
        protected virtual void OnFilterKeyPress(EventArgs args, RadzenDataGridColumn<TItem> column)
        {
            Debounce(() => DebounceFilter(column), FilterDelay);
        }

        async Task DebounceFilter(RadzenDataGridColumn<TItem> column)
        {
            var inputValue = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", getFilterInputId(column));
            if (!object.Equals(column.GetFilterValue(), inputValue))
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                await InvokeAsync(() => { OnFilter(new ChangeEventArgs() { Value = inputValue }, column); });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        /// <summary>
        /// Applies the date filter by filter operator.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="filterOperator">The filter operator.</param>
        protected void ApplyDateFilterByFilterOperator(RadzenDataGridColumn<TItem> column, FilterOperator filterOperator)
        {
            column.SetFilterOperator(filterOperator);
            SaveSettings();
        }

        internal IJSRuntime GetJSRuntime()
        {
            return JSRuntime;
        }

        private List<RadzenDataGridColumn<TItem>> columns = new List<RadzenDataGridColumn<TItem>>();
        internal readonly List<RadzenDataGridColumn<TItem>> childColumns = new List<RadzenDataGridColumn<TItem>>();
        internal List<RadzenDataGridColumn<TItem>> allColumns = new List<RadzenDataGridColumn<TItem>>();
        private List<RadzenDataGridColumn<TItem>> allPickableColumns = new List<RadzenDataGridColumn<TItem>>();

        /// <summary>
        /// Gives the grid a custom header, allowing the adding of components to create custom tool bars in addtion to column grouping and column picker
        /// </summary>
        [Parameter]
        public RenderFragment HeaderTemplate { get; set; }

        /// <summary>
        /// Gives the grid a custom footer, allowing the adding of components to create custom tool bars or custom pagination
        /// </summary>
        [Parameter]
        public RenderFragment FooterTemplate { get; set; }

        internal object selectedColumns;

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [Parameter]
        public RenderFragment Columns { get; set; }

        internal void UpdateColumnsOrder()
        {
            if (allColumns.Any(c => c.GetOrderIndex().HasValue))
            {
                var columnsWithoutOrderIndex = columns.Where(c => !c.GetOrderIndex().HasValue).ToList();
                for (var i = 0; i < columnsWithoutOrderIndex.Count; i++)
                {
                    columnsWithoutOrderIndex[i].SetOrderIndex(columns.IndexOf(columnsWithoutOrderIndex[i]));
                }

                columns = columns.OrderBy(c => c.GetOrderIndex()).ToList();

                if (AllowColumnPicking)
                {
                    allPickableColumns = allColumns.Where(c => c.Pickable).OrderBy(c => c.GetOrderIndex()).ToList();
                }
            }
        }

        internal void AddColumn(RadzenDataGridColumn<TItem> column)
        {
            if (!columns.Contains(column) && column.Parent == null)
            {
                columns.Add(column);
            }

            if (!childColumns.Contains(column) && column.Parent != null)
            {
                childColumns.Add(column);

                var level = column.GetLevel();
                if (level > deepestChildColumnLevel)
                {
                    deepestChildColumnLevel = level;
                }
            }

            var descriptor = sorts.Where(d => d.Property == column?.GetSortProperty()).FirstOrDefault();
            if (descriptor == null && column.SortOrder.HasValue)
            {
                descriptor = new SortDescriptor() { Property = column.GetSortProperty(), SortOrder = column.SortOrder.Value };
                sorts.Add(descriptor);
            }

            if (!allColumns.Contains(column))
            {
                allColumns.Add(column);
            }

            if (AllowColumnPicking)
            {
                selectedColumns = allColumns.Where(c => c.Pickable && c.GetVisible()).ToList();
                allPickableColumns = allColumns.Where(c => c.Pickable).ToList();
            }

            UpdateColumnsOrder();

            StateHasChanged();
        }

        internal void UpdatePickableColumn(RadzenDataGridColumn<TItem> column, bool visible)
        {
            if (selectedColumns == null)
                return;

            var columnsList = ((IEnumerable<object>)selectedColumns).ToList();
            if (visible)
            {
                if (!columnsList.Contains(column))
                {
                    columnsList.Add(column);
                }
            }
            else
            {
                if (columnsList.Contains(column))
                {
                    columnsList.Remove(column);
                }
            }

            selectedColumns = columnsList;
        }

        /// <summary>
        /// Updates pickable columns.
        /// </summary>
        public void UpdatePickableColumns()
		{
			if (allColumns.Any(c => c.Pickable))
			{
				if (AllowColumnPicking)
				{
					allPickableColumns = allColumns.Where(c => c.Pickable).OrderBy(c => c.GetOrderIndex()).ToList();
				}
			}
		}

		internal void RemoveColumn(RadzenDataGridColumn<TItem> column)
        {
            if (columns.Contains(column))
            {
                columns.Remove(column);
            }

            if (childColumns.Contains(column))
            {
                childColumns.Remove(column);
            }

            if (allColumns.Contains(column))
            {
                allColumns.Remove(column);
            }

            UpdateColumnsOrder();

            if (!disposed)
            {
                try { InvokeAsync(StateHasChanged); } catch { }
            }
        }

        void ToggleColumns()
        {
            var selected = ((IEnumerable<object>)selectedColumns).Cast<RadzenDataGridColumn<TItem>>();

            foreach (var c in allPickableColumns)
            {
                c.SetVisible(selected.Contains(c));
            }

            PickedColumnsChanged.InvokeAsync(new DataGridPickedColumnsChangedEventArgs<TItem>() { Columns = selected });
            SaveSettings();
        }

        /// <summary>
        /// Gets or sets the picked columns changed callback.
        /// </summary>
        /// <value>The picked columns changed callback.</value>
        [Parameter]
        public EventCallback<DataGridPickedColumnsChangedEventArgs<TItem>> PickedColumnsChanged { get; set; }

        internal string getFilterInputId(RadzenDataGridColumn<TItem> column)
        {
            return string.Join("", $"{UniqueID}".Split('.')) + column.GetFilterProperty();
        }

        internal string getFilterDateFormat(RadzenDataGridColumn<TItem> column)
        {
            if (column != null && !string.IsNullOrEmpty(column.FormatString))
            {
                return column.FormatString.Replace("{0:", "").Replace("}", "");
            }

            return FilterDateFormat;
        }

        internal RenderFragment DrawNumericFilter(RadzenDataGridColumn<TItem> column, bool force = true, bool isFirst = true)
        {
            return new RenderFragment(builder =>
            {
                var type = Nullable.GetUnderlyingType(column.FilterPropertyType) != null ?
                    column.FilterPropertyType : typeof(Nullable<>).MakeGenericType(column.FilterPropertyType);

                var numericType = typeof(RadzenNumeric<>).MakeGenericType(type);

                builder.OpenComponent(0, numericType);

                builder.AddAttribute(1, "Value", isFirst ? column.GetFilterValue() : column.GetSecondFilterValue());
                builder.AddAttribute(2, "ShowUpDown", column.ShowUpDownForNumericFilter());
                builder.AddAttribute(3, "Style", "width:100%");
                builder.AddAttribute(4, "InputAttributes", new Dictionary<string,object>(){ { "aria-label", column.Title + $"{(!isFirst ? " second " : " ")}filter value " + (isFirst ? column.GetFilterValue() : column.GetSecondFilterValue()) } });
                builder.AddAttribute(5, "id", getFilterInputId(column) + (isFirst ? "f" : "s"));

                Action<object> action;
                if (force)
                {
                    action = args => InvokeAsync(() => OnFilter(new ChangeEventArgs() { Value = args }, column, isFirst));
                }
                else
                {
                    action = args => { column.SetFilterValue(args, isFirst); SaveSettings(); };
                }

                var eventCallbackGenericCreate = typeof(NumericFilterEventCallback).GetMethod("Create").MakeGenericMethod(type);
                var eventCallbackGenericAction = typeof(NumericFilterEventCallback).GetMethod("Action").MakeGenericMethod(type);

                builder.AddAttribute(3, "Change", eventCallbackGenericCreate.Invoke(this,
                    new object[] { this, eventCallbackGenericAction.Invoke(this, new object[] { action }) }));

                builder.AddAttribute(4, "Disabled", !column.CanSetFilterValue());

                builder.CloseComponent();
            });
        }

        /// <summary>
        /// Called when filter.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        /// <param name="column">The column.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="isFirst">if set to <c>true</c> [is first].</param>
        protected virtual async Task OnFilter(ChangeEventArgs args, RadzenDataGridColumn<TItem> column, bool force = false, bool isFirst = true)
        {
            string property = column.GetFilterProperty();
            if (AllowFiltering && column.Filterable)
            {
                if (!object.Equals(isFirst ? column.GetFilterValue() : column.GetSecondFilterValue(), args.Value) || force)
                {
                    column.SetFilterValue(args.Value, isFirst);
                    skip = 0;
                    CurrentPage = 0;

                    await Filter.InvokeAsync(new DataGridColumnFilterEventArgs<TItem>()
                    {
                        Column = column,
                        FilterValue = column.GetFilterValue(),
                        SecondFilterValue = column.GetSecondFilterValue(),
                        FilterOperator = column.GetFilterOperator(),
                        SecondFilterOperator = column.GetSecondFilterOperator(),
                        LogicalFilterOperator = column.GetLogicalFilterOperator()
                    });

                    if (FilterMode == FilterMode.CheckBoxList)
                    {
                        allColumns.ToList().ForEach(c =>
                        {
                            c.ClearFilterValues();
                        });
                    }

                    SaveSettings();

                    if (LoadData.HasDelegate && IsVirtualizationAllowed())
                    {
                        isOData = Data != null && typeof(ODataEnumerable<TItem>).IsAssignableFrom(Data.GetType());
                        Data = null;
                        ResetLoadData();
                    }

                    await InvokeAsync(ReloadInternal);

                    if (IsVirtualizationAllowed())
                    {
                        StateHasChanged();
                    }
                }
            }
        }


        /// <summary>
        /// Gets the columns collection.
        /// </summary>
        /// <value>The columns collection.</value>
        public IList<RadzenDataGridColumn<TItem>> ColumnsCollection
        {
            get
            {
                return columns;
            }
        }

        /// <summary>
        /// Gets or sets the column sort callback.
        /// </summary>
        /// <value>The column sort callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnSortEventArgs<TItem>> Sort { get; set; }

        internal async Task OnSort(EventArgs args, RadzenDataGridColumn<TItem> column)
        {
            if (AllowSorting && column.Sortable)
            {
                if (GotoFirstPageOnSort)
                {
                    topPager?.FirstPage();
                    bottomPager?.FirstPage();
                    CurrentPage = 0;
                }

                var property = column.GetSortProperty();

                SetColumnSortOrder(column);
                await Sort.InvokeAsync(new DataGridColumnSortEventArgs<TItem>() { Column = column, SortOrder = column.GetSortOrder() });
                SaveSettings();

                if (LoadData.HasDelegate && IsVirtualizationAllowed())
                {
                    Data = null;
                    ResetLoadData();
                }

                await InvokeAsync(ReloadInternal);

                if (IsVirtualizationAllowed())
                {
                    StateHasChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the column filter callback.
        /// </summary>
        /// <value>The column filter callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnFilterEventArgs<TItem>> Filter { get; set; }

        /// <summary>
        /// Gets or sets the column filter cleared callback.
        /// </summary>
        /// <value>The column filter callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnFilterEventArgs<TItem>> FilterCleared { get; set; }

        /// <summary>
        /// Gets or sets the render mode.
        /// </summary>
        /// <value>The render mode.</value>
        [Parameter]
        public PopupRenderMode FilterPopupRenderMode { get; set; } = PopupRenderMode.Initial;

        /// <summary>
        /// Сlear filter on the specified column
        /// </summary>
        public async Task ClearFilter(RadzenDataGridColumn<TItem> column, bool closePopup = false, bool shouldReload = true)
        {
            if (closePopup)
            {
                if (column.headerCell != null)
                {
                    await column.headerCell.CloseFilter();
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.closePopup", $"{PopupID}{column.GetFilterProperty()}");
                }
            }

            column.ClearFilters();

            if (FilterMode == FilterMode.CheckBoxList)
            {
                allColumns.ToList().ForEach(c =>
                {
                    c.ClearFilterValues();
                });
            }

            skip = 0;
            CurrentPage = 0;

            SaveSettings();

            await FilterCleared.InvokeAsync(new DataGridColumnFilterEventArgs<TItem>()
            {
                Column = column,
                FilterValue = column.GetFilterValue(),
                SecondFilterValue = column.GetSecondFilterValue(),
                FilterOperator = column.GetFilterOperator(),
                SecondFilterOperator = column.GetSecondFilterOperator(),
                LogicalFilterOperator = column.GetLogicalFilterOperator()
            });

            if (LoadData.HasDelegate && IsVirtualizationAllowed() && shouldReload)
            {
                Data = null;
                ResetLoadData();
            }

            if (closePopup)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closeAllPopups", $"{PopupID}{column.GetFilterProperty()}");
            }

            if (shouldReload)
            {
                await InvokeAsync(ReloadInternal);
            }
        }

        /// <summary>
        /// Apply filter to the specified column
        /// </summary>
        public async Task ApplyFilter(RadzenDataGridColumn<TItem> column, bool closePopup = false)
        {
            if (closePopup)
            {
                if (column.headerCell != null)
                {
                    await column.headerCell.CloseFilter();
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.closePopup", $"{PopupID}{column.GetFilterProperty()}");
                }
            }
            await OnFilter(new ChangeEventArgs() { Value = column.GetFilterValue() }, column, true);

            if (closePopup)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closeAllPopups", $"{PopupID}{column.GetFilterProperty()}");
            }
        }

        internal IReadOnlyDictionary<string, object> CellAttributes(TItem item, RadzenDataGridColumn<TItem> column)
        {
            var args = new Radzen.DataGridCellRenderEventArgs<TItem>() { Data = item, Column = column };

            if (CellRender != null)
            {
                CellRender(args);
            }

            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(args.Attributes);
        }

        internal IReadOnlyDictionary<string, object> HeaderCellAttributes(RadzenDataGridColumn<TItem> column)
        {
            var args = new Radzen.DataGridCellRenderEventArgs<TItem>() { Column = column };

            if (HeaderCellRender != null)
            {
                HeaderCellRender(args);
            }

            var sortOrder = column.GetSortOrder();
            switch (sortOrder)
            {
                case SortOrder.Ascending:
                    args.Attributes.Add("aria-sort", "ascending");
                    break;
                case SortOrder.Descending:
                    args.Attributes.Add("aria-sort", "descending");
                    break;
            }

            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(args.Attributes);
        }

        internal IReadOnlyDictionary<string, object> FooterCellAttributes(RadzenDataGridColumn<TItem> column)
        {
            var args = new Radzen.DataGridCellRenderEventArgs<TItem>() { Column = column };

            if (FooterCellRender != null)
            {
                FooterCellRender(args);
            }

            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(args.Attributes);
        }

        internal Dictionary<int, int> rowSpans = new Dictionary<int, int>();

        /// <summary>
        /// Gets or sets the logical filter operator.
        /// </summary>
        /// <value>The logical filter operator.</value>
        [Parameter]
        public LogicalFilterOperator LogicalFilterOperator { get; set; } = LogicalFilterOperator.And;

        /// <summary>
        /// Gets or sets the filter mode.
        /// </summary>
        /// <value>The filter mode.</value>
        [Parameter]
        public FilterMode FilterMode { get; set; } = FilterMode.Advanced;

        /// <summary>
        /// Gets or sets the expand mode.
        /// </summary>
        /// <value>The expand mode.</value>
        [Parameter]
        public DataGridExpandMode ExpandMode { get; set; } = DataGridExpandMode.Multiple;

        /// <summary>
        /// Gets or sets whether the expandable indicator column is visible.
        /// </summary>
        /// <value>The expandable indicator column visibility.</value>
        [Parameter]
        public bool ShowExpandColumn { get; set; } = true;

        /// <summary>
        /// Gets or sets the edit mode.
        /// </summary>
        /// <value>The edit mode.</value>
        [Parameter]
        public DataGridEditMode EditMode { get; set; } = DataGridEditMode.Multiple;

        /// <summary>
        /// Gets or set the filter icon to use.
        /// </summary>
        [Parameter]
        public string FilterIcon { get; set; } = "filter_alt";

        /// <summary>
        /// Gets or sets the filter text.
        /// </summary>
        /// <value>The filter text.</value>
        [Parameter]
        public string FilterText { get; set; } = "Filter";

        /// <summary>
        /// Gets or sets the enum filter select text.
        /// </summary>
        /// <value>The enum filter select text.</value>
        [Parameter]
        public string EnumFilterSelectText { get; set; } = "Select...";

        /// <summary>
        /// Gets or sets the nullable enum for null value filter text.
        /// </summary>
        /// <value>The enum filter select text.</value>
        [Parameter]
        public string EnumNullFilterText { get; set; } = "No value";

        /// <summary>
        /// Gets or sets the and operator text.
        /// </summary>
        /// <value>The and operator text.</value>
        [Parameter]
        public string AndOperatorText { get; set; } = "And";

        /// <summary>
        /// Gets or sets the or operator text.
        /// </summary>
        /// <value>The or operator text.</value>
        [Parameter]
        public string OrOperatorText { get; set; } = "Or";

        /// <summary>
        /// Gets or sets the apply filter text.
        /// </summary>
        /// <value>The apply filter text.</value>
        [Parameter]
        public string ApplyFilterText { get; set; } = "Apply";

        /// <summary>
        /// Gets or sets the clear filter text.
        /// </summary>
        /// <value>The clear filter text.</value>
        [Parameter]
        public string ClearFilterText { get; set; } = "Clear";

        /// <summary>
        /// Gets or sets the equals text.
        /// </summary>
        /// <value>The equals text.</value>
        [Parameter]
        public string EqualsText { get; set; } = "Equals";

        /// <summary>
        /// Gets or sets the not equals text.
        /// </summary>
        /// <value>The not equals text.</value>
        [Parameter]
        public string NotEqualsText { get; set; } = "Not equals";

        /// <summary>
        /// Gets or sets the less than text.
        /// </summary>
        /// <value>The less than text.</value>
        [Parameter]
        public string LessThanText { get; set; } = "Less than";

        /// <summary>
        /// Gets or sets the less than or equals text.
        /// </summary>
        /// <value>The less than or equals text.</value>
        [Parameter]
        public string LessThanOrEqualsText { get; set; } = "Less than or equals";

        /// <summary>
        /// Gets or sets the greater than text.
        /// </summary>
        /// <value>The greater than text.</value>
        [Parameter]
        public string GreaterThanText { get; set; } = "Greater than";

        /// <summary>
        /// Gets or sets the greater than or equals text.
        /// </summary>
        /// <value>The greater than or equals text.</value>
        [Parameter]
        public string GreaterThanOrEqualsText { get; set; } = "Greater than or equals";

        /// <summary>
        /// Gets or sets the ends with text.
        /// </summary>
        /// <value>The ends with text.</value>
        [Parameter]
        public string EndsWithText { get; set; } = "Ends with";

        /// <summary>
        /// Gets or sets the contains text.
        /// </summary>
        /// <value>The contains text.</value>
        [Parameter]
        public string ContainsText { get; set; } = "Contains";

        /// <summary>
        /// Gets or sets the does not contain text.
        /// </summary>
        /// <value>The does not contain text.</value>
        [Parameter]
        public string DoesNotContainText { get; set; } = "Does not contain";

        /// <summary>
        /// Gets or sets the in operator text.
        /// </summary>
        /// <value>The in operator text.</value>
        [Parameter]
        public string InText { get; set; } = "In";

        /// <summary>
        /// Gets or sets the not in operator text.
        /// </summary>
        /// <value>The not in operator text.</value>
        [Parameter]
        public string NotInText { get; set; } = "Not in";

        /// <summary>
        /// Gets or sets the starts with text.
        /// </summary>
        /// <value>The starts with text.</value>
        [Parameter]
        public string StartsWithText { get; set; } = "Starts with";

        /// <summary>
        /// Gets or sets the not null text.
        /// </summary>
        /// <value>The not null text.</value>
        [Parameter]
        public string IsNotNullText { get; set; } = "Is not null";

        /// <summary>
        /// Gets or sets the is null text.
        /// </summary>
        /// <value>The null text.</value>
        [Parameter]
        public string IsNullText { get; set; } = "Is null";

        /// <summary>
        /// Gets or sets the is empty text.
        /// </summary>
        /// <value>The empty text.</value>
        [Parameter]
        public string IsEmptyText { get; set; } = "Is empty";

        /// <summary>
        /// Gets or sets the is not empty text.
        /// </summary>
        /// <value>The not empty text.</value>
        [Parameter]
        public string IsNotEmptyText { get; set; } = "Is not empty";

        /// <summary>
        /// Gets or sets the custom filter operator text.
        /// </summary>
        /// <value>The custom filter operator text.</value>
        [Parameter]
        public string CustomText { get; set; } = "Custom";

        internal class NumericFilterEventCallback
        {
            public static EventCallback<T> Create<T>(object receiver, Action<T> action)
            {
                return EventCallback.Factory.Create<T>(receiver, action);
            }

            public static Action<T> Action<T>(Action<object> action)
            {
                return args => action(args);
            }
        }

        /// <summary>
        /// Gets or sets the filter case sensitivity.
        /// </summary>
        /// <value>The filter case sensitivity.</value>
        [Parameter]
        public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.Default;

        /// <summary>
        /// Gets or sets the filter delay.
        /// </summary>
        /// <value>The filter delay.</value>
        [Parameter]
        public int FilterDelay { get; set; } = 500;

        /// <summary>
        /// Gets or sets the filter date format.
        /// </summary>
        /// <value>The filter date format.</value>
        [Parameter]
        public string FilterDateFormat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether input is allowed in filter DatePicker.
        /// </summary>
        /// <value><c>true</c> if input is allowed in filter DatePicker; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowFilterDateInput { get; set; }

        /// <summary>
        /// Gets or sets the width of all columns.
        /// </summary>
        /// <value>The width of the columns.</value>
        [Parameter]
        public string ColumnWidth { get; set; }

        private string _emptyText = "No records to display.";
        /// <summary>
        /// Gets or sets the empty text shown when Data is empty collection.
        /// </summary>
        /// <value>The empty text.</value>
        [Parameter]
        public string EmptyText
        {
            get { return _emptyText; }
            set
            {
                if (value != _emptyText)
                {
                    _emptyText = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the empty template shown when Data is empty collection.
        /// </summary>
        /// <value>The empty template.</value>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }

        /// <summary>
        /// Gets or sets the edit template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<TItem> EditTemplate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance loading indicator is shown.
        /// </summary>
        /// <value><c>true</c> if this instance loading indicator is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool IsLoading { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether sorting is allowed.
        /// </summary>
        /// <value><c>true</c> if sorting is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowSorting { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether multi column sorting is allowed.
        /// </summary>
        /// <value><c>true</c> if multi column sorting is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowMultiColumnSorting { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether multi column sorting index is shown.
        /// </summary>
        /// <value><c>true</c> if multi column sorting index is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowMultiColumnSortingIndex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether filtering is allowed.
        /// </summary>
        /// <value><c>true</c> if filtering is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowFiltering { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether column resizing is allowed.
        /// </summary>
        /// <value><c>true</c> if column resizing is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowColumnResize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether column reorder is allowed.
        /// </summary>
        /// <value><c>true</c> if column reorder is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowColumnReorder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether column picking is allowed.
        /// </summary>
        /// <value><c>true</c> if column picking is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowColumnPicking { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether cell data should be shown as tooltip.
        /// </summary>
        /// <value><c>true</c> if cell data is shown as tooltip; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowCellDataAsTooltip { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether column title should be shown as tooltip.
        /// </summary>
        /// <value><c>true</c> if column title is shown as tooltip; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowColumnTitleAsTooltip { get; set; } = true;

        /// <summary>
        /// Gets or sets the column picker columns showing text.
        /// </summary>
        /// <value>The column picker columns showing text.</value>
        [Parameter]
        public string ColumnsShowingText { get; set; } = "columns showing";

        /// <summary>
        /// Gets or sets the column picker max selected labels.
        /// </summary>
        /// <value>The column picker max selected labels.</value>
        [Parameter]
        public int ColumnsPickerMaxSelectedLabels { get; set; } = 2;

        /// <summary>
        /// Gets or sets the column picker all columns text.
        /// </summary>
        /// <value>The column picker all columns text.</value>
        [Parameter]
        public string AllColumnsText { get; set; } = "All";

        /// <summary>
        /// Gets or sets the column picker columns text.
        /// </summary>
        /// <value>The column picker columns text.</value>
        [Parameter]
        public string ColumnsText { get; set; } = "Columns";

        /// <summary>
        /// Gets or sets the remove group button aria label text.
        /// </summary>
        /// <value>The remove group button aria label text.</value>
        [Parameter]
        public string RemoveGroupAriaLabel { get; set; } = "Remove group";

        /// <summary>
        /// Gets or sets the select visible columns aria label text.
        /// </summary>
        /// <value>The select visible columns aria label text.</value>
        [Parameter]
        public string SelectVisibleColumnsAriaLabel { get; set; } = "select visible columns";

        /// <summary>
        /// Gets or sets the column logical filter value aria label text.
        /// </summary>
        /// <value>The the column logical filter value aria label text.</value>
        [Parameter]
        public string LogicalOperatorAriaLabel { get; set; } = " logical filter operator ";

        /// <summary>
        /// Gets or sets the column filter value aria label text.
        /// </summary>
        /// <value>The the column filter value aria label text.</value>
        [Parameter]
        public string FilterOperatorAriaLabel { get; set; } = " filter operator ";

        /// <summary>
        /// Gets or sets the column filter value aria label text.
        /// </summary>
        /// <value>The the column filter value aria label text.</value>
        [Parameter]
        public string SecondFilterOperatorAriaLabel { get; set; } = " second filter operator ";

        /// <summary>
        /// Gets or sets the column filter value aria label text.
        /// </summary>
        /// <value>The the column filter value aria label text.</value>
        [Parameter]
        public string FilterValueAriaLabel { get; set; } = " filter value ";

        /// <summary>
        /// Gets or sets the column filter value aria label text.
        /// </summary>
        /// <value>The the column filter value aria label text.</value>
        [Parameter]
        public string SecondFilterValueAriaLabel { get; set; } = " second filter value ";

        /// <summary>
        /// Gets or sets a value indicating whether user can pick all columns in column picker.
        /// </summary>
        /// <value><c>true</c> if pick of all columns is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowPickAllColumns { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether user can filter columns in column picker.
        /// </summary>
        /// <value><c>true</c> if user can filter columns in column picker; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ColumnsPickerAllowFiltering { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether grouping is allowed.
        /// </summary>
        /// <value><c>true</c> if grouping is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowGrouping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether grouped column should be hidden.
        /// </summary>
        /// <value><c>true</c> if grouped columns should be hidden; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool HideGroupedColumn { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether group footers are visible even when the group is collapsed.
        /// </summary>
        /// <value><c>true</c> if group footers are visible when the group is collapsed otherwise, <c>false</c>.</value>
        [Parameter]
        public bool GroupFootersAlwaysVisible { get; set; }

        /// <summary>
        /// Gets or sets the group header template.
        /// </summary>
        /// <value>The group header template.</value>
        [Parameter]
        public RenderFragment<Group> GroupHeaderTemplate { get; set; }

        /// <summary>
        /// Gets or sets the group header with option to add custom toggle visibility button template.
        /// </summary>
        /// <value>The group header template with option to add custom toggle visibility.</value>
        [Parameter]
        public RenderFragment<(Group Group, RadzenDataGridGroupRow<TItem> GroupHeader)> GroupHeaderToggleTemplate { get; set; }

        /// <summary>
        /// Gets or sets the group panel text.
        /// </summary>
        /// <value>The group panel text.</value>
        [Parameter]
        public string GroupPanelText { get; set; } = "Drag a column header here and drop it to group by that column";

        internal string getColumnUniqueId(int columnIndex)
        {
            return string.Join("", $"{UniqueID}".Split('.')) + columnIndex;
        }

        internal async Task StartColumnResize(MouseEventArgs args, int columnIndex)
        {
            await JSRuntime.InvokeVoidAsync("Radzen.startColumnResize", getColumnUniqueId(columnIndex), Reference, columnIndex, args.ClientX);
        }

        internal async Task StopColumnResize(MouseEventArgs args, int columnIndex)
        {
            await JSRuntime.InvokeVoidAsync("Radzen.stopColumnResize", getColumnUniqueId(columnIndex), Reference, columnIndex);
        }

        int? indexOfColumnToReoder;
        string uniqueIDOfColumnToReoder;

        internal async Task StartColumnReorder(MouseEventArgs args, int columnIndex, string uniqueID)
        {
            indexOfColumnToReoder = columnIndex;
            uniqueIDOfColumnToReoder = uniqueID;
            await JSRuntime.InvokeVoidAsync("Radzen.startColumnReorder", getColumnUniqueId(columnIndex));
        }

        internal async Task EndColumnReorder(MouseEventArgs args, int columnIndex)
        {
            if (indexOfColumnToReoder != null && AllowColumnReorder)
            {
                var visibleColumns = columns.Where(c => c.GetVisible()).ToList();
                var columnToReorder = visibleColumns.ElementAtOrDefault(indexOfColumnToReoder.Value);
                var columnToReorderTo = visibleColumns.ElementAtOrDefault(columnIndex);

                if (columnToReorder != null && columnToReorderTo != null)
                {
                    var actualColumnIndexFrom = columns.IndexOf(columnToReorder);
                    var actualColumnIndexTo = columns.IndexOf(columnToReorderTo);

                    var reorderingArgs = new DataGridColumnReorderingEventArgs<TItem>
                    {
                        Column = columnToReorder,
                        ToColumn = columnToReorderTo
                    };

                    await ColumnReordering.InvokeAsync(reorderingArgs);

                    if(reorderingArgs.Cancel)
                    {
                        indexOfColumnToReoder = null;
                        return;
                    }

                    columns.Remove(columnToReorder);
                    columns.Insert(actualColumnIndexTo, columnToReorder);

                    columns.ForEach(c => c.SetOrderIndex(columns.IndexOf(c)));

                    UpdateColumnsOrder();

                    await ColumnReordered.InvokeAsync(new DataGridColumnReorderedEventArgs<TItem>
                    {
                        Column = columnToReorder,
                        OldIndex = actualColumnIndexFrom,
                        NewIndex = actualColumnIndexTo
                    });

                    SaveSettings();

                    StateHasChanged();
                }

                indexOfColumnToReoder = null;
            }
        }

        /// <summary>
        /// Called when column is resized.
        /// </summary>
        /// <param name="columnIndex">Index of the column.</param>
        /// <param name="value">The value.</param>
        [JSInvokable("RadzenGrid.OnColumnResized")]
        public async Task OnColumnResized(int columnIndex, double value)
        {
            var column = columns.Where(c => c.GetVisible()).ToList()[columnIndex];
            column.SetWidth($"{Math.Round(value)}px");
            await ColumnResized.InvokeAsync(new DataGridColumnResizedEventArgs<TItem>
            {
                Column = column,
                Width = value,
            });
            SaveSettings();
        }

        internal string GetOrderBy()
        {
            return string.Join(",", sorts.Select(d => GetSortOrderAsString(d, IsOData())));
        }

        internal string GetSortOrderAsString(SortDescriptor d, bool isOData)
        {
            var property = d.Property;
            if (string.IsNullOrEmpty(property))
                return "";
            var p = isOData ? property.Replace('.', '/') : PropertyAccess.GetProperty(property);
            return $"{p} {(d.SortOrder == Radzen.SortOrder.Ascending ? "asc" : "desc")}";
        }

        /// <summary>
        /// Gets or sets the column resized callback.
        /// </summary>
        /// <value>The column resized callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnResizedEventArgs<TItem>> ColumnResized { get; set; }

        /// <summary>
        /// Gets or sets the column reordering callback.
        /// </summary>
        /// <value>The column reordering callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnReorderingEventArgs<TItem>> ColumnReordering { get; set; }

        /// <summary>
        /// Gets or sets the column reordered callback.
        /// </summary>
        /// <value>The column reordered callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnReorderedEventArgs<TItem>> ColumnReordered { get; set; }

        internal IQueryable<TItem> GetSelfRefView(IQueryable<TItem> view, string orderBy)
        {
            if (!string.IsNullOrEmpty(orderBy))
            {
                if (typeof(TItem) == typeof(object))
                {
                    var firstItem = view.FirstOrDefault();
                    if (firstItem != null)
                    {
                        view = QueryableExtension.Cast(view, firstItem.GetType()).AsQueryable().OrderBy(orderBy).Cast<TItem>();
                    }
                }
                else
                {
                    view = view.OrderBy<TItem>(orderBy);
                }
            }

            var viewList = view.ToList();
            var countWithChildren = viewList.Count + childData.SelectMany(d => d.Value.Data).Count();

            for (int i = 0; i < countWithChildren; i++)
            {
                var item = viewList.ElementAtOrDefault(i);

                if (item != null && childData.ContainsKey(item))
                {
                    var level = 1;
                    var parentChildData = childData[item].ParentChildData;
                    while (parentChildData != null)
                    {
                        parentChildData = parentChildData.ParentChildData;
                        level++;
                    }

                    childData[item].Level = level;

                    var cd = childData[item].Data.AsQueryable();
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        cd = cd.OrderBy<TItem>(orderBy);
                    }

                    viewList.InsertRange(viewList.IndexOf(item) + 1, cd);
                }
            }

            view = viewList.AsQueryable()
                .Where(i => childData.ContainsKey(i) && childData[i].Data.AsQueryable().Where<TItem>(allColumns).Any()
                    || viewList.AsQueryable().Where<TItem>(allColumns).Contains(i));

            return view;
        }

        /// <summary>
        /// Gets the view - Data with sorting, filtering and paging applied.
        /// </summary>
        /// <value>The view.</value>
        public override IQueryable<TItem> View
        {
            get
            {
                var orderBy = GetOrderBy();
                Query.OrderBy = orderBy;

                if (LoadData.HasDelegate)
                {
                    if (childData.Any())
                    {
                        return GetSelfRefView(base.View, orderBy);

                    }
                    else
                    {
                        return base.View;
                    }
                }

                IQueryable<TItem> view;

                if (childData.Any())
                {
                    view = GetSelfRefView(base.View, orderBy);
                }
                else
                {
                    view = base.View.Where<TItem>(allColumns);

                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        if (typeof(TItem) == typeof(object))
                        {
                            var firstItem = view.FirstOrDefault();
                            if (firstItem != null)
                            {
                                view = QueryableExtension.Cast(view, firstItem.GetType());
                                view = view.OrderBy(orderBy).Cast<TItem>();
                            }
                        }
                        else
                        {
                            view = view.OrderBy(orderBy);
                        }
                    }
                }

                if (!IsVirtualizationAllowed() || AllowPaging)
                {
                    var count = view.Count();
                    if (count != Count)
                    {
                        Count = count;

                        if (skip >= Count && Count > PageSize)
                        {
                            skip = Count - PageSize;
                        }

                        if (Count <= PageSize)
                        {
                            skip = 0;
                            CurrentPage = 0;
                        }

                        CalculatePager();

                        StateHasChanged();
                    }
                }

                return view;
            }
        }

        internal bool IsVirtualizationAllowed()
        {
            return AllowVirtualization;
        }

        IList<TItem> _value;

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        /// <value>The selected item.</value>
        [Parameter]
        public IList<TItem> Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        /// <summary>
        /// Gets or sets the value changed callback.
        /// </summary>
        /// <value>The value changed callback.</value>
        [Parameter]
        public EventCallback<IList<TItem>> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the row select callback.
        /// </summary>
        /// <value>The row select callback.</value>
        [Parameter]
        public EventCallback<TItem> RowSelect { get; set; }

        /// <summary>
        /// Gets or sets the row deselect callback.
        /// </summary>
        /// <value>The row deselect callback.</value>
        [Parameter]
        public EventCallback<TItem> RowDeselect { get; set; }

        /// <summary>
        /// Gets or sets the row click callback.
        /// </summary>
        /// <value>The row click callback.</value>
        [Parameter]
        public EventCallback<DataGridRowMouseEventArgs<TItem>> RowClick { get; set; }

        /// <summary>
        /// Gets or sets the row double click callback.
        /// </summary>
        /// <value>The row double click callback.</value>
        [Parameter]
        public EventCallback<DataGridRowMouseEventArgs<TItem>> RowDoubleClick { get; set; }

        /// <summary>
        /// Gets or sets the cell click callback.
        /// </summary>
        /// <value>The cell click callback.</value>
        [Parameter]
        public EventCallback<DataGridCellMouseEventArgs<TItem>> CellClick { get; set; }

        /// <summary>
        /// Gets or sets the cell double click callback.
        /// </summary>
        /// <value>The cell double click callback.</value>
        [Parameter]
        public EventCallback<DataGridCellMouseEventArgs<TItem>> CellDoubleClick { get; set; }

        /// <summary>
        /// Gets or sets the row click callback.
        /// </summary>
        /// <value>The row click callback.</value>
        [Parameter]
        public EventCallback<DataGridCellMouseEventArgs<TItem>> CellContextMenu { get; set; }

        /// <summary>
        /// Gets or sets the row expand callback.
        /// </summary>
        /// <value>The row expand callback.</value>
        [Parameter]
        public EventCallback<TItem> RowExpand { get; set; }

        /// <summary>
        /// Gets or sets the group row expand callback.
        /// </summary>
        /// <value>The group row expand callback.</value>
        [Parameter]
        public EventCallback<Group> GroupRowExpand { get; set; }

        /// <summary>
        /// Gets or sets the row collapse callback.
        /// </summary>
        /// <value>The row collapse callback.</value>
        [Parameter]
        public EventCallback<TItem> RowCollapse { get; set; }

        /// <summary>
        /// Gets or sets the group row collapse callback.
        /// </summary>
        /// <value>The group row collapse callback.</value>
        [Parameter]
        public EventCallback<Group> GroupRowCollapse { get; set; }

        /// <summary>
        /// Gets or sets the row render callback. Use it to set row attributes.
        /// </summary>
        /// <value>The row render callback.</value>
        [Parameter]
        public Action<RowRenderEventArgs<TItem>> RowRender { get; set; }

        /// <summary>
        /// Gets or sets the group row render callback. Use it to set group row attributes.
        /// </summary>
        /// <value>The group row render callback.</value>
        [Parameter]
        public Action<GroupRowRenderEventArgs> GroupRowRender { get; set; }

        /// <summary>
        /// Gets or sets the cell render callback. Use it to set cell attributes.
        /// </summary>
        /// <value>The cell render callback.</value>
        [Parameter]
        public Action<DataGridCellRenderEventArgs<TItem>> CellRender { get; set; }

        /// <summary>
        /// Gets or sets the header cell render callback. Use it to set header cell attributes.
        /// </summary>
        /// <value>The cell render callback.</value>
        [Parameter]
        public Action<DataGridCellRenderEventArgs<TItem>> HeaderCellRender { get; set; }

        /// <summary>
        /// Gets or sets the footer cell render callback. Use it to set footer cell attributes.
        /// </summary>
        /// <value>The cell render callback.</value>
        [Parameter]
        public Action<DataGridCellRenderEventArgs<TItem>> FooterCellRender { get; set; }

        /// <summary>
        /// Gets or sets the render callback.
        /// </summary>
        /// <value>The render callback.</value>
        [Parameter]
        public Action<DataGridRenderEventArgs<TItem>> Render { get; set; }

        /// <summary>
        /// Gets or sets the render async callback.
        /// </summary>
        /// <value>The render async callback.</value>
        [Parameter]
        public Func<DataGridRenderEventArgs<TItem>, Task> RenderAsync { get; set; }

        /// <summary>
        /// Gets or sets the load settings callback.
        /// </summary>
        /// <value>The load settings callback.</value>
        [Parameter]
        public Action<DataGridLoadSettingsEventArgs> LoadSettings { get; set; }

        /// <summary>
        /// Called when data is changed.
        /// </summary>
        protected override void OnDataChanged()
        {
            if (!string.IsNullOrEmpty(KeyProperty) && keyPropertyGetter == null)
            {
                keyPropertyGetter = PropertyAccess.Getter<TItem, object>(KeyProperty);
            }

            Reset(!IsOData() && !LoadData.HasDelegate && !AllowColumnPicking && !AllowColumnReorder && !AllowColumnResize);

            if (!IsOData() && !LoadData.HasDelegate && !Page.HasDelegate)
            {
                skip = 0;
                CurrentPage = 0;
                CalculatePager();
            }
        }

        /// <inheritdoc />
        protected override void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            InvokeAsync(Reload);
        }

        /// <summary>
        /// Resets the DataGrid instance to initial state with no sorting, grouping and/or filtering, column visibility.
        /// </summary>
        /// <param name="resetColumnState">if set to <c>true</c> [reset column state].</param>
        /// <param name="resetRowState">if set to <c>true</c> [reset row state].</param>
        public void Reset(bool resetColumnState = true, bool resetRowState = false)
        {
            _groupedPagedView = null;
            _view = null;
            _value = new List<TItem>();

            if (resetRowState)
            {
                selectedItems.Clear();
                expandedItems.Clear();
                collapsedGroupItems.Clear();
            }

            if (resetColumnState)
            {
                allColumns.ToList().ForEach(c =>
                {
                    c.ClearFilters();
                    c.ResetSortOrder();
                    c.SetOrderIndex(null);
                    c.SetWidth(null);
                    c.SetVisible(null);
                });
                selectedColumns = allColumns.Where(c => c.Pickable && c.GetVisible()).ToList();
                sorts.Clear();
                columns = allColumns.Where(c => c.Parent == null).ToList();
           }
        }

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        public async override Task Reload()
        {
            ResetLoadData();
            await ReloadInternal();
        }

        internal async Task ReloadInternal()
        {
            _groupedPagedView = null;
            _view = null;

            if (Data != null && !LoadData.HasDelegate)
            {
                Count = 1;
            }

            if (AllowVirtualization)
            {
                if (!LoadData.HasDelegate)
                {
                    if (virtualize != null)
                    {
                        await virtualize.RefreshDataAsync();
                    }

                    if (groupVirtualize != null)
                    {
                        await groupVirtualize.RefreshDataAsync();
                    }
                }
                else
                {
                    Data = null;
                }
            }

            if (!IsVirtualizationAllowed())
            {
                await InvokeLoadData(skip, PageSize);
            }

            CalculatePager();

            if (!LoadData.HasDelegate)
            {
                StateHasChanged();
            }
            else
            {
                if (AllowVirtualization)
                {
                    if (virtualize != null)
                    {
                        await virtualize.RefreshDataAsync();
                    }

                    if (groupVirtualize != null)
                    {
                        await groupVirtualize.RefreshDataAsync();
                    }
                }
            }

            if (LoadData.HasDelegate && View.Count() == 0 && Count > 0)
            {
                if (CurrentPage > 1)
                {
                    await GoToPage(CurrentPage - 1);
                }
                else
                {
                    await FirstPage();
                }
            }
        }

        IEnumerable<FilterDescriptor> filters = Enumerable.Empty<FilterDescriptor>();

        internal async Task InvokeLoadData(int start, int top)
        {
            var orderBy = GetOrderBy();

            Query.Skip = skip;
            Query.Top = PageSize;
            Query.OrderBy = orderBy;

            Query.GetFilter = () => allColumns.ToList().ToFilterString<TItem>();

            filters = allColumns.ToList()
                .Where(c => c.Filterable && c.GetVisible() && (c.GetFilterValue() != null
                    || c.GetFilterOperator() == FilterOperator.IsNotNull || c.GetFilterOperator() == FilterOperator.IsNull
                    || c.GetFilterOperator() == FilterOperator.IsEmpty | c.GetFilterOperator() == FilterOperator.IsNotEmpty))
                .Select(c => new FilterDescriptor()
                {
                    Property = !string.IsNullOrEmpty(c.FilterProperty) && c.FilterProperty != c.Property ? c.Property : c.GetFilterProperty(),
                    FilterProperty = !string.IsNullOrEmpty(c.FilterProperty) && c.FilterProperty != c.Property ? c.FilterProperty : null,
                    FilterValue = c.GetFilterValue(),
                    FilterOperator = c.GetFilterOperator(),
                    SecondFilterValue = c.GetSecondFilterValue(),
                    SecondFilterOperator = c.GetSecondFilterOperator(),
                    LogicalFilterOperator = c.GetLogicalFilterOperator()
                })
                .ToList();

            Query.Filters = filters;
            Query.Sorts = sorts;
            if (LoadData.HasDelegate)
            {
                await LoadData.InvokeAsync(new Radzen.LoadDataArgs()
                {
                    Skip = start,
                    Top = top,
                    OrderBy = orderBy,
                    GetFilter = () => IsOData() ? allColumns.ToList().ToODataFilterString<TItem>() : allColumns.ToList().ToFilterString<TItem>(),
                    Filters = filters,
                    Sorts = sorts
                });
            }
        }

        internal async Task ChangeState()
        {
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Called when parameters set asynchronous.
        /// </summary>
        /// <returns>Task.</returns>
        protected override async Task OnParametersSetAsync()
        {
            if (Visible && !LoadData.HasDelegate && _view == null)
            {
                await InvokeAsync(Reload);
            }
            else
            {
                CalculatePager();
            }

            await Task.CompletedTask;
        }

        internal Dictionary<RadzenDataGridGroupRow<TItem>, bool> collapsedGroupItems = new Dictionary<RadzenDataGridGroupRow<TItem>, bool>();

        internal string ExpandedGroupItemStyle(RadzenDataGridGroupRow<TItem> item, bool? expandedOnLoad)
        {
            return collapsedGroupItems.Keys.Contains(item) || expandedOnLoad == false ? "notranslate rz-row-toggler rzi-grid-sort rzi-chevron-circle-right" : "rz-row-toggler rzi-grid-sort rzi-chevron-circle-down";
        }

        internal bool IsGroupItemExpanded(RadzenDataGridGroupRow<TItem> item)
        {
            return !collapsedGroupItems.Keys.Contains(item);
        }

        /// <summary>
        /// Gets or sets a value indicating whether all groups should be expanded when DataGrid is grouped.
        /// </summary>
        /// <value><c>true</c> if groups are expanded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool? AllGroupsExpanded { get; set; }

        /// <summary>
        /// Gets or sets the AllGroupsExpanded changed callback.
        /// </summary>
        /// <value>The AllGroupsExpanded changed callback.</value>
        [Parameter]
        public EventCallback<bool?> AllGroupsExpandedChanged { get; set; }

        /// <summary>
        /// Gets or sets the key property.
        /// </summary>
        /// <value>The key property.</value>
        [Parameter]
        public string KeyProperty { get; set; }

        internal Func<TItem, object> keyPropertyGetter;
        /// <summary>
        /// Compares two items
        /// </summary>
        /// <param name="item">The first item</param>
        /// <param name="otherItem">The second item</param>
        /// <returns>Are items equal</returns>
        protected bool ItemEquals(TItem item, TItem otherItem)
        {
            return keyPropertyGetter != null ? keyPropertyGetter(item).Equals(keyPropertyGetter(otherItem)) : item.Equals(otherItem);
        }

        internal bool? allGroupsExpanded;

        /// <summary>
        /// Expand group item.
        /// </summary>
        public async System.Threading.Tasks.Task ExpandGroupItem(RadzenDataGridGroupRow<TItem> item, bool? expandedOnLoad)
        {
            if (expandedOnLoad == true)
                return;

            allGroupsExpanded = null;
            await AllGroupsExpandedChanged.InvokeAsync(allGroupsExpanded);

            if (!collapsedGroupItems.Keys.Contains(item))
            {
                await GroupRowCollapse.InvokeAsync(item.Group);
                collapsedGroupItems.Add(item, true);
            }
            else
            {
                await GroupRowExpand.InvokeAsync(item.Group);
                collapsedGroupItems.Remove(item);
            }

            await InvokeAsync(StateHasChanged);
        }

        internal Dictionary<TItem, bool> expandedItems = new Dictionary<TItem, bool>();

        internal string ExpandedItemStyle(TItem item)
        {
            return expandedItems.Keys.Any(i => ItemEquals(i, item)) ? "notranslate rz-row-toggler rzi-chevron-circle-down" : "rz-row-toggler rzi-chevron-circle-right";
        }

        internal Dictionary<TItem, bool> selectedItems = new Dictionary<TItem, bool>();

        internal string RowStyle(TItem item, int index)
        {
            var isInEditMode = IsRowInEditMode(item) ? "rz-datatable-edit" : "";

            return (RowSelect.HasDelegate || ValueChanged.HasDelegate || SelectionMode == DataGridSelectionMode.Multiple) && selectedItems.Keys.Any(i => ItemEquals(i, item)) ? $"rz-state-highlight rz-data-row {isInEditMode} " : $"rz-data-row {isInEditMode} ";
        }

        internal Tuple<Radzen.RowRenderEventArgs<TItem>, IReadOnlyDictionary<string, object>> RowAttributes(TItem item, int index)
        {
            var args = new Radzen.RowRenderEventArgs<TItem>() { Data = item, Index = index, Expandable = Template != null || LoadChildData.HasDelegate };

            if (RowRender != null)
            {
                RowRender(args);
            }

            return new Tuple<Radzen.RowRenderEventArgs<TItem>, IReadOnlyDictionary<string, object>>(args, new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(args.Attributes));
        }

        internal Tuple<GroupRowRenderEventArgs, IReadOnlyDictionary<string, object>> GroupRowAttributes(RadzenDataGridGroupRow<TItem> item)
        {
            var args = new Radzen.GroupRowRenderEventArgs() { Group = item.Group, FirstRender = firstRender };

            if (GroupRowRender != null)
            {
                GroupRowRender(args);
            }

            return new Tuple<GroupRowRenderEventArgs, IReadOnlyDictionary<string, object>>(args, new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(args.Attributes));
        }

        bool settingsChanged = false;
        bool visibleChanged = false;
        internal bool firstRender = true;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            bool emptyTextChanged = false, allowColumnPickingChanged = false, valueChanged = false, allGroupsExpandedChanged = false;

            foreach (var parameter in parameters) {
                switch (parameter.Name) {
                    case nameof(EmptyText):
                        emptyTextChanged = HasChanged(parameter.Value, EmptyText); break;

                    case nameof(AllowColumnPicking):
                        allowColumnPickingChanged = HasChanged(parameter.Value, AllowColumnPicking); break;

                    case nameof(Visible):
                        visibleChanged = HasChanged(parameter.Value, Visible); break;

                    case nameof(Settings):
                        settingsChanged = HasChanged(parameter.Value, Settings); break;

                    case nameof(AllGroupsExpanded):
                        allGroupsExpandedChanged = HasChanged(parameter.Value, AllGroupsExpanded);
                        if (allGroupsExpandedChanged)
                        {
                            allGroupsExpanded = (bool?)parameter.Value;
                        }
                        break;

                    case nameof(Value):
                        valueChanged = HasChanged(parameter.Value, Value); break;
                }
            }

            await base.SetParametersAsync(parameters);

            if (valueChanged)
            {
                selectedItems.Clear();

                if (Value != null)
                {
                    Value.Where(v => v != null).ToList().ForEach(v => selectedItems.Add(v, true));
                }
            }

            if (allowColumnPickingChanged || emptyTextChanged || allGroupsExpandedChanged && Groups.Any())
            {
                if (allGroupsExpandedChanged && Groups.Any() && allGroupsExpanded == true)
                {
                    collapsedGroupItems.Clear();
                }

                if (allowColumnPickingChanged)
                {
                    selectedColumns = allColumns.Where(c => c.Pickable && c.GetVisible()).ToList();
                    allPickableColumns = allColumns.Where(c => c.Pickable).ToList();
                }

                await ChangeState();
            }

            if (visibleChanged && !firstRender)
            {
                if (Visible == false)
                {
                    Dispose();
                }
            }

        }
        private static bool HasChanged<T>(object newValue, T oldValue) => !EqualityComparer<T>.Default.Equals((T)newValue, oldValue);

        internal override async Task ReloadOnFirstRender()
        {
            if (firstRender && Visible && (LoadData.HasDelegate && Data == null) && IsVirtualizationAllowed())
            {
                await Task.Yield();
                Data = Enumerable.Empty<TItem>().Append(default(TItem));
            }
            else if(settings == null)
            {
                await base.ReloadOnFirstRender();
            }
        }

        /// <summary>
        /// Force load of the DataGrid Settings.
        /// This method triggers a reload of the DataGrid settings, optionally forcing a reload even if the settings are already loaded.
        /// </summary>
        /// <param name="forceReload">If true, forces a reload of the settings regardless of their current state. Default is false.</param>
        public async Task ReloadSettings(bool forceReload = false)
        {
            if (settings != null)
            {
                await LoadSettingsInternal(settings, forceReload);
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (LoadSettings != null)
            {
                var args = new Radzen.DataGridLoadSettingsEventArgs() { Settings = settings };

                if (LoadSettings != null)
                {
                    LoadSettings(args);
                }

                if (args.Settings != settings)
                {
                    settings = args.Settings;
                    settingsChanged = true;
                }
            }

            await base.OnAfterRenderAsync(firstRender);

            if (Visible)
            {
                if (settings != null && settingsChanged)
                {
                    await LoadSettingsInternal(settings);
                }

                if (Render != null)
                {
                    Render(new Radzen.DataGridRenderEventArgs<TItem>() { Grid = this, FirstRender = firstRender });
                }

                if (RenderAsync != null)
                {
                    await RenderAsync(new Radzen.DataGridRenderEventArgs<TItem>() { Grid = this, FirstRender = firstRender });
                }
            }

            this.firstRender = firstRender;

            if (firstRender || visibleChanged)
            {
                visibleChanged = false;
            }
        }

        /// <summary>
        /// Expands the row to show the content defined in Template property.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task ExpandRow(TItem item)
        {
            await ExpandItem(item);
        }

        /// <summary>
        /// Gets boolean value indicating if the row is expanded or not.
        /// </summary>
        /// <param name="item">The item.</param>
        public bool IsRowExpanded(TItem item)
        {
            return expandedItems.Keys.Any(i => ItemEquals(i, item));
        }

        /// <summary>
        /// Expands a range of rows.
        /// </summary>
        /// <param name="items">The range of rows.</param>
        public async System.Threading.Tasks.Task ExpandRows(IEnumerable<TItem> items)
        {
            // Only allow the functionality when multiple row expand is allowed
            if (this.ExpandMode != DataGridExpandMode.Multiple) return;

            foreach (TItem item in items)
            {
                if (!expandedItems.Keys.Any(i => ItemEquals(i, item)))
                {
                    expandedItems.Add(item, true);
                    await RowExpand.InvokeAsync(item);

                    var args = new DataGridLoadChildDataEventArgs<TItem>() { Item = item };
                    await LoadChildData.InvokeAsync(args);

                    if (args.Data != null && !childData.ContainsKey(item))
                    {
                        childData.Add(item, new DataGridChildData<TItem>() { Data = args.Data, ParentChildData = childData.Where(c => c.Value.Data.Contains(item)).Select(c => c.Value).FirstOrDefault() });
                        _view = null;
                    }
                }
            }
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Collapse all rows that are expanded
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task CollapseAll()
        {
            foreach(var item in expandedItems.Keys.ToList())
            {
                await CollapseItem(item);
            }
        }


        /// <summary>
        /// Collapse a range of rows.
        /// </summary>
        /// <param name="items">The range of rows.</param>
        public async System.Threading.Tasks.Task CollapseRows(IEnumerable<TItem> items)
        {
            // Only allow the functionality when multiple row expand is allowed
            if (this.ExpandMode != DataGridExpandMode.Multiple) return;

            foreach (TItem item in items.Where(x=> expandedItems.Keys.Any(i => ItemEquals(i, x))))
            {
                await CollapseItem(item);
            }
            await InvokeAsync(StateHasChanged);
        }

        private async Task CollapseItem(TItem item)
        {
            expandedItems.Remove(item);
            await RowCollapse.InvokeAsync(item);

            if (childData.ContainsKey(item))
            {
                childData.Remove(item);
                _view = null;
            }
        }

        internal async System.Threading.Tasks.Task ExpandItem(TItem item)
        {
            if (ExpandMode == DataGridExpandMode.Single && expandedItems.Keys.Any() && !LoadChildData.HasDelegate)
            {
                var itemToCollapse = expandedItems.Keys.FirstOrDefault();
                if (itemToCollapse != null)
                {
                    expandedItems.Remove(itemToCollapse);
                    await RowCollapse.InvokeAsync(itemToCollapse);

                    if (object.Equals(item, itemToCollapse))
                    {
                        return;
                    }

                }
            }

            if (!expandedItems.Keys.Any(i => ItemEquals(i, item)))
            {
                expandedItems.Add(item, true);
                await RowExpand.InvokeAsync(item);

                var args = new DataGridLoadChildDataEventArgs<TItem>() { Item = item };
                await LoadChildData.InvokeAsync(args);

                if (args.Data != null && !childData.ContainsKey(item))
                {
                    childData.Add(item, new DataGridChildData<TItem>() { Data = args.Data, ParentChildData = childData.Where(c => c.Value.Data.Contains(item)).Select(c => c.Value).FirstOrDefault() });
                    _view = null;
                }
            }
            else
            {
                await CollapseItem(item);
            }

            await InvokeAsync(StateHasChanged);
        }

        internal Dictionary<TItem, DataGridChildData<TItem>> childData = new Dictionary<TItem, DataGridChildData<TItem>>();

        /// <summary>
        /// Gets or sets a value indicating whether DataGrid row can be selected on row click.
        /// </summary>
        /// <value><c>true</c> if DataGrid row can be selected on row click; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowRowSelectOnRowClick { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether DataGrid should use alternating row styles.
        /// </summary>
        /// <value><c>true</c> if DataGrid is using alternating row styles; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowAlternatingRows { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show group visibility column
        /// </summary>
        /// <value><c>true</c> if want to show left column with group visibility toggle, otherwise <c>false</c>.</value>
        [Parameter]
        public bool ShowGroupExpandColumn { get; set; } = true;

        /// <summary>
        /// Gets or sets the grid lines.
        /// </summary>
        /// <value>The grid lines.</value>
        [Parameter]
        public DataGridGridLines GridLines { get; set; } = DataGridGridLines.Default;

        internal bool ShowGridLines(RadzenDataGridColumn<TItem> column)
        {
            return column.Columns != null || column.Parent != null;
        }

        /// <summary>
        /// Gets or sets the ability to automatically goto the first page when sorting is changed.
        /// </summary>
        [Parameter]
        public bool GotoFirstPageOnSort { get; set; } = false;

        internal string getCompositeCellCSSClass(RadzenDataGridColumn<TItem> column)
        {
            return column.Columns != null || column.Parent != null ? "rz-composite-cell" : "";
        }

        internal string getGridLinesCSSClass()
        {
            if (GridLines == DataGridGridLines.Default)
            {
                return "";
            }

            return $"rz-grid-gridlines-{Enum.GetName(typeof(DataGridGridLines), GridLines).ToLower()}";
        }

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        /// <value>The selection mode.</value>
        [Parameter]
        public DataGridSelectionMode SelectionMode { get; set; } = DataGridSelectionMode.Single;

        internal async Task OnCellContextMenu(DataGridCellMouseEventArgs<TItem> args)
        {
            await CellContextMenu.InvokeAsync(args);
        }

        internal async Task OnCellClick(DataGridCellMouseEventArgs<TItem> args)
        {
            await CellClick.InvokeAsync(args);
        }

        internal async Task OnCellDblClick(DataGridCellMouseEventArgs<TItem> args)
        {
            await CellDoubleClick.InvokeAsync(args);
        }

        internal async Task OnRowClick(DataGridRowMouseEventArgs<TItem> args)
        {
            await RowClick.InvokeAsync(args);
            if (AllowRowSelectOnRowClick)
            {
                await OnRowSelect(args.Data);
            }
        }

        internal async System.Threading.Tasks.Task OnRowSelect(TItem item, bool raiseChange = true)
        {
            var focusedIndexResult = PagedView.ToList()
                .Select((x, i) => new { Item = x, Index = i })
                .Where(itemWithIndex => ItemEquals(itemWithIndex.Item, item))
                .FirstOrDefault();

            if (focusedIndexResult != null)
            {
                focusedIndex = focusedIndexResult.Index + 1;
            }

            if (SelectionMode == DataGridSelectionMode.Single && item != null && selectedItems.Keys.Any(i => ItemEquals(i, item)))
            {
                // Legacy RowSelect raise
                if (raiseChange)
                {
                    await RowSelect.InvokeAsync(item);
                }
                return;
            }

            if (SelectionMode == DataGridSelectionMode.Single && selectedItems.Keys.Any())
            {
                var itemToDeselect = selectedItems.Keys.FirstOrDefault();
                if (itemToDeselect != null)
                {
                    selectedItems.Remove(itemToDeselect);
                    await RowDeselect.InvokeAsync(itemToDeselect);
                }
            }

            if (item != null)
            {
                if (!selectedItems.Keys.Any(i => ItemEquals(i, item)))
                {
                    selectedItems.Add(item, true);
                    if (raiseChange)
                    {
                        await RowSelect.InvokeAsync(item);
                    }
                }
                else
                {
                    var itemToRemove = selectedItems.Keys.FirstOrDefault(i => ItemEquals(i, item));
                    if (itemToRemove != null)
                    {
                        selectedItems.Remove(itemToRemove);
                    }
                    await RowDeselect.InvokeAsync(item);
                }
            }
            else
            {
                if (raiseChange)
                {
                    await RowSelect.InvokeAsync(item);
                }
            }

            _value = selectedItems.Keys.ToList();

            await ValueChanged.InvokeAsync(_value);

            StateHasChanged();
        }

        /// <summary>
        /// Selects the row.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="raiseEvent">Should raise RowSelect event.</param>
        public async System.Threading.Tasks.Task SelectRow(TItem item, bool raiseEvent = true)
        {
            await OnRowSelect(item, raiseEvent);
        }

        internal async System.Threading.Tasks.Task OnRowDblClick(DataGridRowMouseEventArgs<TItem> args)
        {
            await RowDoubleClick.InvokeAsync(args);
        }

        /// <summary>
        /// Gets or sets the row edit callback.
        /// </summary>
        /// <value>The row edit callback.</value>
        [Parameter]
        public EventCallback<TItem> RowEdit { get; set; }

        /// <summary>
        /// Gets or sets the row update callback.
        /// </summary>
        /// <value>The row update callback.</value>
        [Parameter]
        public EventCallback<TItem> RowUpdate { get; set; }

        /// <summary>
        /// Gets or sets the row create callback.
        /// </summary>
        /// <value>The row create callback.</value>
        [Parameter]
        public EventCallback<TItem> RowCreate { get; set; }


        internal Dictionary<TItem, bool> editedItems = new Dictionary<TItem, bool>();

        internal Dictionary<TItem, EditContext> editContexts = new Dictionary<TItem, EditContext>();

        /// <summary>
        /// Edits the row.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task EditRow(TItem item)
        {
            if(itemsToInsert.Count() > 0 && EditMode == DataGridEditMode.Single)
            {
                var itemsToCancel = itemsToInsert.ToList();
                foreach( var itemToCancel in itemsToCancel)
                {
                    CancelEditRow(itemToCancel);
                }
            }

            await EditRowInternal(item);
        }

        async System.Threading.Tasks.Task EditRowInternal(TItem item)
        {
            if (EditMode == DataGridEditMode.Single && editedItems.Keys.Any())
            {
                var itemToCancel = editedItems.Keys.FirstOrDefault();
                if (itemToCancel != null)
                {
                    editedItems.Remove(itemToCancel);
                    editContexts.Remove(itemToCancel);
                }
            }

            if (!editedItems.Keys.Any(i => ItemEquals(i, item)))
            {
                editedItems.Add(item, true);

                var editContext = new EditContext(item);
                editContexts.Add(item, editContext);

                await RowEdit.InvokeAsync(item);

                StateHasChanged();
            }
        }

        /// <summary>
        /// Edits a range of rows.
        /// </summary>
        /// <param name="items">The range of rows.</param>
        public async System.Threading.Tasks.Task EditRows(IEnumerable<TItem> items)
        {
            // Only allow the functionality when multiple row edits is allowed
            if (this.EditMode != DataGridEditMode.Multiple) return;

            foreach (TItem item in items)
            {
                if (!editedItems.Keys.Any(i => ItemEquals(i, item)))
                {
                    editedItems.Add(item, true);

                    var editContext = new EditContext(item);
                    editContexts.Add(item, editContext);

                    await RowEdit.InvokeAsync(item);
                }
            }
            StateHasChanged();
        }

        /// <summary>
        /// Updates the row.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task UpdateRow(TItem item)
        {
            if (editedItems.Keys.Any(i => ItemEquals(i, item)))
            {
                var editContext = editContexts.FirstOrDefault(i => ItemEquals(i.Key, item)).Value;

                if (editContext?.Validate() == true)
                {
                    editedItems = editedItems.Where(i => !ItemEquals(i.Key, item)).ToDictionary(i => i.Key, i => i.Value);
                    editContexts = editContexts.Where(i => !ItemEquals(i.Key, item)).ToDictionary(i => i.Key, i => i.Value);

                    if (itemsToInsert.Contains(item))
                    {
                        await RowCreate.InvokeAsync(item);
                        itemsToInsert.Remove(item);
                    }
                    else
                    {
                        await RowUpdate.InvokeAsync(item);
                    }
                }

                StateHasChanged();
            }
        }

        /// <summary>
        /// Cancels the edited row.
        /// </summary>
        /// <param name="item">The item.</param>
        public void CancelEditRow(TItem item)
        {
            if (itemsToInsert.Contains(item))
            {
                if(!IsVirtualizationAllowed())
                {
                    var list = this.PagedView.ToList();
                    list.Remove(item);
                    this._view = list.AsQueryable();
                    this.Count--;
                    itemsToInsert.Remove(item);
                    StateHasChanged();
                }
                else
                {
                    itemsToInsert.Remove(item);
                    if(virtualize != null)
                    {
                        virtualize.RefreshDataAsync();
                    }

                    if(groupVirtualize != null)
                    {
                        groupVirtualize.RefreshDataAsync();
                    }
                }
            }
            else
            {
                if (editedItems.Keys.Any(i => ItemEquals(i, item)))
                {
                    editedItems.Remove(item);
                    editContexts.Remove(item);

                    StateHasChanged();
                }
            }
        }

        /// <summary>
        /// Cancels the edit of a range of rows.
        /// </summary>
        /// <param name="items">The range of rows.</param>
        public void CancelEditRows(IEnumerable<TItem> items)
        {
            foreach (TItem item in items)
            {
                if (editedItems.Keys.Any(i => ItemEquals(i, item)))
                {
                    editedItems.Remove(item);
                    editContexts.Remove(item);
                }
            }
            StateHasChanged();
        }

        /// <summary>
        /// Determines whether row in edit mode.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if row in edit mode; otherwise, <c>false</c>.</returns>
        public bool IsRowInEditMode(TItem item)
        {
            return editedItems.Keys.Any(i => ItemEquals(i, item));
        }

        IList<TItem> itemsToInsert = new List<TItem>();

        /// <summary>
        /// Inserts new row.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task InsertRow(TItem item)
        {
            await InsertRowAtIndex(item);
        }

        /// <summary>
        /// Inserts new row after specific row item.
        /// </summary>
        /// <param name="itemToInsert">The item.</param>
        /// <param name="rowItem">Row item to insert after</param>
        public async System.Threading.Tasks.Task InsertAfterRow(TItem itemToInsert, TItem rowItem)
        {
            var list = this.PagedView.ToList();
            var index = list.IndexOf(rowItem);
            await InsertRowAtIndex(itemToInsert, index + 1);
        }

        private async System.Threading.Tasks.Task InsertRowAtIndex(TItem item, int insertIndex = 0)
        {
            itemsToInsert.Add(item);
            if (!IsVirtualizationAllowed())
            {
                var list = this.PagedView.ToList();
                list.Insert(insertIndex, item);
                this._view = list.AsQueryable();
                this.Count++;
            }
            else
            {
                if (virtualize != null)
                {
                    await virtualize.RefreshDataAsync();
                }

                if (groupVirtualize != null)
                {
                    await groupVirtualize.RefreshDataAsync();
                }
            }

            await EditRowInternal(item);
        }


        bool? isOData;

        internal bool IsOData()
        {
            if(isOData == null && Data != null)
            {
                isOData = typeof(ODataEnumerable<TItem>).IsAssignableFrom(Data.GetType());
            }

            return isOData != null ? isOData.Value : false;
        }

        internal List<SortDescriptor> sorts = new List<SortDescriptor>();

        internal void SetColumnSortOrder(RadzenDataGridColumn<TItem> column)
        {
            if (!AllowMultiColumnSorting)
            {
                foreach (var c in allColumns.ToList().Where(c => c != column))
                {
                    c.SetSortOrderInternal(null);
                }
                sorts.Clear();
            }

            var descriptor = sorts.Where(d => d.Property == column?.GetSortProperty()).FirstOrDefault();
            if (descriptor == null)
            {
                descriptor = new SortDescriptor() { Property = column.GetSortProperty() };
            }

            if (column.GetSortOrder() == null)
            {
                column.SetSortOrderInternal(SortOrder.Ascending);
                descriptor.SortOrder = SortOrder.Ascending;
            }
            else if (column.GetSortOrder() == SortOrder.Ascending)
            {
                column.SetSortOrderInternal(SortOrder.Descending);
                descriptor.SortOrder = SortOrder.Descending;
            }
            else if (column.GetSortOrder() == SortOrder.Descending)
            {
                column.SetSortOrderInternal(null);
                if (sorts.Where(d => d.Property == column?.GetSortProperty()).Any())
                {
                    sorts.Remove(descriptor);
                }
                descriptor = null;
            }

            if (descriptor != null && !sorts.Where(d => d.Property == column?.GetSortProperty()).Any())
            {
                sorts.Add(descriptor);
            }
        }

        void GroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                RadzenDataGridColumn<TItem> column;
                column = columns.FirstOrDefault(c => c.GetGroupProperty() == ((GroupDescriptor)args.NewItems[0]).Property);

                if(column == null)
                {
                   column = allColumns.FirstOrDefault(c => c.GetGroupProperty() == ((GroupDescriptor)args.NewItems[0]).Property);
                }

                if (column != null && HideGroupedColumn)
                {
                    column.SetVisible(false);
                    if (!groupedColumns.Contains(column))
                    {
                        groupedColumns.Add(column);
                    }
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                RadzenDataGridColumn<TItem> column;
                column = columns.FirstOrDefault(c => c.GetGroupProperty() == ((GroupDescriptor)args.OldItems[0]).Property);

                if (column == null)
                {
                    column = allColumns.FirstOrDefault(c => c.GetGroupProperty() == ((GroupDescriptor)args.OldItems[0]).Property);
                }

                if (column != null && HideGroupedColumn)
                {
                    column.SetVisible(true);
                    if (groupedColumns.Contains(column))
                    {
                        groupedColumns.Remove(column);
                    }
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var column in groupedColumns)
                {
                    if (HideGroupedColumn)
                    {
                        column.SetVisible(true);
                    }
                }
            }

            SaveSettings();
        }

        List<RadzenDataGridColumn<TItem>> groupedColumns = new List<RadzenDataGridColumn<TItem>>();
        /// <summary>
        /// Gets or sets the group descriptors.
        /// </summary>
        /// <value>The groups.</value>
        public ObservableCollection<GroupDescriptor> Groups
        {
            get
            {
                if (groups == null)
                {
                    groups = new ObservableCollection<GroupDescriptor>();
                    groups.CollectionChanged -= GroupsCollectionChanged;
                    groups.CollectionChanged += GroupsCollectionChanged;
                }

                return groups;
            }
            set
            {
                groups = value;
            }
        }

        ObservableCollection<GroupDescriptor> groups;

        internal async Task EndColumnDropToGroup()
        {
            if(indexOfColumnToReoder != null && uniqueIDOfColumnToReoder != null && AllowGrouping)
            {
                var functionName = $"Radzen['{getColumnUniqueId(indexOfColumnToReoder.Value)}end']";
                await JSRuntime.InvokeVoidAsync("eval", $"{functionName} && {functionName}()");

                var column = allColumns.Where(c => (c.UniqueID ?? c.Property) == uniqueIDOfColumnToReoder).FirstOrDefault();

                if (column != null && column.Groupable && !string.IsNullOrEmpty(column.GetGroupProperty()))
                {
                    var descriptor = Groups.Where(d => d.Property == column.GetGroupProperty()).FirstOrDefault();
                    if (descriptor == null)
                    {
                        descriptor = new GroupDescriptor() { Property = column.GetGroupProperty(), Title = column.GetTitle(), SortOrder = column.GetSortOrder() ?? SortOrder.Ascending, FormatString = column.FormatString  };
                        Groups.Add(descriptor);
                        _groupedPagedView = null;

                        await Group.InvokeAsync(new DataGridColumnGroupEventArgs<TItem>() { Column = column, GroupDescriptor = descriptor });

                        if (IsVirtualizationAllowed())
                        {
                            await Reload();
                        }
                    }
                }

                indexOfColumnToReoder = null;
                uniqueIDOfColumnToReoder = null;
            }
        }

        /// <summary>
        /// Gets or sets the sort descriptors.
        /// </summary>
        /// <value>The sort.</value>
        public ObservableCollection<SortDescriptor> Sorts
        {
            get
            {
                if (sortDescriptors == null)
                {
                    sortDescriptors = new ObservableCollection<SortDescriptor>();
                    sortDescriptors.CollectionChanged -= SortsCollectionChanged;
                    sortDescriptors.CollectionChanged += SortsCollectionChanged;
                }

                return sortDescriptors;
            }
            set
            {
                sortDescriptors = value;
            }
        }

        ObservableCollection<SortDescriptor> sortDescriptors;

        void SortsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                var column = columns.Where(c => c.GetSortProperty() == ((SortDescriptor)args.NewItems[0]).Property).FirstOrDefault();

                if (column != null)
                {
                    column.SetSortOrder(((SortDescriptor)args.NewItems[0]).SortOrder);
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                var column = columns.Where(c => c.GetSortProperty() == ((SortDescriptor)args.OldItems[0]).Property).FirstOrDefault();

                if (column != null)
                {
                    column.ResetSortOrder();
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                allColumns.ToList().ForEach(c =>
                {
                    c.ResetSortOrder();
                });
            }

            SaveSettings();
            InvokeAsync(ReloadInternal);
        }

        /// <summary>
        /// Orders the DataGrid by property name.
        /// </summary>
        /// <param name="property">The property name.</param>
        public void OrderBy(string property)
        {
            var p = IsOData() ? property.Replace('.', '/') : PropertyAccess.GetProperty(property);

            var column = allColumns.ToList().Where(c => c.GetSortProperty() == property).FirstOrDefault();

            if (column != null)
            {
                SetColumnSortOrder(column);
                Sort.InvokeAsync(new DataGridColumnSortEventArgs<TItem>() { Column = column, SortOrder = column.GetSortOrder() });
                SaveSettings();
            }

            if (LoadData.HasDelegate && IsVirtualizationAllowed())
            {
                Data = null;
                ResetLoadData();
            }

            InvokeAsync(ReloadInternal);
        }

        /// <summary>
        /// Orders descending the DataGrid by property name.
        /// </summary>
        /// <param name="property">The property name.</param>
        public void OrderByDescending(string property)
        {
            var p = IsOData() ? property.Replace('.', '/') : PropertyAccess.GetProperty(property);

            var column = allColumns.ToList().Where(c => c.GetSortProperty() == p).FirstOrDefault();

            if (column != null)
            {
                column.SetSortOrderInternal(SortOrder.Ascending);
                SetColumnSortOrder(column);

                Sort.InvokeAsync(new DataGridColumnSortEventArgs<TItem>() { Column = column, SortOrder = column.GetSortOrder() });
                SaveSettings();
            }

            if (LoadData.HasDelegate && IsVirtualizationAllowed())
            {
                Data = null;
                ResetLoadData();
            }

            InvokeAsync(ReloadInternal);
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-has-pager rz-datatable rz-datatable-scrollable")
            .Add("rz-has-height", CurrentStyle.ContainsKey("height"))
            .Add("rz-datatable-reflow", Responsive)
            .Add("rz-density-compact", Density == Density.Compact)
            .Add("rz-selectable", RowSelect.HasDelegate || ValueChanged.HasDelegate || SelectionMode == DataGridSelectionMode.Multiple)
            .ToString();

        internal string getHeaderStyle()
        {
            var additionalStyle = Style != null && Style.IndexOf("height:") != -1 ? "padding-right: 17px;" : "";
            return $"margin-left:0px;{additionalStyle}";
        }

        /// <summary>
        /// Gets the query.
        /// </summary>
        /// <value>The query.</value>
        public Query Query { get; private set; } = new Query();

        internal string PopupID
        {
            get
            {
                return $"popup{UniqueID}";
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (groups != null)
            {
                groups.CollectionChanged -= GroupsCollectionChanged;
            }

            if (IsJSRuntimeAvailable)
            {
                foreach (var column in allColumns.ToList().Where(c => c.GetVisible()))
                {
                    JSRuntime.InvokeVoid("Radzen.destroyPopup", $"{PopupID}{column.GetFilterProperty()}");
                }
            }
        }

        internal int deepestChildColumnLevel;

        /// <inheritdoc />
        protected override async Task OnPageSizeChanged(int value)
        {
            await base.OnPageSizeChanged(value);
            await PageSizeChanged.InvokeAsync(value);
            SaveSettings();
        }

        /// <summary>
        /// Gets or sets the page size changed callback.
        /// </summary>
        /// <value>The page size changed callback.</value>
        [Parameter]
        public EventCallback<int> PageSizeChanged { get; set; }

        /// <summary>
        /// Saves DataGrid settings as JSON string.
        /// </summary>
        public void SaveSettings()
        {
            if (SettingsChanged.HasDelegate && canSaveSettings)
            {
                var allColumns = ColumnsCollection.Concat(ColumnsCollection.SelectManyRecursive(c => c.ColumnsCollection))
                    .Where(c => !string.IsNullOrEmpty(c.Property) || !string.IsNullOrEmpty(c.UniqueID)).ToList();

                settings = new DataGridSettings()
                {
                    Columns = allColumns.Select(c => new DataGridColumnSettings()
                    {
                        UniqueID = c.UniqueID,
                        Property = c.Property,
                        Width = c.GetWidth(),
                        Visible = c.GetVisible(),
                        OrderIndex = c.GetOrderIndex(),
                        SortOrder = c.GetSortOrder(),
                        SortIndex = c.GetSortIndex(),
                        FilterValue = c.GetFilterValue(),
                        FilterOperator = c.GetFilterOperator(),
                        SecondFilterValue = c.GetSecondFilterValue(),
                        SecondFilterOperator = c.GetSecondFilterOperator(),
                        LogicalFilterOperator = c.GetLogicalFilterOperator(),
                        CustomFilterExpression = c.GetCustomFilterExpression()
                    }).ToList(),
                    CurrentPage = CurrentPage,
                    PageSize = PageSize,
                    Groups = Groups
                };

                SettingsChanged.InvokeAsync(settings);
            }
        }

        /// <summary>
        /// Load DataGrid settings saved from the GetSettings() method.
        /// This internal method handles the actual loading or updating of the DataGrid settings.
        /// </summary>
        /// <param name="settings">The DataGridSettings object containing the settings to be loaded.</param>
        /// <param name="forceUpdate">If true, forces an update of the settings even if they haven't changed. Default is false.</param>
        internal async Task LoadSettingsInternal(DataGridSettings settings, bool forceUpdate = false)
        {
            if (SettingsChanged.HasDelegate)
            {
                var shouldUpdateState = forceUpdate;
                var hasFilter = settings.Columns != null && settings.Columns.Any(c =>
                    c.FilterValue != null || c.SecondFilterValue != null ||
                    c.FilterOperator == FilterOperator.IsNull || c.FilterOperator == FilterOperator.IsNotNull ||
                    c.FilterOperator == FilterOperator.IsEmpty || c.FilterOperator == FilterOperator.IsNotEmpty ||
                    c.SecondFilterOperator == FilterOperator.IsNull || c.SecondFilterOperator == FilterOperator.IsNotNull ||
                    c.SecondFilterOperator == FilterOperator.IsEmpty || c.SecondFilterOperator == FilterOperator.IsNotEmpty);

                if (settings.Columns != null)
                {
                    var allColumns = ColumnsCollection.Concat(ColumnsCollection.SelectManyRecursive(c => c.ColumnsCollection));

                    foreach (var column in settings.Columns.OrderBy(c => c.SortIndex))
                    {
                        var gridColumn = allColumns.Where(c => !string.IsNullOrEmpty(column.Property) && c.Property == column.Property).FirstOrDefault() ??
                                allColumns.Where(c => !string.IsNullOrEmpty(column.UniqueID) && c.UniqueID == column.UniqueID).FirstOrDefault();
                        if (gridColumn != null)
                        {
                            // Sorting
                            if (gridColumn.GetSortOrder() != column.SortOrder)
                            {
                                gridColumn.SetSortOrder(column.SortOrder);
                                shouldUpdateState = true;
                            }
                        }
                    }

                    foreach (var column in settings.Columns)
                    {
                        var gridColumn = allColumns.Where(c => !string.IsNullOrEmpty(column.Property) && c.Property == column.Property).FirstOrDefault() ??
                                allColumns.Where(c => !string.IsNullOrEmpty(column.UniqueID) && c.UniqueID == column.UniqueID).FirstOrDefault();
                        if (gridColumn != null)
                        {
                            // Visibility
                            if (gridColumn.GetVisible() != column.Visible)
                            {
                                gridColumn.SetVisible(column.Visible);
                                shouldUpdateState = true;
                            }

                            // Width
                            if (gridColumn.GetWidth() != column.Width)
                            {
                                gridColumn.SetWidth(column.Width);
                                shouldUpdateState = true;
                            }

                            // OrderIndex
                            if (gridColumn.GetOrderIndex() != column.OrderIndex)
                            {
                                gridColumn.SetOrderIndex(column.OrderIndex);
                                shouldUpdateState = true;
                            }

                            // Filtering
                            if (!AreObjectsEqual(gridColumn.GetFilterValue(), GetFilterValue(column.FilterValue, gridColumn.FilterPropertyType)))
                            {
                                gridColumn.SetFilterValue(GetFilterValue(column.FilterValue, gridColumn.FilterPropertyType));
                                shouldUpdateState = true;
                            }

                            if (gridColumn.GetFilterOperator() != column.FilterOperator)
                            {
                                gridColumn.SetFilterOperator(column.FilterOperator);
                                shouldUpdateState = true;
                            }

                            if (!AreObjectsEqual(gridColumn.GetSecondFilterValue(), GetFilterValue(column.SecondFilterValue, gridColumn.FilterPropertyType)))
                            {
                                gridColumn.SetFilterValue(GetFilterValue(column.SecondFilterValue, gridColumn.FilterPropertyType), false);
                                shouldUpdateState = true;
                            }

                            if (gridColumn.GetSecondFilterOperator() != column.SecondFilterOperator)
                            {
                                gridColumn.SetSecondFilterOperator(column.SecondFilterOperator);
                                shouldUpdateState = true;
                            }

                            if (gridColumn.GetLogicalFilterOperator() != column.LogicalFilterOperator)
                            {
                                gridColumn.SetLogicalFilterOperator(column.LogicalFilterOperator);
                                shouldUpdateState = true;
                            }

                            if (gridColumn.GetCustomFilterExpression() != column.CustomFilterExpression &&
                                !string.IsNullOrEmpty(column.CustomFilterExpression) &&
                                gridColumn.FilterOperator == FilterOperator.Custom)
                            {
                                gridColumn.SetCustomFilterExpression(column.CustomFilterExpression);
                                shouldUpdateState = true;
                            }
                        }
                    }
                }

                if (settings.Groups != null && !settings.Groups.SequenceEqual(Groups))
                {
                    groups.CollectionChanged -= GroupsCollectionChanged;
                    Groups.Clear();
                    settings.Groups.ToList().ForEach(Groups.Add);
                    shouldUpdateState = true;
                    groups.CollectionChanged += GroupsCollectionChanged;
                }

                if (settings.CurrentPage != null && settings.CurrentPage != CurrentPage)
                {
                    CurrentPage = settings.CurrentPage.Value;
                    shouldUpdateState = true;
                }

                if (settings.PageSize != null && settings.PageSize != GetPageSize())
                {
                    SetPageSize(settings.PageSize.Value);
                    shouldUpdateState = true;
                }

                if (!IsVirtualizationAllowed() && !View.Any() && Query.Top == null)
                {
                    shouldUpdateState = true;
                }

                if (shouldUpdateState)
                {
                    skip = CurrentPage * GetPageSize();

                    if (hasFilter && View.Any() ? skip <= View.Count() : true)
                    {
                        CalculatePager();
                        UpdateColumnsOrder();
                        await Reload();
                    }
                }
            }
        }

		/// <summary>
		/// Compares two objects for equality.
		/// </summary>
		/// <param name="object1">The first object to compare.</param>
		/// <param name="object2">The second object to compare.</param>
		/// <returns>True if the objects are equal, false otherwise.</returns>
		private static bool AreObjectsEqual(object object1, object object2)
		{
			// If both objects are null, they are considered equal
			if (object1 == null && object2 == null)
			{
				return true;
			}

			// If only one of the objects is null, they are considered not equal
			if (object1 == null || object2 == null)
			{
				return false;
			}

			// If both objects are enumerable, compare their elements
			if (object1 is IEnumerable list1 && object2 is IEnumerable list2)
			{
				// Create hash sets from the enumerable objects
				var set1 = new HashSet<object>(list1.Cast<object>());
				var set2 = new HashSet<object>(list2.Cast<object>());

				// Check if the hash sets are equal
				return set1.SetEquals(set2);
			}

			// If the objects are not enumerable, compare them using the Equals method
			return object1.Equals(object2);
		}

		object GetFilterValue(object value, Type type)
        {
            if (value != null && value is JsonElement)
            {
                var element = (JsonElement)value;
                if (type == typeof(Int16) || type == typeof(Int16?))
                {
                    return element.GetInt16();
                }
                else if (type == typeof(Int32) || type == typeof(Int32?))
                {
                    return element.GetInt32();
                }
                else if (type == typeof(Int64) || type == typeof(Int64?))
                {
                    return element.GetInt64();
                }
                else if (type == typeof(UInt16) || type == typeof(UInt16?))
                {
                    return element.GetUInt16();
                }
                else if (type == typeof(UInt32) || type == typeof(UInt32?))
                {
                    return element.GetUInt32();
                }
                else if (type == typeof(UInt64) || type == typeof(UInt64?))
                {
                    return element.GetUInt64();
                }
                else if (type == typeof(double) || type == typeof(double?))
                {
                    return element.GetDouble();
                }
                else if (type == typeof(float) || type == typeof(float?))
                {
                    return element.GetSingle();
                }
                else if (type == typeof(decimal) || type == typeof(decimal?))
                {
                    return element.GetDecimal();
                }
                else if (type == typeof(bool) || type == typeof(bool?))
                {
                    return element.GetBoolean();
                }
                else if (type == typeof(DateTime) || type == typeof(DateTime?))
                {
                    return element.GetDateTime();
                }
                else if (type == typeof(DateOnly) || type == typeof(DateOnly?))
                {
                    return DateOnly.FromDateTime(element.GetDateTime());
                }
                else if (type == typeof(TimeOnly) || type == typeof(TimeOnly?))
                {
                    return TimeOnly.FromDateTime(element.GetDateTime());
                }
                else if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
                {
                    return element.GetDateTimeOffset();
                }
                else if (type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true)
                {
                    return element.GetInt32();
                }
                else if (!typeof(string).IsAssignableFrom(type) && (typeof(IEnumerable<>).IsAssignableFrom(type) || typeof(IEnumerable).IsAssignableFrom(type)))
                {
                    var valueType = type.GetGenericArguments().Any() ? type.GetGenericArguments().FirstOrDefault() : typeof(object);
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(valueType));

                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var v in element.EnumerateArray())
                        {
                            list.Add(GetFilterValue(v, valueType));
                        }
                    }

                    return list;
                }
                else
                {
                    if (element.ValueKind == JsonValueKind.Number)
                    {
                        return element.GetDouble();
                    }
                    else if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                    {
                        return element.GetBoolean();
                    }
                    else
                    {
                        return element.GetString();
                    }
                }
            }
            else
            {
                return value;
            }
        }

        bool canSaveSettings = true;

        DataGridSettings settings;

        /// <summary>
        /// Gets or sets DataGrid settings.
        /// </summary>
        [Parameter]
        public DataGridSettings Settings
        {
            get
            {
                return settings;
            }
            set
            {
                if (settings != value)
                {
                    settings = value;

                    if (settings == null)
                    {
                        canSaveSettings = false;

                        Groups.Clear();
                        CurrentPage = 0;
                        skip = 0;
                        Reset(true);
                        columns = allColumns.Where(c => c.Parent == null).ToList();
                        InvokeAsync(Reload);

                        canSaveSettings = true;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the settings changed callback.
        /// </summary>
        /// <value>The settings callback.</value>
        [Parameter]
        public EventCallback<DataGridSettings> SettingsChanged { get; set; }

        async Task ChangePage(PagerEventArgs args)
        {
            CurrentPage = args.PageIndex;
            SaveSettings();

            await OnPageChanged(args);
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            focusedIndex = focusedIndex == -1 ? 0 : focusedIndex;

            base.OnInitialized();
        }
    }
}
