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
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        [Parameter]
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

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
        /// Gets or sets the footer template.
        /// </summary>
        /// <value>The footer template.</value>
        [Parameter]
        public RenderFragment FooterTemplate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the selected items will be displayed as chips. Set to <c>false</c> by default.
        /// Requires <see cref="DropDownBase{T}.Multiple" /> to be set to <c>true</c>.
        /// </summary>
        /// <value><c>true</c> to display the selected items as chips; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Chips { get; set; }

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
            if (disabledPropertyGetter != null && disabledPropertyGetter(args.Data) as bool? == true)
            {
                args.Attributes.Add("class", "rz-data-row rz-state-disabled");
            }

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

            if (IsVirtualizationAllowed())
            {
                await grid.RefreshDataAsync();
            }

            await JSRuntime.InvokeVoidAsync(OpenOnFocus ? "Radzen.openPopup" : "Radzen.togglePopup", Element, PopupID, true);

            if (FocusFilterOnPopup)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.focusElement", isFilter ? UniqueID : SearchID);
            }

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
        /// Gets or sets a value indicating DataGrid density.
        /// </summary>
        [Parameter]
        public Density Density { get; set; }

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
        /// Gets or sets the action to be executed when the Add button is clicked.
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> Add { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the create button is shown.
        /// </summary>
        /// <value><c>true</c> if the create button is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowAdd { get; set; } = false;

        /// <summary>
        /// Gets or sets preserving the selected row index on pageing.
        /// </summary>
        /// <value>Row selection preservation on pageing.</value>
        [Parameter]
        public bool PreserveRowSelectionOnPaging { get; set; } = false;

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
        /// Gets or sets the pager's first page button's title attribute.
        /// </summary>
        [Parameter]
        public string FirstPageTitle { get; set; } = "First page.";

        /// <summary>
        /// Gets or sets the pager's first page button's aria-label attribute.
        /// </summary>
        [Parameter]
        public string FirstPageAriaLabel { get; set; } = "Go to first page.";

        /// <summary>
        /// Gets or sets the pager's previous page button's title attribute.
        /// </summary>
        [Parameter]
        public string PrevPageTitle { get; set; } = "Previous page";

        /// <summary>
        /// Gets or sets the pager's previous page button's aria-label attribute.
        /// </summary>
        [Parameter]
        public string PrevPageAriaLabel { get; set; } = "Go to previous page.";

        /// <summary>
        /// Gets or sets the pager's last page button's title attribute.
        /// </summary>
        [Parameter]
        public string LastPageTitle { get; set; } = "Last page";

        /// <summary>
        /// Gets or sets the pager's last page button's aria-label attribute.
        /// </summary>
        [Parameter]
        public string LastPageAriaLabel { get; set; } = "Go to last page.";

        /// <summary>
        /// Gets or sets the pager's next page button's title attribute.
        /// </summary>
        [Parameter]
        public string NextPageTitle { get; set; } = "Next page";

        /// <summary>
        /// Gets or sets the pager's next page button's aria-label attribute.
        /// </summary>
        [Parameter]
        public string NextPageAriaLabel { get; set; } = "Go to next page.";

        /// <summary>
        /// Gets or sets the pager's numeric page number buttons' title attributes.
        /// </summary>
        [Parameter]
        public string PageTitleFormat { get; set; } = "Page {0}";

        /// <summary>
        /// Gets or sets the pager's numeric page number buttons' aria-label attributes.
        /// </summary>
        [Parameter]
        public string PageAriaLabelFormat { get; set; } = "Go to page {0}.";

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
        public string SearchTextPlaceholder { get; set; } = "Search...";

        /// <summary>
        /// Gets or sets the add button aria-label attribute.
        /// </summary>
        [Parameter]
        public string AddAriaLabel { get; set; } = "Add";

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

        /// <summary>
        /// Gets or sets the selected items text.
        /// </summary>
        /// <value>The selected items text.</value>
        [Parameter]
        public string SelectedItemsText { get; set; } = "items selected";

        /// <summary>
        /// Gets or sets whether popup automatically focuses on filter input.
        /// </summary>
        /// <value><c>true</c> if filter input should auto focus when opened; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool FocusFilterOnPopup { get; set; } = true;

        /// <summary>
        /// Gets popup element reference.
        /// </summary>
        protected ElementReference popup;

        bool isFirstRender;
        /// <summary>
        /// Called when [after render asynchronous].
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>Task.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            isFirstRender = firstRender;

            if (firstRender)
            {
                if(Visible && LoadData.HasDelegate && Data == null)
                {
                    await LoadData.InvokeAsync(new Radzen.LoadDataArgs() { Skip = 0, Top = PageSize, Filter = searchText });
                }

                StateHasChanged();

                if (!Multiple && grid != null && SelectedItem != null)
                {
                    var items = (LoadData.HasDelegate ? Data != null ? Data : Enumerable.Empty<object>() : (pagedData != null ? pagedData : Enumerable.Empty<object>())).OfType<object>().ToList();
                    if (items.Any())
                    {
                        selectedIndex = items.IndexOf(SelectedItem);
                        if (selectedIndex >= 0)
                        {
                            await JSRuntime.InvokeAsync<int[]>("Radzen.focusTableRow", grid.GridId(), "ArrowDown", selectedIndex - 1, null);
                        }
                    }
                }
            }

            await base.OnAfterRenderAsync(firstRender);
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
                await OnLoadData(new Radzen.LoadDataArgs() { Skip = 0, Top = PageSize, OrderBy = "" });
            }
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);

            if (!string.IsNullOrEmpty(searchText) && !LoadData.HasDelegate)
            {
                await OnLoadData(new Radzen.LoadDataArgs() { Skip = skip, Top = PageSize, OrderBy = "" });
            }
        }

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        public async Task Reload()
        {
            searchText = null;
            await OnLoadData(new Radzen.LoadDataArgs() { Skip = 0, Top = PageSize, OrderBy = "" });
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
            if (column.Type == typeof(string)) return true;

            var property = column.GetFilterProperty();
            var itemType = Data != null ? Data.AsQueryable().ElementType : typeof(object);
            var type = PropertyAccess.GetPropertyType(itemType, property);

            return type == typeof(string);
        }

        string prevSearch;
        string prevOrder = "";
        int? skip;
        async Task OnLoadData(LoadDataArgs args)
        {
            skip = args.Skip;

            if (prevSearch != searchText)
            {
                prevSearch = searchText;
                skip = 0;
            }

            if (!LoadData.HasDelegate)
            {
                var query = Query;

                if (query == null)
                    return;

                if (!string.IsNullOrEmpty(searchText))
                {
                    string filterCaseSensitivityOperator = FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? ".ToLower()" : "";

                    if (AllowFilteringByAllStringColumns && grid != null)
                    {
                        if (AllowFilteringByWord)
                        {
                            string[] words = searchText.Split(' ');

                            foreach (string word in words)
                            {
                                query = query.Where(DynamicLinqCustomTypeProvider.ParsingConfig, string.Join(" || ", grid.ColumnsCollection.Where(c => c.Filterable && IsColumnFilterPropertyTypeString(c))
                                    .Select(c => GetPropertyFilterExpression(c.GetFilterProperty(), filterCaseSensitivityOperator))),
                                        FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? word.ToLower() : word);
                            }
                        }
                        else
                        {
                            query = query.Where(DynamicLinqCustomTypeProvider.ParsingConfig, string.Join(" || ", grid.ColumnsCollection.Where(c => c.Filterable && IsColumnFilterPropertyTypeString(c))
                                .Select(c => GetPropertyFilterExpression(c.GetFilterProperty(), filterCaseSensitivityOperator))),
                                    FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? searchText.ToLower() : searchText);
                        }
                    }
                    else
                    {
                        if (AllowFilteringByWord)
                        {
                            string[] words = searchText.Split(' ');

                            foreach (string word in words)
                            {
                                query = query.Where(DynamicLinqCustomTypeProvider.ParsingConfig, $"{GetPropertyFilterExpression(TextProperty, filterCaseSensitivityOperator)}",
                                    FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? word.ToLower() : word);
                            }
                        }
                        else
                        {
                            query = query.Where(DynamicLinqCustomTypeProvider.ParsingConfig, $"{GetPropertyFilterExpression(TextProperty, filterCaseSensitivityOperator)}",
                                FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? searchText.ToLower() : searchText);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(args.OrderBy))
                {
                    query = query.OrderBy(DynamicLinqCustomTypeProvider.ParsingConfig, args.OrderBy);
                }

                count = await Task.FromResult(query.Count());

                pagedData = await Task.FromResult(QueryableExtension.ToList(query.Skip(skip.HasValue ? skip.Value : 0).Take(args.Top.HasValue ? args.Top.Value : PageSize)).Cast<object>());

                _internalView = query;

                if (prevOrder != args.OrderBy)
                {
                    prevOrder = args.OrderBy;
                    await JSRuntime.InvokeVoidAsync("eval");
                }
            }
            else
            {
                await LoadData.InvokeAsync(new Radzen.LoadDataArgs() { Skip = skip, Top = args.Top, OrderBy = args.OrderBy, Filter = searchText });
            }
            
            if(PreserveRowSelectionOnPaging && selectedIndex != -1)
            {	
                var items = (LoadData.HasDelegate ? Data != null ? Data : Enumerable.Empty<object>() : (pagedData != null ? pagedData : Enumerable.Empty<object>())).OfType<object>().ToList();
                selectedIndex = Math.Clamp(selectedIndex, 0, items.Count - 1);
                
                await JSRuntime.InvokeAsync<int[]>("Radzen.focusTableRow", grid.GridId(), "ArrowDown", selectedIndex - 1, null);

                await grid.OnRowSelect(items[selectedIndex], false);
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
                    bool raiseChange = false;

                    if (!string.IsNullOrEmpty(ValueProperty))
                    {
                        var item = Query.Where(DynamicLinqCustomTypeProvider.ParsingConfig, $@"{ValueProperty} == @0", value).FirstOrDefault();
                        if (item != null && SelectedItem != item)
                        {
                            SelectedItem = item;
                            raiseChange = true;
                        }
                    }
                    else
                    {
                        if (SelectedItem != internalValue)
                        {
                            SelectedItem = internalValue;
                            raiseChange = true;
                        }
                    }

                    if (raiseChange)
                    {
                        SelectedItemChanged.InvokeAsync(SelectedItem);
                        selectedItems.Clear();
                        selectedItems.Add(SelectedItem);
                        try
                        {
                            if (grid != null && !isFirstRender)
                            {
                                InvokeAsync(() => grid.SelectRow(SelectedItem, false));
                                JSRuntime.InvokeAsync<int[]>("Radzen.focusTableRow", grid.GridId(), "ArrowDown", Items.ToList().IndexOf(SelectedItem) - 1, null);
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    var values = value as IEnumerable;
                    if (values != null)
                    {
                        var valueList = values.Cast<object>().ToList();
                        if (!string.IsNullOrEmpty(ValueProperty))
                        {
                            foreach (object v in valueList)
                            {
                                var item = Query.Where(DynamicLinqCustomTypeProvider.ParsingConfig, $@"{ValueProperty} == @0", v).FirstOrDefault();
                                if (item != null && !selectedItems.AsQueryable().Where(i => object.Equals(GetItemOrValueFromProperty(i, ValueProperty), v)).Any())
                                {
                                    selectedItems.Add(item);
                                }
                            }
                        }
                        else
                        {
                            foreach (object v in valueList)
                            {
                                selectedItems.Add(v);
                            }
                        }

                    }
                }
            }
            else
            {
                selectedItem = null;
                selectedItems.Clear();
                if (grid != null)
                {
                    grid.selectedItems.Clear();
                }
            }
        }

        async System.Threading.Tasks.Task Clear()
        {
            if (Disabled)
                return;

            var canRequest = searchText != null;

            searchText = null;
            internalValue = default(TValue);
            selectedItem = null;

            selectedItems.Clear();

            await ValueChanged.InvokeAsync((TValue)internalValue);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(internalValue);

            if (!Multiple)
            {
                await grid.SelectRow(null);
            }

            if (canRequest)
            {
                await OnLoadData(new Radzen.LoadDataArgs() { Skip = 0, Top = PageSize, OrderBy = "" });
            }

            StateHasChanged();
        }

        string previousSearch;

        /// <inheritdoc />
        protected override async Task HandleKeyPress(KeyboardEventArgs args, bool isFilter, bool? shouldSelectOnChange = null)
        {
            var items = (LoadData.HasDelegate ? Data != null ? Data : Enumerable.Empty<object>() : (pagedData != null ? pagedData : Enumerable.Empty<object>())).OfType<object>().ToList();

            var key = args.Code != null ? args.Code : args.Key;

            if (!args.AltKey && (key == "ArrowDown" || key == "ArrowUp"))
            {
                preventKeydown = true;

                try
                {
                    var newSelectedIndex = Math.Clamp(selectedIndex + (key == "ArrowUp" ? -1 : 1), 0, items.Count - 1);
                    var shouldChange = newSelectedIndex != selectedIndex;
                    if (shouldChange)
                    {
                        selectedIndex = newSelectedIndex;
                        await JSRuntime.InvokeAsync<int[]>("Radzen.focusTableRow", grid.GridId(), key, selectedIndex + (key == "ArrowUp" ? 1 : -1), null);
                        await grid.OnRowSelect(items[selectedIndex], false);
                    }

                    if (!Multiple)
                    {
                        var popupOpened = await JSRuntime.InvokeAsync<bool>("Radzen.popupOpened", PopupID);

                        if (shouldChange && (!popupOpened || grid.IsVirtualizationAllowed()))
                        {
                            await OnSelectItem(items[selectedIndex], true);
                        }
                    }
                }
                catch (Exception)
                {
                    //
                }
            }
            else if ((key == "ArrowLeft" || key == "ArrowRight") && !grid.IsVirtualizationAllowed())
            {
                if (key == "ArrowLeft")
                {
                    await grid.PrevPage();
                }
                else
                {
                    await grid.NextPage();
                }
            }
            else if (key == "Enter" || key == "NumpadEnter")
            {
                preventKeydown = false;

                var popupOpened = await JSRuntime.InvokeAsync<bool>("Radzen.popupOpened", PopupID);

                if (!popupOpened)
                {
                    await OpenPopup(key, isFilter);
                }
                else
                {
                    if (!grid.IsVirtualizationAllowed())
                    {
                        if (selectedIndex >= 0 && selectedIndex <= items.Count - 1)
                        {
                            await OnSelectItem(items[selectedIndex], true);
                        }
                    }

                    await CloseAndFocus();
                }
            }
            else if (args.AltKey && key == "ArrowDown")
            {
                preventKeydown = true;

                await OpenPopup(key, isFilter);
            }
            else if (key == "Escape")
            {
                preventKeydown = false;

                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }
            else if (key == "Tab")
            {
                preventKeydown = false;

                if (!ShowSearch && !ShowAdd)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
                }
            }
            else if (key == "Delete" && AllowClear)
            {
                preventKeydown = true;

                if (!Multiple && selectedItem != null)
                {
                    selectedIndex = -1;
                    await OnSelectItem(null, true);
                }

                if (AllowFiltering && isFilter)
                {
                    Debounce(DebounceFilter, FilterDelay);
                }
            }
            else if (AllowFiltering && isFilter && FilterAsYouType)
            {
                preventKeydown = true;

                selectedIndex = -1;
                Debounce(DebounceFilter, FilterDelay);
            }
            else
            {
                preventKeydown = false;
            }
        }

        async Task DebounceFilter()
        {
            if (searchText != previousSearch)
            {
                previousSearch = searchText;
                _view = null;

                await InvokeAsync(RefreshAfterFilter);
            }
        }

        async Task CloseOnEscape(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;
            if (key == "Escape")
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }
        }

        async Task RefreshAfterFilter()
        {
            if (IsVirtualizationAllowed() && grid != null)
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

                if (grid.Virtualize != null)
                {
                    await grid.RefreshDataAsync();
                }
                else
                {
                    if(grid.LoadData.HasDelegate)
                    {
                        await grid.InvokeLoadData(0, PageSize);
                    }
                    else
                    {
                        await grid.Reload();
                    }
                }
            }

            StateHasChanged();

            if (!IsVirtualizationAllowed())
            {
                await grid.FirstPage(true);
            }

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
        /// Gets or sets a value indicating whether filtering by each entered word in the search term, sperated by a space, is allowed.
        /// </summary>
        /// <value><c>true</c> if filtering by individual words is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowFilteringByWord { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether DataGrid row can be selected on row click.
        /// </summary>
        /// <value><c>true</c> if DataGrid row can be selected on row click; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowRowSelectOnRowClick { get; set; } = true;

        async Task OnRowSelect(object item)
        {
            await CloseAndFocus();

            if (AllowRowSelectOnRowClick)
            {
                await SelectItem(item);
            }
        }

        async Task CloseAndFocus()
        {
            if (!Disabled && !Multiple)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }

            var of = OpenOnFocus;
            OpenOnFocus = false;

            await JSRuntime.InvokeVoidAsync("Radzen.focusElement", UniqueID);

            OpenOnFocus = of;
        }

        private async Task OnChipRemove(object item)
        {
            if (!Disabled)
            {
                await SelectItem(item);
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-dropdown").Add("rz-dropdown-chips", Chips && selectedItems.Count > 0).Add("rz-clear", AllowClear).ToString();
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

        bool clicking;
        /// <summary>
        /// Handles the <see cref="E:Click" /> event.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        public async Task OnAddClick(MouseEventArgs args)
        {
            if (clicking)
            {
                return;
            }

            try
            {
                clicking = true;

                await Add.InvokeAsync(args);
            }
            finally
            {
                clicking = false;
            }
        }

        /// <summary>
        /// Handles the reference to the DataGrid component.
        /// </summary>
        public RadzenDataGrid<object> DataGrid
        {
            get
            {
                return grid;
            }
        }

        /// <summary>
        /// Resets component and deselects row
        /// </summary>
        public new async Task Reset() {
            base.Reset();

            if (!Multiple)
            {
                await grid.SelectRow(null);
            }
        }
    }
}
