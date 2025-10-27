using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A list box component that displays a scrollable list of items with single or multiple selection support.
    /// RadzenListBox provides an always-visible alternative to dropdowns, ideal for showing multiple options without requiring a popup.
    /// Displays all items in a scrollable container, making all options visible at once (unlike dropdowns which hide options in a popup).
    /// Supports single selection (default) or multiple selection via Multiple property, built-in search/filter with configurable operators and case sensitivity,
    /// binding to any IEnumerable data source with TextProperty and ValueProperty, custom item templates for rich list item content,
    /// efficient rendering of large lists via IQueryable support, optional "Select All" checkbox for multiple selection mode, and keyboard navigation (Arrow keys, Page Up/Down, Home/End) for accessibility.
    /// Use when you want to show all available options without requiring clicks to open a dropdown, or when multiple selection is needed and checkboxes would take too much space.
    /// </summary>
    /// <typeparam name="TValue">The type of the selected value. Can be a single value or IEnumerable for multiple selection.</typeparam>
    /// <example>
    /// Basic list box:
    /// <code>
    /// &lt;RadzenListBox @bind-Value=@selectedId TValue="int" Data=@countries TextProperty="Name" ValueProperty="Id" Style="height: 300px;" /&gt;
    /// </code>
    /// Multiple selection with Select All:
    /// <code>
    /// &lt;RadzenListBox @bind-Value=@selectedIds TValue="IEnumerable&lt;int&gt;" Multiple="true" SelectAllText="Select All"
    ///                 Data=@items TextProperty="Name" ValueProperty="Id" Style="height: 400px;" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenListBox<TValue> : DropDownBase<TValue>
    {
        /// <summary>
        /// Gets or sets the select all text.
        /// </summary>
        /// <value>The select all text.</value>
        [Parameter]
        public string SelectAllText { get; set; }

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
        public Action<ListBoxItemRenderEventArgs<TValue>> ItemRender { get; set; }

        internal ListBoxItemRenderEventArgs<TValue> ItemAttributes(RadzenListBoxItem<TValue> item)
        {
            var disabled = !string.IsNullOrEmpty(DisabledProperty) ? GetItemOrValueFromProperty(item.Item, DisabledProperty) : false;

            var args = new ListBoxItemRenderEventArgs<TValue>()
            {
                ListBox = this,
                Item = item.Item,
                Disabled = disabled is bool ? (bool)disabled : false,
            };

            if (ItemRender != null)
            {
                ItemRender(args);
            }

            return args;
        }

        internal override void RenderItem(RenderTreeBuilder builder, object item)
        {
            builder.OpenComponent(0, typeof(RadzenListBoxItem<TValue>));
            builder.AddAttribute(1, "ListBox", this);
            builder.AddAttribute(2, "Item", item);

            if (DisabledProperty != null)
            {
                builder.AddAttribute(3, "Disabled", GetItemOrValueFromProperty(item, DisabledProperty));
            }

            builder.SetKey(GetKey(item));
            builder.CloseComponent();
        }

        /// <summary>
        /// Handles the <see cref="E:KeyDown" /> event.
        /// </summary>
        /// <param name="args">The <see cref="Microsoft.AspNetCore.Components.Web.KeyboardEventArgs"/> instance containing the event data.</param>
        protected async System.Threading.Tasks.Task OnKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args)
        {
            if (Disabled)
                return;

            var key = $"{args.Key}".Trim();

            if (AllowFiltering && key.Length == 1)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.focusElement", SearchID);
                await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", search, key);
            }

            await OnKeyPress(args, false);
        }

        private bool visibleChanged = false;
        private bool disabledChanged = false;
        private bool firstRender = true;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);
            disabledChanged = parameters.DidParameterChange(nameof(Disabled), Disabled);

            await base.SetParametersAsync(parameters);

            if ((visibleChanged || disabledChanged) && !firstRender)
            {
                if (Visible == false || Disabled == true)
                {
                    Dispose();
                }
            }
        }

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
                        reload = true;
                    }

                    if (reload)
                    {
                        StateHasChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether is read only.
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-listbox rz-inputtext").ToString();
        }

        /// <inheritdoc />
        protected override Task SelectAll()
        {
            if (ReadOnly)
            {
                return Task.CompletedTask;
            }

            return base.SelectAll();
        }
    }
}
