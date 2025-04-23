﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDropDown component.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenDropDown @bind-Value=@customerID TValue="string" Data=@customers TextProperty="CompanyName" ValueProperty="CustomerID" Change=@(args => Console.WriteLine($"Selected CustomerID: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenDropDown<TValue> : DropDownBase<TValue>
    {
        bool isOpen;
        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        [Parameter]
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is read only.
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the value template.
        /// </summary>
        /// <value>The value template.</value>
        [Parameter]
        public RenderFragment<dynamic> ValueTemplate { get; set; }

        /// <summary>
        /// Gets or sets the empty template.
        /// </summary>
        /// <value>The empty template.</value>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether popup should open on focus. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if popup should open on focus; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool OpenOnFocus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether search field need to be cleared after selection. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if need to be cleared; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ClearSearchAfterSelection { get; set; }

        /// <summary>
        /// Gets or sets the filter placeholder.
        /// </summary>
        /// <value>The filter placeholder.</value>
        [Parameter]
        public string FilterPlaceholder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or Sets the filter autocomplete type.
        /// </summary>
        /// <value>The filter autocomplete type. Default: Off</value>
        [Parameter]
        public AutoCompleteType FilterAutoCompleteType { get; set; } = AutoCompleteType.Off;

        /// <summary>
        /// Gets or sets the row render callback. Use it to set row attributes.
        /// </summary>
        /// <value>The row render callback.</value>
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

        private async Task OnFocus(Microsoft.AspNetCore.Components.Web.FocusEventArgs args)
        {
            if (OpenOnFocus)
            {
                await OpenPopup("Enter", false);
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
        /// Gets or sets the number of maximum selected labels.
        /// </summary>
        /// <value>The number of maximum selected labels.</value>
        [Parameter]
        public int MaxSelectedLabels { get; set; } = 4;

        /// <summary>
        /// Gets or sets the Popup height.
        /// </summary>
        /// <value>The number Popup height.</value>
        [Parameter]
        public string PopupStyle { get; set; } = "max-height:200px;overflow-x:hidden";

        /// <summary>
        /// Gets or sets a value indicating whether the selected items will be displayed as chips. Set to <c>false</c> by default.
        /// Requires <see cref="DropDownBase{T}.Multiple" /> to be set to <c>true</c>.
        /// </summary>
        /// <value><c>true</c> to display the selected items as chips; otherwise, <c>false</c>.</value>
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
                JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
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
