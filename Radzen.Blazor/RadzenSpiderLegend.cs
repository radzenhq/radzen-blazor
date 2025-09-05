using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Configures the legend of a <see cref="RadzenSpiderChart{TItem}"/>.
    /// </summary>
    public class RadzenSpiderLegend : ComponentBase
    {
        /// <summary>
        /// Gets or sets the legend position.
        /// </summary>
        [Parameter]
        public LegendPosition Position { get; set; } = LegendPosition.Right;

        /// <summary>
        /// Gets or sets a value indicating whether the legend is visible.
        /// </summary>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets the parent spider chart (non-generic contract).
        /// </summary>
        [CascadingParameter]
        public IRadzenSpiderChart? Chart { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (Chart != null)
            {
                Chart.Legend = this;
            }
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var positionChanged = parameters.DidParameterChange(nameof(Position), Position);
            var visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if ((positionChanged || visibleChanged) && Chart != null)
            {
                await Chart.Refresh();
            }
        }
    }
}


