using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen
{
    /// <summary>
    /// Base class of components that display a list of items.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DropDownBase<T> : DataBoundFormComponent<T>
    {
        /// <summary>
        /// Gets or sets a value that determines how many additional items will be rendered before and after the visible region. This help to reduce the frequency of rendering during scrolling. However, higher values mean that more elements will be present in the page.
        /// </summary>
        [Parameter]
        public int VirtualizationOverscanCount { get; set; }

        internal Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<object>? virtualize;

        /// <summary>
        /// The Virtualize instance.
        /// </summary>
        public Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<object>? Virtualize
        {
            get
            {
                return virtualize;
            }
        }

        List<object>? virtualItems;

        private async ValueTask<Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<object>> LoadItems(Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderRequest request)
        {
            var data = Data != null ? Data.Cast<object>() : Enumerable.Empty<object>();
            var view = (LoadData.HasDelegate ? data : View)?.Cast<object>().AsQueryable() ?? Enumerable.Empty<object>().AsQueryable();
            var totalItemsCount = LoadData.HasDelegate ? Count : view.Count();
            var top = request.Count;

            if (top <= 0)
            {
                top = PageSize;
            }

            if (LoadData.HasDelegate)
            {
                await LoadData.InvokeAsync(new Radzen.LoadDataArgs() { Skip = request.StartIndex, Top = top, Filter = searchText });
            }

            virtualItems = (LoadData.HasDelegate ? (Data ?? Enumerable.Empty<object>()) : view.Skip(request.StartIndex).Take(top)).Cast<object>().ToList();

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

        /// <summary>
        /// Determines whether virtualization is allowed.
        /// </summary>
        /// <returns><c>true</c> if virtualization is allowed; otherwise, <c>false</c>.</returns>
        internal bool IsVirtualizationAllowed()
        {
            return AllowVirtualization;
        }

        internal int GetVirtualizationOverscanCount()
        {
            return VirtualizationOverscanCount;
        }

        /// <summary>
        /// Renders the items.
        /// </summary>
        /// <returns>RenderFragment.</returns>
        internal virtual RenderFragment RenderItems()
        {
            return new RenderFragment(builder =>
            {
                if (AllowVirtualization)
                {
                    builder.OpenComponent(0, typeof(Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<object>));
                    builder.AddAttribute(1, "ItemsProvider", new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderDelegate<object>(LoadItems));
                    builder.AddAttribute(2, "ChildContent", (RenderFragment<object>)((context) =>
                    {
                        return b =>
                        {
                            RenderItem(b, context);
                        };
                    }));

                    if (VirtualizationOverscanCount != default)
                    {
                        builder.AddAttribute(3, "OverscanCount", VirtualizationOverscanCount);
                    }

                    builder.AddComponentReferenceCapture(7, c => { virtualize = (Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<object>)c; });

                    builder.CloseComponent();
                }
                else
                {
                    var items = LoadData.HasDelegate ? Data : View;
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            RenderItem(builder, item);
                        }
                    }
                }
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

        HashSet<object> keys = new HashSet<object>();

        internal object? GetKey(object item)
        {
            var value = GetItemOrValueFromProperty(item, ValueProperty ?? string.Empty);

            if (value != null)
            {
                keys.Add(value);
            }

            return value;
        }

        /// <summary>
        /// Gets or sets the header template.
        /// </summary>
        /// <value>The header template.</value>
        [Parameter]
        public RenderFragment? HeaderTemplate { get; set; }

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
        /// Gets or sets a value indicating the selected index should reset to the top item when filtering, resulting in a down arrow action will start moving from the top.
        /// </summary>
        /// <value><c>true</c> to reset selected index to -1 when filtering; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ResetSelectedIndexOnFilter { get; set; }

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
        public RenderFragment<dynamic>? Template { get; set; }

        /// <summary>
        /// Gets or sets the value property.
        /// </summary>
        /// <value>The value property.</value>
        [Parameter]
        public string? ValueProperty { get; set; }

        /// <summary>
        /// Gets or sets the disabled property.
        /// </summary>
        /// <value>The disabled property.</value>
        [Parameter]
        public string? DisabledProperty { get; set; }

        /// <summary>
        /// Gets or sets the remove chip button title.
        /// </summary>
        /// <value>The remove chip button title.</value>
        [Parameter]
        public string RemoveChipTitle { get; set; } = "Remove";

        /// <summary>
        /// Gets or sets the search aria label text.
        /// </summary>
        /// <value>The search aria label text.</value>
        [Parameter]
        public string SearchAriaLabel { get; set; } = "Search";

        /// <summary>
        /// Gets or sets the empty value aria label text.
        /// </summary>
        /// <value>The empty value aria label text.</value>
        [Parameter]
        public string EmptyAriaLabel { get; set; } = "Empty";

        /// <summary>
        /// Gets or sets the selected item changed.
        /// </summary>
        /// <value>The selected item changed.</value>
        [Parameter]
        public EventCallback<object> SelectedItemChanged { get; set; }

        /// <summary>
        /// The selected items
        /// </summary>
        protected ISet<object> selectedItems = new HashSet<object>();
        /// <summary>
        /// The selected item
        /// </summary>
        protected object? selectedItem;
        Type GetItemType(IEnumerable items)
        {
            var firstType = items.Cast<object>().FirstOrDefault()?.GetType() ?? typeof(object);
            var hasNull = items.Cast<object>().Where(i => i == null).Any();

            if (hasNull && firstType.IsValueType && Nullable.GetUnderlyingType(firstType) == null)
            {
                return typeof(Nullable<>).MakeGenericType(firstType);
            }

            return firstType;
        }

        /// <summary>
        /// Selects all.
        /// </summary>
        protected virtual async System.Threading.Tasks.Task SelectAll()
        {
            if (Disabled || View == null)
            {
                return;
            }

            if (selectedItems.Count != View.Cast<object>().ToList().Where(i => disabledPropertyGetter == null || disabledPropertyGetter(i) as bool? != true).Count())
            {
                selectedItems.Clear();
                selectedItems = View.Cast<object>().ToList().Where(i => disabledPropertyGetter == null || disabledPropertyGetter(i) as bool? != true).ToHashSet(ItemComparer);
            }
            else
            {
                selectedItems.Clear();
            }

            if (!string.IsNullOrEmpty(ValueProperty) && Data != null)
            {
                var elementType = PropertyAccess.GetElementType(Data.GetType());
                System.Reflection.PropertyInfo? pi = PropertyAccess.GetProperty(elementType, ValueProperty);
                if (pi != null)
                {
                    internalValue = selectedItems.Select(i => GetItemOrValueFromProperty(i, ValueProperty)).AsQueryable().Cast(pi.PropertyType);
                }
            }
            else
            {
                var type = typeof(T).IsGenericType ? typeof(T).GetGenericArguments()[0] :
                    QueryableExtension.IsEnumerable(typeof(T)) ? GetItemType(selectedItems) : typeof(T);
                internalValue = selectedItems.AsQueryable().Cast(type);
            }

            if (internalValue != null)
            {
                await collectionAssignment.MakeAssignment((IEnumerable)internalValue, ValueChanged);
            }
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(internalValue);

            StateHasChanged();

            if (JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.focusElement", GetId());
            }
        }

        internal bool IsAllSelected()
        {
            List<object> notDisabledItemsInList = View != null ? View.Cast<object>().ToList()
                .Where(i => disabledPropertyGetter == null || disabledPropertyGetter(i) as bool? != true)
                .ToList() : new List<object>();

            if (LoadData.HasDelegate && !string.IsNullOrEmpty(ValueProperty))
            {
                return View != null && notDisabledItemsInList.Count > 0 && notDisabledItemsInList
                    .All(i => { 
                        var value = GetItemOrValueFromProperty(i, ValueProperty);
                        return value != null ? IsItemSelectedByValue(value) : false;
                    });
            }

            return View != null && notDisabledItemsInList.Count > 0 && selectedItems.Count == notDisabledItemsInList.Count;
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
        protected async Task ClearAll()
        {
            if (Disabled)
                return;

            searchText = null;
            await SearchTextChanged.InvokeAsync(searchText);
            if (JSRuntime != null)
            {
                await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", search, "");
            }

            internalValue = collectionAssignment.GetCleared();
            selectedItem = null;

            selectedItems.Clear();

            selectedIndex = -1;

            await ValueChanged.InvokeAsync(internalValue != null ? (T)internalValue : default(T)!);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(internalValue);

            await OnFilter(new ChangeEventArgs());

            StateHasChanged();
        }

        /// <summary>
        /// The data
        /// </summary>
        IEnumerable? _data;

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public override IEnumerable? Data
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

                if (type == typeof(object) && typeof(EnumerableQuery).IsAssignableFrom(query.GetType()) && query.Cast<object>().Any())
                {
                    var firstElement = query.Cast<object>().FirstOrDefault(i => i != null);
                    if (firstElement != null)
                    {
                        type = firstElement.GetType();
                    }
                }

                if (!string.IsNullOrEmpty(ValueProperty))
                {
                    valuePropertyGetter = GetGetter(ValueProperty, type);
                }

                if (!string.IsNullOrEmpty(TextProperty))
                {
                    textPropertyGetter = GetGetter(TextProperty, type);
                }

                if (!string.IsNullOrEmpty(DisabledProperty))
                {
                    disabledPropertyGetter = GetGetter(DisabledProperty, type);
                }

                if (selectedItems.Count == 0)
                {
                    selectedItems = new HashSet<object>(ItemComparer);
                }
            }
        }

        Func<object, object?> GetGetter(string propertyName, Type type)
        {
            if (propertyName?.Contains('[', StringComparison.Ordinal) == true)
            {
                var getter = typeof(PropertyAccess).GetMethod("Getter", [typeof(string), typeof(Type)]);
                if (getter == null)
                {
                    return PropertyAccess.Getter<object, object?>(propertyName, type);
                }
                var getterMethod = getter.MakeGenericMethod([type, typeof(object)]);

                return (i) => getterMethod.Invoke(i, [propertyName, type])!;
            }

            return PropertyAccess.Getter<object, object?>(propertyName ?? string.Empty, type);
        }

        internal Func<object, object?>? valuePropertyGetter;
        internal Func<object, object?>? textPropertyGetter;
        internal Func<object, object?>? disabledPropertyGetter;

        /// <summary>
        /// Gets the item or value from property.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="property">The property.</param>
        /// <returns>System.Object.</returns>
        public object? GetItemOrValueFromProperty(object? item, string property)
        {
            if (item != null)
            {
                var enumValue = item as Enum;
                if (enumValue != null)
                {
                    return Radzen.Blazor.EnumExtensions.GetDisplayDescription(enumValue);
                }

                if (property == TextProperty)
                {
                    return textPropertyGetter != null ? textPropertyGetter(item) : PropertyAccess.GetItemOrValueFromProperty(item, property);
                }
                else if (property == ValueProperty)
                {
                    return valuePropertyGetter != null ? valuePropertyGetter(item) : PropertyAccess.GetItemOrValueFromProperty(item, property);
                }
                else if (property == DisabledProperty)
                {
                    return disabledPropertyGetter != null ? disabledPropertyGetter(item) : PropertyAccess.GetItemOrValueFromProperty(item, property);
                }
            }

            return item;
        }

        /// <inheritdoc/>
        protected override async Task OnDataChanged()
        {
            await base.OnDataChanged();

            if (AllowVirtualization && Virtualize != null && !LoadData.HasDelegate)
            {
                await InvokeAsync(Virtualize.RefreshDataAsync);
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
        public string SearchID
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

            if (JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.togglePopup", Element, PopupID, true);
                await JSRuntime.InvokeVoidAsync("Radzen.focusElement", isFilter ? UniqueID : SearchID);
            }

            if (list != null && JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.selectListItem", search, list, selectedIndex);
            }
        }

        internal bool preventKeydown;

        /// <summary>
        /// Handles the key press.
        /// </summary>
        /// <param name="args">The <see cref="Microsoft.AspNetCore.Components.Web.KeyboardEventArgs"/> instance containing the event data.</param>
        /// <param name="isFilter">if set to <c>true</c> [is filter].</param>
        /// <param name="shouldSelectOnChange">Should select item on item change with keyboard.</param>
        protected virtual async Task HandleKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args, bool isFilter = false, bool? shouldSelectOnChange = null)
        {
            if (Disabled || Data == null || args == null)
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
                    items = virtualItems ?? Enumerable.Empty<object>().ToList();
                }
                else
                {
                    items = View != null ? View.Cast<object>().ToList() : Enumerable.Empty<object>().ToList();
                }
            }

            var key = args.Code != null ? args.Code : args.Key;

            if (!args.AltKey && (key == "ArrowDown" || key == "ArrowLeft" || key == "ArrowUp" || key == "ArrowRight"))
            {
                preventKeydown = true;

                try
                {
                    if (JSRuntime != null)
                    {
                        selectedIndex = await JSRuntime.InvokeAsync<int>("Radzen.focusListItem", search, list, key == "ArrowDown" || key == "ArrowRight", selectedIndex);

                        var popupOpened = await JSRuntime.InvokeAsync<bool>("Radzen.popupOpened", PopupID);

                        if (!Multiple && !popupOpened && shouldSelectOnChange != false)
                        {
                            var itemToSelect = items.ElementAtOrDefault(selectedIndex);
                            if (itemToSelect != null)
                            {
                                await OnSelectItem(itemToSelect, true);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    //
                }
            }
            else if (key == "Enter" || key == "NumpadEnter" || (key == "Space" && !isFilter))
            {
                preventKeydown = true;

                if (selectedIndex == -1 && items.Count == 1)
                {
                    selectedIndex = 0;
                }

                if (JSRuntime != null)
                {
                    if (selectedIndex >= 0 && selectedIndex <= items.Count - 1)
                    {
                        var itemToSelect = items.ElementAtOrDefault(selectedIndex);

                        await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", search, $"{searchText}".Trim());

                        if (itemToSelect != null)
                        {
                            await OnSelectItem(itemToSelect, true);
                        }
                    }

                    var popupOpened = await JSRuntime.InvokeAsync<bool>("Radzen.popupOpened", PopupID);

                    if (!popupOpened)
                    {
                        if (key != "Space")
                        {
                            await OpenPopup(key, isFilter);
                        }
                    }
                    else
                    {
                        if (!Multiple && (!isFilter || key != "Space"))
                        {
                            if (!Multiple && !isFilter)
                            {
                                await ClosePopup(key);
                            }
                        }
                    }
                }
            }
            else if (args.AltKey && key == "ArrowDown")
            {
                preventKeydown = true;

                await OpenPopup(key, isFilter);
            }
            else if (key == "Escape" || key == "Tab")
            {
                preventKeydown = false;

                await ClosePopup(key);
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

                if (ResetSelectedIndexOnFilter)
                {
                    selectedIndex = -1;
                }                                

                Debounce(DebounceFilter, FilterDelay);
            }
            else if (args.Key.Length == 1 && !args.CtrlKey && !args.AltKey && !args.ShiftKey)
            {
                // searching for element
                if (Query == null)
                {
                    return;
                }
                var query = Query;
                var elementType = query.ElementType;
                if (elementType == null)
                {
                    return;
                }
                var filteredItems = (!string.IsNullOrEmpty(TextProperty) ?
                    query.Where(TextProperty, args.Key, StringFilterOperator.StartsWith, FilterCaseSensitivity.CaseInsensitive) :
                    query)
                    .Cast(elementType).Cast<dynamic>().ToList();

                if (previousKey != args.Key)
                {
                    previousKey = args.Key;
                    itemIndex = -1;
                }

                itemIndex = itemIndex + 1 >= filteredItems.Count ? 0 : itemIndex + 1;
                var itemToSelect = filteredItems.ElementAtOrDefault(itemIndex);

                if (itemToSelect is not null)
                {
                    if (!Multiple)
                    {
                        await SelectItem(itemToSelect);
                    }

                    var result = items.Select((x, i) => new { Item = x, Index = i }).FirstOrDefault(itemWithIndex => object.Equals(itemWithIndex.Item, itemToSelect));
                    if (result != null)
                    {
                        if (!Multiple)
                        {
                            selectedIndex = result.Index;
                        }
                        if (JSRuntime != null)
                        {
                            await JSRuntime.InvokeVoidAsync("Radzen.selectListItem", list, list, result.Index);
                        }
                    }
                }

                preventKeydown = false;
            }
        }

        internal virtual async Task ClosePopup(string key)
        {
            if (JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }
        }

        int itemIndex;
        string? previousKey;

        /// <summary>
        /// Handles the FilterKeyPress event.
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
                    if (virtualize != null)
                    {
                        await virtualize.RefreshDataAsync();
                    }
                }

                await InvokeAsync(() => { StateHasChanged(); });
            }
            else
            {
                var args = await GetLoadDataArgs();

                if (IsVirtualizationAllowed())
                {
                    if (virtualize != null)
                    {
                        await InvokeAsync(virtualize.RefreshDataAsync);
                    }
                    else
                    {
                        await InvokeAsync(() => LoadData.InvokeAsync(args));
                    }
                    await InvokeAsync(() => { StateHasChanged(); });
                }
                else
                {
                    await InvokeAsync(() => LoadData.InvokeAsync(args));
                }
            }

            if (Multiple)
                selectedIndex = -1;
            if (JSRuntime != null)
            {
                await JSRuntime.InvokeAsync<string>("Radzen.repositionPopup", Element, PopupID);
            }
            await InvokeAsync(() => SearchTextChanged.InvokeAsync(SearchText));
        }

        /// <summary>
        /// Handles the KeyPress event.
        /// </summary>
        /// <param name="args">The <see cref="Microsoft.AspNetCore.Components.Web.KeyboardEventArgs"/> instance containing the event data.</param>
        /// <param name="shouldSelectOnChange">Should select item on item change with keyboard.</param>
        protected virtual async System.Threading.Tasks.Task OnKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args, bool? shouldSelectOnChange = null)
        {
            await HandleKeyPress(args, false, shouldSelectOnChange);
        }

        /// <summary>
        /// Called when [select item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="isFromKey">if set to <c>true</c> [is from key].</param>
        protected virtual async System.Threading.Tasks.Task OnSelectItem(object? item, bool isFromKey = false)
        {
            await SelectItem(item);
        }

        /// <summary>
        /// Handles the Filter event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        protected virtual async System.Threading.Tasks.Task OnFilter(ChangeEventArgs args)
        {
            if (!FilterAsYouType)
            {
                await DebounceFilter();
            }
        }

        /// <summary>
        /// Gets the load data arguments.
        /// </summary>
        /// <returns>LoadDataArgs.</returns>
        internal virtual async System.Threading.Tasks.Task<LoadDataArgs> GetLoadDataArgs()
        {
            if (AllowVirtualization)
            {
                return await Task.FromResult(new Radzen.LoadDataArgs() { Skip = 0, Top = PageSize, Filter = searchText });
            }
            else
            {
                return await Task.FromResult(new Radzen.LoadDataArgs() { Filter = searchText });
            }
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
            // check for changes before setting the properties through the base call
            var selectedItemChanged = parameters.DidParameterChange(nameof(SelectedItem), SelectedItem);
            var visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);
            var valueChanged = parameters.DidParameterChange(nameof(Value), Value);

            // manually set properties that are not set through the base call
            if (valueChanged)
            {
                internalValue = parameters.GetValueOrDefault<object>(nameof(Value));
                if (PreserveCollectionOnSelection && internalValue != null)
                {
                    collectionAssignment = new ReferenceGenericCollectionAssignment((T)internalValue);
                }
            }
            
            var pageSize = parameters.GetValueOrDefault<int>(nameof(PageSize));
            if (pageSize != default(int))
            {
                PageSize = pageSize;
            }
            
            // allow the base class to process parameters and set the properties
            // after this call the parameters object should be considered stale
            await base.SetParametersAsync(parameters);

            // handle changes
            if (selectedItemChanged)
            {
                await SelectItem(selectedItem, false);
            }

            var shouldClose = visibleChanged && !Visible;
            if (shouldClose && !firstRender && JSRuntime != null)
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
                if (!valueAsEnumerable.Cast<object>().SequenceEqual(selectedItems.Select(i => string.IsNullOrEmpty(ValueProperty) ? i : GetItemOrValueFromProperty(i, ValueProperty))))
                {
                    selectedItems.Clear();
                }
            }

            if (internalValue != null)
            {
                SelectItemFromValue(internalValue);
            }

            return base.OnParametersSetAsync();
        }

        /// <summary>
        /// Handles the Change event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        protected void OnChange(ChangeEventArgs args)
        {
            if (args != null)
            {
                internalValue = args.Value;
            }
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
                var value = GetItemOrValueFromProperty(item, ValueProperty);
                return value != null ? IsItemSelectedByValue(value) : false;
            }
            else
            {
                if (Multiple)
                {
                    return selectedItems.Contains(item);
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
        public object? SelectedItem
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
        protected override IEnumerable? View
        {
            get
            {
                if (_view == null && Query != null)
                {
                    _view = Query.Where(TextProperty ?? string.Empty, searchText, FilterOperator, FilterCaseSensitivity);
                }

                return _view;
            }
        }

        /// <summary>
        /// Sets the selected index from selected item.
        /// </summary>
        void SetSelectedIndexFromSelectedItem()
        {
            if (selectedItem != null && View != null)
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
        internal async Task SelectItemInternal(object item, bool raiseChange = true)
        {
            await SelectItem(item, raiseChange);
        }

        internal object? internalValue;
        
        /// <summary>
        /// Will add/remove selected items from a bound ICollection&lt;T&gt;, instead of replacing it.
        /// </summary>
        protected bool PreserveCollectionOnSelection;
        private DefaultCollectionAssignment collectionAssignment = new();

        /// <summary>
        /// Selects the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="raiseChange">if set to <c>true</c> [raise change].</param>
        public async Task SelectItem(object? item, bool raiseChange = true)
        {
            if (disabledPropertyGetter != null && item != null && disabledPropertyGetter(item) as bool? == true)
            {
                return;
            }

            if (!Multiple)
            {
                if (object.Equals(item, selectedItem))
                    return;

                selectedItem = item;
                if (!string.IsNullOrEmpty(ValueProperty) && item != null)
                {
                    internalValue = PropertyAccess.GetItemOrValueFromProperty(item, ValueProperty);
                }
                else
                {
                    internalValue = item;
                }

                SetSelectedIndexFromSelectedItem();

                await SelectedItemChanged.InvokeAsync(selectedItem);
            }
            else
            {
                if (item != null)
                {
                    UpdateSelectedItems(item);
                }

                if (!string.IsNullOrEmpty(ValueProperty) && Data != null)
                {
                    var elementType = PropertyAccess.GetElementType(Data.GetType());
                    System.Reflection.PropertyInfo? pi = PropertyAccess.GetProperty(elementType, ValueProperty);
                    if(pi != null)
                    {
                        internalValue = selectedItems.Select(i => GetItemOrValueFromProperty(i, ValueProperty)).AsQueryable().Cast(pi.PropertyType);
                    }
                }
                else
                {
                    if (Data == null)
                    {
                        return;
                    }
                    var query = Data.AsQueryable();
                    var elementType = query.ElementType;

                    if (elementType == typeof(object) && typeof(EnumerableQuery).IsAssignableFrom(query.GetType()) && query.Cast<object>().Any())
                    {
                        var firstElement = query.Cast<object>().FirstOrDefault(i => i != null);
                        if (firstElement != null)
                        {
                            elementType = firstElement.GetType();
                        }
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
                    if (Multiple)
                    {
                        if (internalValue != null)
                        {
                            await collectionAssignment.MakeAssignment((IEnumerable)internalValue, ValueChanged);
                        }
                    }
                    else
                    {
                        await ValueChanged.InvokeAsync(internalValue != null ? (T)internalValue : default(T)!);
                    }
                }

                if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }

                await Change.InvokeAsync(internalValue);
            }
            StateHasChanged();
        }

        /// <inheritdoc />
        public override object? GetValue()
        {
            return internalValue;
        }

        internal void UpdateSelectedItems(object item)
        {
            if (!string.IsNullOrEmpty(ValueProperty))
            {
                var value = GetItemOrValueFromProperty(item, ValueProperty);

                if (value != null && !IsItemSelectedByValue(value))
                {
                    selectedItems.Add(item);
                }
                else
                {
                    selectedItems = selectedItems.AsQueryable().Where(i => !object.Equals(GetItemOrValueFromProperty(i, ValueProperty), value)).ToHashSet(ItemComparer);
                }
            }
            else
            {
                if (!selectedItems.Add(item))
                {
                    selectedItems.Remove(item);
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
                            SelectedItem = view.AsQueryable().Where(new FilterDescriptor[] 
                            { 
                                new FilterDescriptor() 
                                { 
                                    Property = ValueProperty, 
                                    FilterValue = value 
                                } 
                            },
                            LogicalFilterOperator.And,
                            FilterCaseSensitivity.Default).FirstOrDefault();
                        }
                    }
                    else
                    {
                        selectedItem = internalValue;
                    }

                    SetSelectedIndexFromSelectedItem();
                }
                else
                {
                    var values = value as IEnumerable;
                    if (values != null)
                    {
                        if (!string.IsNullOrEmpty(ValueProperty))
                        {
                            foreach (object v in values.Cast<dynamic>().ToList())
                            {
                                dynamic item;

                                if (typeof(EnumerableQuery).IsAssignableFrom(view.GetType()))
                                {
                                    item = view.OfType<object>().Where(i => object.Equals(GetItemOrValueFromProperty(i, ValueProperty), v)).FirstOrDefault()!;
                                }
                                else
                                {
                                    item = view.AsQueryable().Where(new FilterDescriptor[]
                                    {
                                        new FilterDescriptor()
                                        {
                                            Property = ValueProperty,
                                            FilterValue = v
                                        }
                                    },
                                    LogicalFilterOperator.And,
                                    FilterCaseSensitivity.Default).FirstOrDefault()!;
                                }

                                if (!object.Equals(item, null) && !selectedItems.AsQueryable().Where(i => object.Equals(GetItemOrValueFromProperty(i, ValueProperty), v)).Any())
                                {
                                    selectedItems.Add(item);
                                }
                            }
                        }
                        else
                        {
                            selectedItems = values.Cast<object>().ToHashSet(ItemComparer);
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
        /// For lists of objects, an IEqualityComparer to control how selected items are determined
        /// </summary>
        [Parameter] public IEqualityComparer<object>? ItemComparer { get; set; }

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

            GC.SuppressFinalize(this);
        }
        
        private class DefaultCollectionAssignment
        {
            public virtual async Task MakeAssignment(IEnumerable selectedItems, EventCallback<T> valueChanged)
            {
                if (typeof(IList).IsAssignableFrom(typeof(T)))
                {
                    if (object.Equals(selectedItems, null))
                    {
                        await valueChanged.InvokeAsync(default(T)!);
                    }
                    else
                    {
                        var list = (IList?)Activator.CreateInstance<T>();
                        if (list != null)
                        {
                            foreach (var i in (IEnumerable)selectedItems)
                            {
                                list.Add(i);
                            }
                            await valueChanged.InvokeAsync((T)(object)list);
                        }
                        else
                        {
                            await valueChanged.InvokeAsync(default(T)!);
                        }
                    }
                }
                else if (typeof(T).IsGenericType && typeof(ICollection<>).MakeGenericType(typeof(T).GetGenericArguments()[0]).IsAssignableFrom(typeof(T)))
                {
                    if (object.Equals(selectedItems, null))
                    {
                        await valueChanged.InvokeAsync(default(T)!);
                    }
                    else
                    {
                        var list = (IList?)Activator.CreateInstance(typeof(List<>).MakeGenericType(typeof(T).GetGenericArguments()[0]));
                        if (list != null)
                        {
                            foreach (var i in (IEnumerable)selectedItems)
                            {
                                list.Add(i);
                            }
                            await valueChanged.InvokeAsync((T)(object)list);
                        }
                        else
                        {
                            await valueChanged.InvokeAsync(default(T)!);
                        }
                    }
                }
                else
                {
                    await valueChanged.InvokeAsync(object.Equals(selectedItems, null) ? default(T)! : (T)selectedItems);
                }
            }

            public virtual T? GetCleared()
            {
                return default(T);
            }
        }

        private class ReferenceGenericCollectionAssignment : DefaultCollectionAssignment
        {
            private readonly T originalCollection;
            private readonly bool canHandle;
            private readonly System.Reflection.MethodInfo? clearMethod;
            private readonly System.Reflection.MethodInfo? addMethod;
            private readonly System.Reflection.MethodInfo? removeMethod;

            public ReferenceGenericCollectionAssignment(T originalCollection)
            {
                this.originalCollection = originalCollection;
                // Pre-calculate if we can handle this instance and get method info
                if (originalCollection != null)
                {
                    var actualType = originalCollection.GetType();
                    if (actualType.IsGenericType && !actualType.IsArray)
                    {
                        var elementType = actualType.GetGenericArguments()[0];
                        var genericCollectionType = typeof(ICollection<>).MakeGenericType(elementType);

                        if (genericCollectionType.IsAssignableFrom(actualType))
                        {
                            clearMethod = actualType.GetMethod("Clear");
                            addMethod = actualType.GetMethod("Add");
                            removeMethod = typeof(T).GetMethod("Remove");
                            canHandle = true;
                        }
                    }
                }
            }

            public override async Task MakeAssignment(IEnumerable selectedItems, EventCallback<T> valueChanged)
            {
                if (!canHandle || originalCollection == null)
                {
                    // Fallback to default behavior when we can't handle the type or originalCollection is null
                    await base.MakeAssignment(selectedItems, valueChanged);
                    return;
                }

                var currentItems = selectedItems.Cast<object>().ToHashSet();
                var existingItems = (originalCollection as IEnumerable)?.Cast<object>().ToHashSet() ?? new HashSet<object>();
                foreach (var i in currentItems)
                {
                    if (!existingItems.Contains(i))
                        addMethod!.Invoke(originalCollection, [i]);
                }
                foreach (var i in existingItems)
                {
                    if (!currentItems.Contains(i))
                        removeMethod!.Invoke(originalCollection, [i]);
                }

                await valueChanged.InvokeAsync(originalCollection);
            }

            public override T? GetCleared()
            {
                if (canHandle && originalCollection != null)
                {
                    clearMethod!.Invoke(originalCollection, null);
                    return originalCollection;
                }
                return base.GetCleared();
            }
        }
    }
}
