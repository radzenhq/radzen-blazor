using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Radzen
{
    /// <summary>
    /// Base class of components that display a list of items.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DropDownBase<T> : DataBoundFormComponent<T>
    {
#if NET5_0_OR_GREATER
        internal Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<object> virtualize;

        /// <summary>
        /// The Virtualize instance.
        /// </summary>
        public Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<object> Virtualize
        {
            get
            {
                return virtualize;
            }
        }

        List<object> virtualItems;

        private async ValueTask<Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<object>> LoadItems(Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderRequest request)
        {
            var data = Data != null ? Data.Cast<object>() : Enumerable.Empty<object>();
            var view = (LoadData.HasDelegate ? data : View).Cast<object>().AsQueryable();
            var totalItemsCount = LoadData.HasDelegate ? Count : view.Count();
            var top = request.Count;

            if(top <= 0)
            {
                top = PageSize;
            }

            if (LoadData.HasDelegate)
            {
                await LoadData.InvokeAsync(new Radzen.LoadDataArgs() { Skip = request.StartIndex, Top = request.Count, Filter = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", search) });
            }

            virtualItems = (LoadData.HasDelegate ? Data : view.Skip(request.StartIndex).Take(top)).Cast<object>().ToList();

            return new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<object>(virtualItems, LoadData.HasDelegate ? Count : totalItemsCount);
        }

        /// <summary>
        /// Specifies the total number of items in the data source.
        /// </summary>
        [Parameter]
        public int Count { get; set; }

        /// <summary>
        /// Specifies wether virtualization is enabled. Set to <c>false</c> by default.
        /// </summary>
        [Parameter]
        public bool AllowVirtualization { get; set; }

        /// <summary>
        /// Specifies the default page size. Set to <c>5</c> by default.
        /// </summary>
        [Parameter]
        public int PageSize { get; set; } = 5;
#endif
        /// <summary>
        /// Determines whether virtualization is allowed.
        /// </summary>
        /// <returns><c>true</c> if virtualization is allowed; otherwise, <c>false</c>.</returns>
        internal bool IsVirtualizationAllowed()
        {
#if NET5_0_OR_GREATER
            return AllowVirtualization;
#else
            return false;
#endif
        }

        /// <summary>
        /// Renders the items.
        /// </summary>
        /// <returns>RenderFragment.</returns>
        internal virtual RenderFragment RenderItems()
        {
            return new RenderFragment(builder =>
            {
#if NET5_0_OR_GREATER
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

        /// <inheritdoc />
        public override bool HasValue
        {
            get
            {
                if (typeof(T) == typeof(string))
                {
                    return !string.IsNullOrEmpty($"{internalValue}");
                }
                return internalValue != null;
            }
        }

        /// <summary>
        /// Renders the item.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="item">The item.</param>
        internal virtual void RenderItem(RenderTreeBuilder builder, object item)
        {
            //
        }

        System.Collections.Generic.HashSet<object> keys = new System.Collections.Generic.HashSet<object>();

        internal object GetKey(object item)
        {
            var value = PropertyAccess.GetItemOrValueFromProperty(item, ValueProperty);

            if (!keys.Contains(value))
            {
                keys.Add(value);
                return value;
            }
            else
            {
                return item;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether filtering is allowed. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if filtering is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public virtual bool AllowFiltering { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user can clear the value. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if clearing is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowClear { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="DropDownBase{T}"/> is multiple.
        /// </summary>
        /// <value><c>true</c> if multiple; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Multiple { get; set; }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<dynamic> Template { get; set; }

        /// <summary>
        /// Gets or sets the value property.
        /// </summary>
        /// <value>The value property.</value>
        [Parameter]
        public string ValueProperty { get; set; }

        /// <summary>
        /// Gets or sets the disabled property.
        /// </summary>
        /// <value>The disabled property.</value>
        [Parameter]
        public string DisabledProperty { get; set; }

        /// <summary>
        /// Gets or sets the selected item changed.
        /// </summary>
        /// <value>The selected item changed.</value>
        [Parameter]
        public Action<object> SelectedItemChanged { get; set; }

        /// <summary>
        /// The selected items
        /// </summary>
        protected IList<object> selectedItems = new List<object>();
        /// <summary>
        /// The selected item
        /// </summary>
        protected object selectedItem = null;

        /// <summary>
        /// Selects all.
        /// </summary>
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
                internalValue = selectedItems.Select(i => PropertyAccess.GetItemOrValueFromProperty(i, ValueProperty)).AsQueryable().Cast(pi.PropertyType);
            }
            else
            {
                var type = typeof(T).IsGenericType ? typeof(T).GetGenericArguments()[0] : typeof(T);
                internalValue = selectedItems.AsQueryable().Cast(type);
            }

            await ValueChanged.InvokeAsync((T)internalValue);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(internalValue);

            StateHasChanged();
        }

        internal bool IsAllSelected()
        {
            if (LoadData.HasDelegate && !string.IsNullOrEmpty(ValueProperty))
            {
                return View != null && View.Cast<object>().All(i => IsItemSelectedByValue(PropertyAccess.GetValue(i, ValueProperty)));
            }

            return View != null && selectedItems.Count == View.Cast<object>().Count();
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            InvokeAsync(ClearAll);
            InvokeAsync(DebounceFilter);
        }

        /// <summary>
        /// Clears all.
        /// </summary>
        protected async System.Threading.Tasks.Task ClearAll()
        {
            if (Disabled)
                return;

            searchText = null;
            await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", search, "");

            internalValue = default(T);
            selectedItem = null;

            selectedItems.Clear();

            selectedIndex = -1;

            await ValueChanged.InvokeAsync((T)internalValue);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(internalValue);

            await OnFilter(new ChangeEventArgs());

            StateHasChanged();
        }

        /// <summary>
        /// The data
        /// </summary>
        IEnumerable _data;

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
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

                    OnDataChanged();

                    StateHasChanged();
                }
            }
        }

        /// <summary>
        /// Gets the popup identifier.
        /// </summary>
        /// <value>The popup identifier.</value>
        protected string PopupID
        {
            get
            {
                return $"popup-{GetId()}";
            }
        }

        /// <summary>
        /// Gets the search identifier.
        /// </summary>
        /// <value>The search identifier.</value>
        protected string SearchID
        {
            get
            {
                return $"search-{GetId()}";
            }
        }

        /// <summary>
        /// Opens the popup script.
        /// </summary>
        /// <returns>System.String.</returns>
        protected string OpenPopupScript()
        {
            if (Disabled)
            {
                return string.Empty;
            }

            return $"Radzen.togglePopup(this.parentNode, '{PopupID}', true);Radzen.focusElement('{SearchID}');";
        }

        /// <summary>
        /// Opens the popup script from parent.
        /// </summary>
        /// <returns>System.String.</returns>
        protected string OpenPopupScriptFromParent()
        {
            if (Disabled)
            {
                return string.Empty;
            }

            return $"Radzen.togglePopup(this, '{PopupID}', true);Radzen.focusElement('{SearchID}');";
        }

        /// <summary>
        /// Gets or sets the filter delay.
        /// </summary>
        /// <value>The filter delay.</value>
        [Parameter]
        public int FilterDelay { get; set; } = 500;

        /// <summary>
        /// The search
        /// </summary>
        protected ElementReference search;
        /// <summary>
        /// The list
        /// </summary>
        protected ElementReference? list;
        /// <summary>
        /// The selected index
        /// </summary>
        protected int selectedIndex = -1;

        /// <summary>
        /// Opens the popup.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="isFilter">if set to <c>true</c> [is filter].</param>
        /// <param name="isFromClick">if set to <c>true</c> [is from click].</param>
        protected virtual async System.Threading.Tasks.Task OpenPopup(string key = "ArrowDown", bool isFilter = false, bool isFromClick = false)
        {
            if (Disabled)
                return;

            await JSRuntime.InvokeVoidAsync("Radzen.togglePopup", Element, PopupID, true);
            await JSRuntime.InvokeVoidAsync("Radzen.focusElement", isFilter ? UniqueID : SearchID);

            if (list != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.selectListItem", search, list, selectedIndex);
            }
        }

        /// <summary>
        /// Handles the key press.
        /// </summary>
        /// <param name="args">The <see cref="Microsoft.AspNetCore.Components.Web.KeyboardEventArgs"/> instance containing the event data.</param>
        /// <param name="isFilter">if set to <c>true</c> [is filter].</param>
        private async System.Threading.Tasks.Task HandleKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args, bool isFilter = false)
        {
            if (Disabled)
                return;

            List<object> items = Enumerable.Empty<object>().ToList();

            if (LoadData.HasDelegate)
            {
                if (Data != null)
                {
                    items = Data.Cast<object>().ToList();
                }
            }
            else
            {
                if (IsVirtualizationAllowed())
                {
#if NET5_0_OR_GREATER
                    items = virtualItems;
#endif
                }
                else
                {
                    items = View.Cast<object>().ToList();
                }
            }

            var key = args.Code != null ? args.Code : args.Key;

            if (!args.AltKey && (key == "ArrowDown" || key == "ArrowLeft" || key == "ArrowUp" || key == "ArrowRight"))
            {
                try
                {
                    var currentViewIndex = Multiple ? selectedIndex : items.IndexOf(selectedItem);

                    var newSelectedIndex = await JSRuntime.InvokeAsync<int>("Radzen.focusListItem", search, list, key == "ArrowDown" || key == "ArrowRight", currentViewIndex);

                    if (!Multiple)
                    {
                        if (newSelectedIndex != currentViewIndex && newSelectedIndex >= 0 && newSelectedIndex <= items.Count() - 1)
                        {
                            selectedIndex = newSelectedIndex;
                            await OnSelectItem(items.ElementAt(selectedIndex), true);
                        }
                    }
                    else
                    {
                        selectedIndex = await JSRuntime.InvokeAsync<int>("Radzen.focusListItem", search, list, key == "ArrowDown", currentViewIndex);
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
            else if (key == "Delete" && AllowClear)
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
            else if (AllowFiltering && isFilter)
            {
                Debounce(DebounceFilter, FilterDelay);
            }
        }

        /// <summary>
        /// Handles the <see cref="E:FilterKeyPress" /> event.
        /// </summary>
        /// <param name="args">The <see cref="Microsoft.AspNetCore.Components.Web.KeyboardEventArgs"/> instance containing the event data.</param>
        protected virtual async System.Threading.Tasks.Task OnFilterKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args)
        {
            await HandleKeyPress(args, true);
        }

        /// <summary>
        /// Debounces the filter.
        /// </summary>
        async Task DebounceFilter()
        {
            if (!LoadData.HasDelegate)
            {
                searchText = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", search);
                _view = null;
                if (IsVirtualizationAllowed())
                {
#if NET5_0_OR_GREATER
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
#if NET5_0_OR_GREATER
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

            if (Multiple)
                selectedIndex = -1;

            await JSRuntime.InvokeAsync<string>("Radzen.repositionPopup", Element, PopupID);
        }

        /// <summary>
        /// Handles the <see cref="E:KeyPress" /> event.
        /// </summary>
        /// <param name="args">The <see cref="Microsoft.AspNetCore.Components.Web.KeyboardEventArgs"/> instance containing the event data.</param>
        protected async System.Threading.Tasks.Task OnKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args)
        {
            await HandleKeyPress(args);
        }

        /// <summary>
        /// Called when [select item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="isFromKey">if set to <c>true</c> [is from key].</param>
        protected virtual async System.Threading.Tasks.Task OnSelectItem(object item, bool isFromKey = false)
        {
            await SelectItem(item);
        }

        /// <summary>
        /// Handles the <see cref="E:Filter" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        protected virtual async System.Threading.Tasks.Task OnFilter(ChangeEventArgs args)
        {
            await DebounceFilter();
        }

        /// <summary>
        /// Gets the load data arguments.
        /// </summary>
        /// <returns>LoadDataArgs.</returns>
        internal virtual async System.Threading.Tasks.Task<LoadDataArgs> GetLoadDataArgs()
        {
#if NET5_0_OR_GREATER
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

        /// <summary>
        /// The first render
        /// </summary>
        private bool firstRender = true;

        /// <summary>
        /// Called when [after render asynchronous].
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>Task.</returns>
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            this.firstRender = firstRender;

            return base.OnAfterRenderAsync(firstRender);
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            internalValue = Value;

            base.OnInitialized();
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
#if NET5_0_OR_GREATER
            var pageSize = parameters.GetValueOrDefault<int>(nameof(PageSize));
            if(pageSize != default(int))
            {
                PageSize = pageSize;
            }
#endif
            var selectedItemChanged = parameters.DidParameterChange(nameof(SelectedItem), SelectedItem);
            if (selectedItemChanged)
            {
                await SelectItem(selectedItem, false);
            }

            var shouldClose = false;

            if (parameters.DidParameterChange(nameof(Visible), Visible))
            {
                var visible = parameters.GetValueOrDefault<bool>(nameof(Visible));
                shouldClose = !visible;
            }

            if (parameters.DidParameterChange(nameof(Value), Value))
            {
                internalValue = parameters.GetValueOrDefault<object>(nameof(Value));
            }

            await base.SetParametersAsync(parameters);

            if (shouldClose && !firstRender)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
            }
        }

        /// <summary>
        /// Called when [parameters set asynchronous].
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task OnParametersSetAsync()
        {
            var valueAsEnumerable = internalValue as IEnumerable;

            if (valueAsEnumerable != null)
            {
                if (valueAsEnumerable.OfType<object>().Count() != selectedItems.Count)
                {
                    selectedItems.Clear();
                }
            }

            SelectItemFromValue(internalValue);

            return base.OnParametersSetAsync();
        }

        /// <summary>
        /// Handles the <see cref="E:Change" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        protected void OnChange(ChangeEventArgs args)
        {
            internalValue = args.Value;
        }

        /// <summary>
        /// Determines whether the specified item is selected.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if the specified item is selected; otherwise, <c>false</c>.</returns>
        internal bool isSelected(object item)
        {
            if (LoadData.HasDelegate && !string.IsNullOrEmpty(ValueProperty))
            {
                return IsItemSelectedByValue(PropertyAccess.GetValue(item, ValueProperty));
            }
            else
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
        }

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        /// <value>The selected item.</value>
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
                }
            }
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <value>The items.</value>
        protected virtual IEnumerable<object> Items
        {
            get
            {
                return (LoadData.HasDelegate ? Data != null ? Data : Enumerable.Empty<object>() : (View != null ? View : Enumerable.Empty<object>())).Cast<object>();
            }
        }

        /// <summary>
        /// Gets the view.
        /// </summary>
        /// <value>The view.</value>
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

                        if (typeof(EnumerableQuery).IsAssignableFrom(Query.GetType()))
                        {
                            query.Add("ToString()");
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
                            _view = (typeof(IQueryable).IsAssignableFrom(Data.GetType())) ? (Query as IEnumerable).Cast<object>().ToList().AsQueryable() : Query;
                        }
                    }
                }

                return _view;
            }
        }

        /// <summary>
        /// Sets the selected index from selected item.
        /// </summary>
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

        /// <summary>
        /// Selects the item internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="raiseChange">if set to <c>true</c> [raise change].</param>
        internal async System.Threading.Tasks.Task SelectItemInternal(object item, bool raiseChange = true)
        {
            await SelectItem(item, raiseChange);
        }

        internal object internalValue;

        /// <summary>
        /// Selects the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="raiseChange">if set to <c>true</c> [raise change].</param>
        public async System.Threading.Tasks.Task SelectItem(object item, bool raiseChange = true)
        {
            if (!Multiple)
            {
                if (object.Equals(item, selectedItem))
                    return;

                selectedItem = item;
                if (!string.IsNullOrEmpty(ValueProperty))
                {
                    internalValue = PropertyAccess.GetItemOrValueFromProperty(item, ValueProperty);
                }
                else
                {
                    internalValue = item;
                }

                SetSelectedIndexFromSelectedItem();

                SelectedItemChanged?.Invoke(selectedItem);
            }
            else
            {
                if (!string.IsNullOrEmpty(ValueProperty))
                {
                    if (LoadData.HasDelegate)
                    {
                        var v = PropertyAccess.GetValue(item, ValueProperty);
                        var si = (selectedItems ?? Enumerable.Empty<object>()).AsQueryable().Where($@"object.Equals({ValueProperty},@0)", v).FirstOrDefault();
                        if (si == null)
                        {
                            selectedItems.Add(item);
                        }
                        else
                        {
                            selectedItems.Remove(si);
                        }
                    }
                    else
                    {
                        UpdateSelectedItems(item);
                    }
                }
                else
                {
                    UpdateSelectedItems(item);
                }

                if (!string.IsNullOrEmpty(ValueProperty))
                {
                    System.Reflection.PropertyInfo pi = PropertyAccess.GetElementType(Data.GetType()).GetProperty(ValueProperty);
                    internalValue = selectedItems.Select(i => PropertyAccess.GetItemOrValueFromProperty(i, ValueProperty)).AsQueryable().Cast(pi.PropertyType);
                }
                else
                {
                    var firstElement = Data.Cast<object>().FirstOrDefault();
                    var elementType = firstElement != null ? firstElement.GetType() : null;
                    if (elementType != null)
                    {
                        internalValue = selectedItems.AsQueryable().Cast(elementType);
                    }
                    else
                    {
                        internalValue = selectedItems;
                    }
                }
            }
            if (raiseChange)
            {
                if (ValueChanged.HasDelegate)
                {
                    await ValueChanged.InvokeAsync(object.Equals(internalValue, null) ? default(T) : (T)internalValue);
                }

                if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }

                await Change.InvokeAsync(internalValue);
            }
            StateHasChanged();
        }

        internal void UpdateSelectedItems(object item)
        {
            if (selectedItems.IndexOf(item) == -1)
            {
                selectedItems.Add(item);
            }
            else
            {
                selectedItems.Remove(item);
            }
        }

        /// <summary>
        /// Selects the item from value.
        /// </summary>
        /// <param name="value">The value.</param>
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
                        selectedItem = internalValue;
                    }

                    SetSelectedIndexFromSelectedItem();

                    SelectedItemChanged?.Invoke(selectedItem);
                }
                else
                {
                    var values = value as IEnumerable;
                    if (values != null)
                    {
                        if (!string.IsNullOrEmpty(ValueProperty))
                        {
                            foreach (object v in values.ToDynamicList())
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

                                if (!object.Equals(item, null) && (LoadData.HasDelegate ? !IsItemSelectedByValue(v) : selectedItems.IndexOf(item) == -1))
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

        internal bool IsItemSelectedByValue(object v)
        {
            return ((Multiple ? selectedItems : selectedItem != null ? new[] { selectedItem } : Enumerable.Empty<object>()) ?? Enumerable.Empty<object>())
                .AsQueryable().Where($@"object.Equals({ValueProperty},@0)", v).Any();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            keys.Clear();
        }
    }
}
