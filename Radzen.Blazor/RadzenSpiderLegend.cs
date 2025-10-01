using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSpiderLegend component for RadzenSpiderChart.
    /// </summary>
    public class RadzenSpiderLegend : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the legend position.
        /// </summary>
        [Parameter]
        public LegendPosition Position { get; set; } = LegendPosition.Right;

        /// <summary>
        /// Gets or sets the parent spider chart.
        /// </summary>
        [CascadingParameter]
        public object Chart { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            // Register with the chart using reflection since we don't know the generic type
            if (Chart != null)
            {
                var legendProperty = Chart.GetType().GetProperty("Legend");
                if (legendProperty != null && legendProperty.PropertyType == typeof(RadzenSpiderLegend))
                {
                    legendProperty.SetValue(Chart, this);
                }
            }
        }
    }
}