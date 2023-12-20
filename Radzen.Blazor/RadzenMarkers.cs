using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenMarkers.
    /// </summary>
    public class RadzenMarkers : RadzenChartComponentBase
    {
        /// <summary>
        /// Gets or sets whether marker is visible.
        /// </summary>
        /// <value>Visibility.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets the fill.
        /// </summary>
        /// <value>The fill.</value>
        [Parameter]
      public string Fill { get; set; }

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
      public double StrokeWidth { get; set; } = 2;

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [Parameter]
      public double Size { get; set; } = 5;

        /// <summary>
        /// Gets or sets the type of the marker.
        /// </summary>
        /// <value>The type of the marker.</value>
        [Parameter]
      public MarkerType MarkerType { get; set; } = MarkerType.None;

        /// <summary>
        /// Sets the series.
        /// </summary>
        /// <value>The series.</value>
        [CascadingParameter]
      public IChartSeries Series 
      {
        set
        {
          value.Markers = this;
        }
      }

        /// <summary>
        /// Shoulds the refresh chart.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool ShouldRefreshChart(ParameterView parameters)
      {
          return parameters.DidParameterChange(nameof(MarkerType), MarkerType) || DidParameterChange(parameters, nameof(Visible), Visible);
      }
    }
}