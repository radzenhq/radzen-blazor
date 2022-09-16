using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Common configuration of <see cref="RadzenColumnSeries{TItem}" />.
    /// </summary>
    public partial class RadzenColumnOptions : RadzenChartComponentBase
    {
        /// <summary>
        /// Gets or sets the border radius of the bars. 
        /// </summary>
        /// <value>The radius. Values greater than <c>0</c> make rounded corners.</value>
        [Parameter]
        public double Radius { get; set; }

        /// <summary>
        /// Gets or sets the margin between columns.
        /// </summary>
        /// <value>The margin. By default set to <c>10</c></value>
        [Parameter]
        public double Margin { get; set; } = 10;

        /// <summary>
        /// Gets or sets the width of all columns in pixels. By default it is automatically calculated depending on the chart width.
        /// </summary>
        /// <value>The pixel width of the column. By default set to <c>null</c></value>
        [Parameter]
        public double? Width { get; set;}

        /// <inheritdoc />
        protected override void Initialize()
        {
            Chart.ColumnOptions = this;
        }

        /// <inheritdoc />
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(Radius), Radius) || 
                   DidParameterChange(parameters, nameof(Width), Width) ||
                   DidParameterChange(parameters, nameof(Margin), Margin);
        }
    }
}
