using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// A dropdown select component that allows users to choose one or multiple items from a popup list.
    /// RadzenDropDown supports data binding, filtering, templates, virtual scrolling, and both single and multiple selection modes.
    /// Binds to a data source via the Data property and uses TextProperty and ValueProperty to determine what to display and what value to bind.
    /// Supports filtering (with configurable operators and case sensitivity), custom item templates, empty state templates, value templates for the selected item display,
    /// and can be configured as editable to allow custom text entry. For multiple selection, set Multiple=true and bind to a collection type.
    /// </summary>
    /// <typeparam name="TValue">The type of the selected value. Can be a primitive type, complex object, or collection for multiple selection.</typeparam>
    /// <example>
    /// Basic dropdown with data binding:
    /// <code>
    /// &lt;RadzenDropDown @bind-Value=@customerID TValue="string" Data=@customers TextProperty="CompanyName" ValueProperty="CustomerID" /&gt;
    /// </code>
    /// Dropdown with filtering and placeholder:
    /// <code>
    /// &lt;RadzenDropDown @bind-Value=@selectedId TValue="int" Data=@items TextProperty="Name" ValueProperty="Id" 
    ///                  AllowFiltering="true" FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive" Placeholder="Select an item..." /&gt;
    /// </code>
    /// Multiple selection dropdown:
    /// <code>
    /// &lt;RadzenDropDown @bind-Value=@selectedIds TValue="IEnumerable&lt;int&gt;" Data=@items Multiple="true" Chips="true" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenDropDown<TValue> : DropDownBase<TValue>
    {
        bool isOpen;
        /// <summary>
        /// Gets or sets additional HTML attributes to be applied to the underlying input element.
        /// This allows passing custom attributes like data-* attributes, aria-* attributes, or other HTML attributes directly to the input.
        /// </summary>
        /// <value>A dictionary of custom HTML attributes.</value>
        [Parameter]
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        /// <summary>
        /// Gets or sets whether the dropdown is read-only and cannot be changed by user interaction.
        /// When true, the dropdown displays the selected value but prevents changing the selection.
        /// </summary>
        /// <value><c>true</c> if the dropdown is read-only; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the template used to render the currently selected value in the dropdown input.
        /// This allows custom formatting or layout for the displayed selection. The template receives the selected item as context.
        /// </summary>
        /// <value>The render fragment for customizing the selected value display.</value>
        [Parameter]
        public RenderFragment<dynamic> ValueTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template displayed when the dropdown data source is empty or no items match the filter.
        /// Use this to show a custom "No items found" or "Empty list" message.
        /// </summary>
        /// <value>The render fragment for the empty state.</value>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }

        /// <summary>
        /// Gets or sets the footer template.
        /// </summary>
        /// <value>The footer template.</value>
        [Parameter]
        public RenderFragment FooterTemplate { get; set; }

        /// <summary>
        /// Gets or sets whether the dropdown popup should automatically open when the input receives focus.
        /// Useful for improving user experience by reducing clicks needed to interact with the dropdown.
        /// </summary>
        /// <value><c>true</c> to open the popup on focus; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool OpenOnFocus { get; set; }

        /// <summary>
        /// Gets or sets whether the filter search text should be cleared after an item is selected.
        /// When true, selecting an item will reset the filter, showing all items again on the next open.
        /// </summary>
        /// <value><c>true</c> to clear the search text after selection; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool ClearSearchAfterSelection { get; set; }

        /// <summary>
        /// Gets or sets the placeholder text displayed in the filter search box within the dropdown popup.
        /// This helps users understand they can filter the list by typing.
        /// </summary>
        /// <value>The filter search placeholder text. Default is empty string.</value>
        [Parameter]
        public string FilterPlaceholder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTML autocomplete attribute value for the filter search input.
        /// Controls whether the browser should provide autocomplete suggestions for the filter field.
        /// </summary>
        /// <value>The autocomplete type. Default is <see cref="AutoCompleteType.Off"/>.</value>
        [Parameter]
        public AutoCompleteType FilterAutoCompleteType { get; set; } = AutoCompleteType.Off;

        /// <summary>
        /// Gets or sets a callback invoked when rendering each dropdown item.
        /// Use this to customize item attributes, such as adding CSS classes or data attributes based on item properties.
        /// </summary>
        /// <value>The item render callback that receives event arguments with the item and allows setting custom attributes.</value>
        [Parameter]
        public Action<DropDownItemRenderEventArgs<TValue>> ItemRender { get; set; }

        internal DropDownItemRenderEventArgs<TValue> ItemAttributes(RadzenDropDownItem<TValue> item)
        {
            var disabled = !string.IsNullOrEmpty(DisabledProperty) ? GetItemOrValueFromProperty(item.Item, DisabledProperty) : false;

            var args = new DropDownItemRenderEventArgs<TValue>() 
            { 
                DropDown = this, 
                Item = item.Item, 
                Disabled = disabled is bool ? (bool)disabled : false,
            };

            if (ItemRender != null)
            {
                ItemRender(args);
            }

            return args;
        }

        /// <summary>
        /// Gets or sets the keyboard key that triggers opening the popup when <see cref="OpenOnFocus"/> is enabled.
        /// Default is <c>"Enter"</c>.
        /// </summary>
        /// <value>The keyboard key used to open the popup.</value>
        [Parameter]
        public string OpenPopupKey { get; set; } = "Enter";

        private async Task OnFocus()
        {
            if (OpenOnFocus)
            {
                await OpenPopup(OpenPopupKey, false);
            }
        }

        internal override async Task ClosePopup(string key)
        {
            bool of = false;

            if (key == "Enter")
            {
                of = OpenOnFocus;
                OpenOnFocus = false;
            }
            isOpen = false;
            await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID, Reference, nameof(OnClose));

            if (key == "Enter")
            {
                await JSRuntime.InvokeVoidAsync("Radzen.focusElement", UniqueID);
                OpenOnFocus = of;
            }
        }

        /// <summary>
        /// Opens the popup.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="isFilter">if set to <c>true</c> [is filter].</param>
        /// <param name="isFromClick">if set to <c>true</c> [is from click].</param>
        protected override async Task OpenPopup(string key = "ArrowDown", bool isFilter = false, bool isFromClick = false)
        {
            if (Disabled)
                return;

            if (!isOpen)
            {
                await Open.InvokeAsync(null);
            }

            isOpen = true;
            if (OpenOnFocus)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.openPopup", Element, PopupID, true, null, null, null, Reference, nameof(OnClose));
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("Radzen.togglePopup", Element, PopupID, true, Reference, nameof(OnClose));
            }
            await JSRuntime.InvokeVoidAsync("Radzen.focusElement", isFilter ? UniqueID : SearchID);

            if (list != null && selectedIndex != -1)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.selectListItem", search, list, selectedIndex);
            }
        }

        internal override void RenderItem(RenderTreeBuilder builder, object item)
        {
            builder.OpenComponent(0, typeof(RadzenDropDownItem<TValue>));
            builder.AddAttribute(1, "DropDown", this);
            builder.AddAttribute(2, "Item", item);

            if (DisabledProperty != null)
            {
                builder.AddAttribute(3, "Disabled", GetItemOrValueFromProperty(item, DisabledProperty));
            }

            builder.SetKey(GetKey(item));
            builder.CloseComponent();
        }

        /// <summary>
        /// Gets or sets the maximum number of selected item labels to display in the input before showing a count summary.
        /// When multiple selection is enabled and more items are selected than this value, the input will show "N items selected" instead of listing all labels.
        /// Only applicable when <see cref="DropDownBase{T}.Multiple"/> is true.
        /// </summary>
        /// <value>The maximum number of labels to display. Default is 4.</value>
        [Parameter]
        public int MaxSelectedLabels { get; set; } = 4;

        /// <summary>
        /// Gets or sets the CSS style applied to the dropdown popup container.
        /// Use this to control the popup dimensions, especially max-height to limit scrollable area.
        /// </summary>
        /// <value>The CSS style string for the popup. Default is "max-height:200px;overflow-x:hidden".</value>
        [Parameter]
        public string PopupStyle { get; set; } = "max-height:200px;overflow-x:hidden";

        /// <summary>
        /// Gets or sets whether selected items should be displayed as removable chips in the input area.
        /// When enabled in multiple selection mode, each selected item appears as a chip with an X button for quick removal.
        /// Requires <see cref="DropDownBase{T}.Multiple"/> to be set to <c>true</c>.
        /// </summary>
        /// <value><c>true</c> to display selected items as chips; <c>false</c> to show a comma-separated list or count. Default is <c>false</c>.</value>
        [Parameter]
        public bool Chips { get; set; }

        /// <summary>
        /// Gets or sets the selected items text.
        /// </summary>
        /// <value>The selected items text.</value>
        [Parameter]
        public string SelectedItemsText { get; set; } = "items selected";

        /// <summary>
        /// Gets or sets the select all text.
        /// </summary>
        /// <value>The select all text.</value>
        [Parameter]
        public string SelectAllText { get; set; }

        /// <summary>
        /// Callback for when a dropdown is opened.
        /// </summary>
        [Parameter]
        public EventCallback Open { get; set; }

        /// <summary>
        /// Callback for when a dropdown is closed.
        /// </summary>
        [Parameter]
        public EventCallback Close { get; set; }

        private bool visibleChanged = false;
        private bool disabledChanged = false;
        private bool firstRender = true;

        /// <inheritdoc />
        protected override Task SelectAll()
        {
            if (ReadOnly)
            {
                return Task.CompletedTask;
            }

            return base.SelectAll();
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);
            disabledChanged = parameters.DidParameterChange(nameof(Disabled), Disabled);

            await base.SetParametersAsync(parameters);

            if (visibleChanged && !firstRender)
            {
                if (Visible == false)
                {
                    Dispose();
                }
            }
        }

        private bool shouldReposition;

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            this.firstRender = firstRender;

            if (firstRender || visibleChanged || disabledChanged)
            {
                visibleChanged = false;
                disabledChanged = false;

                if (Visible)
                {
                    bool reload = false;
                    if (LoadData.HasDelegate && Data == null)
                    {
                        await LoadData.InvokeAsync(await GetLoadDataArgs());
                        reload = true;
                    }

                    if (!Disabled)
                    {
                        await JSRuntime.InvokeVoidAsync("Radzen.preventArrows", Element);
                    }

                    if (reload)
                    {
                        StateHasChanged();
                    }
                }
            }

            if (shouldReposition)
            {
                shouldReposition = false;

                await JSRuntime.InvokeVoidAsync("Radzen.repositionPopup", Element, PopupID);
            }
        }

        /// <inheritdoc />
        protected override async Task HandleKeyPress(KeyboardEventArgs args, bool isFilter, bool? shouldSelectOnChange = null)
        {
            if (!ReadOnly)
            {
                await base.HandleKeyPress(args, isFilter, shouldSelectOnChange);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Called when item is selected.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="isFromKey">if set to <c>true</c> [is from key].</param>
        protected override async Task OnSelectItem(object item, bool isFromKey = false)
        {
            if (!ReadOnly)
            {
                if (!Multiple && !isFromKey)
                {
                    isOpen = false;
                    await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID, Reference, nameof(OnClose));
                }

                if (ClearSearchAfterSelection)
                {
                    await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", search, string.Empty);
                    searchText = null;
                    await SearchTextChanged.InvokeAsync(searchText);
                    await OnFilter(null);
                }

                await SelectItem(item);
            }
        }

        private async Task OnChipRemove(object item)
        {
            if (!Disabled)
            {
                await OnSelectItemInternal(item);
            }
        }

        internal async Task OnSelectItemInternal(object item, bool isFromKey = false)
        {
            await OnSelectItem(item, isFromKey);

            if (Chips)
            {
                shouldReposition = true;
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-dropdown")
                        .Add("rz-clear", AllowClear)
                        .Add("rz-dropdown-chips", Chips && selectedItems.Count > 0)
                        .ToString();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoid("Radzen.destroyPopup", PopupID);
            }
        }

        /// <summary>
        /// Called when popup is closed.
        /// </summary>
        [JSInvokable]
        public async Task OnClose()
        {
            isOpen = false;
            await Close.InvokeAsync();
        }

        internal async Task PopupClose()
        {
            isOpen = false;
            await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID, Reference, nameof(OnClose));
        }
    }
}
