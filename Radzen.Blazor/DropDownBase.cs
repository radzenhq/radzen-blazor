using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Radzen.Blazor;

namespace Radzen
{
    public class DropDownBase<T> : DataBoundFormComponent<T>
    {
#if NET5
        internal Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<object> virtualize;

        private async ValueTask<Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<object>> LoadItems(Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderRequest request)
        {
            var data = Data != null ? Data.Cast<object>() : Enumerable.Empty<object>();
            var view = (LoadData.HasDelegate ? data : View).Cast<object>().AsQueryable();
            var totalItemsCount = LoadData.HasDelegate ? Count : view.Count();
            var top = Math.Min(request.Count, totalItemsCount - request.StartIndex);

            if (LoadData.HasDelegate)
            {
                await LoadData.InvokeAsync(new Radzen.LoadDataArgs() { Skip = request.StartIndex, Top = top, Filter = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", search) });
            }

            return new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<object>(LoadData.HasDelegate ? Data.Cast<object>() : view.Skip(request.StartIndex).Take(top), totalItemsCount);
        }

        [Parameter]
        public int Count { get; set; }

        [Parameter]
        public bool AllowVirtualization { get; set; }

        [Parameter]
        public int PageSize { get; set; } = 5;
#endif
        internal bool IsVirtualizationAllowed()
        {
#if NET5
            return AllowVirtualization;
#else
            return false;
#endif
        }

        internal virtual RenderFragment RenderItems()
        {
            return new RenderFragment(builder =>
            {
#if NET5
                if (AllowVirtualization)
                {
                    builder.OpenComponent(0, typeof(Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<object>));
                    builder.AddAttribute(1, "ItemsProvider", new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderDelegate<object>(LoadItems));
                    builder.AddAttribute(2, "ChildContent", (RenderFragment<object>)((context) =>
                    {
                        return (RenderFragment)((b) =>
                        {
                            RenderItem(b, context);
                        });
                    }));

                    builder.AddComponentReferenceCapture(7, c => { virtualize = (Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<object>)c; });

                    builder.CloseComponent();
                }
                else
                {
                    foreach (var item in LoadData.HasDelegate ? Data : View)
                    {
                        RenderItem(builder, item);
                    }
                }
#else
                foreach (var item in LoadData.HasDelegate ? Data : View)
                {
                    RenderItem(builder, item);
                }
#endif
            });
        }

        internal virtual void RenderItem(RenderTreeBuilder builder, object item)
        {
            //
        }

        [Parameter]
        public virtual bool AllowFiltering { get; set; }

        [Parameter]
        public bool AllowClear { get; set; }

        [Parameter]
        public bool Multiple { get; set; }

        [Parameter]
        public RenderFragment<dynamic> Template { get; set; }

        [Parameter]
        public string ValueProperty { get; set; }

        [Parameter]
        public Action<object> SelectedItemChanged { get; set; }

        protected List<object> selectedItems = new List<object>();
        protected object selectedItem = null;

        protected async System.Threading.Tasks.Task SelectAll()
        {
            if (Disabled)
            {
                return;
            }

            if (selectedItems.Count != View.Cast<object>().Count())
            {
                selectedItems.Clear();
                selectedItems = View.Cast<object>().ToList();
            }
            else
            {
                selectedItems.Clear();
            }

            if (!string.IsNullOrEmpty(ValueProperty))
            {
                System.Reflection.PropertyInfo pi = PropertyAccess.GetElementType(Data.GetType()).GetProperty(ValueProperty);
                Value = selectedItems.Select(i => PropertyAccess.GetItemOrValueFromProperty(i, ValueProperty)).AsQueryable().Cast(pi.PropertyType);
            }
            else
            {
                var type = typeof(T).IsGenericType ? typeof(T).GetGenericArguments()[0] : typeof(T);
                Value = selectedItems.AsQueryable().Cast(type);
            }

            await ValueChanged.InvokeAsync((T)Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            StateHasChanged();
        }

        public void Reset()
        {
            InvokeAsync(ClearAll);
            InvokeAsync(DebounceFilter);
        }

        protected async System.Threading.Tasks.Task ClearAll()
        {
            if (Disabled)
                return;

            searchText = null;
            await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", search, "");

            Value = default(T);
            selectedItem = null;

            selectedItems.Clear();

            selectedIndex = -1;

            await ValueChanged.InvokeAsync((T)Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            await OnFilter(new ChangeEventArgs());

            StateHasChanged();
        }

        IEnumerable _data;

        [Parameter]
        public override IEnumerable Data
        {
            get
            {
                return _data;
            }
            set
            {
                if (_data != value)
                {
                    _data = value;
                    _view = null;

                    if (!LoadData.HasDelegate)
                    {
                        selectedItem = null;
                        selectedItems.Clear();
                    }

                    SelectItemFromValue(Value);

                    OnDataChanged();

                    StateHasChanged();
                }
            }
        }

        protected string PopupID
        {
            get
            {
                return $"popup{UniqueID}";
            }
        }

        protected string SearchID
        {
            get
            {
                return $"search{UniqueID}";
            }
        }

        protected string OpenPopupScript()
        {
            if (Disabled)
            {
                return string.Empty;
            }

            return $"Radzen.togglePopup(this.parentNode, '{PopupID}', true);Radzen.focusElement('{SearchID}');";
        }

        protected string OpenPopupScriptFromParent()
        {
            if (Disabled)
            {
                return string.Empty;
            }

            return $"Radzen.togglePopup(this, '{PopupID}', true);Radzen.focusElement('{SearchID}');";
        }

        [Parameter]
        public int FilterDelay { get; set; } = 500;

        protected ElementReference search;
        protected ElementReference list;
        protected int selectedIndex = -1;

        protected virtual async System.Threading.Tasks.Task OpenPopup(string key = "ArrowDown", bool isFilter = false, bool isFromClick = false)
        {
            if (Disabled)
                return;

            await JSRuntime.InvokeVoidAsync("Radzen.togglePopup", Element, PopupID, true);
            await JSRuntime.InvokeVoidAsync("Radzen.focusElement", isFilter ? UniqueID : SearchID);

            await JSRuntime.InvokeVoidAsync("Radzen.selectListItem", search, list, selectedIndex);
        }

        private async System.Threading.Tasks.Task HandleKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args, bool isFilter = false)
        {
            if (Disabled)
                return;

            var items = (LoadData.HasDelegate ? Data != null ? Data : Enumerable.Empty<object>() : (View != null ? View : Enumerable.Empty<object>())).Cast<object>();

            var key = args.Code != null ? args.Code : args.Key;

            if (!args.AltKey && (key == "ArrowDown" || key == "ArrowLeft" || key == "ArrowUp" || key == "ArrowRight"))
            {
                try
                {
                    var newSelectedIndex = await JSRuntime.InvokeAsync<int>("Radzen.focusListItem", search, list, key == "ArrowDown" || key == "ArrowRight", selectedIndex);

                    if (!Multiple)
                    {
                        if (newSelectedIndex != selectedIndex && newSelectedIndex >= 0 && newSelectedIndex <= items.Count() - 1)
                        {
                            selectedIndex = newSelectedIndex;
                            await OnSelectItem(items.ElementAt(selectedIndex), true);
                        }
                    }
                    else
                    {
                        selectedIndex = await JSRuntime.InvokeAsync<int>("Radzen.focusListItem", search, list, key == "ArrowDown", selectedIndex);
                    }
                }
                catch (Exception)
                {
                    //
                }
            }
            else if (Multiple && key == "Space")
            {
                if (selectedIndex >= 0 && selectedIndex <= items.Count() - 1)
                {
                    await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", search, $"{searchText}".Trim());
                    await OnSelectItem(items.ElementAt(selectedIndex), true);
                }
            }
            else if (key == "Enter" || (args.AltKey && key == "ArrowDown"))
            {
                await OpenPopup(key, isFilter);
            }
            else if (key == "Escape" || key == "Tab")
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }
            else if (key == "Delete")
            {
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
            else if(AllowFiltering && isFilter)
            {
                Debounce(DebounceFilter, FilterDelay);
            }
        }

        protected virtual async System.Threading.Tasks.Task OnFilterKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args)
        {
            await HandleKeyPress(args, true);
        }

        async Task DebounceFilter()
        {
            if (!LoadData.HasDelegate)
            {
                searchText = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", search);
                _view = null;
                if (IsVirtualizationAllowed())
                {
#if NET5
                    if (virtualize != null)
                    {
                        await virtualize.RefreshDataAsync();
                    }
                    await InvokeAsync(() => { StateHasChanged(); });
#endif 
                }
                else
                {
                    await InvokeAsync(() => { StateHasChanged(); });
                }
            }
            else
            {
                if (IsVirtualizationAllowed())
                {
#if NET5
                    if (virtualize != null)
                    {
                        await InvokeAsync(virtualize.RefreshDataAsync);
                    }
                    await InvokeAsync(() => { StateHasChanged(); });
#endif 
                }
                else
                {
                    await LoadData.InvokeAsync(await GetLoadDataArgs());
                }   
            }

            await JSRuntime.InvokeAsync<string>("Radzen.repositionPopup", Element, PopupID);
        }

        protected async System.Threading.Tasks.Task OnKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args)
        {
            await HandleKeyPress(args);
        }

        protected virtual async System.Threading.Tasks.Task OnSelectItem(object item, bool isFromKey = false)
        {
            await SelectItem(item);
        }

        protected virtual async System.Threading.Tasks.Task OnFilter(ChangeEventArgs args)
        {
            await DebounceFilter();
        }

        internal virtual async System.Threading.Tasks.Task<LoadDataArgs> GetLoadDataArgs()
        {
#if NET5
            if (AllowVirtualization)
            {
                return new Radzen.LoadDataArgs() { Skip = 0, Top = PageSize, Filter = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", search) };
            }
            else
            {
                return new Radzen.LoadDataArgs() { Filter = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", search) };
            }
#else
            return new Radzen.LoadDataArgs() { Filter = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", search) };
#endif
        }

        private bool firstRender = true;

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            this.firstRender = firstRender;

            return base.OnAfterRenderAsync(firstRender);
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var shouldClose = false;

            if (parameters.DidParameterChange(nameof(Visible), Visible))
            {
                var visible = parameters.GetValueOrDefault<bool>(nameof(Visible));
                shouldClose = !visible;
            }

            await base.SetParametersAsync(parameters);

            if (shouldClose && !firstRender)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
            }
        }

        protected override Task OnParametersSetAsync()
        {
            var valueAsEnumerable = Value as IEnumerable;

            if (valueAsEnumerable != null)
            {
                if (valueAsEnumerable.OfType<object>().Count() != selectedItems.Count)
                {
                    selectedItems.Clear();
                }
            }

            SelectItemFromValue(Value);

            return base.OnParametersSetAsync();
        }

        protected void OnChange(ChangeEventArgs args)
        {
            Value = args.Value;
        }

        internal bool isSelected(object item)
        {
            if (Multiple)
            {
                return selectedItems.IndexOf(item) != -1;
            }
            else
            {
                return item == selectedItem;
            }
        }

        [Parameter]
        public object SelectedItem
        {
            get
            {
                return selectedItem;
            }
            set
            {
                if (selectedItem != value)
                {
                    selectedItem = object.Equals(value, "null") ? null : value;
                    SelectItem(selectedItem, false).Wait();
                }
            }
        }

        protected virtual IEnumerable<object> Items
        {
            get
            {
                return (LoadData.HasDelegate ? Data != null ? Data : Enumerable.Empty<object>() : (View != null ? View : Enumerable.Empty<object>())).Cast<object>();
            }
        }

        protected override IEnumerable View
        {
            get
            {
                if (_view == null && Query != null)
                {
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        var ignoreCase = FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive;

                        var query = new List<string>();

                        if (!string.IsNullOrEmpty(TextProperty))
                        {
                            query.Add(TextProperty);
                        }

                        if (ignoreCase)
                        {
                            query.Add("ToLower()");
                        }

                        query.Add($"{Enum.GetName(typeof(StringFilterOperator), FilterOperator)}(@0)");

                        _view = Query.Where(String.Join(".", query), ignoreCase ? searchText.ToLower() : searchText);
                    }
                    else
                    {
                        if (IsVirtualizationAllowed())
                        {
                            _view = Query;
                        }
                        else
                        {
                            _view = (typeof(IQueryable).IsAssignableFrom(Data.GetType())) ? Query.Cast<object>().ToList().AsQueryable() : Query;
                        }
                    }
                }

                return _view;
            }
        }

        void SetSelectedIndexFromSelectedItem()
        {
            if (selectedItem != null)
            {
                if (typeof(EnumerableQuery).IsAssignableFrom(View.GetType()))
                {
                    var result = Items.Select((x, i) => new { Item = x, Index = i }).FirstOrDefault(itemWithIndex => object.Equals(itemWithIndex.Item, selectedItem));
                    if (result != null)
                    {
                        selectedIndex = result.Index;
                    }
                }
            }
            else
            {
                selectedIndex = -1;
            }
        }

        internal async System.Threading.Tasks.Task SelectItemInternal(object item, bool raiseChange = true)
        {
            await SelectItem(item, raiseChange);
        }

        protected async System.Threading.Tasks.Task SelectItem(object item, bool raiseChange = true)
        {
            if (!Multiple)
            {
                if (object.Equals(item, selectedItem))
                    return;

                selectedItem = item;
                if (!string.IsNullOrEmpty(ValueProperty))
                {
                    Value = PropertyAccess.GetItemOrValueFromProperty(item, ValueProperty);
                }
                else
                {
                    Value = item;
                }

                SetSelectedIndexFromSelectedItem();

                SelectedItemChanged?.Invoke(selectedItem);
            }
            else
            {
                if (selectedItems.IndexOf(item) == -1)
                {
                    selectedItems.Add(item);
                }
                else
                {
                    selectedItems.Remove(item);
                }

                if (!string.IsNullOrEmpty(ValueProperty))
                {
                    System.Reflection.PropertyInfo pi = PropertyAccess.GetElementType(Data.GetType()).GetProperty(ValueProperty);
                    Value = selectedItems.Select(i => PropertyAccess.GetItemOrValueFromProperty(i, ValueProperty)).AsQueryable().Cast(pi.PropertyType);
                }
                else
                {
                    var firstElement = Data.Cast<object>().FirstOrDefault();
                    var elementType = firstElement != null ? firstElement.GetType() : null;
                    if (elementType != null)
                    {
                        Value = selectedItems.AsQueryable().Cast(elementType);
                    }
                    else
                    {
                        Value = selectedItems;
                    }
                }
            }
            if (raiseChange)
            {
                await ValueChanged.InvokeAsync(object.Equals(Value, null) ? default(T) : (T)Value);
                if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
                await Change.InvokeAsync(Value);
            }
            StateHasChanged();
        }

        protected virtual void SelectItemFromValue(object value)
        {
            if (value != null && View != null)
            {
                if (!Multiple)
                {
                    if (!string.IsNullOrEmpty(ValueProperty))
                    {
                        if (typeof(EnumerableQuery).IsAssignableFrom(View.GetType()))
                        {
                            SelectedItem = View.OfType<object>().Where(i => object.Equals(PropertyAccess.GetValue(i, ValueProperty), value)).FirstOrDefault();
                        }
                        else
                        {
                            SelectedItem = View.AsQueryable().Where($@"{ValueProperty} == @0", value).FirstOrDefault();
                        }
                    }
                    else
                    {
                        selectedItem = Value;
                    }

                    SetSelectedIndexFromSelectedItem();

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
                                dynamic item;

                                if (typeof(EnumerableQuery).IsAssignableFrom(View.GetType()))
                                {
                                    item = View.OfType<object>().Where(i => object.Equals(PropertyAccess.GetValue(i, ValueProperty), v)).FirstOrDefault();
                                }
                                else
                                {
                                    item = View.AsQueryable().Where($@"{ValueProperty} == @0", v).FirstOrDefault();
                                }

                                if (item != null && selectedItems.IndexOf(item) == -1)
                                {
                                    selectedItems.Add(item);
                                }
                            }
                        }
                        else
                        {
                            selectedItems = ((IEnumerable)values).Cast<object>().ToList();
                        }

                    }
                }
            }
            else
            {
                selectedItem = null;
            }
        }
    }
}
