using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenListBox<TValue> : DropDownBase<TValue>
    {
        internal override void RenderItem(RenderTreeBuilder builder, object item)
        {
            builder.OpenComponent(0, typeof(RadzenListBoxItem<TValue>));
            builder.AddAttribute(1, "ListBox", this);
            builder.AddAttribute(2, "Item", item);
            builder.SetKey(item);
            builder.CloseComponent();
        }

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

            await OnKeyPress(args);
        }

        private bool visibleChanged = false;
        private bool disabledChanged = false;
        private bool firstRender = true;

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

        [Parameter]
        public bool ReadOnly { get; set; }

        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-listbox rz-inputtext").ToString();
        }
    }
}