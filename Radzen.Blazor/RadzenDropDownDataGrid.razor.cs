using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDropDownDataGrid component.
    /// </summary>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenDropDownDataGrid @bind-Value=@customerID TValue="string" Data=@customers TextProperty="CompanyName" ValueProperty="CustomerID" Change=@(args => Console.WriteLine($"Selected CustomerID: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenDropDownDataGrid<TValue> : DropDownBase<TValue>
    {
        /// <summary>
        /// Gets or sets the row render callback. Use it to set row attributes.
        /// </summary>
        /// <value>The row render callback.</value>
        [Parameter]
        public Action<RowRenderEventArgs<object>> RowRender { get; set; }

        /// <summary>
        /// Gets or sets the cell render callback. Use it to set cell attributes.
        /// </summary>
        /// <value>The cell render callback.</value>
        [Parameter]
        public Action<DataGridCellRenderEventArgs<object>> CellRender { get; set; }

        /// <summary>
        /// Executes CellRender callback.
        /// </summary>
        protected virtual void OnCellRender(DataGridCellRenderEventArgs<object> args)
        {
            if (CellRender != null)
            {
                CellRender(args);
            }
        }

        /// <summary>
        /// Executes RowRender callback.
        /// </summary>
        protected virtual void OnRowRender(RowRenderEventArgs<object> args)
        {
            if (RowRender != null)
            {
                RowRender(args);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether popup should open on focus. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if popup should open on focus; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool OpenOnFocus { get; set; }

        private async Task OnFocus(Microsoft.AspNetCore.Components.Web.FocusEventArgs args)
        {
            if (OpenOnFocus)
            {
                await OpenPopup("Enter", false);
            }
        }

        /// <summary>
        /// Opens the popup.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="isFilter">if set to <c>true</c> [is filter].</param>
        /// <param name="isFromClick">if set to <c>true</c> [is from click].</param>
        protected override async System.Threading.Tasks.Task OpenPopup(string key = "ArrowDown", bool isFilter = false, bool isFromClick = false)
        {
            if (Disabled)
                return;

            await JSRuntime.InvokeVoidAsync(OpenOnFocus ? "Radzen.openPopup" : "Radzen.togglePopup", Element, PopupID, true);
            await JSRuntime.InvokeVoidAsync("Radzen.focusElement", isFilter ? UniqueID : SearchID);

            if (list != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.selectListItem", search, list, selectedIndex);
            }
        }

        /// <summary>
        /// Gets or sets the value template.
        /// </summary>
        /// <value>The value template.</value>
        [Parameter]
        public RenderFragment<dynamic> ValueTemplate { get; set; }

        /// <summary>
        /// Gets or sets the empty template shown when Data is empty collection.
        /// </summary>
        /// <value>The empty template.</value>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether pager is visible even when not enough data for paging.
        /// </summary>
        /// <value><c>true</c> if pager is visible even when not enough data for paging otherwise, <c>false</c>.</value>
        [Parameter]
        public bool PagerAlwaysVisible { get; set; }

        /// <summary>
        /// Gets or sets the horizontal align.
        /// </summary>
        /// <value>The horizontal align.</value>
        [Parameter]
        public HorizontalAlign PagerHorizontalAlign { get; set; } = HorizontalAlign.Justify;

        /// <summary>
        /// Gets or sets a value indicating whether column resizing is allowed.
        /// </summary>
        /// <value><c>true</c> if column resizing is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowColumnResize { get; set; }

        /// <summary>
        /// Gets or sets the width of all columns.
        /// </summary>
        /// <value>The width of all columns.</value>
        [Parameter]
        public string ColumnWidth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDropDownDataGrid{TValue}"/> is responsive.
        /// </summary>
        /// <value><c>true</c> if responsive; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Responsive { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether search button is shown.
        /// </summary>
        /// <value><c>true</c> if search button is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowSearch { get; set; } = true;

        /// <summary>
        /// Gets or sets the page numbers count.
        /// </summary>
        /// <value>The page numbers count.</value>
        [Parameter]
        public int PageNumbersCount { get; set; } = 2;

        /// <summary>
        /// Gets or sets the pager summary visibility.
        /// </summary>
        /// <value>The pager summary visibility.</value>
        [Parameter]
        public bool ShowPagingSummary { get; set; } = false;

        /// <summary>
        /// Gets or sets the pager summary format.
        /// </summary>
        /// <value>The pager summary format.</value>
        [Parameter]
        public string PagingSummaryFormat { get; set; } = "Page {0} of {1} ({2} items)";

        /// <summary>
        /// Gets or sets the empty text.
        /// </summary>
        /// <value>The empty text.</value>
        [Parameter]
        public string EmptyText { get; set; } = "No records to display.";

        /// <summary>
        /// Gets or sets the search input placeholder text.
        /// </summary>
        /// <value>The search input placeholder text.</value>
        [Parameter]
        public string SearchText { get; set; } = "Search...";

        /// <summary>
        /// Gets or sets the selected value.
        /// </summary>
        /// <value>The selected value.</value>
        [Parameter]
        public object SelectedValue { get; set; }

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [Parameter]
        public RenderFragment Columns { get; set; }

        RadzenDataGrid<object> grid;
        IEnumerable<object> pagedData;
        int count;

        /// <summary>
        /// Gets or sets the number of maximum selected labels.
        /// </summary>
        /// <value>The maximum selected labels.</value>
        [Parameter]
        public int MaxSelectedLabels { get; set; } = 4;

#if !NET5_0_OR_GREATER
        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        /// <value>The page size.</value>
        [Parameter]
        public int PageSize { get; set; } = 5;

        /// <summary>
        /// Gets or sets the total items count.
        /// </summary>
        /// <value>The total items count.</value>
        [Parameter]
        public int Count { get; set; }
#endif

        /// <summary>
        /// Gets or sets the selected items text.
        /// </summary>
        /// <value>The selected items text.</value>
        [Parameter]
        public string SelectedItemsText { get; set; } = "items selected";

        /// <summary>
        /// Gets popup element reference.
        /// </summary>
        protected ElementReference popup;

        /// <summary>
        /// Called when [after render asynchronous].
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>Task.</returns>
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
    #if NET5_0_OR_GREATER
                if (grid != null)
                {
                    grid.SetAllowVirtualization(AllowVirtualization);
                }
    #endif            
                if(Visible && LoadData.HasDelegate && Data == null)
                {
                    LoadData.InvokeAsync(new Radzen.LoadDataArgs() { Skip = 0, Top = PageSize });
                }

                StateHasChanged();
            }

            return base.OnAfterRenderAsync(firstRender);
        }

        /// <summary>
        /// Called when data is changed.
        /// </summary>
        protected override async Task OnDataChanged()
        {
            await base.OnDataChanged();

            if (!LoadData.HasDelegate)
            {
                searchText = null;
                await OnLoadData(new Radzen.LoadDataArgs() { Skip = 0, Top = PageSize });
            }
        }

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        public async Task Reload()
        {
            searchText = null;
            await OnLoadData(new Radzen.LoadDataArgs() { Skip = 0, Top = PageSize });
        }

        private string GetPropertyFilterExpression(string property, string filterCaseSensitivityOperator)
        {
            if (property == null)
            {
                property = "it";
            }
            var p = $@"({property} == null ? """" : {property})";
            return $"{p}{filterCaseSensitivityOperator}.{Enum.GetName(typeof(StringFilterOperator), FilterOperator)}(@0)";
        }

        private bool IsColumnFilterPropertyTypeString(RadzenDataGridColumn<object> column)
        {
            var property = column.GetFilterProperty();
            var itemType = Data != null ? Data.AsQueryable().ElementType : typeof(object);
            var type = PropertyAccess.GetPropertyType(itemType, property);

            return type == typeof(string);
        }

        async Task OnLoadData(LoadDataArgs args)
        {
            if (!LoadData.HasDelegate)
            {
                var query = Query;

                if (query == null)
                    return;

                if (!string.IsNullOrEmpty(searchText))
                {
                    string filterCaseSensitivityOperator = FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? ".ToLower()" : "";

                    if (AllowFilteringByAllStringColumns)
                    {
                        query = query.Where(string.Join(" || ", grid.ColumnsCollection.Where(c => c.Filterable && IsColumnFilterPropertyTypeString(c))
                            .Select(c => GetPropertyFilterExpression(c.GetFilterProperty(), filterCaseSensitivityOperator))),
                                FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? searchText.ToLower() : searchText);
                    }
                    else
                    {
                        query = query.Where($"{GetPropertyFilterExpression(TextProperty, filterCaseSensitivityOperator)}",
                            FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? searchText.ToLower() : searchText);
                    }
                }

                if (!string.IsNullOrEmpty(args.OrderBy))
                {
                    query = query.OrderBy(args.OrderBy);
                }

                _internalView = query;

                count = query.Count();

                pagedData = QueryableExtension.ToList(query.Skip(args.Skip.HasValue ? args.Skip.Value : 0).Take(args.Top.HasValue ? args.Top.Value : PageSize)).Cast<object>();
            }
            else
            {
                await LoadData.InvokeAsync(new Radzen.LoadDataArgs() { Skip = args.Skip, Top = args.Top, OrderBy = args.OrderBy, Filter = searchText });
            }
        }

        IEnumerable _internalView = Enumerable.Empty<object>();

        /// <summary>
        /// Gets the view. The data with sorting, filtering and paging applied.
        /// </summary>
        /// <value>The view.</value>
        public new IEnumerable View
        {
            get
            {
                return _internalView;
            }
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <value>The items.</value>
        protected override IEnumerable<object> Items
        {
            get
            {
                return pagedData != null ? pagedData : Enumerable.Empty<object>();
            }
        }

        /// <summary>
        /// Selects the item from value.
        /// </summary>
        /// <param name="value">The value.</param>
        protected override void SelectItemFromValue(object value)
        {
            if (value != null && Query != null)
            {
                if (!Multiple)
                {
                    if (!string.IsNullOrEmpty(ValueProperty))
                    {
                        var item = Query.Where($@"{ValueProperty} == @0", value).FirstOrDefault();
                        if (item != null)
                        {
                            SelectedItem = item;
                        }
                    }
                    else
                    {
                        SelectedItem = internalValue;
                    }
                    SelectedItemChanged?.Invoke(SelectedItem);
                    selectedItems.Clear();
                    selectedItems.Add(SelectedItem);
                }
                else
                {
                    var values = value as dynamic;
                    if (values != null)
                    {
                        if (!string.IsNullOrEmpty(ValueProperty))
                        {
                            foreach (object v in values)
                            {
                                var item = Query.Where($@"{ValueProperty} == @0", v).FirstOrDefault();
                                if (item != null && selectedItems.IndexOf(item) == -1)
                                {
                                    selectedItems.Add(item);
                                }
                            }
                        }
                        else
                        {
                            ((List<object>)selectedItems).AddRange(values);
                        }

                    }
                }
            }
            else
            {
                selectedItem = null;
                selectedItems.Clear();
            }
        }

        async System.Threading.Tasks.Task Clear()
        {
            if (Disabled)
                return;

            searchText = null;
            internalValue = default(TValue);
            selectedItem = null;

            selectedItems.Clear();

            await ValueChanged.InvokeAsync((TValue)internalValue);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(internalValue);

            await grid.Reload();

            StateHasChanged();
        }


        string previousSearch;

        /// <summary>
        /// Handles the <see cref="E:FilterKeyPress" /> event.
        /// </summary>
        /// <param name="args">The <see cref="KeyboardEventArgs"/> instance containing the event data.</param>
        protected override async Task OnFilterKeyPress(KeyboardEventArgs args)
        {
            var items = (LoadData.HasDelegate ? Data != null ? Data : Enumerable.Empty<object>() : (pagedData != null ? pagedData : Enumerable.Empty<object>())).OfType<object>().ToList();

            var key = args.Code != null ? args.Code : args.Key;

            if (key == "ArrowDown" || key == "ArrowUp")
            {
                try
                {
                    if (key == "ArrowDown" && selectedIndex < items.Count - 1)
                    {
                        selectedIndex++;
                    }

                    if (key == "ArrowUp" && selectedIndex > 0)
                    {
                        selectedIndex--;
                    }

                    var item = items.ElementAtOrDefault(selectedIndex);

                    if (item != null && (!Multiple ? selectedItem != item : true))
                    {
                        await grid.OnRowSelect(item, false);
                    }
                }
                catch (Exception)
                {
                    //
                }
            }
            else if (key == "Enter")
            {
                var item = items.ElementAtOrDefault(selectedIndex);
                if (item != null && (!Multiple ? selectedItem != item : true))
                {
                    await OnRowSelect(item);
                }
            }
            else if (key == "Escape")
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }
            else
            {
                selectedIndex = -1;
                Debounce(DebounceFilter, FilterDelay);
            }
        }

        async Task DebounceFilter()
        {
            searchText = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", search);
            if (searchText != previousSearch)
            {
                previousSearch = searchText;
                _view = null;

                await InvokeAsync(RefreshAfterFilter);
            }
        }

        async Task RefreshAfterFilter()
        {
#if NET5_0_OR_GREATER
            if (grid?.virtualize != null)
            {
                if(string.IsNullOrEmpty(searchText))
                {
                    if(LoadData.HasDelegate)
                    {
                        Data = null;
                        await grid.Reload();
                    }
                    else
                    {
                        pagedData = null;
                        StateHasChanged();
                    }
                }
                await grid.virtualize.RefreshDataAsync();
            }
#endif
            StateHasChanged();
            await grid.FirstPage(true);

            await JSRuntime.InvokeAsync<string>("Radzen.repositionPopup", Element, PopupID);
        }

        /// <summary>
        /// Handles the <see cref="E:Filter" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        protected override async Task OnFilter(ChangeEventArgs args)
        {
            await DebounceFilter();
        }

        /// <summary>
        /// Gets or sets a value indicating whether sorting is allowed.
        /// </summary>
        /// <value><c>true</c> if sorting is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowSorting { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether filtering is allowed.
        /// </summary>
        /// <value><c>true</c> if filtering is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public override bool AllowFiltering { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether filtering by all string columns is allowed.
        /// </summary>
        /// <value><c>true</c> if filtering by all string columns is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowFilteringByAllStringColumns { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether DataGrid row can be selected on row click.
        /// </summary>
        /// <value><c>true</c> if DataGrid row can be selected on row click; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowRowSelectOnRowClick { get; set; } = true;

        async Task OnRowSelect(object item)
        {
            if (!Disabled && !Multiple)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }

            if (AllowRowSelectOnRowClick)
            {
                await SelectItem(item);
            }
            
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-dropdown").Add("rz-clear", AllowClear).ToString();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
            }
        }
    }
}
