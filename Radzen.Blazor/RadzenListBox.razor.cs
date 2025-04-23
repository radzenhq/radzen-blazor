using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenListBox component.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenListBox @bind-Value=@customerID TValue="string" Data=@customers TextProperty="CompanyName" ValueProperty="CustomerID" Change=@(args => Console.WriteLine($"Selected CustomerID: {args}")) /&gt;
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
                await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", search, key);
                await JSRuntime.InvokeVoidAsync("Radzen.focusElement", SearchID);
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
