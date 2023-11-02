using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Radzen.Blazor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Radzen
{
    /// <summary>
    /// Base class of components that display a list of items.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DropDownBase<T> : DataBoundFormComponent<T>
    {
#if NET5_0_OR_GREATER
        /// <summary>
        /// Gets or sets a value that determines how many additional items will be rendered before and after the visible region. This help to reduce the frequency of rendering during scrolling. However, higher values mean that more elements will be present in the page.
        /// </summary>
        [Parameter]
        public int VirtualizationOverscanCount { get; set; }

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
                await LoadData.InvokeAsync(new Radzen.LoadDataArgs() { Skip = request.StartIndex, Top = request.Count, Filter = searchText });
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

        internal int GetVirtualizationOverscanCount()
        {
#if NET5_0_OR_GREATER
            return VirtualizationOverscanCount;
#else
            return 0;
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

                    if(VirtualizationOverscanCount != default(int))
                    {
                        builder.AddAttribute(3, "OverscanCount", VirtualizationOverscanCount);
                    }

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
                else if (typeof(IEnumerable).IsAssignableFrom(typeof(T)))
                {
                    return internalValue != null && ((IEnumerable)internalValue).Cast<object>().Any();
                }
                else
                {
                    return internalValue != null;
                }
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
            var value = GetItemOrValueFromProperty(item, ValueProperty);

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
        /// Gets or sets a value indicating whether filtering is allowed as you type. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if filtering is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public virtual bool FilterAsYouType { get; set; } = true;

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
        /// Gets or sets a value indicating whether the user can select all values in multiple selection. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if select all values is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowSelectAll { get; set; } = true;

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

            if (selectedItems.Count != View.Cast<object>().ToList().Where(i => disabledPropertyGetter != null ? disabledPropertyGetter(i) as bool? != true : true).Count())
            {
                selectedItems.Clear();
                selectedItems = View.Cast<object>().ToList().Where(i => disabledPropertyGetter != null ? disabledPropertyGetter(i) as bool? != true : true).ToList();
            }
            else
            {
                selectedItems.Clear();
            }

            if (!string.IsNullOrEmpty(ValueProperty))
            {
                System.Reflection.PropertyInfo pi = PropertyAccess.GetElementType(Data.GetType()).GetProperty(ValueProperty);
                internalValue = selectedItems.Select(i => GetItemOrValueFromProperty(i, ValueProperty)).AsQueryable().Cast(pi.PropertyType);
            }
            else
            {
                var type = typeof(T).IsGenericType ? typeof(T).GetGenericArguments()[0] : typeof(T);
                internalValue = selectedItems.AsQueryable().Cast(type);
            }

            if (typeof(IList).IsAssignableFrom(typeof(T)))
            {
                var list = (IList)Activator.CreateInstance(typeof(T));
                foreach (var i in (IEnumerable)internalValue)
                {
                    list.Add(i);
                }
                await ValueChanged.InvokeAsync((T)(object)list);
            }
            else if (typeof(T).IsGenericType && typeof(ICollection<>).MakeGenericType(typeof(T).GetGenericArguments()[0]).IsAssignableFrom(typeof(T)))
            {
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(typeof(T).GetGenericArguments()[0]));
                foreach (var i in (IEnumerable)internalValue)
                {
                    list.Add(i);
                }
                await ValueChanged.InvokeAsync((T)(object)list);
            }
            else
            {
                await ValueChanged.InvokeAsync((T)internalValue);
            }
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(internalValue);

            StateHasChanged();
        }

        internal bool IsAllSelected()
        {
            if (LoadData.HasDelegate && !string.IsNullOrEmpty(ValueProperty))
            {
                return View != null && View.Cast<object>().ToList()
                    .Where(i => disabledPropertyGetter != null ? disabledPropertyGetter(i) as bool? != true : true)
                    .All(i => IsItemSelectedByValue(GetItemOrValueFromProperty(i, ValueProperty)));
            }

            return View != null && selectedItems.Count == View.Cast<object>().ToList()
                    .Where(i => disabledPropertyGetter != null ? disabledPropertyGetter(i) as bool? != true : true).Count();
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
            await SearchTextChanged.InvokeAsync(searchText);
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

                    InvokeAsync(OnDataChanged);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (_data != null)
            {
                var query = _data.AsQueryable();

                var type = query.ElementType;

                if (type == typeof(object) && typeof(EnumerableQuery).IsAssignableFrom(query.GetType()) && query.Any())
                {
                    type = query.FirstOrDefault().GetType();
                }

                if (!string.IsNullOrEmpty(ValueProperty))
                {
                    valuePropertyGetter = PropertyAccess.Getter<object, object>(ValueProperty, type);
                }

                if (!string.IsNullOrEmpty(TextProperty))
                {
                    textPropertyGetter = PropertyAccess.Getter<object, object>(TextProperty, type);
                }

                if (!string.IsNullOrEmpty(DisabledProperty))
                {
                    disabledPropertyGetter = PropertyAccess.Getter<object, object>(DisabledProperty, type);
                }
            }
        }

        internal Func<object, object> valuePropertyGetter;
        internal Func<object, object> textPropertyGetter;
        internal Func<object, object> disabledPropertyGetter;

        /// <summary>
        /// Gets the item or value from property.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="property">The property.</param>
        /// <returns>System.Object.</returns>
        public object GetItemOrValueFromProperty(object item, string property)
        {
            if (item != null)
            {
                if (property == TextProperty && textPropertyGetter != null)
                {
                    return textPropertyGetter(item);
                }
                else if (property == ValueProperty && valuePropertyGetter != null)
                {
                    return valuePropertyGetter(item);
                }
                else if (property == DisabledProperty && disabledPropertyGetter != null)
                {
                    return disabledPropertyGetter(item);
                }

                var enumValue = item as Enum;
                if (enumValue != null)
                {
                    return Radzen.Blazor.EnumExtensions.GetDisplayDescription(enumValue);
                }
            }

            return item;
        }

#if NET5_0_OR_GREATER
        /// <inheritdoc/>
        protected override async Task OnDataChanged()
        {
            await base.OnDataChanged();

            if (AllowVirtualization && Virtualize != null && !LoadData.HasDelegate)
            {
                await InvokeAsync(Virtualize.RefreshDataAsync);
            }
        }
#endif

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
        protected virtual async System.Threading.Tasks.Task HandleKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args, bool isFilter = false)
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
            else if (Multiple && key == "Enter")
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
            else if (AllowFiltering && isFilter && FilterAsYouType)
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
            await SearchTextChanged.InvokeAsync(SearchText);
        }

        /// <summary>
        /// Handles the <see cref="E:KeyPress" /> event.
        /// </summary>
        /// <param name="args">The <see cref="Microsoft.AspNetCore.Components.Web.KeyboardEventArgs"/> instance containing the event data.</param>
        protected virtual async System.Threading.Tasks.Task OnKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args)
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
                return await Task.FromResult(new Radzen.LoadDataArgs() { Skip = 0, Top = PageSize, Filter = searchText });
            }
            else
            {
                return await Task.FromResult(new Radzen.LoadDataArgs() { Filter = searchText });
            }
#else
            return await Task.FromResult(new Radzen.LoadDataArgs() { Filter = searchText });
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
                if (!valueAsEnumerable.Cast<object>().SequenceEqual(selectedItems.Select(i => GetItemOrValueFromProperty(i, ValueProperty))))
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
        internal bool IsSelected(object item)
        {
            if (!string.IsNullOrEmpty(ValueProperty))
            {
                return IsItemSelectedByValue(GetItemOrValueFromProperty(item, ValueProperty));
            }
            else
            {
                if (Multiple)
                {
                    return selectedItems.IndexOf(item) != -1;
                }
                else
                {
                    return object.Equals(item, selectedItem);
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
        /// Gets or sets the item separator for Multiple dropdown.
        /// </summary>
        /// <value>Item separator</value>
        [Parameter]
        public string Separator { get; set; } = ",";

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

                        var search = ignoreCase ? searchText.ToLower() : searchText;

                        if (Query.ElementType == typeof(Enum))
                        {
                            _view = Query.Cast<Enum>()
                                .Where((Func<Enum, bool>)(i =>
                                {
                                    var value = ignoreCase ? i.GetDisplayDescription().ToLower() : i.GetDisplayDescription();

                                    if (FilterOperator == StringFilterOperator.Contains)
                                    {
                                        return value.Contains(search);
                                    }
                                    else if (FilterOperator == StringFilterOperator.StartsWith)
                                    {
                                        return value.StartsWith(search);
                                    }
                                    else if (FilterOperator == StringFilterOperator.EndsWith)
                                    {
                                        return value.EndsWith(search);
                                    }

                                    return value == search;
                                })).AsQueryable();
                        }
                        else
                        {
                            _view = Query.Where(String.Join(".", query), search);
                        }
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
            if (disabledPropertyGetter != null && disabledPropertyGetter(item) as bool? == true)
            {
                return;
            }

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
                UpdateSelectedItems(item);

                if (!string.IsNullOrEmpty(ValueProperty))
                {
                    System.Reflection.PropertyInfo pi = PropertyAccess.GetElementType(Data.GetType()).GetProperty(ValueProperty);
                    internalValue = selectedItems.Select(i => GetItemOrValueFromProperty(i, ValueProperty)).AsQueryable().Cast(pi.PropertyType);
                }
                else
                {
                    var query = Data.AsQueryable();
                    var elementType = query.ElementType;

                    if (elementType == typeof(object) && typeof(EnumerableQuery).IsAssignableFrom(query.GetType()) && query.Any())
                    {
                        elementType = query.FirstOrDefault().GetType();
                    }

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
                    if (typeof(IList).IsAssignableFrom(typeof(T)))
                    {
                        if (object.Equals(internalValue, null))
                        {
                            await ValueChanged.InvokeAsync(default(T));
                        }
                        else
                        {
                            var list = (IList)Activator.CreateInstance(typeof(T));
                            foreach (var i in (IEnumerable)internalValue)
                            {
                                list.Add(i);
                            }
                            await ValueChanged.InvokeAsync((T)(object)list);
                        }
                    }
                    else if (typeof(T).IsGenericType && typeof(ICollection<>).MakeGenericType(typeof(T).GetGenericArguments()[0]).IsAssignableFrom(typeof(T)))
                    {
                        if (object.Equals(internalValue, null))
                        {
                            await ValueChanged.InvokeAsync(default(T));
                        }
                        else
                        {
                            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(typeof(T).GetGenericArguments()[0]));
                            foreach (var i in (IEnumerable)internalValue)
                            {
                                list.Add(i);
                            }
                            await ValueChanged.InvokeAsync((T)(object)list);
                        }
                    }
                    else
                    {
                        await ValueChanged.InvokeAsync(object.Equals(internalValue, null) ? default(T) : (T)internalValue);
                    }
                }

                if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }

                await Change.InvokeAsync(internalValue);
            }
            StateHasChanged();
        }

        /// <inheritdoc />
        public override object GetValue()
        {
            return internalValue;
        }

        internal void UpdateSelectedItems(object item)
        {
            if (!string.IsNullOrEmpty(ValueProperty))
            {
                var value = GetItemOrValueFromProperty(item, ValueProperty);

                if (!IsItemSelectedByValue(value))
                {
                    selectedItems.Add(item);
                }
                else
                {
                    selectedItems = selectedItems.AsQueryable().Where($@"!object.Equals(it.{ValueProperty},@0)", value).ToList();
                }
            }
            else
            {
                if (!selectedItems.Any(i => object.Equals(i, item)))
                {
                    selectedItems.Add(item);
                }
                else
                {
                    selectedItems = selectedItems.Where(i => !object.Equals(i, item)).ToList();
                }
            }
        }

        /// <summary>
        /// Selects the item from value.
        /// </summary>
        /// <param name="value">The value.</param>
        protected virtual void SelectItemFromValue(object value)
        {
            var view = LoadData.HasDelegate ? Data : View;
            if (value != null && view != null)
            {
                if (!Multiple)
                {
                    if (!string.IsNullOrEmpty(ValueProperty))
                    {
                        if (typeof(EnumerableQuery).IsAssignableFrom(view.GetType()))
                        {
                            SelectedItem = view.OfType<object>().Where(i => object.Equals(GetItemOrValueFromProperty(i, ValueProperty), value)).FirstOrDefault();
                        }
                        else
                        {
                            SelectedItem = view.AsQueryable().Where($@"{ValueProperty} == @0", value).FirstOrDefault();
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

                                if (typeof(EnumerableQuery).IsAssignableFrom(view.GetType()))
                                {
                                    item = view.OfType<object>().Where(i => object.Equals(GetItemOrValueFromProperty(i, ValueProperty), v)).FirstOrDefault();
                                }
                                else
                                {
                                    item = view.AsQueryable().Where($@"{ValueProperty} == @0", v).FirstOrDefault();
                                }

                                if (!object.Equals(item, null) && !selectedItems.AsQueryable().Where($@"object.Equals(it.{ValueProperty},@0)", v).Any())
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
            switch (internalValue)
            {
                case string s:
                    return object.Equals(s, v);
                case IEnumerable enumerable:
                    return enumerable.Cast<object>().Contains(v);
                case null:
                    return false;
                default:
                    return object.Equals(internalValue, v);
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            keys.Clear();
        }
    }
}
