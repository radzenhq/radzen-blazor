using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class GaugeBase.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public abstract class GaugeBase : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public double? Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public double? Height { get; set; }

        /// <summary>
        /// The width and height are set
        /// </summary>
        bool widthAndHeightAreSet = false;
        /// <summary>
        /// The first render
        /// </summary>
        bool firstRender = true;

        /// <summary>
        /// On after render as an asynchronous operation.
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Called when [initialized].
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            Initialize();
        }

        /// <summary>
        /// Resizes the specified width.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
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

        /// <summary>
        /// Initializes this instance.
        /// </summary>
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

        /// <summary>
        /// The visible changed
        /// </summary>
        private bool visibleChanged = false;

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            if (Visible)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroyGauge", Element);
            }
        }

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        public void Reload()
        {
            StateHasChanged();
        }
    }
}