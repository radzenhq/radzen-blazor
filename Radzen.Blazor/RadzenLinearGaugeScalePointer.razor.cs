using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenLinearGaugeScalePointer component.
    /// </summary>
    public partial class RadzenLinearGaugeScalePointer : ComponentBase, IDisposable
    {
        /// <summary>
        /// Gets or sets the pointer fill color.
        /// </summary>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// Gets or sets the pointer value on the scale.
        /// </summary>
        [Parameter]
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets whether the pointer value is rendered.
        /// </summary>
        [Parameter]
        public bool ShowValue { get; set; }

        /// <summary>
        /// Gets or sets the format string used for the pointer value.
        /// </summary>
        [Parameter]
        public string? FormatString { get; set; }

        /// <summary>
        /// Gets or sets the pointer stroke color.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the pointer stroke.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; } = 1;

        /// <summary>
        /// Gets or sets the pointer size in pixels.
        /// </summary>
        [Parameter]
        public double Size { get; set; } = 10;

        /// <summary>
        /// Gets or sets the visual style of the pointer.
        /// </summary>
        [Parameter]
        public LinearGaugePointerType PointerType { get; set; } = LinearGaugePointerType.Arrow;

        /// <summary>
        /// Gets or sets the distance in pixels between the pointer tip and the value label.
        /// </summary>
        [Parameter]
        public double ValueOffset { get; set; } = 12;

        /// <summary>
        /// Gets or sets the parent scale.
        /// </summary>
        [CascadingParameter]
        public RadzenLinearGaugeScale? Scale { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when the value changes via two-way binding.
        /// </summary>
        [Parameter]
        public EventCallback<double> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets whether the pointer can be dragged to change the value.
        /// Requires <see cref="ValueChanged" /> to be bound so that value updates are propagated.
        /// </summary>
        [Parameter]
        public bool Draggable { get; set; }

        /// <summary>
        /// Gets or sets the template used to render the pointer value.
        /// </summary>
        [Parameter]
        public RenderFragment<RadzenLinearGaugeScalePointer>? Template { get; set; }

        /// <summary>
        /// Gets or sets the parent linear gauge.
        /// </summary>
        [CascadingParameter]
        public RadzenLinearGauge? Gauge { get; set; }

        internal Point CurrentPosition => Scale?.ValueToPoint(Value) ?? new Point();

        internal void HandleDragStart(MouseEventArgs args)
        {
            if (Draggable)
            {
                Gauge?.StartDrag(this, args);
            }
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Gauge?.AddPointer(this);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Gauge?.RemovePointer(this);
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var shouldRefresh = false;

            if (parameters.DidParameterChange(nameof(Value), Value) ||
                parameters.DidParameterChange(nameof(ShowValue), ShowValue) ||
                parameters.DidParameterChange(nameof(Size), Size))
            {
                shouldRefresh = true;
            }

            await base.SetParametersAsync(parameters);

            if (shouldRefresh)
            {
                Gauge?.Reload();
            }
        }
    }
}
