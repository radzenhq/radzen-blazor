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
    public partial class RadzenDropDownDataGrid<TValue> : DropDownBase<TValue>
    {
        [Parameter]
        public string ColumnWidth { get; set; }

        [Parameter]
        public bool Responsive { get; set; } = true;

        [Parameter]
        public bool ShowSearch { get; set; } = true;

        [Parameter]
        public int PageNumbersCount { get; set; } = 2;

        [Parameter]
        public string EmptyText { get; set; } = "No records to display.";

        [Parameter]
        public string SearchText { get; set; } = "Search...";

        [Parameter]
        public object SelectedValue { get; set; }

        [Parameter]
        public RenderFragment Columns { get; set; }

        RadzenDataGrid<object> grid;

        IEnumerable<object> pagedData;
        int count;

        [Parameter]
        public int MaxSelectedLabels { get; set; } = 4;

    #if !NET5
        [Parameter]
        public int PageSize { get; set; } = 5;

        [Parameter]
        public int Count { get; set; }
    #endif

        [Parameter]
        public string SelectedItemsText { get; set; } = "items selected";

        protected ElementReference popup;

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

        protected override async Task OnDataChanged()
        {
            await base.OnDataChanged();

            if (!LoadData.HasDelegate)
            {
                searchText = null;
                await OnLoadData(new Radzen.LoadDataArgs() { Skip = 0, Top = PageSize });
            }
        }

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
        public new IEnumerable View
        {
            get
            {
                return _internalView;
            }
        }

        protected override IEnumerable<object> Items
        {
            get
            {
                return pagedData != null ? pagedData : Enumerable.Empty<object>();
            }
        }

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

        string previousSearch;
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

        protected override async Task OnFilter(ChangeEventArgs args)
        {
            await DebounceFilter();
        }

        [Parameter]
        public bool AllowSorting { get; set; } = true;

        [Parameter]
        public override bool AllowFiltering { get; set; } = true;

        [Parameter]
        public bool AllowFilteringByAllStringColumns { get; set; }

        async Task OnRowSelect(object item)
        {
            await SelectItem(item);
            if (!Disabled && !Multiple)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }
        }

        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-dropdown").Add("rz-clear", AllowClear).ToString();
        }

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