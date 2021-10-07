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
    /// Class RadzenDropDownDataGrid.
    /// Implements the <see cref="Radzen.DropDownBase{TValue}" />
    /// </summary>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <seealso cref="Radzen.DropDownBase{TValue}" />
    public partial class RadzenDropDownDataGrid<TValue> : DropDownBase<TValue>
    {
        /// <summary>
        /// Gets or sets the width of the column.
        /// </summary>
        /// <value>The width of the column.</value>
        [Parameter]
        public string ColumnWidth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDropDownDataGrid{TValue}"/> is responsive.
        /// </summary>
        /// <value><c>true</c> if responsive; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Responsive { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [show search].
        /// </summary>
        /// <value><c>true</c> if [show search]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowSearch { get; set; } = true;

        /// <summary>
        /// Gets or sets the page numbers count.
        /// </summary>
        /// <value>The page numbers count.</value>
        [Parameter]
        public int PageNumbersCount { get; set; } = 2;

        /// <summary>
        /// Gets or sets the empty text.
        /// </summary>
        /// <value>The empty text.</value>
        [Parameter]
        public string EmptyText { get; set; } = "No records to display.";

        /// <summary>
        /// Gets or sets the search text.
        /// </summary>
        /// <value>The search text.</value>
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

        /// <summary>
        /// The grid
        /// </summary>
        RadzenDataGrid<object> grid;

        /// <summary>
        /// The paged data
        /// </summary>
        IEnumerable<object> pagedData;
        /// <summary>
        /// The count
        /// </summary>
        int count;

        /// <summary>
        /// Gets or sets the maximum selected labels.
        /// </summary>
        /// <value>The maximum selected labels.</value>
        [Parameter]
        public int MaxSelectedLabels { get; set; } = 4;

#if !NET5
        /// <summary>
        /// Gets or sets the size of the page.
        /// </summary>
        /// <value>The size of the page.</value>
        [Parameter]
        public int PageSize { get; set; } = 5;

        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        /// <value>The count.</value>
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
        /// The popup
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
    #if NET5
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
        /// Called when [data changed].
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

        /// <summary>
        /// Gets the property filter expression.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="filterCaseSensitivityOperator">The filter case sensitivity operator.</param>
        /// <returns>System.String.</returns>
        private string GetPropertyFilterExpression(string property, string filterCaseSensitivityOperator)
        {
            if (property == null)
            {
                property = "it";
            }
            var p = $@"({property} == null ? """" : {property})";
            return $"{p}{filterCaseSensitivityOperator}.{Enum.GetName(typeof(StringFilterOperator), FilterOperator)}(@0)";
        }

        /// <summary>
        /// Determines whether [is column filter property type string] [the specified column].
        /// </summary>
        /// <param name="column">The column.</param>
        /// <returns><c>true</c> if [is column filter property type string] [the specified column]; otherwise, <c>false</c>.</returns>
        private bool IsColumnFilterPropertyTypeString(RadzenDataGridColumn<object> column)
        {
            var property = column.GetFilterProperty();
            var itemType = Data != null ? Data.AsQueryable().ElementType : typeof(object);
            var type = PropertyAccess.GetPropertyType(itemType, property);

            return type == typeof(string);
        }

        /// <summary>
        /// Called when [load data].
        /// </summary>
        /// <param name="args">The arguments.</param>
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

        /// <summary>
        /// The internal view
        /// </summary>
        IEnumerable _internalView = Enumerable.Empty<object>();
        /// <summary>
        /// Gets the view.
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
                        SelectedItem = Query.Where($@"{ValueProperty} == @0", value).FirstOrDefault();
                    }
                    else
                    {
                        selectedItem = Value;
                    }
                    SelectedItemChanged?.Invoke(selectedItem);
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
                            selectedItems.AddRange(values);
                        }

                    }
                }
            }
            else
            {
                selectedItem = null;
            }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        async System.Threading.Tasks.Task Clear()
        {
            if (Disabled)
                return;

            searchText = null;
            Value = default(TValue);
            selectedItem = null;

            selectedItems.Clear();

            await ValueChanged.InvokeAsync((TValue)Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            await grid.Reload();

            StateHasChanged();
        }

        /// <summary>
        /// The previous search
        /// </summary>
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

        /// <summary>
        /// Debounces the filter.
        /// </summary>
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

        /// <summary>
        /// Refreshes the after filter.
        /// </summary>
        async Task RefreshAfterFilter()
        {
    #if NET5
            if (grid?.virtualize != null)
            {
                if(string.IsNullOrEmpty(searchText))
                {
                    if(LoadData.HasDelegate)
                    {
                        Data = null;
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
        /// Gets or sets a value indicating whether [allow sorting].
        /// </summary>
        /// <value><c>true</c> if [allow sorting]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowSorting { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [allow filtering].
        /// </summary>
        /// <value><c>true</c> if [allow filtering]; otherwise, <c>false</c>.</value>
        [Parameter]
        public override bool AllowFiltering { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [allow filtering by all string columns].
        /// </summary>
        /// <value><c>true</c> if [allow filtering by all string columns]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowFilteringByAllStringColumns { get; set; }

        /// <summary>
        /// Called when [row select].
        /// </summary>
        /// <param name="item">The item.</param>
        async Task OnRowSelect(object item)
        {
            await SelectItem(item);
            if (!Disabled && !Multiple)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }
        }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-dropdown").Add("rz-clear", AllowClear).ToString();
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
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