using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenRadialGaugeScalePointer component.
    /// </summary>
    public partial class RadzenRadialGaugeScalePointer : ComponentBase
    {
        /// <summary>
        /// Gets or sets the fill.
        /// </summary>
        /// <value>The fill.</value>
        [Parameter]
        public string Fill { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show value.
        /// </summary>
        /// <value><c>true</c> if value is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowValue { get; set; } = true;

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        /// <value>The radius.</value>
        [Parameter]
        public double Radius { get; set; } = 10;

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        [Parameter]
        public double? Width { get; set; }

        double CurrentWidth
        {
            get
            {
                return Width ?? Radius / 2;
            }
        }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>The length.</value>
        [Parameter]
        public double Length { get; set; } = 1;

        /// <summary>
        /// Gets or sets the format string.
        /// </summary>
        /// <value>The format string.</value>
        [Parameter]
        public string FormatString { get; set; }

        double CurrentLength
        {
            get
            {
                return Scale.CurrentRadius * Length;
            }
        }

        /// <summary>
        /// Gets or sets the stroke.
        /// </summary>
        /// <value>The stroke.</value>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the stroke.
        /// </summary>
        /// <value>The width of the stroke.</value>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets or sets the scale.
        /// </summary>
        /// <value>The scale.</value>
        [CascadingParameter]
        public RadzenRadialGaugeScale Scale { get; set; }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<RadzenRadialGaugeScalePointer> Template { get; set; }

        /// <summary>
        /// Gets or sets the gauge.
        /// </summary>
        /// <value>The gauge.</value>
        [CascadingParameter]
        public RadzenRadialGauge Gauge { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            Gauge.AddPointer(this);
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var shouldRefresh = false;

            if (parameters.DidParameterChange(nameof(Value), Value) || parameters.DidParameterChange(nameof(ShowValue), ShowValue))
            {
                shouldRefresh = true;
            }

            await base.SetParametersAsync(parameters);

            if (shouldRefresh)
            {
                Gauge.Reload();
            }
        }
    }
}
