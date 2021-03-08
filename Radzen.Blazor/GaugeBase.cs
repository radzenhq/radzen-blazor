using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    public abstract class GaugeBase : RadzenComponent
    {
        [Parameter]
        public RenderFragment ChildContent
        {
            get; set;
        }

        public double? Width { get; set; }

        public double? Height { get; set; }

        bool widthAndHeightAreSet = false;
        bool firstRender = true;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            this.firstRender = firstRender;

            if (firstRender || visibleChanged)
            {
                visibleChanged = false;

                if (Visible)
                {
                    var rect = await JSRuntime.InvokeAsync<Rect>("Radzen.createGauge", Element, Reference);

                    if (!widthAndHeightAreSet)
                    {
                        widthAndHeightAreSet = true;

                        Resize(rect.Width, rect.Height);
                    }
                }
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            Initialize();
        }

        [JSInvokable]
        public void Resize(double width, double height)
        {
            var stateHasChanged = false;

            if (width != Width)
            {
                Width = width;
                stateHasChanged = true;
            }

            if (height != Height)
            {
                Height = height;
                stateHasChanged = true;
            }

            if (stateHasChanged)
            {
                StateHasChanged();
            }
        }

        private void Initialize()
        {
            double width = 0;
            double height = 0;

            if (CurrentStyle.ContainsKey("height"))
            {
                var pixelHeight = CurrentStyle["height"];

                if (pixelHeight.EndsWith("px"))
                {
                    height = Convert.ToDouble(pixelHeight.TrimEnd("px".ToCharArray()));
                }
            }

            if (CurrentStyle.ContainsKey("width"))
            {
                var pixelWidth = CurrentStyle["width"];

                if (pixelWidth.EndsWith("px"))
                {
                    width = Convert.ToDouble(pixelWidth.TrimEnd("px".ToCharArray()));
                }
            }

            if (width > 0 && height > 0)
            {
                widthAndHeightAreSet = true;

                Width = width;
                Height = height;
            }
        }

        private bool visibleChanged = false;

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            bool shouldRefresh = parameters.DidParameterChange(nameof(Style), Style);

            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if (visibleChanged && !firstRender)
            {
                if (Visible == false)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.destroyGauge", Element);
                }
            }

            if (shouldRefresh)
            {
                Initialize();
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (Visible)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroyGauge", Element);
            }
        }

        public void Reload()
        {
            StateHasChanged();
        }
    }
}